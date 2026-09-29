using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Entretien;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Synchro;
using HouseOs.Tests.Features.Synchro;
using HouseOs.Tests.Features.Taches;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;

namespace HouseOs.Tests.Features.Mcp;

/// <summary>
/// Parité MCP des packs d'entretien : l'agent voit ce que l'UI voit, et un refus reste
/// actionnable (vault : Serveur MCP, D-2026-09-28 Packs D'entretien En Fichier De Données).
/// </summary>
public class OutilsEntretienTests : TestAvecSqlite
{
    private readonly PacksEntretien _packs =
        LecturePacks.Lire(null, AppContext.BaseDirectory, NullLogger.Instance);
    private readonly DiffuseurMouchard _mouchard = new();

    public OutilsEntretienTests()
    {
        Db.Utilisateurs.Add(new Utilisateur
        {
            Id = Guid.NewGuid(), NomUtilisateur = "ariane", NomAffichage = "Ariane", MotDePasseHash = "x",
        });
        Db.SaveChanges();
    }

    [Fact]
    public async Task Proposer_sans_equipement_liste_le_programme_de_la_maison()
    {
        var propositions = await OutilsEntretien.ProposerEntretiens(Db, _packs);

        Assert.Equal("maison", propositions.Pack);
        Assert.NotEmpty(propositions.Propositions);
        Assert.All(propositions.Propositions, p => Assert.False(p.DejaPresente));
    }

    [Fact]
    public async Task Adopter_cree_au_nom_du_membre_et_annonce_le_lot()
    {
        var equipement = new Equipement
        {
            Id = Guid.NewGuid(), Nom = "Chauffe-eau", Categorie = CategorieEquipement.EauChaude,
            CreeLe = DateTimeOffset.UtcNow,
        };
        Db.Equipements.Add(equipement);
        await Db.SaveChangesAsync();
        var cles = (await OutilsEntretien.ProposerEntretiens(Db, _packs, equipement.Id))
            .Propositions.Select(p => p.Cle).ToArray();

        await OutilsEntretien.AdopterEntretiens(Db, _packs, _mouchard, "Ariane", cles, equipement.Id);

        Assert.Equal(cles.Length, Db.Taches.Count(t => t.EquipementId == equipement.Id));
        var evenement = Assert.Single(_mouchard.Fins);
        Assert.Equal(EvenementSynchro.GenreTachesCreees, evenement.Genre);
        Assert.Equal(EvenementSynchro.SourceMcp, evenement.Source);
        Assert.Equal(cles.Length, evenement.Nombre);

        // Re-proposer les montre présentes ; re-adopter refuse sans rien créer.
        Assert.All((await OutilsEntretien.ProposerEntretiens(Db, _packs, equipement.Id)).Propositions,
            p => Assert.True(p.DejaPresente));
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsEntretien.AdopterEntretiens(Db, _packs, _mouchard, "ariane", [cles[0]], equipement.Id));
        Assert.Contains("Rien n'a été créé", exception.Message);
    }

    [Fact]
    public async Task Un_equipement_inconnu_est_une_erreur_actionnable()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsEntretien.ProposerEntretiens(Db, _packs, Guid.NewGuid()));

        Assert.Contains("introuvable", exception.Message);
    }
}
