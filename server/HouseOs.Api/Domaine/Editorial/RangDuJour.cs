namespace HouseOs.Api.Domaine.Editorial;

/// <summary>
/// Le rang est une fonction du nombre de tâches dues et du plancher, jamais un choix de
/// gabarit (vault : D-2026-09-20 Une Seule Mise En Page À Rangs). Les seuils sont ceux de
/// `grilleDuJour` (`web/src/lib/ecran-vues.ts`) ; le budget de widgets aussi.
/// </summary>
public static class RangDuJour
{
    /// <summary>À partir de ce compte de tâches dues, la journée est « chargée » : le
    /// journal devient un sommaire, et la liste se range par rubrique quel que soit le
    /// plancher (un plancher change la manchette, pas la longueur de la liste).</summary>
    public const int SeuilDuSommaire = 10;

    public static bool JourneeChargee(int tachesDues) => tachesDues >= SeuilDuSommaire;

    public static RangEdition Calculer(int tachesDues, bool plancherDeclenche)
    {
        if (plancherDeclenche)
        {
            return RangEdition.Evenement;
        }
        return tachesDues switch
        {
            0 => RangEdition.Chronique,
            <= 2 => RangEdition.Manchette,
            <= 5 => RangEdition.Resserre,
            < SeuilDuSommaire => RangEdition.Court,
            _ => RangEdition.Sommaire,
        };
    }

    /// <summary>
    /// Ce que le journal <b>veut</b> montrer de widgets à ce rang — pas ce que le papier
    /// tient, qui est l'affaire du consommateur. C'est ce budget qui borne les clés
    /// publiées par l'édition, donc la mémoire de fraîcheur.
    /// </summary>
    public static int BudgetDeWidgets(RangEdition rang) => rang switch
    {
        RangEdition.Chronique => 7,
        RangEdition.Manchette => 5,
        RangEdition.Resserre => 4,
        RangEdition.Court => 3,
        RangEdition.Sommaire => 3,
        _ => 1,
    };
}
