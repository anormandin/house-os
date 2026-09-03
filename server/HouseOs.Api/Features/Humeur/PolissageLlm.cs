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
        Tu écris la phrase d'accueil du tableau de bord maison d'un couple
        québécois. À partir de l'état JSON fourni, écris un titre et un
        sous-titre chaleureux et motivants pour le héros de la page Aujourd'hui.

        Priorités du contenu, dans l'ordre :
        1. Les tâches : celles du jour (nomme-en une ou deux quand il y en a), et ce
           qui s'en vient cette semaine (prochainesTaches, avec l'horizon en jours) —
           donne le goût de s'y mettre, ou quelque chose à anticiper.
        2. Les comptes à rebours (« dodos ») quand ils approchent.
        3. La météo : SEULEMENT si meteoRemarquable est présent, et en passant —
           jamais le sujet principal. S'il est absent, ne parle pas de météo du tout.

        Règles strictes :
        - Registre québécois léger et affectueux (« dodos » pour compter les nuits) ; jamais pompeux.
        - Inspirant sans jouer au coach de vie : de la chaleur et de l'élan, pas de slogans.
        - Jamais de culpabilisation, de reproche ni d'urgence anxiogène — même s'il y a du retard, on rassure.
        - N'utilise que les chiffres et faits présents dans l'état ; n'invente rien.
        - « moment » indique si on est le matin ou le soir — adapte le ton.
        - Le titre est une courte phrase d'ambiance, jamais une salutation ni un résumé.
        - Titre : 45 caractères max. Sous-titre : 140 caractères max. Pas d'emoji.
        - Réponds UNIQUEMENT avec cet objet JSON, rien d'autre :
          {"titre": "…", "sousTitre": "…"}

        Exemples du ton recherché (inspire-t'en sans les recopier) :
        {"titre": "Le garage n'a qu'à bien se tenir.", "sousTitre": "Grand ménage du garage aujourd'hui — pis samedi, les boîtes du salon. Ça avance pour vrai."}
        {"titre": "On y est presque.", "sousTitre": "4 petites choses avant dodo — le camion attend depuis hier, le reste est sous contrôle."}
        {"titre": "La maison respire.", "sousTitre": "Rien aujourd'hui. Prochaine affaire : les filtres de l'échangeur, dans 3 jours."}
        {"titre": "Petit train va loin.", "sousTitre": "2 choses au programme ce soir, rien qui presse — 12 dodos avant le grand départ."}
        """;

    /// <summary>Plafond de l'appel : le ct reçu est le jeton d'arrêt du service — sans
    /// délai propre, un appel qui traîne bloquerait le rattrapage du créneau.</summary>
    public static readonly TimeSpan DelaiMax = TimeSpan.FromSeconds(30);

    public static async Task<(string Titre, string SousTitre)?> Polir(
        EtatMaison etat, string cleApi, string modele, CancellationToken ct)
    {
        using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
        delai.CancelAfter(DelaiMax);
        AnthropicClient client = new() { ApiKey = cleApi };
        var reponse = await client.Messages.Create(new MessageCreateParams
        {
            Model = modele,
            MaxTokens = 300,
            System = PromptSysteme,
            Messages = [new() { Role = Role.User, Content = SerialiserEtat(etat) }],
        }, cancellationToken: delai.Token);

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
            tachesDuJour = etat.TachesDuJour,
            prochainesTaches = etat.ProchainesTaches
                .Select(t => new { titre = t.Titre, dansJours = t.DansJours }),
            comptesARebours = etat.ComptesProches
                .Select(c => new { titre = c.Titre, dodos = c.Dodos }),
            meteoRemarquable = etat.MeteoRemarquable?.Description,
        });

    /// <summary>Parse défensif de la réponse : clôtures de code et prose autour du JSON
    /// tolérées (chaque « { » est essayé comme début d'objet, le texte qui suit est
    /// ignoré), champs requis et textuels, longueurs bornées. Tout écart → null →
    /// banque de gabarits.</summary>
    public static (string Titre, string SousTitre)? Extraire(string texte)
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
                var lecteur = new System.Text.Json.Utf8JsonReader(octets.AsSpan(i));
                if (JsonDocument.TryParseValue(ref lecteur, out var document) == false)
                {
                    continue;
                }
                using (document)
                {
                    if (document.RootElement.TryGetProperty("titre", out var titre) == false
                        || document.RootElement.TryGetProperty("sousTitre", out var sousTitre) == false
                        // Contrat « tout écart → null » : un champ non textuel ferait
                        // lever GetString hors du catch JsonException.
                        || titre.ValueKind != JsonValueKind.String
                        || sousTitre.ValueKind != JsonValueKind.String)
                    {
                        continue;
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
            }
            catch (JsonException)
            {
                // Cette accolade n'ouvrait pas un objet valide — essayer la suivante.
            }
        }
        return null;
    }
}
