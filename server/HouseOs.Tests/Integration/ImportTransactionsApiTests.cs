using System.Net;
using System.Net.Http.Json;
using System.Text;
using HouseOs.Api.Features.Budget;

namespace HouseOs.Tests.Integration;

/// <summary>
/// L'endpoint d'import bancaire (POST /api/budget/import) de bout en bout : formats,
/// encodage, dédup (FITID, numéro de séquence, doublons intra-fichier), ancrage.
/// </summary>
[Collection("integration")]
public class ImportTransactionsApiTests(HouseOsFactory factory)
{
    /// <summary>Base partagée par la collection — même ancrage que BudgetApiTests.</summary>
    private static async Task AncrerAsync(HttpClient client)
    {
        var creation = await client.PostAsJsonAsync("/api/budget/compte", new
        {
            nom = "Fonds de prévoyance",
            institution = "Desjardins",
            soldeInitial = 10_000m,
            dateAncrage = "2026-01-01",
        });
        if (creation.StatusCode == HttpStatusCode.Conflict)
        {
            return; // déjà ancré par un test précédent
        }
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
    }

    private static MultipartFormDataContent Formulaire(HttpContent contenu, string nomFichier = "releve.csv")
    {
        return new MultipartFormDataContent { { contenu, "fichier", nomFichier } };
    }

    private static async Task<HttpResponseMessage> TeleverserAsync(
        HttpClient client, string contenu, string nomFichier = "releve.csv")
    {
        using var formulaire = Formulaire(new StringContent(contenu, Encoding.UTF8, "text/csv"), nomFichier);
        return await client.PostAsync("/api/budget/import", formulaire);
    }

    private static async Task<RapportImportDto> ImporterAsync(
        HttpClient client, string contenu, string nomFichier = "releve.csv")
    {
        var reponse = await TeleverserAsync(client, contenu, nomFichier);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<RapportImportDto>())!;
    }

    private static async Task<List<TransactionDto>> InboxAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<TransactionDto>>("/api/budget/transactions"))!;

    [Fact]
    public async Task ImportCsv_Heureux_CreeLesTransactionsDansLInbox()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");

        var rapport = await ImporterAsync(client, $"""
            Date,Description,Montant
            2026-08-20,EPICERIE {marqueur},-80.25
            2026-08-21,DEPOT {marqueur},500.00
            """);

        Assert.Equal(new RapportImportDto(2, 0, 0), rapport);
        var inbox = await InboxAsync(client);
        var epicerie = Assert.Single(inbox, t => t.Description == $"EPICERIE {marqueur}");
        Assert.Equal(-80.25m, epicerie.Montant);
        Assert.Equal(new DateOnly(2026, 8, 20), epicerie.Date);
        Assert.Equal(500.00m, Assert.Single(inbox, t => t.Description == $"DEPOT {marqueur}").Montant);
    }

    [Fact]
    public async Task Reimporter_LeMemeFichierAccWeb_EstSansEffet()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");
        var csv = $"""
            "EOP";"2026-08-24";"213";"CANADIAN TIRE {marqueur}";"84,12";"";"12 395,88"
            "EOP";"2026-08-26";"214";"VIR INTERAC {marqueur}";"";"675,00";"13 070,88"
            """;

        var premier = await ImporterAsync(client, csv);
        var second = await ImporterAsync(client, csv);

        Assert.Equal(new RapportImportDto(2, 0, 0), premier);
        Assert.Equal(new RapportImportDto(0, 2, 0), second);
    }

    [Fact]
    public async Task DeuxPleinsIdentiquesLeMemeJour_AvecNumerosDeSequence_SurviventTousLesDeux()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");
        var csv = $"""
            "EOP";"2026-08-22";"301";"PLEIN ESSENCE {marqueur}";"60,00";"";"12 000,00"
            "EOP";"2026-08-22";"302";"PLEIN ESSENCE {marqueur}";"60,00";"";"11 940,00"
            """;

        var premier = await ImporterAsync(client, csv);
        var second = await ImporterAsync(client, csv);

        Assert.Equal(new RapportImportDto(2, 0, 0), premier);
        Assert.Equal(new RapportImportDto(0, 2, 0), second); // réimport idempotent
        var pleins = (await InboxAsync(client))
            .Where(t => t.Description == $"PLEIN ESSENCE {marqueur}").ToList();
        Assert.Equal(2, pleins.Count);
        Assert.All(pleins, t => Assert.Equal(-60m, t.Montant));
    }

    [Fact]
    public async Task DeuxLignesIdentiques_SansNumeroDeSequence_SurviventAussi()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");
        var csv = $"""
            Date,Description,Montant
            2026-08-22,PLEIN ESSENCE {marqueur},-60.00
            2026-08-22,PLEIN ESSENCE {marqueur},-60.00
            """;

        var premier = await ImporterAsync(client, csv);
        var second = await ImporterAsync(client, csv);

        Assert.Equal(new RapportImportDto(2, 0, 0), premier); // rang d'occurrence dans la clé
        Assert.Equal(new RapportImportDto(0, 2, 0), second);
    }

    [Fact]
    public async Task ReimporterEnFormatAccWeb_DesTransactionsDejaImporteesSansSequence_ResteSansEffet()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");

        // Importées d'abord sans numéro de séquence (clé de forme historique)…
        var premier = await ImporterAsync(client, $"""
            Date,Description,Montant
            2026-08-23,RENO DEPOT {marqueur},-45.00
            """);
        // …puis le même relevé au format AccWeb complet, séquence incluse.
        var second = await ImporterAsync(client,
            $"\"EOP\";\"2026-08-23\";\"401\";\"RENO DEPOT {marqueur}\";\"45,00\";\"\";\"9 000,00\"");

        Assert.Equal(new RapportImportDto(1, 0, 0), premier);
        Assert.Equal(new RapportImportDto(0, 1, 0), second);
    }

    [Fact]
    public async Task FichierVide_Repond400()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);

        using var formulaire = Formulaire(new ByteArrayContent([]));
        var reponse = await client.PostAsync("/api/budget/import", formulaire);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task FichierMalforme_Repond400_AvecUnMessageClair()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);

        var reponse = await TeleverserAsync(client, "bonjour\nrien à voir avec un relevé");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("export CSV", await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CsvMontantSoldeAmbigu_Repond400_PlutotQueDeviner()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);

        var reponse = await TeleverserAsync(client, """
            2026-08-24,MYSTERE UN,10.00,20.00
            2026-08-25,MYSTERE DEUX,30.00,90.00
            """);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("ambigu", await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TransactionsAnterieuresALAncrage_SontEcartees()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");

        var rapport = await ImporterAsync(client, $"""
            Date,Description,Montant
            2025-11-30,VIEILLE {marqueur},-10.00
            2026-08-20,RECENTE {marqueur},-20.00
            """);

        Assert.Equal(new RapportImportDto(1, 0, 1), rapport);
        var inbox = await InboxAsync(client);
        Assert.DoesNotContain(inbox, t => t.Description == $"VIEILLE {marqueur}");
        Assert.Single(inbox, t => t.Description == $"RECENTE {marqueur}");
    }

    [Fact]
    public async Task ExportWindows1252_LesAccentsSurvivent()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");
        // « DÉPÔT » encodé en Windows-1252 (identique à Latin-1 pour ces caractères) :
        // des octets invalides en UTF-8 — le repli d'encodage doit les préserver.
        var octets = Encoding.Latin1.GetBytes(
            $"Date;Description;Montant\n2026-08-21;DÉPÔT SALAIRE {marqueur};675,00");

        using var formulaire = Formulaire(new ByteArrayContent(octets));
        var reponse = await client.PostAsync("/api/budget/import", formulaire);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Single(await InboxAsync(client), t => t.Description == $"DÉPÔT SALAIRE {marqueur}");
    }

    [Fact]
    public async Task FitidDe200Caracteres_ImporteSansErreur_EtResteIdempotent()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");
        var fitidVerbeux = marqueur + new string('X', 200 - marqueur.Length);
        var ofx = $"""
            OFXHEADER:100
            <OFX>
            <BANKTRANLIST>
            <STMTTRN>
            <DTPOSTED>20260825
            <TRNAMT>-129.99
            <FITID>{fitidVerbeux}
            <NAME>INSTITUTION VERBEUSE {marqueur}
            </STMTTRN>
            </BANKTRANLIST>
            </OFX>
            """;

        var premier = await ImporterAsync(client, ofx, "releve.ofx");
        var second = await ImporterAsync(client, ofx, "releve.ofx");

        Assert.Equal(new RapportImportDto(1, 0, 0), premier);
        Assert.Equal(new RapportImportDto(0, 1, 0), second);
        Assert.Single(await InboxAsync(client), t => t.Description == $"INSTITUTION VERBEUSE {marqueur}");
    }
}
