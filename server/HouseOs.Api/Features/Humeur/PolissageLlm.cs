using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using HouseOs.Api.Domaine.Humeur;

namespace HouseOs.Api.Features.Humeur;

/// <summary>
/// Couche 3 : le polissage LLM de la phrase du jour, via le SDK C# officiel.
/// Deux appels Haiku par jour, jamais dans le chemin de requête ; tout échec
/// retombe silencieusement sur la banque de gabarits.
/// </summary>
public static class PolissageLlm
{
    private const int LongueurMaxTitre = 60;
    private const int LongueurMaxSousTitre = 180;

    private const string PromptSysteme = """
        Tu écris la phrase d'accueil du tableau de bord maison d'Alain et Ariane,
        un couple québécois. À partir de l'état JSON fourni, écris un titre et un
        sous-titre chaleureux pour le héros de la page Aujourd'hui.

        Règles strictes :
        - Registre québécois léger et affectueux (« dodos » pour compter les nuits) ; jamais pompeux.
        - Jamais de culpabilisation, de reproche ni d'urgence anxiogène — même s'il y a du retard, on rassure.
        - N'utilise que les chiffres et faits présents dans l'état ; n'invente rien.
        - « moment » indique si on est le matin ou le soir — adapte le ton.
        - Le titre est une courte phrase d'ambiance, jamais une salutation ni un résumé.
        - Titre : 45 caractères max. Sous-titre : 140 caractères max. Pas d'emoji.
        - Réponds UNIQUEMENT avec cet objet JSON, rien d'autre :
          {"titre": "…", "sousTitre": "…"}

        Exemples du ton recherché (inspire-t'en sans les recopier) :
        {"titre": "On y est presque.", "sousTitre": "4 petites choses avant dodo — le camion attend depuis hier, le reste est sous contrôle."}
        {"titre": "La maison respire.", "sousTitre": "Tout est fait, pis il fait beau — allez donc prendre l'air."}
        {"titre": "Petit train va loin.", "sousTitre": "2 choses au programme ce soir, rien qui presse."}
        """;

    public static async Task<(string Titre, string SousTitre)?> Polir(
        EtatMaison etat, string cleApi, string modele, CancellationToken ct)
    {
        AnthropicClient client = new() { ApiKey = cleApi };
        var reponse = await client.Messages.Create(new MessageCreateParams
        {
            Model = modele,
            MaxTokens = 300,
            System = PromptSysteme,
            Messages = [new() { Role = Role.User, Content = SerialiserEtat(etat) }],
        }, cancellationToken: ct);

        var texte = reponse.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault()?.Text;
        return texte is null ? null : Extraire(texte);
    }

    /// <summary>L'état vu par le LLM : que des faits calculés, en français.</summary>
    public static string SerialiserEtat(EtatMaison etat) =>
        JsonSerializer.Serialize(new
        {
            date = etat.Date.ToString("yyyy-MM-dd"),
            moment = etat.Moment == MomentJournee.Matin ? "matin" : "soir",
            tachesOuvertes = etat.Ouvertes,
            tachesEnRetard = etat.EnRetard,
            tachesFaitesAujourdhui = etat.FaitesAujourdhui,
            etat = etat.Cle.ToString(),
            comptesARebours = etat.ComptesProches
                .Select(c => new { titre = c.Titre, dodos = c.Dodos }),
            meteo = etat.Meteo is null ? null : new
            {
                temperatureMin = Math.Round(etat.Meteo.TemperatureMin),
                temperatureMax = Math.Round(etat.Meteo.TemperatureMax),
                probabilitePluiePct = etat.Meteo.ProbabilitePrecipitationPct,
                bonnesJourneesPour = etat.Meteo.VerdictsFavorables,
            },
        });

    /// <summary>Parse défensif de la réponse : clôtures de code tolérées, champs
    /// requis, longueurs bornées. Tout écart → null → banque de gabarits.</summary>
    public static (string Titre, string SousTitre)? Extraire(string texte)
    {
        var debut = texte.IndexOf('{');
        var fin = texte.LastIndexOf('}');
        if (debut < 0 || fin <= debut)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(texte[debut..(fin + 1)]);
            if (document.RootElement.TryGetProperty("titre", out var titre) == false
                || document.RootElement.TryGetProperty("sousTitre", out var sousTitre) == false)
            {
                return null;
            }
            var t = titre.GetString()?.Trim();
            var s = sousTitre.GetString()?.Trim();
            if (string.IsNullOrEmpty(t) || string.IsNullOrEmpty(s)
                || t.Length > LongueurMaxTitre || s.Length > LongueurMaxSousTitre)
            {
                return null;
            }
            return (t, s);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
