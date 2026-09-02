using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Courriel;
using HouseOs.Api.Features.Documents;
using HouseOs.Api.Features.Synchro;
using HouseOs.Tests.Features.Courriel;
using static HouseOs.Tests.Features.Courriel.FabriqueCourriels;

namespace HouseOs.Tests.Integration;

/// <summary>
/// La tranche courriel par la vraie pile HTTP : relevé déclenché, boîte À classer,
/// aperçu d'un .eml, classement, et l'événement fin qui fait le toast.
/// </summary>
[Collection("integration")]
public class CourrielApiTests(HouseOsFactory factory)
{
    private sealed record Rapport(bool Actif, int NbCourriels, int NbDocuments, int NbIgnores, string[] Erreurs);

    [Fact]
    public async Task Relever_puis_classer_de_bout_en_bout()
    {
        var client = await factory.ClientConnecte();
        factory.Courriels.Vider();
        factory.Synchro.Vider();
        factory.Courriels.Deposer(HtmlSeul(sujet: "Commande Amazon #123", messageId: $"amazon-{Guid.NewGuid():N}@exemple.test"));

        var releve = await client.PostAsync("/api/documents/relever-courriels", null);
        Assert.Equal(HttpStatusCode.OK, releve.StatusCode);
        var rapport = (await releve.Content.ReadFromJsonAsync<Rapport>())!;
        Assert.Equal(1, rapport.NbDocuments);
        Assert.Empty(rapport.Erreurs);
        Assert.Contains(factory.Synchro.Fins, e => e.Genre == EvenementSynchro.GenreDocumentsRecus);

        var aClasser = (await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?aClasser=true"))!;
        var document = Assert.Single(aClasser, d => d.Titre == "Commande Amazon #123");
        Assert.True(document.AClasser);
        Assert.NotNull(document.ImportCourrielId);
        Assert.Equal(EnregistrementDocument.TypeMimeCourriel, document.TypeMime);

        var apercu = (await client.GetFromJsonAsync<CourrielDocumentDto>($"/api/documents/{document.Id}/courriel"))!;
        Assert.Equal("Commande Amazon #123", apercu.Sujet);
        Assert.Contains("alain.normandin@gmail.com", apercu.De);
        Assert.Contains("Total : 129,95 $", apercu.Texte);
        Assert.Empty(apercu.PiecesJointes);

        // Le fichier se télécharge comme n'importe quel document.
        var fichier = await client.GetAsync($"/api/documents/{document.Id}/fichier");
        Assert.Equal(HttpStatusCode.OK, fichier.StatusCode);
        Assert.Equal(EnregistrementDocument.TypeMimeCourriel, fichier.Content.Headers.ContentType!.MediaType);

        var classement = await client.PutAsJsonAsync($"/api/documents/{document.Id}",
            new DocumentRequete("Commande Amazon — étagère", "Facture", null, null, null, null, null, null, AClasser: false));
        Assert.Equal(HttpStatusCode.NoContent, classement.StatusCode);
        var encoreAClasser = (await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?aClasser=true"))!;
        Assert.DoesNotContain(encoreAClasser, d => d.Id == document.Id);
        var classes = (await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?aClasser=false"))!;
        Assert.Contains(classes, d => d.Id == document.Id && d.Titre == "Commande Amazon — étagère");

        // Un simple enregistrement (sans le champ) ne touche pas au classement.
        var edition = await client.PutAsJsonAsync($"/api/documents/{document.Id}",
            new DocumentRequete("Commande Amazon — étagère 2", "Facture", null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.NoContent, edition.StatusCode);
        Assert.DoesNotContain(
            (await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?aClasser=true"))!,
            d => d.Id == document.Id);

        await client.DeleteAsync($"/api/documents/{document.Id}");
    }

    [Fact]
    public async Task Le_meme_courriel_deux_fois_est_ignore()
    {
        var client = await factory.ClientConnecte();
        factory.Courriels.Vider();
        var messageId = $"dup-{Guid.NewGuid():N}@exemple.test";
        factory.Courriels.Deposer(AvecPdf(sujet: "Reçu doublon", messageId: messageId));
        var premier = (await (await client.PostAsync("/api/documents/relever-courriels", null))
            .Content.ReadFromJsonAsync<Rapport>())!;
        Assert.Equal(1, premier.NbDocuments);

        factory.Courriels.Deposer(AvecPdf(sujet: "Reçu doublon", messageId: messageId));
        var second = (await (await client.PostAsync("/api/documents/relever-courriels", null))
            .Content.ReadFromJsonAsync<Rapport>())!;

        Assert.Equal(0, second.NbDocuments);
        Assert.Equal(1, second.NbIgnores);
        Assert.Empty(factory.Courriels.Cles);
        var documents = (await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents"))!;
        var id = Assert.Single(documents, d => d.Titre == "Facture_48211" && d.Notes!.Contains("Reçu doublon")).Id;
        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task L_apercu_courriel_d_un_document_ordinaire_est_introuvable()
    {
        var client = await factory.ClientConnecte();
        var contenu = new MultipartFormDataContent();
        var png = new ByteArrayContent(PetitPng());
        png.Headers.ContentType = new("image/png");
        contenu.Add(png, "fichier", "photo.png");
        var creation = await client.PostAsync("/api/documents", contenu);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var id = (await creation.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var apercu = await client.GetAsync($"/api/documents/{id}/courriel");

        Assert.Equal(HttpStatusCode.NotFound, apercu.StatusCode);
        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task Sans_session_le_releve_est_refuse()
    {
        var reponse = await factory.CreateClient().PostAsync("/api/documents/relever-courriels", null);
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Depot_inactif_repond_503()
    {
        var client = await factory.ClientConnecte();
        factory.Courriels.Actif = false;
        try
        {
            var reponse = await client.PostAsync("/api/documents/relever-courriels", null);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, reponse.StatusCode);
        }
        finally
        {
            factory.Courriels.Actif = true;
        }
    }
}
