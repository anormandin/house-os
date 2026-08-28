namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Noms de modules diffusés. Contrat partagé avec la table d'invalidation du web
/// (web/src/lib/synchro.ts) : ces chaînes sont les clés des deux côtés.
/// </summary>
public static class ModulesSynchro
{
    public const string Taches = "taches";
    public const string Zones = "zones";
    public const string Equipements = "equipements";
    public const string Documents = "documents";
    public const string ComptesARebours = "comptes-a-rebours";
    public const string Meteo = "meteo";
    public const string PhraseDuJour = "phrase-du-jour";
    public const string FluxExternes = "flux-externes";
    public const string Budget = "budget";
}
