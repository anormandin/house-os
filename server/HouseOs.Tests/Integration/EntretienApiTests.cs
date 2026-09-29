using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Entretien;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Integration;

/// <summary>Les packs d'entretien par HTTP : le bout-en-bout contre Postgres, avec le fichier livré.</summary>
[Collection("integration")]
public class EntretienApiTests(HouseOsFactory factory)
{
    private sealed record CorpsId(Guid Id);
    private sealed record CorpsCreees(List<TacheAdopteeDto> Creees);

    [Fact]
    public async Task Proposer_pour_un_equipement_inconnu_repond404()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.GetAsync($"/api/entretien/propositions?equipementId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task Adopter_cree_les_taches_puis_refuse_le_doublon()
    {
        var client = await factory.ClientConnecte();
        var creation = await client.PostAsJsonAsync("/api/equipements",
            new { nom = $"Thermopompe {Guid.NewGuid():N}", categorie = "Chauffage" });
        var equipementId = (await creation.Content.ReadFromJsonAsync<CorpsId>())!.Id;

        var propositions = await client.GetFromJsonAsync<PropositionsDto>(
            $"/api/entretien/propositions?equipementId={equipementId}");
        Assert.Equal("Chauffage", propositions!.Pack);
        Assert.Null(propositions.Raison);
        var filtre = Assert.Single(propositions.Propositions, p => p.Cle == "chauffage.filtre");
        Assert.False(filtre.DejaPresente);

        var adoption = await client.PostAsJsonAsync("/api/entretien/adopter",
            new AdopterRequete(["chauffage.filtre"], equipementId));
        Assert.Equal(HttpStatusCode.Created, adoption.StatusCode);
        var creees = (await adoption.Content.ReadFromJsonAsync<CorpsCreees>())!.Creees;
        var tache = Assert.Single(creees);
        Assert.Equal("Changer ou nettoyer le filtre", tache.Titre);
        Assert.NotNull(tache.Echeance);

        var detail = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{tache.Id}");
        Assert.Equal(equipementId, detail!.EquipementId);
        Assert.Equal("Intervalle", detail.Recurrence.Mode);

        var doublon = await client.PostAsJsonAsync("/api/entretien/adopter",
            new AdopterRequete(["chauffage.filtre"], equipementId));
        Assert.Equal(HttpStatusCode.Conflict, doublon.StatusCode);

        var inconnue = await client.PostAsJsonAsync("/api/entretien/adopter",
            new AdopterRequete(["chauffage.inexistant"], equipementId));
        Assert.Equal(HttpStatusCode.BadRequest, inconnue.StatusCode);
    }

    [Fact]
    public async Task Sans_session_c_est_401()
    {
        var client = factory.CreateClient();

        var reponse = await client.GetAsync("/api/entretien/propositions");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }
}
