namespace HouseOs.Api.Domaine.Editorial;

/// <summary>Une tâche à ranger dans le sommaire : son titre, et le nom de sa zone ou de
/// son équipement quand elle en a.</summary>
public sealed record TacheAGrouper(string Titre, string? Zone, string? Equipement)
{
    /// <summary>Vrai quand le journal ne sait pas la ranger lui-même : c'est celle que
    /// l'éditorialiste peut nommer.</summary>
    public bool ANommer => Zone is null && Equipement is null;
}

/// <summary>
/// Le regroupement des journées chargées (D-2026-09-20 Regroupement Sans Catégorie De
/// Tâche) : par zone, puis par équipement, sur ce que la tâche porte déjà — et le paquet
/// restant, celui sans zone ni équipement, prend les rubriques que l'éditorialiste a
/// nommées. Ce qu'il n'a pas placé tombe dans « Le reste ». Aucun titre n'est inventé
/// ici : on ne fait que ranger des tâches réelles, dans l'ordre où elles arrivent.
/// </summary>
public static class Regroupement
{
    public const string LeReste = "Le reste";

    /// <summary>
    /// Les rubriques du jour, dans l'ordre : les zones et les équipements au fil des
    /// tâches (les retards d'abord, comme la liste), puis les rubriques nommées, puis
    /// « Le reste ». Une rubrique nommée du même nom qu'une zone s'y fond ; une rubrique
    /// nommée qui ne place aucune tâche à nommer disparaît.
    /// </summary>
    public static IReadOnlyList<RubriqueEdition> Regrouper(
        IReadOnlyList<TacheAGrouper> taches, IReadOnlyList<RubriqueEdition> nommees)
    {
        var rubriques = new List<RubriqueEdition>();
        RubriqueEdition Rubrique(string nom)
        {
            // « le garage » et « Le garage » sont la même rubrique : le modèle ne
            // respecte pas toujours la casse d'un nom de zone.
            var existante = rubriques.FirstOrDefault(r => string.Equals(r.Nom, nom, StringComparison.OrdinalIgnoreCase));
            if (existante is null)
            {
                existante = new RubriqueEdition(nom, []);
                rubriques.Add(existante);
            }
            return existante;
        }

        var aNommer = new List<TacheAGrouper>();
        foreach (var tache in taches)
        {
            var nom = tache.Zone ?? tache.Equipement;
            if (nom is null)
            {
                aNommer.Add(tache);
            }
            else
            {
                Rubrique(nom).Taches.Add(tache.Titre);
            }
        }

        foreach (var nommee in nommees)
        {
            if (string.Equals(nommee.Nom, LeReste, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var placees = aNommer.Where(t => nommee.Taches.Contains(t.Titre, StringComparer.Ordinal)).ToList();
            if (placees.Count == 0)
            {
                continue;
            }
            var rubrique = Rubrique(nommee.Nom);
            foreach (var tache in placees)
            {
                rubrique.Taches.Add(tache.Titre);
                aNommer.Remove(tache);
            }
        }

        if (aNommer.Count > 0)
        {
            var reste = Rubrique(LeReste);
            reste.Taches.AddRange(aNommer.Select(t => t.Titre));
        }
        return rubriques;
    }
}
