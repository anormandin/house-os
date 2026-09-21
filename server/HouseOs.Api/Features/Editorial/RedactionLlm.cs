using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Models.Messages;
using HouseOs.Api.Domaine.Editorial;

namespace HouseOs.Api.Features.Editorial;

/// <summary>
/// L'éditorialiste : un appel Opus par édition, jamais dans le chemin de requête
/// (D-2026-09-20 Édition Écrite Par Opus). Le modèle reçoit des faits déjà calculés et
/// écrit le surtitre, la manchette, le chapeau, deux paragraphes et les rubriques ; la
/// sortie est validée strictement, et tout écart retombe sur le gabarit.
/// </summary>
public static partial class RedactionLlm
{
    public const int LongueurMaxSurtitre = 60;
    public const int LongueurMaxManchette = 60;
    public const int LongueurMaxChapeau = 160;
    /// <summary>Le prompt vise 200 signes ; le contrat en tolère 240, parce qu'un modèle
    /// qui compte les signes déborde d'une phrase, et que le clamp du mur fait le filet.
    /// Au-delà, c'est un autre texte que celui demandé.</summary>
    public const int LongueurMaxParagraphe = 240;
    public const int LongueurViseeParagraphe = 200;
    public const int NombreDeParagraphes = 2;
    public const int LongueurMaxRubrique = 30;
    public const int MaxRubriques = 5;

    /// <summary>Le prompt système en vigueur. Public pour que l'atelier
    /// (`server/HouseOs.Essais`) l'imprime, le retouche dans un fichier et rejoue une
    /// matière conservée contre la retouche — sans toucher au code avant d'avoir lu.</summary>
    public const string PromptParDefaut = """
        Tu es l'éditorialiste d'un petit quotidien imprimé chaque matin pour un foyer de
        deux adultes au Québec : « La maison ». Il s'affiche sur un écran mural, en noir
        sur blanc, et se lit de loin. Tu reçois l'état du jour en JSON — les tâches dues,
        le compte à rebours, la météo, et un fonds de tiroir de petites choses vraies
        déjà classées (le ciel, le climat, la maison, le calendrier, la ville, le hasard)
        — et tu écris l'édition.

        Ce que tu écris, en JSON et rien d'autre :
        {"surtitre": "…", "manchette": "…", "chapeau": "…", "paragraphes": ["…", "…"], "rubriques": []}

        - surtitre : la ligne éditoriale au-dessus de la manchette, 60 caractères max.
          Une phrase qui situe la journée (« Première fin de semaine libre depuis le
          26 août », « Deux fois par année, pas plus »). Jamais la date, jamais le compte
          de tâches, jamais la météo : la dateline les dit déjà. Vide si tu n'as rien de
          vrai à y mettre.
        - manchette : le titre, 45 caractères ou moins (60 au plus), sans point final.
          Une phrase courte qui porte une idée (« La maison ne demande rien », « Les
          boîtes, encore, et c'est tout »). Quand « plancher » est présent, la manchette
          est le titre de cette tâche ou de ce compte à rebours, tel quel : il ne peut
          pas être relégué.
        - chapeau : une ligne de faits séparés par « · », 160 caractères max, comme
          « Aucune occurrence due · 33 jours dans la maison · première neige vers le 20 ».
        - paragraphes : exactement deux, 200 caractères chacun au plus (compte-les :
          au-delà, le paragraphe est coupé au mur). Le premier
          fait le rapprochement entre deux faits qu'aucune liste ne ferait ; le second
          regarde devant (ce qui vient, ce que la maison propose). Phrases courtes.
        - rubriques : vide sauf quand tachesDues compte dix tâches et plus (le rang
          « Sommaire », ou « Evenement » sur une journée chargée). Alors le
          journal range lui-même les tâches qui portent une « zone » ou un « equipement »,
          sous ce nom ; tu ne nommes que les autres (zone et equipement à null). Au plus
          cinq rubriques nommées (« Gouvernements », « Argent », « La paperasse »…),
          chacune avec la liste des titres de tâches EXACTS qu'elle regroupe, copiés de
          tachesDues. Vise trois ou quatre rubriques de deux à cinq tâches, avec un nom
          court qui se lit de loin (30 caractères max) : pas une rubrique par tâche, pas une
          seule rubrique pour tout. Ce que tu ne places pas paraît sous « Le reste » ; ne
          nomme donc pas de rubrique « Le reste ».

        Règles strictes :
        - AUCUN FAIT INVENTÉ. Chaque chiffre, chaque date, chaque titre de tâche, chaque
          nom d'équipement ou de pièce vient de l'état fourni. Pas d'heure, pas de
          température, pas de « depuis dix ans » que l'état ne donne pas. Si tu n'as pas
          la matière pour un paragraphe, écris plus court, jamais plus inventé.
        - Les faits marqués « publie »: true paraissent sur la page en widgets : la prose
          ne les redit pas mot pour mot, elle les relie.
        - Registre : un journal de quartier chaleureux et sobre, québécois léger,
          tutoiement absent (« la maison », « on »). Pas de coach de vie, pas de slogan,
          pas d'emoji, pas de point d'exclamation. Jamais de reproche ni d'urgence
          anxiogène : un retard se dit, il ne se crie pas.
        - Le rang dit la place : « Chronique » (rien à faire) est le jour où le journal
          raconte ; « Court » et « Sommaire » veulent des phrases plus brèves.
        - « precedentes » liste ce que les éditions de la semaine ont déjà dit. N'en
          reprends ni la formule, ni l'angle, ni l'image : chaque matin doit surprendre.
          En particulier, ne construis pas le surtitre sur le même patron qu'un
          surtitre précédent (« Le dernier lundi… », « Le samedi où… ») : si la semaine
          a déjà daté ses surtitres par le jour, trouve un autre angle — un chiffre, un
          lieu, une saison, un geste. Même règle pour l'ouverture des paragraphes.
        - Typographie française : guillemets « », espace avant les deux-points et le
          point-virgule, « 18 h 25 » pour les heures, « −5 °C » pour les degrés.
        - Réponds UNIQUEMENT avec l'objet JSON, sans clôture de code ni commentaire.
        """;

    /// <summary>Plafond de l'appel : Opus réfléchit avant d'écrire, et le service de
    /// fond n'est pas pressé — mais il ne doit pas rester coincé jusqu'au lendemain.</summary>
    public static readonly TimeSpan DelaiMax = TimeSpan.FromSeconds(120);

    public static async Task<TexteDEdition?> Rediger(
        MatiereDEdition matiere, string cleApi, string modele, CancellationToken ct) =>
        (await RedigerAvecEcart(matiere, cleApi, modele, ct)).Texte;

    /// <summary>Ce que l'appel a rendu : le texte accepté ou l'écart qui l'a fait
    /// refuser, et dans les deux cas la réponse brute du modèle — pour l'atelier, qui
    /// veut lire ce qu'un texte refusé disait.</summary>
    public sealed record Reponse(TexteDEdition? Texte, string? Ecart, string? Brut);

    /// <summary>L'appel, avec ce qui a été refusé quand le texte est null — pour le
    /// journal. <paramref name="promptSysteme"/> remplace <see cref="PromptParDefaut"/>
    /// pour un essai ; l'application n'en passe jamais.</summary>
    public static async Task<Reponse> RedigerAvecEcart(
        MatiereDEdition matiere, string cleApi, string modele, CancellationToken ct,
        string? promptSysteme = null)
    {
        using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
        delai.CancelAfter(DelaiMax);
        AnthropicClient client = new() { ApiKey = cleApi };
        var reponse = await client.Messages.Create(new MessageCreateParams
        {
            Model = modele,
            // La réflexion adaptative compte dans ce plafond : de la marge, pour que la
            // réponse ne soit pas coupée en plein JSON.
            MaxTokens = 8000,
            System = promptSysteme ?? PromptParDefaut,
            Messages = [new() { Role = Role.User, Content = SerialiserMatiere(matiere) }],
        }, cancellationToken: delai.Token);

        var texte = reponse.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault()?.Text;
        if (texte is null)
        {
            return new Reponse(null, $"aucun texte (arrêt : {reponse.StopReason})", null);
        }
        var extrait = Extraire(texte, matiere, out var ecart);
        return new Reponse(extrait, ecart, texte);
    }

    /// <summary>L'état vu par le modèle : que des faits calculés, en français. C'est
    /// aussi, mot pour mot, ce que l'édition conserve (<c>Edition.Matiere</c>).</summary>
    public static string SerialiserMatiere(MatiereDEdition matiere) =>
        JsonSerializer.Serialize(MatiereJson.Depuis(matiere), OptionsMatiere);

    /// <summary>La matière relue depuis son JSON conservé, pour rejouer une journée
    /// contre un autre prompt. Null quand le texte n'est pas une matière.</summary>
    public static MatiereDEdition? DeserialiserMatiere(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MatiereJson>(json, OptionsMatiere)?.VersMatiere();
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or FormatException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions OptionsMatiere = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// La forme exacte du JSON envoyé au modèle — les noms que le prompt cite
    /// (« tachesDues », « publie », « precedentes »). Un enregistrement plutôt qu'un
    /// objet anonyme pour que la même forme se relise.
    /// </summary>
    private sealed record MatiereJson(
        string Date,
        string JourDeSemaine,
        string? Lieu,
        string Rang,
        PlancherJson? Plancher,
        List<TacheJson> TachesDues,
        CompteJson? ProchainCompteARebours,
        MeteoJson? MeteoDuJour,
        List<FaitJson> FondsDeTiroir,
        List<PrecedenteJson> Precedentes)
    {
        public static MatiereJson Depuis(MatiereDEdition m) => new(
            m.Date.ToString("yyyy-MM-dd"),
            m.Date.ToString("dddd", System.Globalization.CultureInfo.GetCultureInfo("fr-CA")),
            m.Lieu,
            m.Rang.ToString(),
            m.Plancher is { } p ? new PlancherJson(p.Raison.ToString(), p.Titre) : null,
            [.. m.TachesDues.Select(t => new TacheJson(
                t.Titre, t.JoursDeRetard, t.EcheanceFerme, t.Assigne, t.Zone, t.Equipement))],
            m.ProchainCompte is { } c ? new CompteJson(c.Titre, c.Dodos) : null,
            m.Meteo is { } me ? new MeteoJson(me.Description, me.TempMin, me.TempMax) : null,
            [.. m.Faits.Select(f => new FaitJson(f.Cle, f.Famille, f.Etiquette, f.Valeur, f.Texte, f.Publie))],
            [.. m.Precedentes.Select(e => new PrecedenteJson(
                e.Date.ToString("yyyy-MM-dd"), e.Surtitre, e.Manchette, e.Chapeau))]);

        /// <summary>Le jour de semaine se recalcule de la date ; le reste se relit tel quel.</summary>
        public MatiereDEdition VersMatiere() => new(
            DateOnly.ParseExact(Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            Enum.Parse<RangEdition>(Rang),
            Plancher is { } p ? new PlancherDuJour(Enum.Parse<RaisonDePlancher>(p.Raison), p.Titre) : null,
            [.. (TachesDues ?? []).Select(t => new TachePourEdition(
                t.Titre, t.JoursDeRetard, t.EcheanceFerme, t.Assigne, t.Zone, t.Equipement))],
            ProchainCompteARebours is { } c ? new CompteProcheDEdition(c.Titre, c.Dodos) : null,
            MeteoDuJour is { } me ? new MeteoDEdition(me.Description, me.MinC, me.MaxC) : null,
            [.. (FondsDeTiroir ?? []).Select(f => new FaitPourEdition(
                f.Cle, f.Famille, f.Etiquette, f.Valeur, f.Texte, f.Publie))],
            [.. (Precedentes ?? []).Select(e => new EditionPrecedente(
                DateOnly.ParseExact(e.Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                e.Surtitre, e.Manchette, e.Chapeau))],
            Lieu);
    }

    private sealed record PlancherJson(string Raison, string Titre);
    private sealed record TacheJson(
        string Titre, int JoursDeRetard, bool EcheanceFerme, string? Assigne, string? Zone, string? Equipement);
    private sealed record CompteJson(string Titre, int Dodos);
    private sealed record MeteoJson(string Description, double MinC, double MaxC);
    private sealed record FaitJson(string Cle, string Famille, string Etiquette, string Valeur, string Texte, bool Publie);
    private sealed record PrecedenteJson(string Date, string Surtitre, string Manchette, string Chapeau);

    /// <summary>
    /// Parse défensif et validation stricte, sur le patron de
    /// <see cref="Humeur.PolissageLlm.Extraire"/> : clôtures de code et prose tolérées
    /// autour du JSON, champs requis et textuels, longueurs bornées, deux paragraphes
    /// exactement. Les rubriques (journée chargée seulement) ne gardent que des titres de tâches <b>réels</b>, et
    /// seulement ceux que le journal ne range pas déjà par zone ou par équipement — un
    /// titre inventé par le modèle est écarté, jamais affiché ; une tâche déjà rangée
    /// reste où le journal la met. Tout autre écart → null → gabarit.
    /// </summary>
    public static TexteDEdition? Extraire(string texte, MatiereDEdition matiere) =>
        Extraire(texte, matiere, out _);

    /// <param name="ecart">Ce qui a été refusé, en un mot, pour le journal : sans lui,
    /// un « hors contrat » ne dit pas s'il faut retoucher le prompt ou la borne. Null
    /// quand le texte est accepté.</param>
    public static TexteDEdition? Extraire(string texte, MatiereDEdition matiere, out string? ecart)
    {
        ecart = "aucun objet JSON";
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
                    var resultat = Valider(document.RootElement, matiere, out var refus);
                    ecart = resultat is null ? refus : null;
                    return resultat;
                }
            }
            catch (JsonException)
            {
                // Cette accolade n'ouvrait pas un objet valide — essayer la suivante.
            }
        }
        return null;
    }

    private static TexteDEdition? Valider(JsonElement racine, MatiereDEdition matiere, out string ecart)
    {
        var surtitre = Texte(racine, "surtitre", 0, LongueurMaxSurtitre, out ecart);
        if (surtitre is null)
        {
            ecart = $"surtitre : {ecart}";
            return null;
        }
        var manchette = Texte(racine, "manchette", 1, LongueurMaxManchette, out ecart);
        if (manchette is null)
        {
            ecart = $"manchette : {ecart}";
            return null;
        }
        var chapeau = Texte(racine, "chapeau", 1, LongueurMaxChapeau, out ecart);
        if (chapeau is null)
        {
            ecart = $"chapeau : {ecart}";
            return null;
        }

        if (racine.TryGetProperty("paragraphes", out var paragraphesJson) == false
            || paragraphesJson.ValueKind != JsonValueKind.Array
            || paragraphesJson.GetArrayLength() != NombreDeParagraphes)
        {
            ecart = $"paragraphes : il en faut {NombreDeParagraphes}";
            return null;
        }
        var paragraphes = new List<string>();
        foreach (var p in paragraphesJson.EnumerateArray())
        {
            var paragraphe = Texte(p, 1, LongueurMaxParagraphe, out ecart);
            if (paragraphe is null)
            {
                ecart = $"paragraphe {paragraphes.Count + 1} : {ecart}";
                return null;
            }
            paragraphes.Add(paragraphe);
        }

        var rubriques = new List<RubriqueEdition>();
        // La journée chargée, pas le rang : un plancher sur douze tâches fait un rang
        // « événement », et la liste se range quand même par rubrique.
        if (RangDuJour.JourneeChargee(matiere.TachesDues.Count)
            && racine.TryGetProperty("rubriques", out var rubriquesJson)
            && rubriquesJson.ValueKind == JsonValueKind.Array)
        {
            // Seules les tâches sans zone ni équipement sont à nommer : les autres ont
            // déjà leur rubrique, et le modèle n'a pas à les déplacer.
            var titresReels = matiere.TachesDues
                .Where(t => t.ANommer)
                .Select(t => t.Titre)
                .ToHashSet(StringComparer.Ordinal);
            var dejaPlacees = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in rubriquesJson.EnumerateArray())
            {
                ecart = "rubriques : forme inattendue";
                if (r.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }
                var nom = Texte(r, "nom", 1, LongueurMaxRubrique, out _);
                if (nom is null || r.TryGetProperty("taches", out var tachesJson) == false
                    || tachesJson.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }
                // « Le reste » n'est pas une rubrique qu'on nomme : c'est ce qui n'a pas
                // été placé, et le journal l'écrit lui-même.
                if (string.Equals(nom, Regroupement.LeReste, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Un titre inventé n'est pas une erreur du contrat, c'est un mensonge du
                // modèle : on l'écarte et on garde le reste, qui est vrai.
                var taches = tachesJson.EnumerateArray()
                    .Where(t => t.ValueKind == JsonValueKind.String)
                    .Select(t => Nettoyer(t.GetString()))
                    .Where(t => titresReels.Contains(t) && dejaPlacees.Add(t))
                    .ToList();
                if (taches.Count > 0)
                {
                    rubriques.Add(new RubriqueEdition(nom, taches));
                }
            }
            if (rubriques.Count > MaxRubriques)
            {
                ecart = $"rubriques : plus de {MaxRubriques}";
                return null;
            }
        }

        ecart = "";
        return new TexteDEdition(surtitre, manchette, chapeau, paragraphes, rubriques);
    }

    private static string? Texte(JsonElement objet, string nom, int min, int max, out string ecart)
    {
        if (objet.TryGetProperty(nom, out var valeur) == false)
        {
            ecart = "absent";
            return null;
        }
        return Texte(valeur, min, max, out ecart);
    }

    /// <summary>Contrat « tout écart → null » : un champ non textuel ferait lever
    /// GetString hors du catch JsonException.</summary>
    private static string? Texte(JsonElement valeur, int min, int max, out string ecart)
    {
        ecart = "";
        if (valeur.ValueKind != JsonValueKind.String)
        {
            ecart = "pas du texte";
            return null;
        }
        var texte = Nettoyer(valeur.GetString());
        if (texte.Length < min)
        {
            ecart = "vide";
            return null;
        }
        if (texte.Length > max)
        {
            ecart = $"{texte.Length} signes, {max} au plus";
            return null;
        }
        return texte;
    }

    private static string Nettoyer(string? texte) =>
        Blancs().Replace((texte ?? "").Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Blancs();
}
