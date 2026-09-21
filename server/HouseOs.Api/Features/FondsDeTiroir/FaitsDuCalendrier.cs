using static HouseOs.Api.Features.FondsDeTiroir.Mots;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La famille « le calendrier » : ce que le foyer a devant lui — un compte à rebours,
/// des échéances qui s'approchent, une saison de travaux qui s'ouvre ou se ferme, un
/// papier qui expire (vault : Fonds De Tiroir). Que des lectures de tables existantes.
///
/// <para><b>La rareté se compte en jours.</b> Un compte à rebours et un « ça s'en
/// vient » sont vrais tous les jours : ils valent <see cref="Rarete.Quotidien"/>, et
/// c'est la pertinence qui dit ce qu'ils changent à aujourd'hui (1 = de la décoration,
/// 1,5 = ça éclaire la journée, 2 = ça suggère un geste, 3 = ça engage la journée).
/// Les fenêtres saisonnières et les papiers qui expirent, eux, ont une vraie fenêtre de
/// parution, et leur rareté la reprend telle quelle.</para>
///
/// <para>Le fonds de tiroir <b>ne sait pas</b> que le journal dessine déjà un encadré
/// de compte à rebours : il produit le fait, et c'est au consommateur de décider s'il
/// le montre deux fois (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal). La
/// lettre du matin, elle, n'aura pas d'encadré et voudra le fait.</para>
/// </summary>
public static class FaitsDuCalendrier
{
    /// <summary>Au-delà, un compte à rebours est une idée, pas une échéance.</summary>
    private const int JoursDeCompteARebours = 120;

    /// <summary>« Ça s'en vient » commence où finit la liste du jour, et tient un mois.</summary>
    private const int DebutDeLHorizon = 7;
    private const int FinDeLHorizon = 30;

    /// <summary>Une fenêtre qui vient de s'ouvrir se dit une semaine, puis se tait.</summary>
    private const int JoursDOuverture = 7;

    /// <summary>Une fenêtre qui se referme se dit deux semaines d'avance.</summary>
    private const int JoursDeFermeture = 14;

    /// <summary>Un papier qui expire dans plus de deux mois n'est pas une nouvelle.
    /// C'est aussi la rareté du fait : il peut paraître soixante jours par papier.</summary>
    private const int JoursDExpiration = 60;

    public static IEnumerable<FaitDeTiroir> Produire(ContexteDuJour contexte)
    {
        if (contexte.Calendrier is not { } calendrier)
        {
            return [];
        }

        var faits = new List<FaitDeTiroir?>
        {
            CompteARebours(calendrier, contexte.Date),
            CaSEnVient(calendrier, contexte.Date),
            TravauxDeLaSaison(calendrier, contexte.Date),
            Expiration(calendrier, contexte.Date),
        };
        return faits.OfType<FaitDeTiroir>();
    }

    private static FaitDeTiroir? CompteARebours(EtatDuCalendrier calendrier, DateOnly aujourdhui)
    {
        if (calendrier.ProchainCompte is not { } compte)
        {
            return null;
        }
        var jours = compte.DateCible.DayNumber - aujourdhui.DayNumber;
        if (jours < 0 || jours > JoursDeCompteARebours)
        {
            return null;
        }

        // C'est le seul fait de la famille dont l'étiquette est du texte saisi par le
        // foyer : le titre du compte à rebours EST le sujet, et le reléguer au texte
        // long donnerait « DANS 16 JOURS » sans dire de quoi.
        var valeur = jours switch
        {
            0 => "C'est aujourd'hui",
            1 => "C'est demain",
            _ => $"Dans {Jours(jours)}",
        };
        return new FaitDeTiroir(
            "calendrier.compte-a-rebours",
            FamilleDeFait.Calendrier,
            compte.Titre,
            valeur,
            // Sans temps de verbe : le fait se dit aussi bien la veille que le jour même.
            $"Au calendrier, le {DateAvecAnnee(compte.DateCible)}.",
            new ScoreDeFait(
                Rarete.Quotidien, 1, jours <= 7 ? Pertinence.EngageLaJournee : Pertinence.EclaireLaJournee));
    }

    private static FaitDeTiroir? CaSEnVient(EtatDuCalendrier calendrier, DateOnly aujourdhui)
    {
        // La fenêtre commence à sept jours : en deçà, la liste du jour et l'encadré
        // les montrent déjà, et le journal se répéterait.
        var horizon = calendrier.CaSEnVient
            .Where(e => e.Echeance.DayNumber - aujourdhui.DayNumber >= DebutDeLHorizon
                        && e.Echeance.DayNumber - aujourdhui.DayNumber <= FinDeLHorizon)
            .OrderBy(e => e.Echeance)
            .ThenBy(e => e.Titre, StringComparer.Ordinal)
            .ToList();
        // Une seule échéance dans le mois, ce n'est pas « ça s'en vient » : c'est une
        // tâche, et elle sortira d'elle-même le jour venu.
        if (horizon.Count < 2)
        {
            return null;
        }

        var premiere = horizon[0];
        return new FaitDeTiroir(
            "calendrier.ca-s-en-vient",
            FamilleDeFait.Calendrier,
            "Ça s'en vient",
            $"{horizon.Count} échéances",
            $"La première le {DateLongue(premiere.Echeance)} : {TitreCourt(premiere.Titre)}.",
            new ScoreDeFait(Rarete.Quotidien, 1, Pertinence.EclaireLaJournee));
    }

    private static FaitDeTiroir? TravauxDeLaSaison(EtatDuCalendrier calendrier, DateOnly aujourdhui)
    {
        // Une fenêtre qui se referme passe avant une qui s'ouvre : « il reste neuf
        // jours » engage la journée, « c'est la saison » est une invitation.
        var fermeture = calendrier.Saisons
            .Where(s => s.Ouverture <= aujourdhui && aujourdhui <= s.Fermeture
                        && s.Fermeture.DayNumber - aujourdhui.DayNumber <= JoursDeFermeture)
            .OrderBy(s => s.Fermeture)
            .ThenBy(s => s.Titre, StringComparer.Ordinal)
            .FirstOrDefault();
        if (fermeture is not null)
        {
            var reste = fermeture.Fermeture.DayNumber - aujourdhui.DayNumber;
            return new FaitDeTiroir(
                "calendrier.saison",
                FamilleDeFait.Calendrier,
                "La saison se ferme",
                reste == 0 ? "C'est le dernier jour" : $"Il reste {Jours(reste)}",
                $"Après, {TitreCourt(fermeture.Titre)} attendra l'an prochain.",
                new ScoreDeFait(Rarete.ParAn(4 * JoursDeFermeture), 1, Pertinence.SuggereUnGeste));
        }

        var ouverture = calendrier.Saisons
            .Where(s => s.Ouverture <= aujourdhui && aujourdhui <= s.Fermeture
                        && aujourdhui.DayNumber - s.Ouverture.DayNumber < JoursDOuverture)
            .OrderByDescending(s => s.Ouverture)
            .ThenBy(s => s.Titre, StringComparer.Ordinal)
            .FirstOrDefault();
        if (ouverture is null)
        {
            return null;
        }
        var depuis = aujourdhui.DayNumber - ouverture.Ouverture.DayNumber;
        return new FaitDeTiroir(
            "calendrier.saison",
            FamilleDeFait.Calendrier,
            "La saison s'ouvre",
            depuis == 0 ? "C'est aujourd'hui" : $"Depuis {Jours(depuis)}",
            $"{TitreCourt(ouverture.Titre)} redevient possible jusqu'au {DateLongue(ouverture.Fermeture)}.",
            new ScoreDeFait(Rarete.ParAn(4 * JoursDOuverture), 1, Pertinence.Decoration));
    }

    private static FaitDeTiroir? Expiration(EtatDuCalendrier calendrier, DateOnly aujourdhui)
    {
        var expiration = calendrier.Expirations
            .Where(e => e.Date >= aujourdhui && e.Date.DayNumber - aujourdhui.DayNumber <= JoursDExpiration)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Quoi, StringComparer.Ordinal)
            .FirstOrDefault();
        if (expiration is null)
        {
            return null;
        }
        var jours = expiration.Date.DayNumber - aujourdhui.DayNumber;

        return new FaitDeTiroir(
            "calendrier.expiration",
            FamilleDeFait.Calendrier,
            expiration.EstUneGarantie ? "Une garantie expire" : "Un papier expire",
            jours == 0 ? "C'est aujourd'hui" : $"Dans {Jours(jours)}",
            $"{TitreCourt(expiration.Quoi)}, jusqu'au {DateAvecAnnee(expiration.Date)}.",
            new ScoreDeFait(
                Rarete.ParAn(JoursDExpiration),
                1,
                jours <= 14 ? Pertinence.EngageLaJournee : Pertinence.EclaireLaJournee));
    }
}
