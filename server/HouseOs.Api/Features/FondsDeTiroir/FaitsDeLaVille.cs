using static HouseOs.Api.Features.FondsDeTiroir.Mots;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La famille « la ville » : la prochaine collecte, celle qu'on oublie toujours, et ce
/// qui se passe en ville cette semaine (vault : Fonds De Tiroir). Rien de municipal
/// n'entre ici — pas un nom de ville, pas un sélecteur, pas un analyseur de PDF
/// (vault : D-2026-09-20 Sources Municipales Séparées Par Solidité). La matière arrive
/// par les flux externes : un ICS pour les collectes, un flux poussé pour les
/// événements.
///
/// <para><b>Un flux périmé se tait, il ne ment pas.</b> C'est la règle de la famille.
/// Un gratteur mort il y a un mois laisserait des événements vieux d'un mois avoir
/// l'air du programme de la semaine ; un calendrier de collectes qui ne se télécharge
/// plus finirait par annoncer l'an dernier. Au-delà de <see cref="FraicheurMaxJours"/>
/// jours sans remplacement, un flux ne nourrit plus le fonds — l'édition sort sans
/// widget de la ville, ce qui est exactement ce qu'on veut voir
/// (vault : D-2026-09-20 Flux Externe Poussé).</para>
/// </summary>
public static class FaitsDeLaVille
{
    /// <summary>
    /// Un flux ICS se retélécharge toutes les six heures et un flux poussé se remplit
    /// une fois par jour : sept jours de silence, des deux côtés, c'est une panne et
    /// non un creux.
    /// </summary>
    public const int FraicheurMaxJours = 7;

    /// <summary>Au-delà, la collecte n'est pas la nouvelle du jour : elle revient
    /// chaque semaine, et reviendra bien assez tôt.</summary>
    private const int JoursDeCollecte = 7;

    /// <summary>Une collecte qui n'arrive pas deux fois par an se dit deux semaines
    /// d'avance : c'est le temps de sortir ce qu'on a à sortir.</summary>
    private const int JoursDeCollecteSpeciale = 14;

    /// <summary>Ce qui se passe en ville se dit à une semaine — au-delà, c'est un
    /// programme, pas une nouvelle.</summary>
    private const int JoursDEvenement = 7;

    /// <summary>
    /// En deçà, un calendrier de collectes n'a pas assez d'habitudes pour qu'on puisse
    /// dire de l'une d'elles qu'elle sort de l'ordinaire.
    /// </summary>
    private const int CollectesPourJugerDeLOrdinaire = 4;

    public static IEnumerable<FaitDeTiroir> Produire(ContexteDuJour contexte)
    {
        // Pas de flux, pas de ville : la famille se tait, comme le ciel sans
        // coordonnées. Un foyer qui n'a branché aucun calendrier garde tout le reste
        // de son journal.
        if (contexte.Ville is not { } ville)
        {
            return [];
        }

        var toutes = Alimentees(ville.Collectes);
        var collectes = AVenir(toutes, contexte.Date);
        var speciale = ProchaineSpeciale(toutes, collectes, contexte.Date);
        var faits = new List<FaitDeTiroir?>
        {
            // Quand la prochaine collecte EST la collecte spéciale, une seule des deux
            // parle : deux widgets pour le même camion, c'est une colonne perdue. Même
            // croisement voulu que la série et le record de la maison.
            collectes.Count > 0 && collectes[0] == speciale
                ? null
                : ProchaineCollecte(collectes, contexte.Date),
            speciale is null ? null : Speciale(speciale, toutes, ville, contexte.Date),
            Evenement(AVenir(Alimentees(ville.Municipaux), contexte.Date), ville, contexte.Date),
        };
        return faits.OfType<FaitDeTiroir>();
    }

    /// <summary>Tout ce que portent les flux <b>encore alimentés</b> — la fenêtre
    /// entière, passé compris : c'est elle qui dit ce qui est ordinaire.</summary>
    private static List<EvenementDeLaVille> Alimentees(IReadOnlyList<FluxDeLaVille> flux) =>
        [.. flux.Where(f => f.Perime(FraicheurMaxJours) == false).SelectMany(f => f.Evenements)];

    /// <summary>Ce qui reste devant, dans l'ordre.</summary>
    private static List<EvenementDeLaVille> AVenir(List<EvenementDeLaVille> evenements, DateOnly aujourdhui) =>
        [.. evenements
            .Where(e => e.Date >= aujourdhui)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Heure ?? TimeOnly.MinValue)
            .ThenBy(e => e.Titre, StringComparer.Ordinal)];

    private static FaitDeTiroir? ProchaineCollecte(List<EvenementDeLaVille> collectes, DateOnly aujourdhui)
    {
        var prochaine = collectes.FirstOrDefault(e => Dans(e.Date, aujourdhui) <= JoursDeCollecte);
        if (prochaine is null)
        {
            return null;
        }
        var jours = Dans(prochaine.Date, aujourdhui);
        return new FaitDeTiroir(
            "ville.collecte",
            FamilleDeFait.Ville,
            // Étiquette fixe, valeur chiffrée : le titre du bac est saisi par la ville,
            // et il vit dans le texte (vault : Fonds De Tiroir).
            "La prochaine collecte",
            Quand(jours),
            $"{TitreCourt(prochaine.Titre)}, le {DateLongue(prochaine.Date)}.",
            new ScoreDeFait(
                Rarete.Quotidien,
                1,
                // La veille est le seul soir où le bac doit sortir ; le jour même, il
                // est déjà au bord du chemin.
                jours switch
                {
                    1 => Pertinence.EngageLaJournee,
                    0 => Pertinence.SuggereUnGeste,
                    _ => Pertinence.EclaireLaJournee,
                }));
    }

    /// <summary>
    /// La collecte qui ne revient pas : les encombrants, les sapins, les feuilles. On
    /// ne sait pas ce qu'elle <b>est</b> — aucune connaissance municipale n'entre ici —
    /// mais on voit qu'elle ne se répète pas, là où le bac hebdomadaire revient huit
    /// fois dans la fenêtre. C'est la seule marque générique d'une collecte d'espèce,
    /// et elle vaut pour n'importe quelle ville.
    /// </summary>
    private static EvenementDeLaVille? ProchaineSpeciale(
        List<EvenementDeLaVille> toutes, List<EvenementDeLaVille> collectes, DateOnly aujourdhui)
    {
        if (toutes.Count < CollectesPourJugerDeLOrdinaire)
        {
            return null;
        }
        var habitudes = Habitudes(toutes);
        return collectes.FirstOrDefault(e =>
            Dans(e.Date, aujourdhui) <= JoursDeCollecteSpeciale && habitudes[e.Titre] == 1);
    }

    private static FaitDeTiroir Speciale(
        EvenementDeLaVille speciale, List<EvenementDeLaVille> toutes, EtatDeLaVille ville, DateOnly aujourdhui)
    {
        var habitudes = Habitudes(toutes);
        var jours = Dans(speciale.Date, aujourdhui);
        return new FaitDeTiroir(
            "ville.collecte-speciale",
            FamilleDeFait.Ville,
            "Une collecte spéciale",
            Quand(jours),
            $"{TitreCourt(speciale.Titre)}, le {DateLongue(speciale.Date)} — une seule fois dans la saison.",
            new ScoreDeFait(
                RareteDuFlux(
                    toutes.Count(e => habitudes[e.Titre] == 1), ville.FenetreJours, JoursDeCollecteSpeciale),
                1,
                jours <= 1 ? Pertinence.EngageLaJournee : Pertinence.SuggereUnGeste));
    }

    private static FaitDeTiroir? Evenement(
        List<EvenementDeLaVille> municipaux, EtatDeLaVille ville, DateOnly aujourdhui)
    {
        var prochain = municipaux.FirstOrDefault(e => Dans(e.Date, aujourdhui) <= JoursDEvenement);
        if (prochain is null)
        {
            return null;
        }
        var jours = Dans(prochain.Date, aujourdhui);
        var heure = prochain.Heure is { } h ? $", à {Heure(h)}" : "";
        return new FaitDeTiroir(
            "ville.evenement",
            FamilleDeFait.Ville,
            "En ville",
            Quand(jours),
            $"{TitreCourt(prochain.Titre)}, le {DateLongue(prochain.Date)}{heure}.",
            new ScoreDeFait(
                RareteDuFlux(municipaux.Count, ville.FenetreJours, JoursDEvenement),
                1,
                jours <= 1 ? Pertinence.SuggereUnGeste : Pertinence.EclaireLaJournee));
    }

    /// <summary>Combien de fois chaque titre revient dans la fenêtre — la casse ne
    /// distingue pas deux bacs bruns.</summary>
    private static Dictionary<string, int> Habitudes(List<EvenementDeLaVille> evenements) =>
        evenements
            .GroupBy(e => e.Titre, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Combien de jours par an un fait de flux peut paraître, compté <b>dans le flux
    /// lui-même</b> — comme la fête du hasard, dont la rareté se compte dans la banque
    /// (vault : Fonds De Tiroir). Un foyer dont la ville publie deux événements par an
    /// obtient un fait bien plus rare que celui dont la ville en publie cinquante, et
    /// c'est exact, là où un nombre écrit en dur aurait menti pour l'un des deux.
    ///
    /// <para>Le flux ne porte que sa fenêtre d'ingestion : sa densité est ramenée à
    /// l'année, puis multipliée par les jours d'avance où le fait parle. L'estimation
    /// penche du côté « plus fréquent que la vérité » quand l'événement rare tombe
    /// justement dans la fenêtre chargée — et c'est le bon penchant : il fait
    /// <b>baisser</b> le score, jamais monter.</para>
    /// </summary>
    private static double RareteDuFlux(int evenements, int fenetreDuFlux, int joursDAnnonce) =>
        Rarete.ParAn(Math.Clamp(
            evenements * (365.0 / fenetreDuFlux) * joursDAnnonce, joursDAnnonce, 365));

    private static int Dans(DateOnly date, DateOnly aujourdhui) => date.DayNumber - aujourdhui.DayNumber;

    /// <summary>La forme courte porte le chiffre, et rien d'autre.</summary>
    private static string Quand(int jours) => jours switch
    {
        0 => "C'est aujourd'hui",
        1 => "C'est demain",
        _ => $"Dans {Jours(jours)}",
    };
}
