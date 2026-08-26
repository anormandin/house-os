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

    [Fact]
    public async Task FiltreInconnu_Repond400_AuLieuDeToutRetourner()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.GetAsync("/api/occurrences?filtre=pending");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task FaitesSansBornes_Repond400_AuLieuDUnVideSilencieux()
    {
        var client = await factory.ClientConnecte();

        // Avant la garde : SQL comparé à null → 200 avec une liste vide, indébogable.
        var reponse = await client.GetAsync("/api/occurrences?filtre=faites");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/occurrences/{0}/completer")]
    [InlineData("POST", "/api/occurrences/{0}/annuler-completion")]
    [InlineData("POST", "/api/occurrences/{0}/passer")]
    [InlineData("DELETE", "/api/taches/{0}")]
    public async Task IdInconnu_Repond404(string methode, string gabarit)
    {
        var client = await factory.ClientConnecte();
        var route = string.Format(gabarit, Guid.NewGuid());

        var reponse = methode == "DELETE"
            ? await client.DeleteAsync(route)
            : await client.PostAsync(route, null);

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task Bilan_LaBorneDeEstInclusive_EtLaBorneAExclusive()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test bornes bilan", null, Aujourdhui, null, null, null, null, null));
        var occurrence = await OccurrenceEnAttente(client, id);
        (await client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null))
            .EnsureSuccessStatusCode();
        var completees = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            "/api/occurrences?filtre=completees");
        var instant = completees!.Single(o => o.TacheId == id).CompleteeLe!.Value;

        string Format(DateTimeOffset i) => Uri.EscapeDataString(i.ToString("o"));
        var inclusDepuisLaBorne = await client.GetFromJsonAsync<List<DateTimeOffset>>(
            $"/api/journal/bilan?de={Format(instant)}&a={Format(instant.AddMinutes(1))}");
        var excluALaBorne = await client.GetFromJsonAsync<List<DateTimeOffset>>(
            $"/api/journal/bilan?de={Format(instant.AddMinutes(-1))}&a={Format(instant)}");

        // Sémantique [de, a) : une complétion à minuit pile compte dans UNE semaine.
        Assert.Contains(instant, inclusDepuisLaBorne!);
        Assert.DoesNotContain(instant, excluALaBorne!);
    }

    [Fact]
    public async Task SupprimerUneTacheCompletee_LaisseLaCompletionDansLeBilan()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test bilan survivant", null, Aujourdhui, null, null, null, null, null));
        var occurrence = await OccurrenceEnAttente(client, id);
        (await client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null))
            .EnsureSuccessStatusCode();
        var completees = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            "/api/occurrences?filtre=completees");
        var instant = completees!.Single(o => o.TacheId == id).CompleteeLe!.Value;

        (await client.DeleteAsync($"/api/taches/{id}")).EnsureSuccessStatusCode();

        // Le journal n'a pas de FK et survit : le bilan garde la complétion, alors
        // que le filtre « faites » (joint aux occurrences) ne la voit plus. Divergence
        // assumée — l'histogramme compte le travail fait, même sur une tâche disparue.
        string Format(DateTimeOffset i) => Uri.EscapeDataString(i.ToString("o"));
        var bilan = await client.GetFromJsonAsync<List<DateTimeOffset>>(
            $"/api/journal/bilan?de={Format(instant.AddSeconds(-1))}&a={Format(instant.AddSeconds(1))}");
        Assert.Contains(instant, bilan!);
    }

    [Fact]
    public async Task DeuxCompletionsParalleles_NeCreentQuUneSeuleSuivante()
    {
        var client = await factory.ClientConnecte();
        var id = await CreerTache(client, new CreerTacheRequete(
            "Test course de complétion", null, Aujourdhui, null, null, null, "Fixe",
            new RecurrenceDto("Intervalle", null, null, null, null, null, 7,
                null, null, null, null, true)));
        var occurrence = await OccurrenceEnAttente(client, id);

        // Deux clics simultanés : l'index unique « une seule en attente par tâche »
        // tranche — un 204, un 409, jamais deux journaux ni deux suivantes.
        var (premiere, deuxieme) = (
            client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null),
            client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null));
        await Task.WhenAll(premiere, deuxieme);

        var statuts = new[] { premiere.Result.StatusCode, deuxieme.Result.StatusCode };
        Assert.Single(statuts, s => s == HttpStatusCode.NoContent);
        Assert.Single(statuts, s => s == HttpStatusCode.Conflict);

        var suivante = await OccurrenceEnAttente(client, id); // Single → une seule en attente
        Assert.Equal(Aujourdhui.AddDays(7), suivante.Echeance);
        var faites = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            "/api/occurrences?filtre=completees");
        Assert.Single(faites!, o => o.TacheId == id);
    }

    /// <summary>Téléverse un petit PDF et retourne son id (pour les liens tâche→document).</summary>
    private static async Task<Guid> TeleverserDocument(HttpClient client)
    {
        var fichier = new ByteArrayContent("%PDF-1.4\n%%EOF"u8.ToArray());
        fichier.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        var formulaire = new MultipartFormDataContent { { fichier, "fichier", "reference.pdf" } };
        var reponse = await client.PostAsync("/api/documents", formulaire);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<CorpsId>())!.Id;
    }

    [Fact]
    public async Task DocumentsLies_CreationPutEtSuppression_FontLAllerRetour()
    {
        var client = await factory.ClientConnecte();
        var doc1 = await TeleverserDocument(client);
        var doc2 = await TeleverserDocument(client);
        var id = await CreerTache(client, new CreerTacheRequete(
            "Tâche avec références", null, Aujourdhui, null, null, null, null, null, [doc1, doc2]));

        var detail = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{id}");
        Assert.Equal(2, detail!.DocumentIds.Length);
        Assert.Contains(doc1, detail.DocumentIds);
        Assert.Contains(doc2, detail.DocumentIds);

        // PUT sans documentIds (null) : les liens survivent.
        var sansListe = await client.PutAsJsonAsync($"/api/taches/{id}", new ModifierTacheRequete(
            "Tâche avec références", null, Aujourdhui, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.NoContent, sansListe.StatusCode);
        detail = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{id}");
        Assert.Equal(2, detail!.DocumentIds.Length);

        // Supprimer un document retire le lien sans toucher la tâche.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/documents/{doc2}")).StatusCode);
        detail = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{id}");
        Assert.Equal([doc1], detail!.DocumentIds);

        // PUT avec [] : tout délier ; le document restant survit.
        var deliaison = await client.PutAsJsonAsync($"/api/taches/{id}", new ModifierTacheRequete(
            "Tâche avec références", null, Aujourdhui, null, null, null, null, null, []));
        Assert.Equal(HttpStatusCode.NoContent, deliaison.StatusCode);
        detail = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{id}");
        Assert.Empty(detail!.DocumentIds);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/documents/{doc1}")).StatusCode);
    }

    [Fact]
    public async Task DocumentsLies_IdInconnu_Repond400()
    {
        var client = await factory.ClientConnecte();
        var reponse = await client.PostAsJsonAsync("/api/taches", new CreerTacheRequete(
            "Tâche au lien fantôme", null, null, null, null, null, null, null, [Guid.NewGuid()]));
        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }
}
