using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Entretien;
using HouseOs.Api.Features.Taches;
using HouseOs.Tests.Features.Taches;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.Entretien;

/// <summary>
/// Proposer et adopter, sur le harnais Sqlite : le « déjà présent » par titre, le lot
/// tout-ou-rien, les liens à l'équipement et à sa pièce (vault : Emménagement V2).
/// </summary>
public class OperationsEntretienTests : TestAvecSqlite
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 29);
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 29, 12, 0, 0, TimeSpan.FromHours(-4));

    private readonly Utilisateur _alain;
    private readonly PacksEntretien _packs;

    public OperationsEntretienTests()
    {
        _alain = new Utilisateur { Id = Guid.NewGuid(), NomUtilisateur = "alain", NomAffichage = "Alain", MotDePasseHash = "x" };
        Db.Utilisateurs.Add(_alain);
        Db.SaveChanges();
        _packs = LecturePacks.Retenir(new FichierDePacks
        {
            Maison =
            [
                Item("maison.gouttieres", "Nettoyer les gouttières", Annuelle(11, 1)),
                Item("maison.detecteurs", "Tester les détecteurs de fumée et de CO", JourDuMois(1)),
            ],
            ParCategorie = new Dictionary<string, List<ItemDePack>>
            {
                ["Chauffage"] =
                [
                    Item("chauffage.filtre", "Changer ou nettoyer le filtre", Intervalle(90), "Alternance"),
                    Item("chauffage.entretien", "Entretien annuel par un technicien", Annuelle(9, 15)),
                ],
            },
        }, NullLogger.Instance, Aujourdhui);
    }

    private static ItemDePack Item(string cle, string titre, RecurrenceDto recurrence, string? strategie = null) =>
        new() { Cle = cle, Titre = titre, Recurrence = recurrence, Strategie = strategie };

    private static RecurrenceDto Annuelle(int mois, int jour) =>
        new("Fixe", "Annuelle", null, null, mois, jour, null, null, null, null, null, null);

    private static RecurrenceDto JourDuMois(int jour) =>
        new("Fixe", "JourDuMois", null, jour, null, null, null, null, null, null, null, null);

    private static RecurrenceDto Intervalle(int jours) =>
        new("Intervalle", null, null, null, null, null, jours, null, null, null, null, null);

    private async Task<Equipement> CreerEquipement(CategorieEquipement? categorie, Guid? zoneId = null)
    {
        var equipement = new Equipement
        {
            Id = Guid.NewGuid(), Nom = "Thermopompe", Categorie = categorie, ZoneId = zoneId,
            CreeLe = Maintenant,
        };
        Db.Equipements.Add(equipement);
        await Db.SaveChangesAsync();
        return equipement;
    }

    private Task<ResultatAdoption> Adopter(Guid? equipementId, params string[] cles) =>
        OperationsEntretien.AdopterAsync(Db, _packs, new AdopterRequete(cles, equipementId), _alain.Id, Maintenant, Aujourdhui);

    [Fact]
    public async Task Le_programme_de_la_maison_marque_ce_qui_existe_deja_par_titre_sans_accents()
    {
        // Titre saisi à la main, sans accents et avec un blanc de trop : c'est la même tâche.
        Db.Taches.Add(Tache.CreerPonctuelle("nettoyer les  gouttieres", null, null, null, _alain.Id, Maintenant));
        await Db.SaveChangesAsync();

        var propositions = (await OperationsEntretien.ProposerAsync(Db, _packs, null))!;

        Assert.Equal("maison", propositions.Pack);
        Assert.Null(propositions.Raison);
        var gouttieres = Assert.Single(propositions.Propositions, p => p.Cle == "maison.gouttieres");
        Assert.True(gouttieres.DejaPresente);
        Assert.NotNull(gouttieres.TacheExistanteId);
        Assert.False(Assert.Single(propositions.Propositions, p => p.Cle == "maison.detecteurs").DejaPresente);
    }

    [Fact]
    public async Task Un_equipement_sans_categorie_ne_recoit_rien_mais_dit_pourquoi()
    {
        var equipement = await CreerEquipement(null);

        var propositions = (await OperationsEntretien.ProposerAsync(Db, _packs, equipement.Id))!;

        Assert.Empty(propositions.Propositions);
        Assert.Contains("Classe d'abord", propositions.Raison);
        Assert.Equal("Thermopompe", propositions.Equipement);
    }

    [Fact]
    public async Task Un_equipement_inconnu_est_null()
    {
        Assert.Null(await OperationsEntretien.ProposerAsync(Db, _packs, Guid.NewGuid()));
        Assert.Equal(404, (await Adopter(Guid.NewGuid(), "chauffage.filtre")).Statut);
    }

    [Fact]
    public async Task Le_deja_present_d_un_equipement_ne_regarde_que_ses_tâches()
    {
        var autre = await CreerEquipement(CategorieEquipement.Chauffage);
        var celuiCi = await CreerEquipement(CategorieEquipement.Chauffage);
        var tacheDeLAutre = Tache.CreerPonctuelle("Changer ou nettoyer le filtre", null, null, null, _alain.Id, Maintenant);
        tacheDeLAutre.EquipementId = autre.Id;
        Db.Taches.Add(tacheDeLAutre);
        await Db.SaveChangesAsync();

        var propositions = (await OperationsEntretien.ProposerAsync(Db, _packs, celuiCi.Id))!;

        // La fournaise a son filtre ; la thermopompe n'a pas encore le sien.
        Assert.False(Assert.Single(propositions.Propositions, p => p.Cle == "chauffage.filtre").DejaPresente);
        Assert.Equal("Chauffage", propositions.Pack);
    }

    [Fact]
    public async Task Adopter_cree_les_taches_liees_a_l_equipement_et_a_sa_piece()
    {
        var zone = new Zone { Id = Guid.NewGuid(), Nom = "Sous-sol", Type = TypeZone.Interieur };
        Db.Zones.Add(zone);
        await Db.SaveChangesAsync();
        var equipement = await CreerEquipement(CategorieEquipement.Chauffage, zone.Id);

        var resultat = await Adopter(equipement.Id, "chauffage.filtre", "chauffage.entretien");

        Assert.Equal(201, resultat.Statut);
        Assert.Equal(2, resultat.Creees!.Count);
        var filtre = Db.Taches.Single(t => t.Titre == "Changer ou nettoyer le filtre");
        Assert.Equal(equipement.Id, filtre.EquipementId);
        Assert.Equal(zone.Id, filtre.ZoneId);
        Assert.Equal(ModeRecurrence.Intervalle, filtre.Recurrence.Mode);
        Assert.Equal(StrategieAssignation.Alternance, filtre.Strategie);
        // La première occurrence est matérialisée par le moteur, pas par nous.
        Assert.NotNull(Assert.Single(resultat.Creees, c => c.Titre == "Changer ou nettoyer le filtre").Echeance);
        Assert.Equal(2, Db.Occurrences.Count());
    }

    [Fact]
    public async Task Adopter_un_titre_deja_present_refuse_tout_le_lot()
    {
        var equipement = await CreerEquipement(CategorieEquipement.Chauffage);
        Assert.Equal(201, (await Adopter(equipement.Id, "chauffage.filtre")).Statut);

        var resultat = await Adopter(equipement.Id, "chauffage.entretien", "chauffage.filtre");

        Assert.Equal(409, resultat.Statut);
        Assert.Contains("déjà dans vos tâches", resultat.Erreur!.Message);
        Assert.Single(Db.Taches); // l'entretien annuel n'a pas été créé non plus
    }

    [Fact]
    public async Task Une_cle_inconnue_ou_d_un_autre_pack_refuse_le_lot()
    {
        var equipement = await CreerEquipement(CategorieEquipement.Chauffage);

        var inconnue = await Adopter(equipement.Id, "chauffage.filtre", "chauffage.rien");
        Assert.Equal(400, inconnue.Statut);
        Assert.Contains("inconnue", inconnue.Erreur!.Message);

        var autrePack = await Adopter(equipement.Id, "maison.gouttieres");
        Assert.Equal(400, autrePack.Statut);
        Assert.Contains("n'est pas du pack", autrePack.Erreur!.Message);

        var sansCategorie = await Adopter((await CreerEquipement(null)).Id, "chauffage.filtre");
        Assert.Equal(400, sansCategorie.Statut);

        Assert.Empty(Db.Taches);
    }

    [Fact]
    public async Task Le_programme_de_la_maison_s_adopte_sans_equipement_et_sans_cles_en_double()
    {
        var resultat = await Adopter(null, "maison.gouttieres", " maison.gouttieres ", "maison.detecteurs");

        Assert.Equal(201, resultat.Statut);
        Assert.Equal(2, resultat.Creees!.Count);
        Assert.All(Db.Taches, t => Assert.Null(t.EquipementId));
        Assert.Equal(400, (await Adopter(null)).Statut);
    }
}
