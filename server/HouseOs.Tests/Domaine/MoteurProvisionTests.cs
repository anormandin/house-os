using HouseOs.Api.Domaine;

namespace HouseOs.Tests.Domaine;

public class MoteurProvisionTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 8, 26);

    private static Enveloppe Creer(
        TypeEnveloppe type = TypeEnveloppe.Equipement,
        decimal? cible = null,
        DateOnly? dateCible = null,
        Guid? tacheId = null,
        List<Versement>? echeancier = null,
        StatutEnveloppe statut = StatutEnveloppe.Active) => new()
    {
        Id = Guid.NewGuid(),
        Nom = "Test",
        Type = type,
        MontantCible = cible,
        DateCible = dateCible,
        TacheId = tacheId,
        Echeancier = echeancier,
        Statut = statut,
    };

    // ——— Mois restants : les 1ᵉʳˢ du mois dans (aujourd'hui, cible], plancher 1 ———

    [Theory]
    [InlineData(2027, 6, 1, 10)]   // sept. → juin = 10 occasions de virement
    [InlineData(2026, 9, 1, 1)]    // le prochain 1ᵉʳ compte
    [InlineData(2026, 8, 30, 1)]   // dans le mois courant → plancher 1
    [InlineData(2026, 3, 1, 1)]    // échue → plancher 1 (tout le manque d'un coup)
    [InlineData(2033, 6, 1, 82)]   // la toiture décennale
    public void MoisRestants_compte_les_premiers_du_mois(int annee, int mois, int jour, int attendu) =>
        Assert.Equal(attendu, MoteurProvision.MoisRestants(Aujourdhui, new DateOnly(annee, mois, jour)));

    [Fact]
    public void MoisRestants_depuis_un_premier_du_mois_exclut_aujourdhui()
    {
        // Le 1ᵉʳ courant n'est pas une occasion à venir : seul le 1ᵉʳ septembre compte.
        Assert.Equal(1, MoteurProvision.MoisRestants(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void MoisRestants_fin_de_mois_vers_debut_de_mois()
    {
        Assert.Equal(2, MoteurProvision.MoisRestants(new DateOnly(2027, 1, 31), new DateOnly(2027, 3, 1)));
    }

    // ——— Provision à cible et date ———

    [Fact]
    public void Enveloppe_liee_a_une_tache_derive_son_echeance()
    {
        var enveloppe = Creer(cible: 4500, tacheId: Guid.NewGuid(), dateCible: new DateOnly(2027, 1, 1));
        var provision = MoteurProvision.Calculer(
            enveloppe, solde: 2260, Aujourdhui, echeanceTacheLiee: new DateOnly(2033, 6, 1));

        // (4 500 − 2 260) ÷ 82 = 27,3 → 27 $ ; la DateCible saisie est ignorée.
        Assert.Equal(27, provision.Montant);
        Assert.Equal(new DateOnly(2033, 6, 1), provision.DateEffective);
        Assert.False(provision.EnRetard);
    }

    [Fact]
    public void Tache_liee_sans_occurrence_en_attente_donne_zero_provision()
    {
        var enveloppe = Creer(cible: 4500, tacheId: Guid.NewGuid(), dateCible: new DateOnly(2027, 1, 1));
        var provision = MoteurProvision.Calculer(enveloppe, solde: 100, Aujourdhui, echeanceTacheLiee: null);

        Assert.Equal(0, provision.Montant);
        Assert.Null(provision.DateEffective);
    }

    [Fact]
    public void Enveloppe_libre_utilise_sa_date_cible()
    {
        var enveloppe = Creer(TypeEnveloppe.Projet, cible: 8000, dateCible: new DateOnly(2028, 9, 1));
        var provision = MoteurProvision.Calculer(enveloppe, solde: 3400, Aujourdhui, echeanceTacheLiee: null);

        // (8 000 − 3 400) ÷ 25 = 184 $
        Assert.Equal(184, provision.Montant);
        Assert.Equal(new DateOnly(2028, 9, 1), provision.DateEffective);
    }

    [Fact]
    public void Cible_atteinte_donne_zero()
    {
        var enveloppe = Creer(cible: 1000, dateCible: new DateOnly(2027, 6, 1));
        Assert.Equal(0, MoteurProvision.Calculer(enveloppe, 1000, Aujourdhui, null).Montant);
        Assert.Equal(0, MoteurProvision.Calculer(enveloppe, 1500, Aujourdhui, null).Montant);
    }

    [Fact]
    public void Cible_echue_reclame_tout_le_manque_et_marque_le_retard()
    {
        var enveloppe = Creer(cible: 1000, dateCible: new DateOnly(2026, 5, 1));
        var provision = MoteurProvision.Calculer(enveloppe, solde: 400, Aujourdhui, null);

        Assert.Equal(600, provision.Montant);
        Assert.True(provision.EnRetard);
    }

    [Fact]
    public void Sans_cible_ou_sans_date_pas_de_provision()
    {
        Assert.Equal(0, MoteurProvision.Calculer(
            Creer(cible: null, dateCible: new DateOnly(2027, 6, 1)), 0, Aujourdhui, null).Montant);
        Assert.Equal(0, MoteurProvision.Calculer(
            Creer(cible: 5000, dateCible: null), 0, Aujourdhui, null).Montant);
    }

    [Fact]
    public void Reserve_et_fermee_jamais_de_provision()
    {
        Assert.Equal(0, MoteurProvision.Calculer(
            Creer(TypeEnveloppe.Reserve, cible: 5000, dateCible: new DateOnly(2027, 6, 1)),
            0, Aujourdhui, null).Montant);
        Assert.Equal(0, MoteurProvision.Calculer(
            Creer(cible: 5000, dateCible: new DateOnly(2027, 6, 1), statut: StatutEnveloppe.Fermee),
            0, Aujourdhui, null).Montant);
    }

    [Fact]
    public void Provision_arrondie_au_dollar()
    {
        // 100 ÷ 3 = 33,33 → 33 $ ; 200 ÷ 3 = 66,67 → 67 $
        Assert.Equal(33, MoteurProvision.ProvisionCible(100, 0, Aujourdhui, new DateOnly(2026, 11, 1)));
        Assert.Equal(67, MoteurProvision.ProvisionCible(200, 0, Aujourdhui, new DateOnly(2026, 11, 1)));
    }

    // ——— Taxes : lissage sur l'échéancier, couverture chronologique ———

    private static readonly List<Versement> TaxesMunicipales =
    [
        new(new DateOnly(2026, 3, 1), 1240),
        new(new DateOnly(2026, 9, 1), 1240),
        new(new DateOnly(2027, 3, 1), 1240),
    ];

    [Fact]
    public void Taxes_le_solde_couvre_les_versements_en_ordre_chronologique()
    {
        // Solde 1 500 : couvre sept. (1 240) puis 260 sur mars 2027.
        // Mars 2027 manque 980 ÷ 7 mois = 140 $ ; sept. couvert → 0.
        var provision = MoteurProvision.ProvisionTaxes(TaxesMunicipales, 1500, Aujourdhui);
        Assert.Equal(140, provision);
    }

    [Fact]
    public void Taxes_versements_passes_ignores()
    {
        // Le versement de mars 2026 est passé : il ne réclame rien et ne reçoit
        // aucune couverture. Solde 0 : sept. 1 240 ÷ 1 + mars 1 240 ÷ 7 = 1 240 + 177.
        var provision = MoteurProvision.ProvisionTaxes(TaxesMunicipales, 0, Aujourdhui);
        Assert.Equal(1240 + 177, provision);
    }

    [Fact]
    public void Taxes_solde_negatif_traite_comme_zero()
    {
        Assert.Equal(
            MoteurProvision.ProvisionTaxes(TaxesMunicipales, 0, Aujourdhui),
            MoteurProvision.ProvisionTaxes(TaxesMunicipales, -50, Aujourdhui));
    }

    [Fact]
    public void Taxes_echeancier_tout_passe_signale_le_renouvellement()
    {
        var enveloppe = Creer(TypeEnveloppe.Taxes, echeancier:
            [new(new DateOnly(2026, 3, 1), 1240), new(new DateOnly(2026, 6, 1), 1240)]);
        var provision = MoteurProvision.Calculer(enveloppe, 500, Aujourdhui, null);

        Assert.True(provision.EcheancierARenouveler);
        Assert.Equal(0, provision.Montant);
        Assert.Null(provision.DateEffective);
    }

    [Fact]
    public void Taxes_date_effective_est_le_prochain_versement()
    {
        var enveloppe = Creer(TypeEnveloppe.Taxes, echeancier: TaxesMunicipales);
        var provision = MoteurProvision.Calculer(enveloppe, 0, Aujourdhui, null);
        Assert.Equal(new DateOnly(2026, 9, 1), provision.DateEffective);
    }

    // ——— L'invariant ———

    [Fact]
    public void Non_affecte_est_le_reste_du_compte()
    {
        Assert.Equal(935, MoteurProvision.NonAffecte(12480, [1950m, 2260m, 3400m, 1020m, 115m, 2800m]));
    }

    [Fact]
    public void Sur_allocation_donne_un_non_affecte_negatif()
    {
        Assert.Equal(-320, MoteurProvision.NonAffecte(12480, [12800m]));
    }

    [Fact]
    public void Solde_compte_est_ancrage_plus_transactions()
    {
        Assert.Equal(12480 - 84.12m, MoteurProvision.SoldeCompte(12480, [675m, -675m, -84.12m]));
    }
}
