using static HouseOs.Api.Features.FondsDeTiroir.Mots;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La famille « la maison » : ce que le foyer sait de lui-même, tiré du journal de
/// complétion, des équipements et des zones (vault : Fonds De Tiroir). Aucune table
/// neuve, aucune migration — que des lectures.
///
/// <para><b>C'est la famille la plus muette la première année.</b> Sans historique il
/// n'y a ni série, ni record, ni « ce jour-là l'an dernier » : les faits ne sortent
/// pas, et le journal doit tenir sans eux jusqu'à ce que la maison se soit constitué
/// une année de mémoire. C'est un argument pour la bâtir tôt, pas tard — le jour où le
/// journal commencera à parler, le code sera déjà là.</para>
///
/// <para><b>La rareté se compte en jours, pas en envies.</b> Elle dit « combien de
/// jours par année le fait <i>peut</i> paraître », et rien d'autre. Plusieurs faits de
/// cette famille sont vrais <b>tous les jours</b> une fois leur condition remplie — le
/// doyen a toujours seize ans, la pièce est toujours négligée, le coût de l'année est
/// toujours là. Les coter « quelques fois par an » les mettrait en tête du journal tous
/// les matins de l'année, puisque la fraîcheur se remet à neuf au bout de sept jours :
/// exactement le radotage que le score existe pour empêcher. Ils valent donc
/// <see cref="Rarete.Quotidien"/>, et c'est la <b>pertinence</b> qui dit ce qu'ils
/// changent à aujourd'hui, sur l'échelle du fonds : 1 = de la décoration,
/// 1,5 = ça éclaire la journée, 2 = ça suggère un geste, 3 = ça engage la journée.</para>
///
/// <para>Règle de forme, apprise au rendu de l'étape 2 : <b>l'étiquette porte le sujet,
/// la valeur porte le chiffre</b>, et le texte long doit <b>ajouter</b> quelque chose.
/// Ici s'y ajoute une règle propre à cette famille : les noms saisis par le foyer
/// (titres de tâches, noms de zones et d'équipements) vivent dans le <b>texte long</b>,
/// jamais dans l'étiquette ni dans la valeur. Une pièce nommée « Aucune » ferait
/// autrement mentir l'invariant « le texte ne redit jamais l'étiquette ».</para>
/// </summary>
public static class FaitsDeLaMaison
{
    /// <summary>En deçà, une série n'est pas une série : c'est deux jours de suite.</summary>
    private const int SerieMinimale = 3;

    /// <summary>En deçà, un « record » n'en est pas un : c'est une bonne lancée.</summary>
    private const int RecordMinimal = 5;

    /// <summary>En deçà, « N séances depuis le… » n'a pas encore d'histoire à raconter.</summary>
    private const int SeancesMinimales = 10;

    /// <summary>Avant deux mois sans rien, une pièce n'est pas négligée, elle est tranquille.</summary>
    private const int JoursDeNegligence = 60;

    public static IEnumerable<FaitDeTiroir> Produire(ContexteDuJour contexte)
    {
        // Pas de journal de complétion, pas de maison : la famille entière se tait,
        // exactement comme le ciel sans coordonnées.
        if (contexte.Maison is not { } maison)
        {
            return [];
        }

        var serie = SerieEnCours(maison.JoursActifs, contexte.Date);
        var record = MeilleureSerie(maison.JoursActifs);

        var faits = new List<FaitDeTiroir?>
        {
            Record(serie, record),
            SerieDuJour(serie, record),
            Seances(maison),
            Doyen(maison, contexte.Date),
            PieceOubliee(maison, contexte.Date),
            CoutDeLAnnee(maison),
            Anniversaire(maison, contexte.Date),
            LAnDernier(maison),
        };
        return faits.OfType<FaitDeTiroir>();
    }

    /// <summary>
    /// Les journées d'affilée où quelque chose a été coché. La série se termine
    /// aujourd'hui si la journée a déjà donné quelque chose, et hier sinon : une série
    /// ne se casse qu'à la fin de la journée, pas à son premier café.
    /// </summary>
    public static int SerieEnCours(IReadOnlyList<DateOnly> joursActifs, DateOnly aujourdhui)
    {
        var actifs = joursActifs.ToHashSet();
        var jour = actifs.Contains(aujourdhui) ? aujourdhui : aujourdhui.AddDays(-1);
        var serie = 0;
        while (actifs.Contains(jour))
        {
            serie++;
            jour = jour.AddDays(-1);
        }
        return serie;
    }

    /// <summary>La plus longue série de toute l'histoire du journal, celle d'aujourd'hui comprise.</summary>
    public static int MeilleureSerie(IReadOnlyList<DateOnly> joursActifs)
    {
        var meilleure = 0;
        var courante = 0;
        DateOnly? precedent = null;
        foreach (var jour in joursActifs.Distinct().Order())
        {
            courante = precedent is { } veille && veille.AddDays(1) == jour ? courante + 1 : 1;
            precedent = jour;
            meilleure = Math.Max(meilleure, courante);
        }
        return meilleure;
    }

    private static FaitDeTiroir? Record(int serie, int record)
    {
        // « Égaler » suffit : le jour où la maison rejoint son record est celui où elle
        // peut encore le battre, et c'est ce jour-là qu'il faut le dire.
        if (serie < RecordMinimal || serie < record)
        {
            return null;
        }
        return new FaitDeTiroir(
            "maison.record",
            FamilleDeFait.Maison,
            "Un record de la maison",
            $"{Jours(serie)} d'affilée",
            "Jamais elle n'avait tenu aussi longtemps sans une journée blanche.",
            new ScoreDeFait(Rarete.ParAn(6), 1, Pertinence.SuggereUnGeste));
    }

    private static FaitDeTiroir? SerieDuJour(int serie, int record)
    {
        if (serie < SerieMinimale)
        {
            return null;
        }
        // Se taire quand la série est le record, oui — mais seulement quand le fait du
        // dessus parle vraiment, c'est-à-dire à partir de cinq jours. Sans cette
        // seconde condition, une maison qui coche trois jours d'affilée pour la
        // première fois n'a personne pour le dire : la série se tait parce qu'elle est
        // le record, et le record se tait parce qu'il est trop court. C'était
        // exactement le premier moment que ce fait existe pour raconter.
        if (serie >= record && serie >= RecordMinimal)
        {
            return null;
        }
        return new FaitDeTiroir(
            "maison.serie",
            FamilleDeFait.Maison,
            "La série en cours",
            $"{Jours(serie)} d'affilée",
            // Citer « le record est de trois jours » quand la série EST ces trois jours
            // serait redire le chiffre du dessus au lieu d'ajouter quelque chose.
            serie >= record
                ? "C'est ce que la maison a fait de mieux jusqu'ici."
                : $"Le record de la maison est de {Jours(record)}.",
            new ScoreDeFait(Rarete.Quotidien, 1, Pertinence.EclaireLaJournee));
    }

    private static FaitDeTiroir? Seances(EtatDeLaMaison maison)
    {
        var tete = maison.Seances
            .Where(s => s.Nombre >= SeancesMinimales)
            .OrderByDescending(s => s.Nombre)
            .ThenBy(s => s.Titre, StringComparer.Ordinal)
            .FirstOrDefault();
        if (tete is null)
        {
            return null;
        }

        // Le compte est vrai tous les jours, donc quotidien. Ce qui change, c'est le
        // jour où il franchit une dizaine : c'est le chiffre qu'on retient (« ça fait
        // 30 fois »), et ce jour-là le fait mérite la place.
        var rond = tete.Nombre % 10 == 0;
        return new FaitDeTiroir(
            "maison.seances",
            FamilleDeFait.Maison,
            $"Depuis le {DateLongue(tete.Premiere)}",
            $"{tete.Nombre} séances",
            $"Toutes pour la même tâche, {TitreCourt(tete.Titre)}.",
            new ScoreDeFait(Rarete.Quotidien, 1, rond ? Pertinence.EngageLaJournee : Pertinence.EclaireLaJournee));
    }

    private static FaitDeTiroir? Doyen(EtatDeLaMaison maison, DateOnly aujourdhui)
    {
        var doyen = maison.Equipements
            .Where(e => e.DateAchat is not null)
            .OrderBy(e => e.DateAchat!.Value)
            .ThenBy(e => e.Nom, StringComparer.Ordinal)
            .FirstOrDefault();
        if (doyen?.DateAchat is not { } achat)
        {
            return null;
        }
        var ans = Annees(achat, aujourdhui);
        // Une maison neuve n'a pas de doyen : tout y a moins d'un an, et « 0 an » ne
        // se dit pas.
        if (ans < 1)
        {
            return null;
        }

        var entretien = doyen.ProchainEntretien;
        var texte = entretien is { } prochain
            ? $"{TitreCourt(doyen.Nom)}, dont le prochain entretien est le {DateLongue(prochain)}."
            : $"{TitreCourt(doyen.Nom)}, entré dans la maison en {achat.Year}.";
        // Le doyen ne change pas d'âge d'un jour à l'autre : il est vrai tous les
        // jours. Ce qui engage la journée, c'est son entretien qui approche.
        var pertinence = entretien is { } date && date.DayNumber - aujourdhui.DayNumber <= 14
            ? Pertinence.EngageLaJournee
            : Pertinence.EclaireLaJournee;

        return new FaitDeTiroir(
            "maison.doyen",
            FamilleDeFait.Maison,
            "Le doyen de la maison",
            $"{ans} an{Marque(ans)}",
            texte,
            new ScoreDeFait(Rarete.Quotidien, 1, pertinence));
    }

    private static FaitDeTiroir? PieceOubliee(EtatDeLaMaison maison, DateOnly aujourdhui)
    {
        // Une zone sans tâche n'est pas négligée, elle est vide — et le journal n'a
        // rien à en dire. C'est la distinction que le compte de tâches sert à faire.
        var candidates = maison.Zones.Where(z => z.Taches > 0).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var oubliee = candidates
            .OrderBy(z => z.DerniereCompletion ?? DateOnly.MinValue)
            .ThenBy(z => z.Nom, StringComparer.Ordinal)
            .First();

        if (oubliee.DerniereCompletion is not { } derniere)
        {
            return new FaitDeTiroir(
                "maison.piece-oubliee",
                FamilleDeFait.Maison,
                "La pièce oubliée",
                "Jamais rien",
                $"{TitreCourt(oubliee.Nom)} : aucune de ses tâches n'a encore été cochée.",
                new ScoreDeFait(Rarete.Quotidien, 1, Pertinence.SuggereUnGeste));
        }

        var jours = aujourdhui.DayNumber - derniere.DayNumber;
        if (jours < JoursDeNegligence)
        {
            return null;
        }
        // Une pièce négligée le reste tant que personne n'y va : le fait est vrai
        // tous les jours, et c'est l'ampleur de l'oubli qui décide de sa place.
        return new FaitDeTiroir(
            "maison.piece-oubliee",
            FamilleDeFait.Maison,
            "La pièce oubliée",
            Jours(jours),
            $"{TitreCourt(oubliee.Nom)} : rien de coché depuis le {DateLongue(derniere)}.",
            new ScoreDeFait(
                Rarete.Quotidien, 1, jours >= 180 ? Pertinence.EngageLaJournee : Pertinence.SuggereUnGeste));
    }

    private static FaitDeTiroir? CoutDeLAnnee(EtatDeLaMaison maison)
    {
        if (maison.CoutDeLAnnee <= 0 || maison.InterventionsDeLAnnee == 0)
        {
            return null;
        }
        return new FaitDeTiroir(
            "maison.cout",
            FamilleDeFait.Maison,
            "Depuis le 1er janvier",
            Montant(maison.CoutDeLAnnee),
            $"Réparti sur {maison.InterventionsDeLAnnee} intervention{Marque(maison.InterventionsDeLAnnee)} consignée{Marque(maison.InterventionsDeLAnnee)}.",
            new ScoreDeFait(Rarete.Quotidien, 1, Pertinence.EclaireLaJournee));
    }

    private static FaitDeTiroir? Anniversaire(EtatDeLaMaison maison, DateOnly aujourdhui)
    {
        var anniversaire = maison.Anniversaires
            .Where(a => a.Depuis.Month == aujourdhui.Month && a.Depuis.Day == aujourdhui.Day)
            .Select(a => (a.Quoi, a.Depuis, Ans: Annees(a.Depuis, aujourdhui)))
            .Where(a => a.Ans >= 1)
            .OrderByDescending(a => a.Ans)
            .ThenBy(a => a.Quoi, StringComparer.Ordinal)
            .FirstOrDefault();
        if (anniversaire.Quoi is null)
        {
            return null;
        }

        return new FaitDeTiroir(
            "maison.anniversaire",
            FamilleDeFait.Maison,
            "Un anniversaire",
            $"{anniversaire.Ans} an{Marque(anniversaire.Ans)} aujourd'hui",
            $"{TitreCourt(anniversaire.Quoi)}, depuis {anniversaire.Depuis.Year}.",
            new ScoreDeFait(Rarete.ParAn(12), 1, Pertinence.SuggereUnGeste));
    }

    private static FaitDeTiroir? LAnDernier(EtatDeLaMaison maison)
    {
        // Vide pendant toute la première année : c'est l'état normal d'une maison qui
        // n'a pas encore un an de journal, pas une panne.
        if (maison.FaitLAnDernier.Count == 0)
        {
            return null;
        }
        var nombre = maison.FaitLAnDernier.Count;
        var premier = TitreCourt(maison.FaitLAnDernier[0]);

        return new FaitDeTiroir(
            "maison.an-dernier",
            FamilleDeFait.Maison,
            "Ce jour-là, l'an dernier",
            nombre == 1 ? "Une chose réglée" : $"{nombre} choses réglées",
            nombre == 1 ? $"C'était {premier}." : $"Dont {premier}, entre autres.",
            // Il ne peut sortir que les jours qui avaient eux-mêmes donné quelque chose
            // un an plus tôt — soit, dans une maison tenue, environ un jour sur deux.
            new ScoreDeFait(Rarete.ParAn(180), 1, Pertinence.EclaireLaJournee));
    }

    /// <summary>L'âge en années révolues : un 29 février compte au 1er mars les autres ans.</summary>
    private static int Annees(DateOnly depuis, DateOnly aujourdhui)
    {
        var ans = aujourdhui.Year - depuis.Year;
        if (depuis.AddYears(ans) > aujourdhui)
        {
            ans--;
        }
        return ans;
    }
}
