namespace HouseOs.Api.Domaine.Editorial;

/// <summary>Ce que le plancher a retenu : la raison, et le titre qui prend la manchette.</summary>
public sealed record PlancherDuJour(RaisonDePlancher Raison, string Titre);

/// <summary>Une occurrence ouverte, réduite à ce que le plancher juge.</summary>
public sealed record OccurrenceDue(string Titre, DateOnly? Echeance, bool EcheanceFerme);

/// <summary>
/// Le plancher non négociable du journal (vault : Journal De La Maison) : un compte à
/// rebours à zéro, une échéance ferme, une tâche en retard de plus de trois jours
/// prennent la manchette quoi qu'il arrive. Le pendant serveur de `plancher()`
/// (`web/src/lib/ecran-vues.ts`), avec le même ordre : c'est lui qui fixe le rang de
/// l'édition et qui, réévalué à chaque rendu, redéclenche une édition quand il change
/// (D-2026-09-20 Une Édition Par Jour Matérialisée).
/// </summary>
public static class Plancher
{
    /// <summary>À partir de ce retard, une tâche ne peut plus être reléguée sous un widget.</summary>
    public const int JoursDeRetard = 3;

    public static PlancherDuJour? Evaluer(
        IEnumerable<OccurrenceDue> ouvertes, (string Titre, DateOnly DateCible)? prochainCompte, DateOnly aujourdhui)
    {
        if (prochainCompte is { } compte && compte.DateCible <= aujourdhui)
        {
            return new PlancherDuJour(RaisonDePlancher.Compte, compte.Titre);
        }
        // Les ouvertes arrivent triées par échéance : la première qui répond est la
        // plus ancienne, comme au client.
        var liste = ouvertes as IReadOnlyList<OccurrenceDue> ?? [.. ouvertes];
        var ferme = liste.FirstOrDefault(o => o.EcheanceFerme && o.Echeance is { } e && e <= aujourdhui);
        if (ferme is not null)
        {
            return new PlancherDuJour(RaisonDePlancher.Ferme, ferme.Titre);
        }
        var tresEnRetard = liste.FirstOrDefault(o =>
            o.Echeance is { } e && aujourdhui.DayNumber - e.DayNumber > JoursDeRetard);
        if (tresEnRetard is not null)
        {
            return new PlancherDuJour(RaisonDePlancher.Retard, tresEnRetard.Titre);
        }
        return null;
    }

    /// <summary>Le surtitre que le gabarit met au-dessus d'une manchette de plancher.</summary>
    public static string Surtitre(RaisonDePlancher raison) => raison switch
    {
        RaisonDePlancher.Compte => "Le compte à rebours est à zéro",
        RaisonDePlancher.Ferme => "Une date qui ne se négocie pas",
        _ => "En retard depuis plus de trois jours",
    };
}
