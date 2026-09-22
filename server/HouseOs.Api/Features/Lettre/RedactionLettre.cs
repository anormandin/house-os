using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Models.Messages;
using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Domaine.Lettre;

namespace HouseOs.Api.Features.Lettre;

/// <summary>
/// La plume de la lettre du matin : un appel Opus par jour, à part de l'édition du mur
/// et sur la même matière étendue (D-2026-09-21 Lettre Écrite À Part Sur La Même
/// Matière). Le modèle écrit le sujet et de trois à cinq paragraphes ; la sortie est
/// validée strictement, et tout écart rend null — un second essai, puis la note.
/// </summary>
public static partial class RedactionLettre
{
    public const int LongueurMaxSujet = 60;
    public const int MinParagraphes = 3;
    public const int MaxParagraphes = 5;
    public const int LongueurMinParagraphe = 60;
    /// <summary>Le prompt vise 250 signes et dit 320 au plus ; le contrat en tolère 360,
    /// parce qu'Opus déborde sa consigne de dix à quinze pour cent sur le paragraphe des
    /// faits (338 et 328 signes aux deux rejeux du 2026-09-21), et qu'un courriel n'a
    /// pas de colonne à respecter — le plafond total fait le filet.</summary>
    public const int LongueurMaxParagraphe = 360;
    /// <summary>Le contrat ne peut pas vérifier qu'un aperçu porte la journée, mais il
    /// peut vérifier qu'il y a un aperçu.</summary>
    public const int LongueurMinPremierParagraphe = 100;
    public const int LongueurMaxTotale = 1400;
    /// <summary>Ce qui part seul dans l'aperçu de notification, et ce que la mémoire
    /// retient d'une lettre.</summary>
    public const int LongueurPremiereLigne = 120;

    /// <summary>Le prompt système en vigueur (vault : Prompt De La Lettre). Public pour
    /// que l'atelier l'imprime, le retouche dans un fichier et rejoue une matière
    /// conservée contre la retouche.</summary>
    public const string PromptParDefaut = """
        Tu écris la lettre du matin d'une maison à ses deux habitants, au Québec. C'est la
        maison qui parle : elle dit « je », elle s'adresse à « vous deux », elle connaît ses
        pièces, ses équipements et sa saison. Le courriel part vers 6 h 30, avant que la maison
        se lève ; il se lit au lit, en entier, en vingt à trente secondes.

        Tu reçois l'état du jour en JSON : la date et le jour de semaine, le lieu, le rang, un
        plancher éventuel, les tâches dues, le prochain compte à rebours en dodos, la météo, un
        fonds de tiroir de petites choses vraies déjà classées (le ciel, le climat, la maison,
        le calendrier, la ville, le hasard), la semaine devant (les échéances des sept
        prochains jours), ce qui a été fait depuis la dernière lettre, et les sept lettres
        précédentes. Chaque tâche due porte sa « serie » : le nombre de fois de suite qu'elle
        a été faite ce même jour de semaine (0 = rien à dire là-dessus).

        Ce que tu écris, en JSON et rien d'autre :
        {"sujet": "…", "paragraphes": ["…", "…", "…"]}

        - sujet : 55 signes visés, 60 au plus, sans point final. Il dit la journée sans la
          vendre : « Demain, le camion », « Rien à faire, sauf sortir les bacs », « Les bacs ce
          soir, le vétérinaire quand vous pourrez ». Jamais la date, jamais « Lettre du matin »,
          jamais de point d'exclamation.
        - paragraphes : de trois à cinq. Vise 250 signes par paragraphe, jamais plus de 320,
          et 1200 signes en tout, 1400 au plus. Compte-les : au-delà, la lettre entière est refusée,
          et la journée retombe sur un second essai puis sur une note de quatre lignes. De la
          prose seulement, aucune liste, aucune puce, aucun titre, aucun gras, aucun lien.
        - Les cent vingt premiers signes du premier paragraphe partent seuls dans l'aperçu de
          notification. Ils doivent suffire à qui n'ouvrira jamais la lettre : ce qui est dû
          aujourd'hui, ou le fait que rien ne l'est. Le reste de la lettre développe.
        - N'écris ni salutation ni signature. Le gabarit pose déjà la date, « Bonjour vous
          deux. » avant tes paragraphes, et « Bonne journée. » puis « — la maison » après.

        Règles strictes :

        - AUCUN FAIT INVENTÉ. Chaque chiffre, chaque date, chaque titre de tâche, chaque prénom,
          chaque nom de pièce ou d'équipement vient de l'état fourni. Pas d'heure, pas de
          température, pas d'objet dans la maison, pas de souvenir d'avant, pas de « depuis dix
          ans » que l'état ne donne pas. Si un paragraphe manque de matière, écris-en un de
          moins ; jamais un de plus, inventé.
        - Les titres de tâches se copient tels quels ou se raccourcissent fidèlement
          (« Banques et caisses — changement d'adresse » devient « les banques et caisses »).
          Jamais reformulés en autre chose, jamais fondus dans une catégorie que l'état ne nomme
          pas.
        - Tu ne prêtes à personne un pronom ni un genre : les gens se nomment par le prénom que
          « assigne » donne. Une tâche sans « assigne » n'appartient à personne, et ça, tu peux
          le dire.
        - « publie » ne te regarde pas : c'est une marque du journal mural, qui a des widgets. La
          lettre n'en a pas. Tous les faits du fonds sont à toi, y compris ceux marqués faux.
        - Ce que tu as le droit de faire, et qui est tout l'intérêt de la lettre : relier deux
          faits que rien d'autre ne rapproche, commenter, plaisanter, glisser une pensée.
          « echeanceFerme » à faux veut dire qu'une échéance peut glisser, et tu peux le dire ;
          « joursDeRetard » se dit tel quel ; deux faits du fonds se comparent.
        - Le passé ne se dit que par « precedentes », « faitesDepuisLaDerniere » et « serie ».
          « La même que les dix derniers dimanches » se dit si la série vaut dix, pas autrement ;
          « comme dimanche dernier » se dit si une lettre précédente ou la série le prouve.
          Ce qui a été fait se mentionne en passant, sans félicitations.
        - Ne radote pas. « precedentes » donne le sujet et la première ligne des sept dernières
          lettres. N'en reprends ni la formule, ni l'angle, ni l'image, ni le patron
          d'ouverture. Si trois lettres de suite ont ouvert sur la météo, ouvre ailleurs ; si la
          semaine a compté les dodos tous les matins, compte autre chose.
        - Le plafond. Une journée à quatorze tâches ne donne pas une lettre plus longue qu'une
          journée à une seule. Nomme ce qui compte, deux ou trois choses, et renvoie au reste en
          une phrase. Le rang dit la forme du jour, pas la longueur de la lettre : « Chronique »
          est le jour où tu as de la place pour parler d'autre chose, pas le jour où tu écris
          moins.
        - Quand « plancher » est là, son titre paraît dans le premier paragraphe : un compte à
          zéro, une échéance ferme ou un retard de plus de trois jours ne se relègue pas.
        - Registre : une maison qui connaît ses gens. Chaleureuse, un peu drôle, jamais
          moralisatrice. Le trait d'esprit, pas la leçon. Un retard se constate, il ne se
          sermonne pas. Pas de coaching, pas de « n'oubliez pas », pas de « bonne motivation »,
          pas d'emoji, pas de point d'exclamation — sauf celui qu'un titre de tâche porte
          déjà, quand tu le cites tel quel. Une inspiration ou une pensée du jour est
          bienvenue si elle est légère et si elle tient en une phrase.
        - Français du Québec naturel : « fin de semaine », « dîner » le midi, « les bacs », « le
          chemin ». Pas de folklore, pas d'accent écrit, pas d'anglicisme forcé. Tutoiement
          collectif : « vous deux », « vous ».
        - Typographie française : guillemets « », espace avant les deux-points et le
          point-virgule, « 18 h 25 » pour les heures, « −5 °C » pour les degrés. Dans une lettre
          les petits nombres s'écrivent volontiers en toutes lettres (« seize dodos »,
          « dix-neuf cet après-midi ») ; garde le même choix d'un bout à l'autre. Pas de tiret
          cadratin de ton cru ; si tu cites un titre de tâche entier, il garde le sien.
        - Réponds UNIQUEMENT avec l'objet JSON, sans clôture de code ni commentaire.
        """;

    public static readonly TimeSpan DelaiMax = TimeSpan.FromSeconds(120);

    public sealed record Reponse(TexteDeLettre? Texte, string? Ecart, string? Brut);

    /// <summary>L'appel, avec ce qui a été refusé quand le texte est null.
    /// <paramref name="promptSysteme"/> remplace <see cref="PromptParDefaut"/> pour un
    /// essai ; l'application n'en passe jamais.</summary>
    public static async Task<Reponse> RedigerAvecEcart(
        MatiereDeLettre matiere, string cleApi, string modele, CancellationToken ct,
        string? promptSysteme = null)
    {
        using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
        delai.CancelAfter(DelaiMax);
        AnthropicClient client = new() { ApiKey = cleApi };
        var reponse = await client.Messages.Create(new MessageCreateParams
        {
            Model = modele,
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

    /// <summary>L'état vu par le modèle. C'est aussi, mot pour mot, ce que la lettre
    /// conserve (<c>LettreDuMatin.Matiere</c>).</summary>
    public static string SerialiserMatiere(MatiereDeLettre matiere) =>
        JsonSerializer.Serialize(LettreJson.Depuis(matiere), OptionsMatiere);

    public static MatiereDeLettre? DeserialiserMatiere(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<LettreJson>(json, OptionsMatiere)?.VersMatiere();
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

    private static readonly System.Globalization.CultureInfo Fr =
        System.Globalization.CultureInfo.GetCultureInfo("fr-CA");

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd");
    private static DateOnly DeIso(string s) =>
        DateOnly.ParseExact(s, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>La forme exacte du JSON envoyé au modèle — les noms que le prompt cite.
    /// À plat, comme celui de l'édition, plus ce que la lettre seule reçoit.</summary>
    private sealed record LettreJson(
        string Date,
        string JourDeSemaine,
        string? Lieu,
        string Rang,
        PlancherJson? Plancher,
        List<TacheJson> TachesDues,
        CompteJson? ProchainCompteARebours,
        MeteoJson? MeteoDuJour,
        List<FaitJson> FondsDeTiroir,
        List<EcheanceJson> SemaineDevant,
        List<FaiteJson> FaitesDepuisLaDerniere,
        List<PrecedenteJson> Precedentes)
    {
        public static LettreJson Depuis(MatiereDeLettre l)
        {
            var m = l.Edition;
            return new(
                Iso(m.Date),
                m.Date.ToString("dddd", Fr),
                m.Lieu,
                m.Rang.ToString(),
                m.Plancher is { } p ? new PlancherJson(p.Raison.ToString(), p.Titre) : null,
                [.. m.TachesDues.Select(t => new TacheJson(
                    t.Titre, t.JoursDeRetard, t.EcheanceFerme, t.Assigne, t.Zone, t.Equipement, l.Serie(t.Titre)))],
                m.ProchainCompte is { } c ? new CompteJson(c.Titre, c.Dodos) : null,
                m.Meteo is { } me ? new MeteoJson(me.Description, me.TempMin, me.TempMax) : null,
                [.. m.Faits.Select(f => new FaitJson(f.Cle, f.Famille, f.Etiquette, f.Valeur, f.Texte, f.Publie))],
                [.. l.SemaineDevant.Select(e => new EcheanceJson(Iso(e.Date), e.Date.ToString("dddd", Fr), e.Titre, e.Assigne, e.EcheanceFerme))],
                [.. l.FaitesDepuisLaDerniere.Select(f => new FaiteJson(Iso(f.Date), f.Titre, f.Par))],
                [.. l.Precedentes.Select(e => new PrecedenteJson(Iso(e.Date), e.Sujet, e.PremiereLigne))]);
        }

        public MatiereDeLettre VersMatiere()
        {
            var taches = (TachesDues ?? []).ToList();
            var edition = new MatiereDEdition(
                DeIso(Date),
                Enum.Parse<RangEdition>(Rang),
                Plancher is { } p ? new PlancherDuJour(Enum.Parse<RaisonDePlancher>(p.Raison), p.Titre) : null,
                [.. taches.Select(t => new TachePourEdition(
                    t.Titre, t.JoursDeRetard, t.EcheanceFerme, t.Assigne, t.Zone, t.Equipement))],
                ProchainCompteARebours is { } c ? new CompteProcheDEdition(c.Titre, c.Dodos) : null,
                MeteoDuJour is { } me ? new MeteoDEdition(me.Description, me.MinC, me.MaxC) : null,
                [.. (FondsDeTiroir ?? []).Select(f => new FaitPourEdition(
                    f.Cle, f.Famille, f.Etiquette, f.Valeur, f.Texte, f.Publie))],
                [],
                Lieu);
            var series = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var t in taches.Where(t => t.Serie > 0))
            {
                series[t.Titre] = t.Serie;
            }
            return new MatiereDeLettre(
                edition,
                [.. (SemaineDevant ?? []).Select(e => new EcheanceDevant(DeIso(e.Date), e.Titre, e.Assigne, e.EcheanceFerme))],
                [.. (FaitesDepuisLaDerniere ?? []).Select(f => new TacheFaite(DeIso(f.Date), f.Titre, f.Par))],
                series,
                [.. (Precedentes ?? []).Select(e => new LettrePrecedente(DeIso(e.Date), e.Sujet, e.PremiereLigne))]);
        }
    }

    private sealed record PlancherJson(string Raison, string Titre);
    private sealed record TacheJson(
        string Titre, int JoursDeRetard, bool EcheanceFerme, string? Assigne, string? Zone, string? Equipement, int Serie);
    private sealed record CompteJson(string Titre, int Dodos);
    private sealed record MeteoJson(string Description, double MinC, double MaxC);
    private sealed record FaitJson(string Cle, string Famille, string Etiquette, string Valeur, string Texte, bool Publie);
    private sealed record EcheanceJson(string Date, string JourDeSemaine, string Titre, string? Assigne, bool EcheanceFerme);
    private sealed record FaiteJson(string Date, string Titre, string? Par);
    private sealed record PrecedenteJson(string Date, string Sujet, string PremiereLigne);

    /// <summary>Parse défensif et validation stricte, sur le patron de l'édition :
    /// clôtures de code et prose tolérées autour du JSON, puis tout écart rend null.
    /// La matière sert à une chose : un point d'exclamation qu'un titre porte déjà
    /// (« Boites! », vu en prod le 2026-09-21) n'est pas un écart quand le modèle cite le
    /// titre tel quel, comme le prompt le lui demande.</summary>
    public static TexteDeLettre? Extraire(string texte, MatiereDeLettre matiere) => Extraire(texte, matiere, out _);

    public static TexteDeLettre? Extraire(string texte, MatiereDeLettre matiere, out string? ecart)
    {
        var titresAvecExclamation = TitresAvecExclamation(matiere);
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
                    var resultat = Valider(document.RootElement, titresAvecExclamation, out var refus);
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

    /// <summary>Tout ce que la matière nomme et qui porte un « ! » : titres de tâches
    /// dues, à venir ou faites, plancher, compte à rebours, textes du fonds.</summary>
    internal static IReadOnlyList<string> TitresAvecExclamation(MatiereDeLettre matiere)
    {
        var e = matiere.Edition;
        IEnumerable<string?> candidats =
        [
            .. e.TachesDues.Select(t => t.Titre),
            .. matiere.SemaineDevant.Select(t => t.Titre),
            .. matiere.FaitesDepuisLaDerniere.Select(t => t.Titre),
            .. e.Faits.Select(f => f.Texte),
            e.Plancher?.Titre,
            e.ProchainCompte?.Titre,
        ];
        return [.. candidats.Where(c => c is not null && c.Contains('!')).Select(c => c!).Distinct()];
    }

    private static string SansLesTitres(string texte, IReadOnlyList<string> titres)
    {
        foreach (var t in titres)
        {
            texte = texte.Replace(t, "", StringComparison.Ordinal);
        }
        return texte;
    }

    private static TexteDeLettre? Valider(JsonElement racine, IReadOnlyList<string> titresAvecExclamation, out string ecart)
    {
        var sujet = Texte(racine, "sujet", 1, LongueurMaxSujet, out ecart);
        if (sujet is null)
        {
            ecart = $"sujet : {ecart}";
            return null;
        }
        if (sujet.EndsWith('.'))
        {
            ecart = "sujet : point final";
            return null;
        }
        if (SansLesTitres(sujet, titresAvecExclamation).Contains('!'))
        {
            ecart = "sujet : point d'exclamation";
            return null;
        }

        if (racine.TryGetProperty("paragraphes", out var paragraphesJson) == false
            || paragraphesJson.ValueKind != JsonValueKind.Array
            || paragraphesJson.GetArrayLength() < MinParagraphes
            || paragraphesJson.GetArrayLength() > MaxParagraphes)
        {
            ecart = $"paragraphes : il en faut de {MinParagraphes} à {MaxParagraphes}";
            return null;
        }
        var paragraphes = new List<string>();
        foreach (var p in paragraphesJson.EnumerateArray())
        {
            var min = paragraphes.Count == 0 ? LongueurMinPremierParagraphe : LongueurMinParagraphe;
            var paragraphe = Texte(p, min, LongueurMaxParagraphe, out ecart);
            if (paragraphe is null)
            {
                ecart = $"paragraphe {paragraphes.Count + 1} : {ecart}";
                return null;
            }
            var refus = RefusPropreALaLettre(paragraphe, titresAvecExclamation, premier: paragraphes.Count == 0);
            if (refus is not null)
            {
                ecart = $"paragraphe {paragraphes.Count + 1} : {refus}";
                return null;
            }
            paragraphes.Add(paragraphe);
        }
        var total = paragraphes.Sum(p => p.Length);
        if (total > LongueurMaxTotale)
        {
            ecart = $"total : {total} signes, {LongueurMaxTotale} au plus";
            return null;
        }
        if (Adieu().IsMatch(paragraphes[^1]))
        {
            ecart = "dernier paragraphe : « Bonne journée » est au gabarit";
            return null;
        }

        ecart = "";
        return new TexteDeLettre(sujet, paragraphes);
    }

    /// <summary>Les trois refus propres à la lettre : la salutation ou la signature
    /// redonnées par réflexe, une liste déguisée, le point d'exclamation.</summary>
    private static string? RefusPropreALaLettre(string paragraphe, IReadOnlyList<string> titresAvecExclamation, bool premier)
    {
        if (SansLesTitres(paragraphe, titresAvecExclamation).Contains('!'))
        {
            return "point d'exclamation";
        }
        if (premier && Salutation().IsMatch(paragraphe))
        {
            return "salutation, elle est au gabarit";
        }
        if (Signature().IsMatch(paragraphe))
        {
            return "signature, elle est au gabarit";
        }
        if (ListeDeguisee().IsMatch(paragraphe))
        {
            return "liste déguisée";
        }
        return null;
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
            ecart = texte.Length == 0 ? "vide" : $"{texte.Length} signes, {min} au moins";
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

    [GeneratedRegex(@"^(bonjour|salut|allô|allo)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Salutation();

    [GeneratedRegex(@"[—–-]\s*la maison\s*\.?$", RegexOptions.IgnoreCase)]
    private static partial Regex Signature();

    [GeneratedRegex(@"bonne journée\s*[.!]?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Adieu();

    [GeneratedRegex(@"^([-•*#]|\d+\.)\s")]
    private static partial Regex ListeDeguisee();
}
