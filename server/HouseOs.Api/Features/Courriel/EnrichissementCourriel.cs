using System.Globalization;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Documents;
using HouseOs.Api.Features.Mcp;

namespace HouseOs.Api.Features.Courriel;

public record EquipementRef(Guid Id, string Nom, string? Marque);

/// <summary>Ce que le LLM voit : les faits du courriel et le vocabulaire fermé.</summary>
public record ContexteEnrichissement(
    string Expediteur,
    string Sujet,
    DateTimeOffset Date,
    string Texte,
    IReadOnlyList<string> NomsPiecesJointes,
    IReadOnlyList<string> Dossiers,
    IReadOnlyList<EquipementRef> Equipements);

/// <summary>La proposition brute du modèle — rien n'est encore validé.</summary>
public record PropositionLlm(
    string? Titre,
    string? Categorie,
    string? Dossier,
    Guid? EquipementId,
    string? DateDocument,
    string? Montant,
    string? Resume);

/// <summary>Métadonnées prêtes à écrire — validées contre le vocabulaire et les colonnes.</summary>
public record MetadonneesDocument(
    string Titre,
    CategorieDocument Categorie,
    string? Dossier,
    Guid? EquipementId,
    DateOnly? DateDocument,
    string Notes);

/// <summary>
/// Un appel Haiku par courriel pour proposer titre, catégorie, dossier, équipement,
/// date, montant et résumé — contraints aux valeurs existantes ; tout écart ou tout
/// échec retombe sur le repli déterministe (D-2026-09-02 Enrichissement LLM À
/// L'ingestion). Même patron que PolissageLlm : JSON seulement, parse défensif.
/// </summary>
public static class EnrichissementCourriel
{
    public const int LongueurMaxTexte = 6000;
    public const int LongueurMaxTitre = 200;
    public const int LongueurMaxNotes = 2000;
    public const int LongueurMaxResume = 600;

    public static readonly TimeSpan DelaiMax = TimeSpan.FromSeconds(30);

    private const string PromptSysteme = """
        Tu es le classeur de la maison d'Alain et Ariane (Québec). On te donne un
        courriel transféré (expéditeur, sujet, date, texte, noms des pièces jointes) et
        le vocabulaire existant du classeur. Propose les métadonnées du document.

        Règles strictes :
        - « categorie » : exactement une des valeurs de la liste fournie.
        - « dossier » : exactement une valeur de la liste des dossiers existants, sinon null.
          N'invente jamais de dossier.
        - « equipementId » : l'id d'un équipement de la liste s'il est clairement concerné
          (le reçu d'achat, la garantie ou le manuel de cet objet), sinon null.
        - « titre » : court et précis, en français, sans mention du courriel
          (ex. « Facture IKEA — bibliothèque BILLY », « Garantie thermopompe Fujitsu »).
          60 caractères max.
        - « dateDocument » : la date portée par le document (achat, facture, contrat) au
          format YYYY-MM-DD, sinon la date du courriel.
        - « montant » : le total en dollars s'il y en a un (ex. « 129,95 $ »), sinon null.
        - « resume » : une ou deux phrases factuelles en français (ce qui a été acheté ou
          reçu, de qui, éléments notables). 300 caractères max. Jamais d'invention.
        - Réponds UNIQUEMENT avec cet objet JSON, rien d'autre :
          {"titre": "…", "categorie": "…", "dossier": null, "equipementId": null,
           "dateDocument": "YYYY-MM-DD", "montant": null, "resume": "…"}
        """;

    public static async Task<PropositionLlm?> ProposerAsync(
        ContexteEnrichissement contexte, string cleApi, string modele, CancellationToken ct)
    {
        using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
        delai.CancelAfter(DelaiMax);
        AnthropicClient client = new() { ApiKey = cleApi };
        var reponse = await client.Messages.Create(new MessageCreateParams
        {
            Model = modele,
            MaxTokens = 500,
            System = PromptSysteme,
            Messages = [new() { Role = Role.User, Content = SerialiserContexte(contexte) }],
        }, cancellationToken: delai.Token);

        var texte = reponse.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault()?.Text;
        return texte is null ? null : Extraire(texte);
    }

    /// <summary>Le contexte vu par le LLM, en français, avec le vocabulaire fermé.</summary>
    public static string SerialiserContexte(ContexteEnrichissement contexte) =>
        JsonSerializer.Serialize(new
        {
            expediteur = contexte.Expediteur,
            sujet = contexte.Sujet,
            dateCourriel = contexte.Date.ToString("yyyy-MM-dd"),
            piecesJointes = contexte.NomsPiecesJointes,
            texte = Tronquer(contexte.Texte, LongueurMaxTexte),
            categoriesPermises = Enum.GetNames<CategorieDocument>(),
            dossiersExistants = contexte.Dossiers,
            equipements = contexte.Equipements.Select(e => new { id = e.Id, nom = e.Nom, marque = e.Marque }),
        }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    /// <summary>Parse défensif : chaque « { » est essayé comme début d'objet ; champs
    /// tous facultatifs, seuls les textes sont lus. Aucun objet valide → null.</summary>
    public static PropositionLlm? Extraire(string texte)
    {
        var octets = System.Text.Encoding.UTF8.GetBytes(texte);
        for (var i = 0; i < octets.Length; i++)
        {
            if (octets[i] != (byte)'{')
            {
                continue;
            }
            try
            {
                var lecteur = new Utf8JsonReader(octets.AsSpan(i));
                if (JsonDocument.TryParseValue(ref lecteur, out var document) == false)
                {
                    continue;
                }
                using (document)
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }
                    var racine = document.RootElement;
                    var equipement = Texte(racine, "equipementId");
                    return new PropositionLlm(
                        Texte(racine, "titre"),
                        Texte(racine, "categorie"),
                        Texte(racine, "dossier"),
                        Guid.TryParse(equipement, out var id) ? id : null,
                        Texte(racine, "dateDocument"),
                        Texte(racine, "montant"),
                        Texte(racine, "resume"));
                }
            }
            catch (JsonException)
            {
                // Cette accolade n'ouvrait pas un objet valide — essayer la suivante.
            }
        }
        return null;
    }

    private static string? Texte(JsonElement racine, string nom) =>
        racine.TryGetProperty(nom, out var valeur) && valeur.ValueKind == JsonValueKind.String
            ? valeur.GetString()?.Trim() is { Length: > 0 } t ? t : null
            : null;

    /// <summary>
    /// Applique une proposition en la validant champ par champ : ce qui sort du
    /// vocabulaire ou des colonnes retombe sur le repli, sans jeter le reste.
    /// </summary>
    public static MetadonneesDocument Appliquer(
        PropositionLlm? proposition, ContexteEnrichissement contexte, string? nomPieceJointe, string typeMime)
    {
        var repli = Repli(contexte, nomPieceJointe, typeMime);
        if (proposition is null)
        {
            return repli;
        }

        var titre = proposition.Titre is { Length: > 0 and <= LongueurMaxTitre } t ? t : repli.Titre;
        var categorie = proposition.Categorie is not null
            && Conversions.ParserEnum(proposition.Categorie, out CategorieDocument cat)
            ? cat
            : repli.Categorie;
        var dossier = proposition.Dossier is not null
            && contexte.Dossiers.Contains(proposition.Dossier, StringComparer.Ordinal)
            ? proposition.Dossier
            : null;
        var equipementId = proposition.EquipementId is { } eq
            && contexte.Equipements.Any(e => e.Id == eq)
            ? eq
            : (Guid?)null;
        var date = proposition.DateDocument is not null
            && DateOnly.TryParseExact(proposition.DateDocument, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d)
            ? d
            : repli.DateDocument;

        var notes = ComposerNotes(
            Tronquer(proposition.Resume, LongueurMaxResume),
            proposition.Montant,
            LigneProvenance(contexte));
        return new MetadonneesDocument(titre, categorie, dossier, equipementId, date, notes);
    }

    /// <summary>Sans LLM : le nom de la pièce ou le sujet, une catégorie par mots-clés,
    /// la date du courriel, et la provenance en notes.</summary>
    public static MetadonneesDocument Repli(ContexteEnrichissement contexte, string? nomPieceJointe, string typeMime)
    {
        var titre = nomPieceJointe is { Length: > 0 }
            ? Path.GetFileNameWithoutExtension(nomPieceJointe)
            : contexte.Sujet;
        if (string.IsNullOrWhiteSpace(titre))
        {
            titre = $"Courriel du {contexte.Date:yyyy-MM-dd}";
        }
        titre = Tronquer(titre.Trim(), LongueurMaxTitre)!;

        var indices = $"{contexte.Sujet} {nomPieceJointe} {Tronquer(contexte.Texte, 2000)}".ToLowerInvariant();
        var categorie = CategorieParMotsCles(indices)
            ?? (typeMime == EnregistrementDocument.TypeMimeCourriel
                ? CategorieDocument.Autre
                : DocumentsEndpoints.CategorieParDefaut(typeMime));

        return new MetadonneesDocument(
            titre, categorie, null, null,
            // L'heure murale de l'expéditeur, pas celle du serveur.
            DateOnly.FromDateTime(contexte.Date.DateTime),
            LigneProvenance(contexte));
    }

    internal static CategorieDocument? CategorieParMotsCles(string indices)
    {
        string[] facture = ["reçu", "recu", "receipt", "facture", "invoice", "commande", "order", "achat", "purchase"];
        string[] garantie = ["garantie", "warranty"];
        string[] assurance = ["assurance", "insurance"];
        string[] contrat = ["contrat", "contract", "entente", "agreement"];
        if (garantie.Any(indices.Contains)) return CategorieDocument.Garantie;
        if (assurance.Any(indices.Contains)) return CategorieDocument.Assurance;
        if (contrat.Any(indices.Contains)) return CategorieDocument.Contrat;
        if (facture.Any(indices.Contains)) return CategorieDocument.Facture;
        return null;
    }

    public static string LigneProvenance(ContexteEnrichissement contexte) =>
        $"Reçu par courriel de {contexte.Expediteur} le {contexte.Date:yyyy-MM-dd}"
        + (string.IsNullOrWhiteSpace(contexte.Sujet) ? "" : $" — {contexte.Sujet}");

    /// <summary>Résumé, montant, provenance — la provenance survit toujours à la coupe.</summary>
    internal static string ComposerNotes(string? resume, string? montant, string provenance)
    {
        var provenanceBornee = Tronquer(provenance, 400)!;
        var lignes = new List<string>();
        if (string.IsNullOrWhiteSpace(resume) == false)
        {
            lignes.Add(resume);
        }
        if (string.IsNullOrWhiteSpace(montant) == false)
        {
            lignes.Add($"Montant : {Tronquer(montant, 60)}");
        }
        lignes.Add(provenanceBornee);
        var notes = string.Join('\n', lignes);
        if (notes.Length <= LongueurMaxNotes)
        {
            return notes;
        }
        var reste = LongueurMaxNotes - provenanceBornee.Length - 1;
        return string.Join('\n', lignes.Take(lignes.Count - 1))[..Math.Max(0, reste)].TrimEnd() + "\n" + provenanceBornee;
    }

    private static string? Tronquer(string? texte, int max) =>
        texte is null ? null : texte.Length <= max ? texte : texte[..max];
}
