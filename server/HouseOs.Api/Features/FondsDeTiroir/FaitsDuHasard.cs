using static HouseOs.Api.Features.FondsDeTiroir.Mots;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La famille « le hasard » : un dicton de l'almanach, et la fête du jour s'il y en a
/// une (vault : Fonds De Tiroir). Aucune table, aucun réseau, aucun calcul — de la
/// <b>donnée d'édition</b>, lue dans un fichier qu'un foyer ailleurs remplace
/// (vault : D-2026-09-20 Banque Du Hasard En Fichier De Données).
///
/// <para><b>Le dicton est le seul bouche-trou du fonds.</b> Il est vrai tous les jours,
/// donc sa rareté est celle d'un fait quotidien ; ce qui le met en queue de classement,
/// c'est sa <b>pertinence</b> : <see cref="Pertinence.BoucheTrou"/>, un cran sous la
/// décoration. Il ne sort donc que les jours où le budget de widgets n'est rempli par
/// rien de mieux — ce qui est exactement ce qu'on lui demande : ne jamais laisser un
/// trou dans le journal, ne jamais prendre la place de quelque chose qui compte.</para>
///
/// <para>C'est aussi le seul fait dont la matière vit dans le <b>texte long</b> : un
/// proverbe n'a pas de chiffre à mettre en valeur, et il ne tiendrait pas dans les
/// trente signes de la forme courte. Ça tombe bien — un bouche-trou ne paraît que
/// lorsque le journal a de la place, donc lorsqu'il montre les textes longs.</para>
///
/// <para>La fête, elle, a une vraie fenêtre de parution, et <b>la banque la compte
/// elle-même</b> : la rareté est l'inverse du nombre de fêtes inscrites. Un foyer qui
/// n'inscrit que ses huit jours chômés obtient un fait deux fois plus rare que celui
/// qui en inscrit vingt — ce qui est exact, et qu'aucun nombre écrit en dur n'aurait
/// su dire.</para>
/// </summary>
public static class FaitsDuHasard
{
    public static IEnumerable<FaitDeTiroir> Produire(ContexteDuJour contexte)
    {
        // Pas de banque, pas de hasard : la famille entière se tait, exactement comme
        // le ciel sans coordonnées.
        if (contexte.Hasard is not { } banque)
        {
            return [];
        }

        var faits = new List<FaitDeTiroir?>
        {
            Fete(banque, contexte.Date),
            Dicton(banque, contexte.Date),
        };
        return faits.OfType<FaitDeTiroir>();
    }

    private static FaitDeTiroir? Fete(BanqueDuHasard banque, DateOnly aujourdhui)
    {
        if (banque.Fetes.Count == 0)
        {
            return null;
        }
        // Deux fêtes le même jour, ça arrive (la Saint-Valentin et une journée
        // internationale) : le jour chômé passe devant, et le nom départage le reste
        // pour que deux tirages de la même journée donnent le même journal.
        var duJour = banque.Fetes
            .Where(f => f.Quand.DansLAnnee(aujourdhui.Year) == aujourdhui)
            .OrderByDescending(f => f.Ferie)
            .ThenBy(f => f.Nom, StringComparer.Ordinal)
            .FirstOrDefault();
        if (duJour is null)
        {
            return null;
        }

        // Le nom vient du fichier, donc d'un inconnu : il passe par l'élagage comme
        // n'importe quel titre saisi, plutôt que d'être coupé au mur.
        return new FaitDeTiroir(
            "hasard.fete",
            FamilleDeFait.Hasard,
            duJour.Ferie ? "Un jour férié" : "Une journée nationale",
            TitreCourt(duJour.Nom, FaitDeTiroir.LongueurDeValeur),
            duJour.Texte,
            new ScoreDeFait(
                Rarete.ParAn(banque.Fetes.Count),
                1,
                duJour.Ferie ? Pertinence.SuggereUnGeste : Pertinence.EclaireLaJournee));
    }

    private static FaitDeTiroir? Dicton(BanqueDuHasard banque, DateOnly aujourdhui)
    {
        var duMois = banque.Dictons
            .Where(d => d.Mois == aujourdhui.Month)
            .OrderBy(d => d.Texte, StringComparer.Ordinal)
            .ToList();
        if (duMois.Count == 0)
        {
            return null;
        }

        // Figé pour la journée, et différent le lendemain : le quantième tourne dans la
        // liste du mois. Rien d'aléatoire, malgré le nom de la famille — un journal qui
        // change de dicton entre deux réveils se contredirait tout seul.
        var choisi = duMois[(aujourdhui.Day - 1) % duMois.Count];

        return new FaitDeTiroir(
            "hasard.dicton",
            FamilleDeFait.Hasard,
            "Le dicton",
            $"Du mois {De(aujourdhui.ToString("MMMM", Francais))}",
            $"« {choisi.Texte} »",
            new ScoreDeFait(Rarete.Quotidien, 1, Pertinence.BoucheTrou));
    }
}
