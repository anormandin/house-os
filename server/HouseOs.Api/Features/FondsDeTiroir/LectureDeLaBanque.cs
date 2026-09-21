using System.Text.Json;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La lecture du fichier de banque, une fois au démarrage. Un fichier immobile relu à
/// chaque rendu d'écran serait du gaspillage : la banque est de la donnée d'édition,
/// pas de l'état.
///
/// <para>Rien ici ne parle d'affichage : c'est une source du fonds de tiroir, au même
/// titre que les éphémérides (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).</para>
/// </summary>
public static class LectureDeLaBanque
{
    /// <summary>La banque livrée avec l'app, relative à la racine de contenu.</summary>
    public const string NomDuFichierParDefaut = "Features/FondsDeTiroir/banque-du-hasard.qc.json";

    /// <summary>
    /// Commentaires et virgules finales tolérés : ce fichier se modifie à la main, à
    /// côté du `.env`, et une virgule de trop ne doit pas coûter le journal du matin.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// La banque à servir. <paramref name="cheminConfigure"/> vide : celle qui est
    /// livrée avec l'app. Réglé : celle-là et rien d'autre — si elle manque ou ne se
    /// lit pas, la famille se tait et le journal sort quand même.
    /// </summary>
    public static BanqueDuHasard Lire(string? cheminConfigure, string racineDeContenu, ILogger journal)
    {
        var configuree = string.IsNullOrWhiteSpace(cheminConfigure) == false;
        var chemin = configuree
            ? cheminConfigure!.Trim()
            : Path.Combine(racineDeContenu, NomDuFichierParDefaut);

        if (File.Exists(chemin) == false)
        {
            // Une banque configurée et absente est une faute de frappe dans le `.env` :
            // elle se dit. La banque livrée absente, c'est un problème d'image.
            journal.LogWarning("Banque du hasard introuvable ({Chemin}) : la famille « le hasard » se tait.", chemin);
            return BanqueDuHasard.Vide;
        }

        try
        {
            var banque = JsonSerializer.Deserialize<BanqueDuHasard>(File.ReadAllText(chemin), Options);
            if (banque is null)
            {
                journal.LogWarning("Banque du hasard vide ({Chemin}) : la famille « le hasard » se tait.", chemin);
                return BanqueDuHasard.Vide;
            }
            var retenue = Retenir(banque);
            journal.LogInformation(
                "Banque du hasard lue ({Chemin}) : {Dictons} dictons, {Fetes} fêtes.",
                chemin, retenue.Dictons.Count, retenue.Fetes.Count);
            return retenue;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            journal.LogWarning(ex, "Banque du hasard illisible ({Chemin}) : la famille « le hasard » se tait.", chemin);
            return BanqueDuHasard.Vide;
        }
    }

    /// <summary>
    /// Ce qui est utilisable. Une entrée incomplète est écartée plutôt que de faire
    /// tomber la lecture entière : une faute dans une ligne ne doit pas coûter la
    /// banque, et surtout pas le journal.
    /// </summary>
    internal static BanqueDuHasard Retenir(BanqueDuHasard banque) =>
        new(
            [.. (banque.Dictons ?? [])
                .Where(d => d.Mois is >= 1 and <= 12 && string.IsNullOrWhiteSpace(d.Texte) == false)],
            [.. (banque.Fetes ?? [])
                .Where(f => f.Quand is not null
                            && string.IsNullOrWhiteSpace(f.Nom) == false
                            && string.IsNullOrWhiteSpace(f.Texte) == false)]);
}
