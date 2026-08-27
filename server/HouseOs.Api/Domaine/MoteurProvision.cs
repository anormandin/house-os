namespace HouseOs.Api.Domaine;

/// <summary>Résultat du calcul de provision d'une enveloppe (rien n'est stocké).</summary>
/// <param name="Montant">Provision mensuelle suggérée, arrondie au dollar, plancher 0.</param>
/// <param name="DateEffective">Échéance retenue : prochaine occurrence de la tâche liée,
/// prochain versement de l'échéancier, ou date cible saisie.</param>
/// <param name="EnRetard">La date effective est passée sans que la cible soit atteinte.</param>
/// <param name="EcheancierARenouveler">Type Taxes : tous les versements sont passés.</param>
public record ProvisionEnveloppe(
    decimal Montant,
    DateOnly? DateEffective,
    bool EnRetard,
    bool EcheancierARenouveler);

/// <summary>
/// Calcul pur des provisions mensuelles — aucune I/O, aucune provision stockée
/// (D-2026-08-26 Cibles D'enveloppe Dérivées Des Tâches). Testé unitairement comme
/// le moteur de récurrence.
/// </summary>
public static class MoteurProvision
{
    /// <summary>
    /// Nombre d'occasions de virement d'ici la cible : les 1ᵉʳˢ du mois dans
    /// (aujourd'hui, cible], plancher 1 — cible échue ou dans le mois courant,
    /// la provision devient tout le manque d'un coup.
    /// </summary>
    public static int MoisRestants(DateOnly aujourdhui, DateOnly cible)
    {
        var mois = (cible.Year - aujourdhui.Year) * 12 + cible.Month - aujourdhui.Month;
        return Math.Max(1, mois);
    }

    /// <summary>Provision d'une enveloppe à cible et date : (cible − solde) ÷ mois restants.</summary>
    public static decimal ProvisionCible(decimal cible, decimal solde, DateOnly aujourdhui, DateOnly dateCible)
    {
        var manque = cible - solde;
        if (manque <= 0)
        {
            return 0;
        }
        return Math.Round(manque / MoisRestants(aujourdhui, dateCible), 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Provision lissée d'une enveloppe Taxes : le solde couvre les versements à venir
    /// en ordre chronologique (le prochain d'abord, l'excédent au suivant), et chaque
    /// versement non couvert contribue (montant − part couverte) ÷ ses mois restants.
    /// Les versements passés sont ignorés.
    /// </summary>
    public static decimal ProvisionTaxes(IEnumerable<Versement> echeancier, decimal solde, DateOnly aujourdhui)
    {
        var reste = Math.Max(0, solde);
        decimal total = 0;
        foreach (var versement in VersementsAVenir(echeancier, aujourdhui))
        {
            var couvert = Math.Min(reste, versement.Montant);
            reste -= couvert;
            var manque = versement.Montant - couvert;
            if (manque > 0)
            {
                total += Math.Round(
                    manque / MoisRestants(aujourdhui, versement.Date), 0, MidpointRounding.AwayFromZero);
            }
        }
        return total;
    }

    /// <summary>
    /// Provision d'une enveloppe. <paramref name="solde"/> : Σ des mouvements (calculé
    /// par l'appelant). <paramref name="echeanceTacheLiee"/> : échéance de la prochaine
    /// occurrence en attente de la tâche liée (null si aucune) — chargée par l'appelant,
    /// le moteur reste pur. Sans cible ou sans date : pas de provision.
    /// </summary>
    public static ProvisionEnveloppe Calculer(
        Enveloppe enveloppe, decimal solde, DateOnly aujourdhui, DateOnly? echeanceTacheLiee)
    {
        if (enveloppe.Statut == StatutEnveloppe.Fermee || enveloppe.Type == TypeEnveloppe.Reserve)
        {
            return new ProvisionEnveloppe(0, null, false, false);
        }

        if (enveloppe.Type == TypeEnveloppe.Taxes && enveloppe.Echeancier is { Count: > 0 } echeancier)
        {
            var prochain = VersementsAVenir(echeancier, aujourdhui).FirstOrDefault();
            var aRenouveler = prochain is null;
            var montant = aRenouveler ? 0 : ProvisionTaxes(echeancier, solde, aujourdhui);
            return new ProvisionEnveloppe(montant, prochain?.Date, false, aRenouveler);
        }

        // La date dérive de la tâche liée quand un lien existe — même rompue (tâche
        // supprimée ou sans occurrence en attente), on ne retombe pas sur DateCible.
        var dateEffective = enveloppe.TacheId is not null ? echeanceTacheLiee : enveloppe.DateCible;
        if (enveloppe.MontantCible is not { } cible || dateEffective is not { } date)
        {
            return new ProvisionEnveloppe(0, dateEffective, false, false);
        }

        var provision = ProvisionCible(cible, solde, aujourdhui, date);
        var enRetard = date < aujourdhui && provision > 0;
        return new ProvisionEnveloppe(provision, date, enRetard, false);
    }

    /// <summary>Solde courant du compte = solde initial ancré + Σ transactions postérieures.</summary>
    public static decimal SoldeCompte(decimal soldeInitial, IEnumerable<decimal> montantsTransactions) =>
        soldeInitial + montantsTransactions.Sum();

    /// <summary>Non affecté = solde du compte − Σ soldes d'enveloppes (l'invariant).</summary>
    public static decimal NonAffecte(decimal soldeCompte, IEnumerable<decimal> soldesEnveloppes) =>
        soldeCompte - soldesEnveloppes.Sum();

    private static IEnumerable<Versement> VersementsAVenir(IEnumerable<Versement> echeancier, DateOnly aujourdhui) =>
        echeancier.Where(v => v.Date >= aujourdhui).OrderBy(v => v.Date);
}
