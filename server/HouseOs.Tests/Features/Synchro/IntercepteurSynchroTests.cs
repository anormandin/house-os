using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Infrastructure;
using HouseOs.Tests.Features.Taches;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.Synchro;

/// <summary>
/// Le filet de la synchro : toute écriture doit produire son événement de module sans
/// que la slice ait rien à publier. Ce sont ces tests qui garantissent qu'un module
/// futur, ou une écriture d'arrière-plan, ne passe pas inaperçu.
/// </summary>
public class IntercepteurSynchroTests : IDisposable
{
    private readonly SqliteConnection _connexion;
    private readonly HouseOsDbContext _db;
    private readonly DiffuseurMouchard _mouchard = new();

    public IntercepteurSynchroTests()
    {
        _connexion = new SqliteConnection("DataSource=:memory:");
        _connexion.Open();
        _db = ContexteAvec(_mouchard);
        _db.Database.EnsureCreated();
    }

    private HouseOsDbContext ContexteAvec(IDiffuseurSynchro diffuseur)
    {
        var options = new DbContextOptionsBuilder<HouseOsDbContext>()
            .UseSqlite(_connexion)
            .AddInterceptors(new IntercepteurSynchro(diffuseur, NullLogger<IntercepteurSynchro>.Instance))
            .Options;
        // Le contexte Sqlite des tests : il re-mappe les colonnes jsonb en TEXT.
        return new HouseOsDbContextSqlite(options);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connexion.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Zone NouvelleZone() =>
        new() { Id = Guid.NewGuid(), Nom = "Cuisine", Type = TypeZone.Interieur };

    private static Utilisateur NouvelUtilisateur() =>
        new() { Id = Guid.NewGuid(), NomUtilisateur = "alain", NomAffichage = "Alain", MotDePasseHash = "x" };

    [Theory]
    [InlineData(typeof(Tache), ModulesSynchro.Taches)]
    [InlineData(typeof(Occurrence), ModulesSynchro.Taches)]
    [InlineData(typeof(EntreeJournal), ModulesSynchro.Taches)]
    [InlineData(typeof(Zone), ModulesSynchro.Zones)]
    [InlineData(typeof(Equipement), ModulesSynchro.Equipements)]
    [InlineData(typeof(Document), ModulesSynchro.Documents)]
    [InlineData(typeof(CompteARebours), ModulesSynchro.ComptesARebours)]
    [InlineData(typeof(PrevisionHoraire), ModulesSynchro.Meteo)]
    [InlineData(typeof(PrevisionQuotidienne), ModulesSynchro.Meteo)]
    [InlineData(typeof(ReleveMeteo), ModulesSynchro.Meteo)]
    [InlineData(typeof(PhraseDuJour), ModulesSynchro.PhraseDuJour)]
    [InlineData(typeof(FluxExterne), ModulesSynchro.FluxExternes)]
    [InlineData(typeof(EvenementExterne), ModulesSynchro.FluxExternes)]
    [InlineData(typeof(CompteBudget), ModulesSynchro.Budget)]
    [InlineData(typeof(Enveloppe), ModulesSynchro.Budget)]
    [InlineData(typeof(MouvementEnveloppe), ModulesSynchro.Budget)]
    [InlineData(typeof(TransactionBancaire), ModulesSynchro.Budget)]
    public void ChaqueEntitePersistee_estRattacheeAUnModule(Type entite, string module) =>
        Assert.Equal(module, IntercepteurSynchro.ModulePour(entite));

    [Fact]
    public void Utilisateur_nEstRattacheAAucunModule() =>
        // L'amorçage au démarrage écrit des utilisateurs et n'a aucun client à prévenir.
        Assert.Null(IntercepteurSynchro.ModulePour(typeof(Utilisateur)));

    [Fact]
    public async Task UneEcriture_diffuseLeModuleTouche()
    {
        _db.Zones.Add(NouvelleZone());
        await _db.SaveChangesAsync();

        var evenement = Assert.Single(_mouchard.Recus);
        Assert.Equal(ModulesSynchro.Zones, evenement.Module);
        // Tier grossier : invalidation seule, jamais de toast.
        Assert.Null(evenement.Genre);
    }

    [Fact]
    public async Task PlusieursEntitesDuMemeModule_neDiffusentQuUnEvenement()
    {
        // Le cas du lot : dix tâches en une sauvegarde ne doivent pas faire dix
        // rafraîchissements.
        for (var i = 0; i < 10; i++)
        {
            _db.Zones.Add(NouvelleZone());
        }
        await _db.SaveChangesAsync();

        Assert.Equal([ModulesSynchro.Zones], _mouchard.Recus.Select(e => e.Module));
    }

    [Fact]
    public async Task UneSauvegardeTouchantDeuxModules_lesDiffuseTousLesDeux()
    {
        _db.Zones.Add(NouvelleZone());
        _db.ComptesARebours.Add(new CompteARebours
        {
            Id = Guid.NewGuid(),
            Titre = "Déménagement",
            DateCible = DateOnly.FromDateTime(DateTime.Now),
            Icone = IconeCompteARebours.Camion,
        });
        await _db.SaveChangesAsync();

        Assert.Equal(
            [ModulesSynchro.ComptesARebours, ModulesSynchro.Zones],
            _mouchard.Recus.Select(e => e.Module).Order());
    }

    [Fact]
    public async Task UneSauvegardeDUtilisateurSeul_neDiffuseRien()
    {
        _db.Utilisateurs.Add(NouvelUtilisateur());
        await _db.SaveChangesAsync();

        Assert.Empty(_mouchard.Recus);
    }

    [Fact]
    public async Task UneModification_diffuseAussi()
    {
        var zone = NouvelleZone();
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync();
        _mouchard.Recus.Clear();

        zone.Nom = "Salon";
        await _db.SaveChangesAsync();

        Assert.Equal(ModulesSynchro.Zones, Assert.Single(_mouchard.Recus).Module);
    }

    [Fact]
    public async Task UneSuppression_diffuseAussi()
    {
        var zone = NouvelleZone();
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync();
        _mouchard.Recus.Clear();

        _db.Zones.Remove(zone);
        await _db.SaveChangesAsync();

        Assert.Equal(ModulesSynchro.Zones, Assert.Single(_mouchard.Recus).Module);
    }

    [Fact]
    public async Task UneSauvegardeSansChangement_neDiffuseRien()
    {
        await _db.SaveChangesAsync();

        Assert.Empty(_mouchard.Recus);
    }

    [Fact]
    public async Task DansUneTransaction_rienNEstDiffuseAvantLeCommit()
    {
        // MeteoIngestionService écrit dans une transaction : diffuser au SaveChanges
        // ferait refetcher le client avant que la donnée soit visible, et aucun second
        // événement ne suivrait — périmé jusqu'au prochain focus.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Zones.Add(NouvelleZone());
        await _db.SaveChangesAsync();

        Assert.Empty(_mouchard.Recus);

        await transaction.CommitAsync();

        Assert.Equal(ModulesSynchro.Zones, Assert.Single(_mouchard.Recus).Module);
    }

    [Fact]
    public async Task UneTransactionAnnulee_neDiffuseRien()
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Zones.Add(NouvelleZone());
        await _db.SaveChangesAsync();
        await transaction.RollbackAsync();

        Assert.Empty(_mouchard.Recus);
    }

    [Fact]
    public async Task PlusieursSauvegardesDansUneTransaction_seFondentEnUnEvenement()
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Zones.Add(NouvelleZone());
        await _db.SaveChangesAsync();
        _db.Zones.Add(NouvelleZone());
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        Assert.Equal([ModulesSynchro.Zones], _mouchard.Recus.Select(e => e.Module));
    }

    [Fact]
    public async Task UnDiffuseurQuiEchoue_neFaitPasEchouerLEcriture()
    {
        // La synchro est un confort : jamais une raison de perdre une écriture.
        await using var db = ContexteAvec(new DiffuseurQuiEchoue());
        db.Zones.Add(NouvelleZone());

        await db.SaveChangesAsync();

        Assert.Equal(1, await db.Zones.CountAsync());
    }
}
