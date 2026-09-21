using HouseOs.Api.Domaine.Meteo;
using static HouseOs.Api.Features.FondsDeTiroir.Mots;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La famille « le climat » : ce que dix ans d'archive disent de la saison qui vient —
/// le premier gel, la première neige, la dernière journée à vingt degrés, le mois le
/// plus sec et le plus arrosé — et ce qu'il a fait ici à pareille date l'an dernier
/// (vault : Fonds De Tiroir).
///
/// <para><b>Une normale n'est pas une prévision.</b> ERA5 est une grille d'environ neuf
/// kilomètres et dix saisons ne font pas une certitude : le journal écrit « vers le
/// 8 octobre » et dit à combien de jours près, jamais une date sèche
/// (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).</para>
///
/// <para><b>Une date normale ne parle qu'en approchant.</b> Annoncer le premier gel en
/// juin, c'est de la décoration ; l'annoncer fin septembre, c'est une consigne — on
/// rentre le boyau d'arrosage. D'où la fenêtre : un mois avant, une semaine après, et
/// c'est ce compte de jours, et rien d'autre, qui fait la rareté du fait.</para>
/// </summary>
public static class FaitsDuClimat
{
    /// <summary>Un mois avant : au-delà, la date ne change rien à aujourd'hui.</summary>
    private const int JoursAvant = 30;

    /// <summary>
    /// La fin de la douceur s'annonce plus court que le gel : trois semaines avant, la
    /// dernière journée à vingt degrés n'est encore qu'une statistique.
    /// </summary>
    private const int JoursAvantLaDouceur = 21;

    /// <summary>
    /// Une semaine après la date normale, le fait continue de sortir : la normale n'a
    /// pas d'heure, et le gel qui se fait attendre est encore la nouvelle du jour.
    /// </summary>
    private const int JoursApres = 7;

    /// <summary>En deçà, la date n'est plus une statistique mais la semaine qui vient.</summary>
    private const int JoursDeLaSemaine = 7;

    /// <summary>Les deux mois extrêmes de l'année, en jours de parution possible.</summary>
    private const int JoursDesMoisExtremes = 61;

    /// <summary>
    /// Au-delà, l'écart avec l'an dernier se sent en sortant : c'est ce qui fait
    /// passer le fait de la décoration à quelque chose qui éclaire la journée.
    /// </summary>
    private const int EcartQuiSeSentC = 8;

    public static IEnumerable<FaitDeTiroir> Produire(ContexteDuJour contexte)
    {
        // Pas de normales, pas de climat : la famille entière se tait, exactement
        // comme le ciel sans coordonnées. Une installation neuve, un réseau coupé au
        // premier démarrage — l'édition sort quand même.
        if (contexte.Climat is not { } climat)
        {
            return [];
        }

        var faits = new List<FaitDeTiroir?>
        {
            Annonce(
                "climat.gel", "Le premier gel", climat.PremierGel, contexte.Date, JoursAvant,
                Pertinence.SuggereUnGeste, Pertinence.EngageLaJournee,
                // « La gelée au sol », et non « la nuit sous zéro » : c'est ce que le
                // calcul cherche, et c'est ce qui compte pour le jardin.
                ecart => $"La gelée au sol, à {Jours(ecart)} près sur {Saisons(climat)}."),
            Annonce(
                "climat.neige", "La première neige", climat.PremiereNeige, contexte.Date, JoursAvant,
                Pertinence.SuggereUnGeste, Pertinence.EngageLaJournee,
                ecart => $"Un centimètre au sol, à {Jours(ecart)} près sur {Saisons(climat)}."),
            Annonce(
                "climat.douceur", "La dernière douceur", climat.DerniereDouceur, contexte.Date,
                JoursAvantLaDouceur, Pertinence.EclaireLaJournee, Pertinence.SuggereUnGeste,
                ecart => $"Les derniers vingt degrés de l'année, à {Jours(ecart)} près."),
            MoisMarquant(climat, contexte.Date),
            AnDernier(climat),
        };
        return faits.OfType<FaitDeTiroir>();
    }

    private static string Saisons(EtatDuClimat climat) =>
        $"{climat.SaisonsObservees} saison{Marque(climat.SaisonsObservees)}";

    /// <summary>
    /// Une date normale qui approche. Le fait ne sort que dans sa fenêtre, et la
    /// fenêtre <b>est</b> sa rareté : « combien de jours par année il peut paraître »
    /// n'est pas une envie, c'est un compte (vault : Fonds De Tiroir).
    /// </summary>
    private static FaitDeTiroir? Annonce(
        string cle, string etiquette, DateNormale? normale, DateOnly aujourdhui, int joursAvant,
        double pertinence, double pertinenceProche, Func<int, string> texte)
    {
        if (normale is null)
        {
            return null;
        }

        var date = ProchaineDate(normale, aujourdhui);
        var jours = date.DayNumber - aujourdhui.DayNumber;
        if (jours > joursAvant)
        {
            return null;
        }

        return new FaitDeTiroir(
            cle,
            FamilleDeFait.Climat,
            etiquette,
            $"Vers le {DateLongue(date)}",
            texte(normale.EcartJours),
            new ScoreDeFait(
                Rarete.ParAn(joursAvant + JoursApres + 1),
                1,
                jours <= JoursDeLaSemaine ? pertinenceProche : pertinence));
    }

    /// <summary>
    /// La date normale la plus proche <b>devant</b> aujourd'hui, ou celle qui vient de
    /// passer si elle a moins d'une semaine. Sans ce rattrapage, un premier gel normal
    /// le 8 octobre disparaîtrait du journal le 9 — le jour même où tout le monde
    /// remarque qu'il n'a pas encore gelé.
    ///
    /// <para>Les trois années sont regardées, et <b>l'an dernier en premier</b> : une
    /// normale au 28 décembre, lue le 2 janvier, vient de passer depuis cinq jours —
    /// en plein dans le rattrapage — mais son occurrence de l'année courante est à
    /// presque douze mois. Ne regarder que l'année courante et la suivante faisait
    /// disparaître le fait précisément dans la fenêtre pour laquelle le rattrapage
    /// existe. Trouvé en revue de code, étape 5.</para>
    /// </summary>
    private static DateOnly ProchaineDate(DateNormale normale, DateOnly aujourdhui)
    {
        var plancher = aujourdhui.AddDays(-JoursApres);
        foreach (var annee in (int[])[aujourdhui.Year - 1, aujourdhui.Year, aujourdhui.Year + 1])
        {
            var date = DansLAnnee(normale, annee);
            if (date >= plancher)
            {
                return date;
            }
        }
        return DansLAnnee(normale, aujourdhui.Year + 1);
    }

    private static DateOnly DansLAnnee(DateNormale normale, int annee) =>
        new(annee, normale.Mois, Math.Min(normale.Jour, DateTime.DaysInMonth(annee, normale.Mois)));

    /// <summary>
    /// Le mois le plus sec ou le plus arrosé de l'année — et seulement quand on y est.
    /// La sécheresse de février ne dit rien un 20 septembre ; elle dit quelque chose le
    /// 3 février.
    /// </summary>
    private static FaitDeTiroir? MoisMarquant(EtatDuClimat climat, DateOnly aujourdhui)
    {
        if (climat.MoisLePlusSec is not { } sec || climat.MoisLePlusPluvieux is not { } pluvieux)
        {
            return null;
        }

        (string Etiquette, MoisNormal Ici, MoisNormal Autre, string Extreme, string Verbe)? choix =
            aujourdhui.Month switch
            {
                var mois when mois == pluvieux.Mois =>
                    ("Le mois le plus pluvieux", pluvieux, sec, "Le plus sec", "n'en reçoit que"),
                var mois when mois == sec.Mois =>
                    ("Le mois le plus sec", sec, pluvieux, "Le plus arrosé", "en compte"),
                _ => null,
            };
        if (choix is not { } mesure)
        {
            return null;
        }

        return new FaitDeTiroir(
            "climat.mois",
            FamilleDeFait.Climat,
            mesure.Etiquette,
            $"{Math.Round(mesure.Ici.PrecipitationMm)} mm en moyenne",
            $"{mesure.Extreme}, {NomDuMois(mesure.Autre.Mois)}, {mesure.Verbe} "
            + $"{Math.Round(mesure.Autre.PrecipitationMm)} mm.",
            new ScoreDeFait(
                Rarete.ParAn(JoursDesMoisExtremes),
                1,
                // Le premier de mois est le jour où le dire ; le 27, c'est de la décoration.
                aujourdhui.Day <= JoursDeLaSemaine ? Pertinence.EclaireLaJournee : Pertinence.Decoration));
    }

    /// <summary>
    /// Ce qu'il a fait ici, à pareille date, l'an dernier. La seule mesure du fonds qui
    /// vienne d'une observation plutôt que d'un calcul — et la seule chose que les
    /// tables de prévisions, remplacées à chaque heure, ne sauront jamais dire.
    /// </summary>
    private static FaitDeTiroir? AnDernier(EtatDuClimat climat)
    {
        if (climat.AnDernier is not { } journee)
        {
            return null;
        }

        var max = (int)Math.Round(journee.MaxC);
        var ecart = climat.MaxDAujourdhuiC is { } prevu ? (int)Math.Round(prevu) - max : (int?)null;

        var texte = ecart switch
        {
            null => $"La nuit était descendue à {Math.Round(journee.MinC)} °C.",
            0 => "Le mercure doit faire pareil aujourd'hui.",
            > 0 => $"{Degres(ecart.Value)} de moins qu'aujourd'hui, à pareille date.",
            _ => $"{Degres(-ecart.Value)} de plus qu'aujourd'hui, à pareille date.",
        };

        return new FaitDeTiroir(
            "climat.an-dernier",
            FamilleDeFait.Climat,
            "Ce jour-là l'an dernier",
            $"Il a fait {max} °C",
            texte,
            new ScoreDeFait(
                Rarete.Quotidien,
                1,
                ecart is { } e && Math.Abs(e) >= EcartQuiSeSentC
                    ? Pertinence.EclaireLaJournee
                    : Pertinence.Decoration));
    }

    private static string Degres(int nombre) => $"{nombre} degré{Marque(nombre)}";

    private static string NomDuMois(int mois) =>
        new DateOnly(2001, mois, 1).ToString("MMMM", Francais);
}
