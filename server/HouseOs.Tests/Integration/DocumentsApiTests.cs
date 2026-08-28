using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HouseOs.Api.Features.Documents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HouseOs.Tests.Integration;

/// <summary>
/// La tranche fichiers de bout en bout : upload multipart, validations avant écriture
/// disque, miniatures, suppression — avec le dossier fichiers isolé du dépôt.
/// </summary>
[Collection("integration")]
public class DocumentsApiTests(HouseOsFactory factory)
{
    private sealed record CorpsId(Guid Id);

    private static byte[] PetitPng()
    {
        using var image = new Image<Rgba32>(4, 4);
        using var memoire = new MemoryStream();
        image.SaveAsPng(memoire);
        return memoire.ToArray();
    }

    private static MultipartFormDataContent Formulaire(
        byte[] contenu, string nomFichier, string typeMime,
        (string Nom, string Valeur)[]? champs = null)
    {
        var fichier = new ByteArrayContent(contenu);
        fichier.Headers.ContentType = MediaTypeHeaderValue.Parse(typeMime);
        var formulaire = new MultipartFormDataContent { { fichier, "fichier", nomFichier } };
        foreach (var (nom, valeur) in champs ?? [])
        {
            formulaire.Add(new StringContent(valeur), nom);
        }
        return formulaire;
    }

    private async Task<Guid> Televerser(HttpClient client, string nomFichier = "photo.png")
    {
        var reponse = await client.PostAsync(
            "/api/documents", Formulaire(PetitPng(), nomFichier, "image/png"));
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<CorpsId>())!.Id;
    }

    [Fact]
    public async Task Upload_Telechargement_Miniature_Suppression_NettoientLeDisque()
    {
        var client = await factory.ClientConnecte();
        var id = await Televerser(client);

        var fichier = await client.GetAsync($"/api/documents/{id}/fichier");
        fichier.EnsureSuccessStatusCode();
        var miniature = await client.GetAsync($"/api/documents/{id}/miniature");
        miniature.EnsureSuccessStatusCode();
        Assert.Equal("image/webp", miniature.Content.Headers.ContentType!.MediaType);

        var suppression = await client.DeleteAsync($"/api/documents/{id}");
        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);

        // Le fichier ET sa miniature en cache disparaissent du disque.
        Assert.Empty(Directory.GetFiles(factory.DossierFichiers)
            .Where(f => Path.GetFileName(f).StartsWith(id.ToString("N"))));
        Assert.Empty(Directory.GetFiles(Path.Combine(factory.DossierFichiers, "miniatures"))
            .Where(f => Path.GetFileName(f).StartsWith(id.ToString("N"))));
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/documents/{id}")).StatusCode);
    }

    [Fact]
    public async Task Upload_FichierVide_Repond400()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsync(
            "/api/documents", Formulaire([], "vide.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Upload_MimeEnMajusculesAvecParametres_EstAccepte()
    {
        var client = await factory.ClientConnecte();

        // « IMAGE/PNG; charset=utf-8 » : les types MIME sont insensibles à la casse
        // et peuvent porter des paramètres — un faux rejet punit un client légitime.
        var reponse = await client.PostAsync(
            "/api/documents", Formulaire(PetitPng(), "photo.png", "IMAGE/PNG; charset=utf-8"));

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
    }

    [Fact]
    public async Task Upload_ContenuQuiNeCorrespondPasAuType_Repond400()
    {
        var client = await factory.ClientConnecte();

        // Le Content-Type client est déclaratif : un PNG annoncé PDF (ou du HTML
        // annoncé PNG) doit être refusé aux magic bytes, pas cru sur parole.
        var pngEnPdf = await client.PostAsync(
            "/api/documents", Formulaire(PetitPng(), "manuel.pdf", "application/pdf"));
        var htmlEnPng = await client.PostAsync(
            "/api/documents", Formulaire("<html>salut</html>"u8.ToArray(), "photo.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, pngEnPdf.StatusCode);
        Assert.Contains("ne correspond pas", await pngEnPdf.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, htmlEnPng.StatusCode);
    }

    [Fact]
    public async Task Upload_AvecEquipementInconnu_Repond400_SansFichierOrphelin()
    {
        var client = await factory.ClientConnecte();
        var avant = Directory.GetFiles(factory.DossierFichiers).Length;

        var reponse = await client.PostAsync("/api/documents", Formulaire(
            PetitPng(), "photo.png", "image/png",
            [("equipementId", Guid.NewGuid().ToString())]));

        // La validation précède l'écriture disque : ni 500, ni fichier jamais réclamé.
        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal(avant, Directory.GetFiles(factory.DossierFichiers).Length);
    }

    [Fact]
    public async Task Upload_NomDeFichierAvecTraversee_EstNeutralise()
    {
        var client = await factory.ClientConnecte();

        var id = await Televerser(client, nomFichier: @"..\..\etc\passwd.png");

        // Le nom d'affichage perd ses séparateurs ; le nom disque est id + extension
        // dérivée du MIME — jamais le nom du client.
        var telechargement = await client.GetAsync($"/api/documents/{id}/fichier");
        telechargement.EnsureSuccessStatusCode();
        Assert.Equal("passwd.png", telechargement.Content.Headers.ContentDisposition!.FileNameStar);
        Assert.True(File.Exists(Path.Combine(factory.DossierFichiers, id.ToString("N") + ".png")));
    }

    [Fact]
    public async Task Upload_TitreTropLong_Repond400()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsync("/api/documents", Formulaire(
            PetitPng(), "photo.png", "image/png",
            [("titre", new string('t', 201))]));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task FichierDisparuDuDisque_Repond404_SansPlanter()
    {
        var client = await factory.ClientConnecte();
        var id = await Televerser(client);
        File.Delete(Path.Combine(factory.DossierFichiers, id.ToString("N") + ".png"));

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/documents/{id}/fichier")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/documents/{id}/miniature")).StatusCode);
        // La suppression réussit quand même : la fiche ne doit pas devenir immortelle.
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/documents/{id}")).StatusCode);
    }

    [Fact]
    public async Task MiniatureDUnPdf_Repond404()
    {
        var client = await factory.ClientConnecte();
        var pdf = "%PDF-1.4\n%%EOF"u8.ToArray();
        var creation = await client.PostAsync(
            "/api/documents", Formulaire(pdf, "manuel.pdf", "application/pdf"));
        var id = (await creation.Content.ReadFromJsonAsync<CorpsId>())!.Id;

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/documents/{id}/miniature")).StatusCode);
    }

    private sealed record DocumentLu(Guid Id, string Titre, string? Dossier);

    [Fact]
    public async Task Dossier_UploadPutEtFiltre_FontLAllerRetour()
    {
        var client = await factory.ClientConnecte();
        // Valeur unique par exécution : la base est partagée entre les tests.
        var dossier = $"17 rue de la Colline {Guid.NewGuid():N}";

        var creation = await client.PostAsync("/api/documents", Formulaire(
            PetitPng(), "promesse.png", "image/png",
            [("dossier", $"  {dossier}  ")]));
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var id = (await creation.Content.ReadFromJsonAsync<CorpsId>())!.Id;

        // Le trim est appliqué et la valeur ressort telle quelle du GET filtré.
        var filtres = await client.GetFromJsonAsync<List<DocumentLu>>(
            $"/api/documents?dossier={Uri.EscapeDataString(dossier)}");
        var lu = Assert.Single(filtres!);
        Assert.Equal(id, lu.Id);
        Assert.Equal(dossier, lu.Dossier);

        // Le PUT (fiche complète) remplace le dossier ; vide → null.
        var modification = await client.PutAsJsonAsync($"/api/documents/{id}", new
        {
            titre = "Promesse d'achat",
            categorie = "Contrat",
            dossier = "   ",
        });
        Assert.Equal(HttpStatusCode.NoContent, modification.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<DocumentLu>>(
            $"/api/documents?dossier={Uri.EscapeDataString(dossier)}"))!);

        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task Telechargement_ServiEnAttachment_AvecLeTypeMime()
    {
        var client = await factory.ClientConnecte();
        var id = await Televerser(client, nomFichier: "recu.png");

        var reponse = await client.GetAsync($"/api/documents/{id}/fichier");

        // Le contrat de téléchargement : type MIME stocké et attachment forcé
        // (jamais de rendu inline d'un contenu téléversé).
        reponse.EnsureSuccessStatusCode();
        Assert.Equal("image/png", reponse.Content.Headers.ContentType!.MediaType);
        var disposition = reponse.Content.Headers.ContentDisposition!;
        Assert.Equal("attachment", disposition.DispositionType);
        Assert.Equal("recu.png", disposition.FileNameStar);
        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task Upload_ChampsMalformes_Repondent400_AuLieuDEtreAvales()
    {
        var client = await factory.ClientConnecte();

        // Avant : equipementId=abc ou echeance=06/10/2026 étaient silencieusement
        // remplacés par null — document créé sans lien ni échéance, donnée perdue.
        var guid = await client.PostAsync("/api/documents", Formulaire(
            PetitPng(), "photo.png", "image/png", [("equipementId", "abc")]));
        Assert.Equal(HttpStatusCode.BadRequest, guid.StatusCode);

        var date = await client.PostAsync("/api/documents", Formulaire(
            PetitPng(), "photo.png", "image/png", [("echeance", "06/10/2026")]));
        Assert.Equal(HttpStatusCode.BadRequest, date.StatusCode);
        Assert.Contains("YYYY-MM-DD", await date.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Put_AvecLienInconnu_Repond400()
    {
        var client = await factory.ClientConnecte();
        var id = await Televerser(client);

        // Sans validation des FK, la violation de contrainte remonterait un 500.
        var equipement = await client.PutAsJsonAsync($"/api/documents/{id}",
            new { titre = "Photo", categorie = "Photo", equipementId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.BadRequest, equipement.StatusCode);

        var zone = await client.PutAsJsonAsync($"/api/documents/{id}",
            new { titre = "Photo", categorie = "Photo", zoneId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.BadRequest, zone.StatusCode);
        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task Put_TitreOuNotesTropLongs_Repond400()
    {
        var client = await factory.ClientConnecte();
        var id = await Televerser(client);

        // Mêmes bornes que le POST (colonnes varchar) : 400 clair, jamais 500.
        var titre = await client.PutAsJsonAsync($"/api/documents/{id}",
            new { titre = new string('t', 201), categorie = "Photo" });
        Assert.Equal(HttpStatusCode.BadRequest, titre.StatusCode);

        var notes = await client.PutAsJsonAsync($"/api/documents/{id}",
            new { titre = "Photo", categorie = "Photo", notes = new string('n', 2001) });
        Assert.Equal(HttpStatusCode.BadRequest, notes.StatusCode);
        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task Categorie_NumeriqueRefusee_MinusculeAcceptee()
    {
        var client = await factory.ClientConnecte();
        var id = await Televerser(client);

        // « 999 » passerait Enum.TryParse et serait persisté puis affiché tel quel.
        var numerique = await client.PutAsJsonAsync($"/api/documents/{id}",
            new { titre = "Photo", categorie = "999" });
        Assert.Equal(HttpStatusCode.BadRequest, numerique.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/documents?categorie=999")).StatusCode);

        // La casse, elle, est tolérée — même contrat que Zones et Icônes.
        var minuscule = await client.PutAsJsonAsync($"/api/documents/{id}",
            new { titre = "Photo", categorie = "contrat" });
        Assert.Equal(HttpStatusCode.NoContent, minuscule.StatusCode);
        (await client.GetAsync("/api/documents?categorie=contrat")).EnsureSuccessStatusCode();
        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task Dossier_TropLong_Repond400_AuPostEtAuPut()
    {
        var client = await factory.ClientConnecte();
        var tropLong = new string('d', 101);

        var creation = await client.PostAsync("/api/documents", Formulaire(
            PetitPng(), "photo.png", "image/png", [("dossier", tropLong)]));
        Assert.Equal(HttpStatusCode.BadRequest, creation.StatusCode);

        var id = await Televerser(client);
        var modification = await client.PutAsJsonAsync($"/api/documents/{id}", new
        {
            titre = "Photo",
            categorie = "Photo",
            dossier = tropLong,
        });
        Assert.Equal(HttpStatusCode.BadRequest, modification.StatusCode);
        await client.DeleteAsync($"/api/documents/{id}");
    }
}
