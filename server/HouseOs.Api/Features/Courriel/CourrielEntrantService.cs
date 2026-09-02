using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Documents;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Courriel;

public record RapportReleve(
    bool Actif,
    int NbCourriels,
    int NbDocuments,
    int NbIgnores,
    IReadOnlyList<string> Erreurs,
    bool DejaEnCours = false)
{
    public static RapportReleve Inactif { get; } = new(false, 0, 0, 0, []);
    public static RapportReleve EnCours { get; } = new(true, 0, 0, 0, [], DejaEnCours: true);
}

/// <summary>
/// Le cœur de Courriel Entrant : un passage = lister le dépôt, puis traiter chaque
/// courriel dans son propre scope (un DbContext par message, comme les flux ICS —
/// un courriel malformé ne bloque pas les suivants). Singleton, injectable : la
/// boucle d'arrière-plan, le endpoint REST et l'outil MCP appellent tous
/// <see cref="ReleverAsync"/> ; le verrou garantit un seul passage à la fois.
/// </summary>
public sealed class CourrielEntrantService(
    IServiceScopeFactory scopeFactory,
    IDepotCourriels depot,
    IOptions<CourrielOptions> options,
    IEnrichisseurCourriel enrichisseur,
    IDiffuseurSynchro diffuseur,
    IConfiguration config,
    IWebHostEnvironment env,
    ILogger<CourrielEntrantService> journal)
{
    private readonly SemaphoreSlim _verrou = new(1, 1);

    private sealed record Resultat(StatutImportCourriel Statut, int NbDocuments, string? Erreur, string? Titre);

    public async Task<RapportReleve> ReleverAsync(CancellationToken ct)
    {
        if (depot.Actif == false)
        {
            return RapportReleve.Inactif;
        }
        if (await _verrou.WaitAsync(0, ct) == false)
        {
            return RapportReleve.EnCours;
        }
        try
        {
            var chrono = System.Diagnostics.Stopwatch.StartNew();
            var courriels = await depot.ListerAsync(ct);
            var nbDocuments = 0;
            var nbIgnores = 0;
            var erreurs = new List<string>();
            foreach (var courriel in courriels)
            {
                try
                {
                    var resultat = await TraiterAsync(courriel, ct);
                    nbDocuments += resultat.NbDocuments;
                    if (resultat.Statut == StatutImportCourriel.Ignore)
                    {
                        nbIgnores++;
                    }
                    if (resultat.Erreur is not null)
                    {
                        erreurs.Add(resultat.Erreur);
                    }
                }
                // Ceinture : un courriel qui échoue hors de sa propre gestion d'erreur
                // (base indisponible…) reste dans le dépôt pour le prochain passage.
                catch (Exception ex) when (ct.IsCancellationRequested == false)
                {
                    journal.LogError(ex, "Courriel : échec isolé de l'objet {Cle} — il sera retenté.", courriel.Cle);
                    erreurs.Add($"{courriel.Cle} : {ex.Message}");
                }
            }
            if (courriels.Count > 0 || erreurs.Count > 0)
            {
                journal.LogInformation(
                    "Courriel : {NbCourriels} courriel(s), {NbDocuments} document(s), {NbIgnores} ignoré(s), {NbErreurs} erreur(s) en {DureeMs} ms.",
                    courriels.Count, nbDocuments, nbIgnores, erreurs.Count, chrono.ElapsedMilliseconds);
            }
            return new RapportReleve(true, courriels.Count, nbDocuments, nbIgnores, erreurs);
        }
        finally
        {
            _verrou.Release();
        }
    }

    private async Task<Resultat> TraiterAsync(CourrielEnDepot courriel, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();

        // Résidu d'un passage précédent dont l'effacement R2 a raté après le commit.
        if (await db.ImportsCourriel.AnyAsync(i => i.CleDepot == courriel.Cle, ct))
        {
            journal.LogInformation("Courriel : objet {Cle} déjà traité — effacé du dépôt.", courriel.Cle);
            await depot.SupprimerAsync(courriel.Cle, ct);
            return new Resultat(StatutImportCourriel.Ignore, 0, null, null);
        }
        if (courriel.Taille > options.Value.TailleMaxOctets)
        {
            return await Rejeter(db, courriel, "", "", DateTimeOffset.UtcNow,
                $"Courriel trop volumineux ({courriel.Taille} octets).", ct);
        }

        var eml = await depot.TelechargerAsync(courriel.Cle, ct);
        CourrielLu lu;
        try
        {
            lu = await LectureCourriel.LireAsync(eml, ct);
        }
        catch (Exception ex) when (ct.IsCancellationRequested == false)
        {
            return await Rejeter(db, courriel, "", "", DateTimeOffset.UtcNow, $"Courriel illisible : {ex.Message}", ct);
        }

        if (lu.MessageId is not null && await db.ImportsCourriel.AnyAsync(i => i.MessageId == lu.MessageId, ct))
        {
            journal.LogInformation(
                "Courriel : « {Sujet} » de {Expediteur} déjà importé (Message-ID {MessageId}) — ignoré.",
                lu.Sujet, lu.Expediteur, lu.MessageId);
            db.ImportsCourriel.Add(new ImportCourriel
            {
                Id = Guid.NewGuid(),
                CleDepot = courriel.Cle,
                Expediteur = Tronquer(lu.Expediteur, 300),
                Sujet = Tronquer(lu.Sujet, 500),
                // Npgsql n'écrit un timestamptz qu'à l'offset zéro.
                RecuLe = lu.Date.ToUniversalTime(),
                TraiteLe = DateTimeOffset.UtcNow,
                Statut = StatutImportCourriel.Ignore,
                Erreur = Tronquer($"Message-ID déjà importé : {lu.MessageId}", 1000),
            });
            await db.SaveChangesAsync(ct);
            await depot.SupprimerAsync(courriel.Cle, ct);
            return new Resultat(StatutImportCourriel.Ignore, 0, null, null);
        }

        var contexte = new ContexteEnrichissement(
            lu.Expediteur, lu.Sujet, lu.Date, lu.Texte,
            lu.PiecesJointes.Select(p => p.NomFichier).ToList(),
            await db.Documents.Where(d => d.Dossier != null).Select(d => d.Dossier!).Distinct().ToListAsync(ct),
            await db.Equipements.Select(e => new EquipementRef(e.Id, e.Nom, e.Marque)).ToListAsync(ct));
        var proposition = await enrichisseur.ProposerAsync(contexte, ct);

        var importId = Guid.NewGuid();
        var dossierFichiers = DocumentsEndpoints.DossierFichiers(config, env);
        var cheminsEcrits = new List<string>();
        var documents = new List<Document>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.ImportsCourriel.Add(new ImportCourriel
            {
                Id = importId,
                CleDepot = courriel.Cle,
                MessageId = lu.MessageId is null ? null : Tronquer(lu.MessageId, 300),
                Expediteur = Tronquer(lu.Expediteur, 300),
                Sujet = Tronquer(lu.Sujet, 500),
                RecuLe = lu.Date.ToUniversalTime(),
                TraiteLe = DateTimeOffset.UtcNow,
                Statut = StatutImportCourriel.Importe,
            });
            await db.SaveChangesAsync(ct);

            foreach (var piece in lu.PiecesJointes)
            {
                var meta = EnrichissementCourriel.Appliquer(proposition, contexte, piece.NomFichier, piece.TypeMime);
                // Le titre du LLM va à la première pièce ; les suivantes gardent leur nom
                // de fichier, sinon trois pièces s'appelleraient pareil.
                var titre = documents.Count == 0 ? meta.Titre : Path.GetFileNameWithoutExtension(piece.NomFichier);
                var document = await Enregistrer(db, piece.Contenu, piece.NomFichier, piece.TypeMime,
                    meta with { Titre = titre }, importId, dossierFichiers, ct);
                if (document is not null)
                {
                    documents.Add(document);
                    cheminsEcrits.Add(Path.Combine(dossierFichiers, document.CheminDisque));
                }
            }
            if (documents.Count == 0)
            {
                var meta = EnrichissementCourriel.Appliquer(proposition, contexte, null, EnregistrementDocument.TypeMimeCourriel);
                var nom = EnregistrementDocument.NettoyerNomFichier(
                    (string.IsNullOrWhiteSpace(lu.Sujet) ? "courriel" : lu.Sujet) + ".eml",
                    EnregistrementDocument.TypeMimeCourriel);
                var document = await Enregistrer(db, eml, nom, EnregistrementDocument.TypeMimeCourriel,
                    meta, importId, dossierFichiers, ct);
                if (document is not null)
                {
                    documents.Add(document);
                    cheminsEcrits.Add(Path.Combine(dossierFichiers, document.CheminDisque));
                }
            }

            var import = await db.ImportsCourriel.SingleAsync(i => i.Id == importId, ct);
            import.NbDocuments = documents.Count;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            // La ligne d'import et les documents partent avec le rollback ; les octets
            // déjà sur disque n'ont plus de propriétaire.
            foreach (var chemin in cheminsEcrits.Where(File.Exists))
            {
                File.Delete(chemin);
            }
            throw;
        }

        journal.LogInformation(
            "Courriel : « {Sujet} » de {Expediteur} → {NbDocuments} document(s) à classer ({Titres}).",
            lu.Sujet, lu.Expediteur, documents.Count, string.Join(" · ", documents.Select(d => d.Titre)));
        try
        {
            await depot.SupprimerAsync(courriel.Cle, ct);
        }
        catch (Exception ex) when (ct.IsCancellationRequested == false)
        {
            // La garde CleDepot rattrape ce cas au prochain passage.
            journal.LogWarning(ex, "Courriel : objet {Cle} importé mais non effacé du dépôt.", courriel.Cle);
        }
        await diffuseur.DiffuserAsync(new EvenementSynchro(
            ModulesSynchro.Documents,
            EvenementSynchro.GenreDocumentsRecus,
            EvenementSynchro.SourceCourriel,
            Libelle: documents.Count == 1 ? documents[0].Titre : null,
            Nombre: documents.Count), ct);
        return new Resultat(StatutImportCourriel.Importe, documents.Count, null, documents.FirstOrDefault()?.Titre);
    }

    private async Task<Document?> Enregistrer(
        HouseOsDbContext db, byte[] contenu, string nomFichier, string typeMime,
        MetadonneesDocument meta, Guid importId, string dossierFichiers, CancellationToken ct)
    {
        var (document, erreur) = await EnregistrementDocument.EnregistrerAsync(
            () => new MemoryStream(contenu, writable: false),
            contenu.Length,
            nomFichier,
            typeMime,
            new DocumentDonneesCreation(
                meta.Titre, meta.Categorie, meta.EquipementId, null, meta.Dossier, meta.Notes,
                meta.DateDocument, null, AClasser: true, ImportCourrielId: importId),
            db,
            dossierFichiers,
            EnregistrementDocument.TypesMimePermisIngestion,
            journal,
            ct);
        if (erreur is not null)
        {
            // Une pièce refusée (contenu qui ne correspond pas à son type, trop grosse)
            // ne condamne pas le courriel : le reste entre, ceci se voit dans Seq.
            journal.LogWarning(
                "Courriel : pièce « {NomFichier} » ({TypeMime}) écartée — {Champ} : {Raison}.",
                nomFichier, typeMime, erreur.Champ, erreur.Message);
        }
        return document;
    }

    private async Task<Resultat> Rejeter(
        HouseOsDbContext db, CourrielEnDepot courriel, string expediteur, string sujet,
        DateTimeOffset recuLe, string raison, CancellationToken ct)
    {
        journal.LogWarning("Courriel : objet {Cle} rejeté — {Raison}", courriel.Cle, raison);
        db.ImportsCourriel.Add(new ImportCourriel
        {
            Id = Guid.NewGuid(),
            CleDepot = courriel.Cle,
            Expediteur = expediteur,
            Sujet = sujet,
            RecuLe = recuLe.ToUniversalTime(),
            TraiteLe = DateTimeOffset.UtcNow,
            Statut = StatutImportCourriel.Erreur,
            Erreur = Tronquer(raison, 1000),
        });
        await db.SaveChangesAsync(ct);
        // Sans effacement, l'objet reviendrait à chaque passage, toutes les deux minutes.
        await depot.SupprimerAsync(courriel.Cle, ct);
        return new Resultat(StatutImportCourriel.Erreur, 0, $"{courriel.Cle} : {raison}", null);
    }

    private static string Tronquer(string texte, int max) => texte.Length <= max ? texte : texte[..max];
}
