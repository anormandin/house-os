using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Features.Zones;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class TachesApiTests(HouseOsFactory factory)
{
    private static readonly DateOnly Aujourdhui = DateOnly.FromDateTime(DateTime.Now);

    private static async Task<Guid> CreerTache(HttpClient client, CreerTacheRequete requete)
    {
        var reponse = await client.PostAsJsonAsync("/api/taches", requete);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var corps = await reponse.Content.ReadFromJsonAsync<CorpsId>();
        return corps!.Id;
    }

    private static async Task<OccurrenceDto> OccurrenceEnAttente(HttpClient client, Guid tacheId)
    {
        var occurrences = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            "/api/occurrences?filtre=en-attente");
        return Assert.Single(occurrences!, o => o.TacheId == tacheId);
    }

    private sealed record CorpsId(Guid Id);

    [Fact]
    public async Task SansCookie_LApiRepond401()
    {
        var client = factory.CreateClient();
        var reponse = await client.GetAsync("/api/occurrences");
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task ObtenirLeDetail_RetourneLEcheanceDeLOccurrenceEnAttente()
    {
        var client = await factory.ClientConnecte();
        var echeance = Aujourdhui.AddDays(3);
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test détail échéance", "Ligne 1\nLigne 2", echeance,
            null, null, null, null, null));

        var detail = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{id}");

        Assert.Equal(echeance, detail!.Echeance);
        Assert.Equal("Ligne 1\nLigne 2", detail.Description);
    }

    [Fact]
    public async Task EditionSansModification_EstSansPerte()
    {
        var client = await factory.ClientConnecte();
        var zoneReponse = await client.PostAsJsonAsync(
            "/api/zones", new { nom = $"Zone sans perte {Guid.NewGuid():N}", type = "Interieur" });
        zoneReponse.EnsureSuccessStatusCode();
        var zoneId = (await zoneReponse.Content.ReadFromJsonAsync<ZoneDto>())!.Id;
        var alain = (await client.GetFromJsonAsync<List<UtilisateurDto>>("/api/utilisateurs"))!
            .Single(u => u.NomUtilisateur == "alain");

        var echeance = Aujourdhui.AddDays(5);
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test édition sans perte", "des détails", echeance,
            alain.Id, zoneId, null, null, null));
        var avant = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{id}");

        // Renvoyer le détail tel quel : rien ne doit changer (bug d'échéance du 2026-08-25).
        var put = await client.PutAsJsonAsync($"/api/taches/{id}", new ModifierTacheRequete(
            avant!.Titre, avant.Description, avant.Echeance, avant.AssigneAId,
            avant.ZoneId, avant.EquipementId, avant.Strategie, avant.Recurrence));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var apres = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{id}");
        Assert.Equal(avant.Titre, apres!.Titre);
        Assert.Equal(avant.Description, apres.Description);
        Assert.Equal(echeance, apres.Echeance);
        Assert.Equal(avant.AssigneAId, apres.AssigneAId);
        Assert.Equal(avant.ZoneId, apres.ZoneId);
        Assert.Equal(avant.Strategie, apres.Strategie);
        Assert.Equal(avant.Recurrence.Mode, apres.Recurrence.Mode);

        var occurrence = await OccurrenceEnAttente(client, id);
        Assert.Equal(echeance, occurrence.Echeance);
    }

    [Fact]
    public async Task CompleterPuisAnnuler_RetourneLOccurrenceEnAttente()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test complétion", null, Aujourdhui, null, null, null, null, null));
        var occurrence = await OccurrenceEnAttente(client, id);

        var completer = await client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null);
        Assert.Equal(HttpStatusCode.NoContent, completer.StatusCode);
        var completees = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            "/api/occurrences?filtre=completees");
        var faite = Assert.Single(completees!, o => o.TacheId == id);
        Assert.Equal("Alain", faite.CompleteePar!.NomAffichage);

        var deuxieme = await client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null);
        Assert.Equal(HttpStatusCode.Conflict, deuxieme.StatusCode);

        var annuler = await client.PostAsync(
            $"/api/occurrences/{occurrence.Id}/annuler-completion", null);
        Assert.Equal(HttpStatusCode.NoContent, annuler.StatusCode);
        Assert.Equal(Aujourdhui, (await OccurrenceEnAttente(client, id)).Echeance);
    }

    [Fact]
    public async Task CompleterUneRecurrente_MaterialiseLaProchaine()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test récurrence intervalle", null, Aujourdhui, null, null, null, "Fixe",
            new RecurrenceDto("Intervalle", null, null, null, null, null, 7,
                null, null, null, null, true)));
        var occurrence = await OccurrenceEnAttente(client, id);

        var completer = await client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null);
        Assert.Equal(HttpStatusCode.NoContent, completer.StatusCode);

        var prochaine = await OccurrenceEnAttente(client, id);
        Assert.NotEqual(occurrence.Id, prochaine.Id);
        Assert.Equal(Aujourdhui.AddDays(7), prochaine.Echeance);
    }

    [Fact]
    public async Task PasserUnePonctuelle_Repond400()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test passer ponctuelle", null, Aujourdhui, null, null, null, null, null));
        var occurrence = await OccurrenceEnAttente(client, id);

        var passer = await client.PostAsync($"/api/occurrences/{occurrence.Id}/passer", null);
        Assert.Equal(HttpStatusCode.BadRequest, passer.StatusCode);
    }

    [Fact]
    public async Task Reporter_RefuseLePasse_EtGlisseLEcheance()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test report", null, Aujourdhui, null, null, null, null, null));
        var occurrence = await OccurrenceEnAttente(client, id);

        var passe = await client.PostAsJsonAsync(
            $"/api/occurrences/{occurrence.Id}/reporter",
            new ReporterRequete(Aujourdhui.AddDays(-1)));
        Assert.Equal(HttpStatusCode.BadRequest, passe.StatusCode);

        var demain = await client.PostAsJsonAsync(
            $"/api/occurrences/{occurrence.Id}/reporter",
            new ReporterRequete(Aujourdhui.AddDays(1)));
        Assert.Equal(HttpStatusCode.NoContent, demain.StatusCode);
        Assert.Equal(Aujourdhui.AddDays(1), (await OccurrenceEnAttente(client, id)).Echeance);
    }

    [Fact]
    public async Task NotesPostHoc_ExigentUneOccurrenceCompletee()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test notes", null, Aujourdhui, null, null, null, null, null));
        var occurrence = await OccurrenceEnAttente(client, id);

        var tropTot = await client.PutAsJsonAsync(
            $"/api/occurrences/{occurrence.Id}/notes", new NotesRequete("coût : 12 $"));
        Assert.Equal(HttpStatusCode.Conflict, tropTot.StatusCode);

        (await client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null))
            .EnsureSuccessStatusCode();
        var apresCompletion = await client.PutAsJsonAsync(
            $"/api/occurrences/{occurrence.Id}/notes", new NotesRequete("coût : 12 $"));
        Assert.Equal(HttpStatusCode.NoContent, apresCompletion.StatusCode);

        var completees = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            "/api/occurrences?filtre=completees");
        Assert.Equal("coût : 12 $", completees!.Single(o => o.TacheId == id).Notes);
    }

    [Fact]
    public async Task SupprimerLaTache_EmporteSesOccurrences()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test suppression", null, Aujourdhui, null, null, null, null, null));
        _ = await OccurrenceEnAttente(client, id);

        var suppression = await client.DeleteAsync($"/api/taches/{id}");
        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);

        var restantes = await client.GetFromJsonAsync<List<OccurrenceDto>>("/api/occurrences");
        Assert.DoesNotContain(restantes!, o => o.TacheId == id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/taches/{id}")).StatusCode);
    }
}
