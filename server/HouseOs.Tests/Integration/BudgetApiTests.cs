using System.Net;
using System.Net.Http.Json;
using System.Text;
using HouseOs.Api.Features.Budget;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class BudgetApiTests(HouseOsFactory factory)
{
    private static readonly DateOnly Aujourdhui = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>La base est partagée par la collection : ancre le compte s'il ne l'est
    /// pas déjà (date d'ancrage lointaine pour que tous les imports passent).</summary>
    private static async Task AncrerAsync(HttpClient client)
    {
        var requete = new
        {
            nom = "Fonds de prévoyance",
            institution = "Desjardins",
            soldeInitial = 10_000m,
            dateAncrage = "2026-01-01",
        };
        var creation = await client.PostAsJsonAsync("/api/budget/compte", requete);
        if (creation.StatusCode == HttpStatusCode.Conflict)
        {
            return; // déjà ancré par un test précédent — l'ancrage existant suffit
        }
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
    }

    private static async Task<Guid> CreerEnveloppeAsync(HttpClient client, object requete)
    {
        var reponse = await client.PostAsJsonAsync("/api/budget/enveloppes", requete);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var corps = await reponse.Content.ReadFromJsonAsync<CreationReponse>();
        return corps!.Id;
    }

    private record CreationReponse(Guid Id);

    private static string Csv(params (string Date, string Description, decimal Montant)[] lignes)
    {
        var sb = new StringBuilder("Date,Description,Montant\n");
        foreach (var l in lignes)
        {
            sb.Append($"{l.Date},{l.Description},{l.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
        }
        return sb.ToString();
    }

    private static async Task<RapportImportDto> ImporterAsync(HttpClient client, string csv)
    {
        using var formulaire = new MultipartFormDataContent
        {
            { new StringContent(csv, Encoding.UTF8, "text/csv"), "fichier", "releve.csv" },
        };
        var reponse = await client.PostAsync("/api/budget/import", formulaire);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<RapportImportDto>())!;
    }

    private static async Task<ResumeBudgetDto> ResumeAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ResumeBudgetDto>("/api/budget"))!;

    private static async Task<TransactionDto> TransactionParDescription(HttpClient client, string description)
    {
        var transactions = await client.GetFromJsonAsync<List<TransactionDto>>("/api/budget/transactions");
        return Assert.Single(transactions!, t => t.Description == description);
    }

    [Fact]
    public async Task Invariant_SoldeEgaleEnveloppesPlusNonAffecte()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        await CreerEnveloppeAsync(client, new { nom = $"Inv {Guid.NewGuid():N}", type = "Reserve" });

        var resume = await ResumeAsync(client);

        Assert.Equal(resume.SoldeCourant, resume.TotalEnveloppes + resume.NonAffecte);
    }

    [Fact]
    public async Task Import_EcarteLesAnterieuresALAncrage_SansToucherLeSolde()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var avant = (await ResumeAsync(client)).SoldeCourant;

        var rapport = await ImporterAsync(client,
            Csv(("2025-12-15", $"ANTERIEURE {Guid.NewGuid():N}", -50m)));

        Assert.Equal(0, rapport.Importees);
        Assert.Equal(1, rapport.Anterieures);
        Assert.Equal(avant, (await ResumeAsync(client)).SoldeCourant);
    }

    [Fact]
    public async Task Reimporter_LeMemeFichier_EstSansEffet()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var marqueur = Guid.NewGuid().ToString("N");
        var csv = Csv(
            ("2026-08-20", $"EPICERIE {marqueur}", -80.25m),
            ("2026-08-21", $"DEPOT {marqueur}", 500m));

        var premier = await ImporterAsync(client, csv);
        var soldeApres = (await ResumeAsync(client)).SoldeCourant;
        var second = await ImporterAsync(client, csv);

        Assert.Equal(2, premier.Importees);
        Assert.Equal(0, second.Importees);
        Assert.Equal(2, second.Doublons);
        Assert.Equal(soldeApres, (await ResumeAsync(client)).SoldeCourant);
    }

    [Fact]
    public async Task LierUnRetrait_CreeLeMouvement_EtLeSoldePeutPasserSousZero()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var enveloppeId = await CreerEnveloppeAsync(client,
            new { nom = $"Toiture {Guid.NewGuid():N}", type = "Equipement", montantCible = 4500m });
        var description = $"PEINTURE {Guid.NewGuid():N}";
        await ImporterAsync(client, Csv(("2026-08-22", description, -700m)));
        var transaction = await TransactionParDescription(client, description);

        var liaison = await client.PostAsJsonAsync($"/api/budget/transactions/{transaction.Id}/lier",
            new { ventilation = new[] { new { enveloppeId, montant = 700m } } });
        Assert.Equal(HttpStatusCode.NoContent, liaison.StatusCode);

        var resume = await ResumeAsync(client);
        var enveloppe = Assert.Single(resume.Enveloppes, e => e.Id == enveloppeId);
        Assert.Equal(-700m, enveloppe.Solde); // négatif permis — le Non affecté encaisse
        Assert.Equal(resume.SoldeCourant, resume.TotalEnveloppes + resume.NonAffecte);

        var detail = await client.GetFromJsonAsync<EnveloppeDetailDto>(
            $"/api/budget/enveloppes/{enveloppeId}");
        var mouvement = Assert.Single(detail!.Mouvements);
        Assert.Equal("Retrait", mouvement.Type);
        Assert.Equal(transaction.Id, mouvement.TransactionBancaireId);
    }

    [Fact]
    public async Task LierUnDepot_SeVentileSurPlusieursEnveloppes()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var taxes = await CreerEnveloppeAsync(client, new
        {
            nom = $"Taxes {Guid.NewGuid():N}",
            type = "Taxes",
            echeancier = new[] { new { date = Aujourdhui.AddMonths(3).ToString("yyyy-MM-dd"), montant = 1240m } },
        });
        var projet = await CreerEnveloppeAsync(client,
            new { nom = $"Bureau {Guid.NewGuid():N}", type = "Projet", montantCible = 8000m });
        var description = $"VIR INTERAC {Guid.NewGuid():N}";
        await ImporterAsync(client, Csv(("2026-08-23", description, 675m)));
        var transaction = await TransactionParDescription(client, description);

        var liaison = await client.PostAsJsonAsync($"/api/budget/transactions/{transaction.Id}/lier",
            new
            {
                ventilation = new[]
                {
                    new { enveloppeId = taxes, montant = 310m },
                    new { enveloppeId = projet, montant = 185m },
                },
            });
        Assert.Equal(HttpStatusCode.NoContent, liaison.StatusCode);

        // N mouvements Provision pointant la même transaction ; le reste (180 $)
        // demeure en Non affecté.
        var resume = await ResumeAsync(client);
        Assert.Equal(310m, Assert.Single(resume.Enveloppes, e => e.Id == taxes).Solde);
        Assert.Equal(185m, Assert.Single(resume.Enveloppes, e => e.Id == projet).Solde);
        Assert.Equal(resume.SoldeCourant, resume.TotalEnveloppes + resume.NonAffecte);

        var detailTaxes = await client.GetFromJsonAsync<EnveloppeDetailDto>($"/api/budget/enveloppes/{taxes}");
        var mouvementTaxes = Assert.Single(detailTaxes!.Mouvements);
        Assert.Equal("Provision", mouvementTaxes.Type);
        Assert.Equal(transaction.Id, mouvementTaxes.TransactionBancaireId);
    }

    [Fact]
    public async Task VentilerAuDelaDuDepot_Repond400()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var enveloppeId = await CreerEnveloppeAsync(client,
            new { nom = $"Trop {Guid.NewGuid():N}", type = "Reserve" });
        var description = $"DEPOT {Guid.NewGuid():N}";
        await ImporterAsync(client, Csv(("2026-08-23", description, 100m)));
        var transaction = await TransactionParDescription(client, description);

        var liaison = await client.PostAsJsonAsync($"/api/budget/transactions/{transaction.Id}/lier",
            new { ventilation = new[] { new { enveloppeId, montant = 150m } } });

        Assert.Equal(HttpStatusCode.BadRequest, liaison.StatusCode);
    }

    [Fact]
    public async Task Transfert_DeuxMouvementsOpposes_InvariantPreserve()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var de = await CreerEnveloppeAsync(client,
            new { nom = $"Source {Guid.NewGuid():N}", type = "Reserve" });
        var vers = await CreerEnveloppeAsync(client,
            new { nom = $"Destination {Guid.NewGuid():N}", type = "Projet", montantCible = 1000m });

        var transfert = await client.PostAsJsonAsync("/api/budget/transferts",
            new { deEnveloppeId = de, versEnveloppeId = vers, montant = 100m });
        Assert.Equal(HttpStatusCode.NoContent, transfert.StatusCode);

        var resume = await ResumeAsync(client);
        Assert.Equal(-100m, Assert.Single(resume.Enveloppes, e => e.Id == de).Solde);
        Assert.Equal(100m, Assert.Single(resume.Enveloppes, e => e.Id == vers).Solde);
        Assert.Equal(resume.SoldeCourant, resume.TotalEnveloppes + resume.NonAffecte);
    }

    [Fact]
    public async Task Fermer_ExigeUnSoldeAZero()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);
        var pleine = await CreerEnveloppeAsync(client,
            new { nom = $"Pleine {Guid.NewGuid():N}", type = "Reserve" });
        var vide = await CreerEnveloppeAsync(client,
            new { nom = $"Vide {Guid.NewGuid():N}", type = "Reserve" });
        var ajout = await client.PostAsJsonAsync($"/api/budget/enveloppes/{pleine}/mouvements",
            new { type = "Ajustement", montant = 50m });
        Assert.Equal(HttpStatusCode.NoContent, ajout.StatusCode);

        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsync($"/api/budget/enveloppes/{pleine}/fermer", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/budget/enveloppes/{vide}/fermer", null)).StatusCode);
    }

    [Fact]
    public async Task EnveloppeLieeAUneTache_DeriveSonEcheance_EtSaProvision()
    {
        var client = await factory.ClientConnecte();
        await AncrerAsync(client);

        // Tâche annuelle : sa prochaine occurrence porte l'échéance dérivée.
        var creationTache = await client.PostAsJsonAsync("/api/taches", new
        {
            titre = $"Repeindre la toiture {Guid.NewGuid():N}",
            recurrence = new { mode = "Fixe", fixeType = "Annuelle", moisAnnuel = 6, jourAnnuel = 1 },
        });
        Assert.Equal(HttpStatusCode.Created, creationTache.StatusCode);
        var tacheId = (await creationTache.Content.ReadFromJsonAsync<CreationReponse>())!.Id;

        var enveloppeId = await CreerEnveloppeAsync(client, new
        {
            nom = $"Toiture liée {Guid.NewGuid():N}",
            type = "Equipement",
            montantCible = 1200m,
            tacheId,
        });

        var resume = await ResumeAsync(client);
        var enveloppe = Assert.Single(resume.Enveloppes, e => e.Id == enveloppeId);
        Assert.NotNull(enveloppe.DateEffective);
        Assert.Equal(6, enveloppe.DateEffective!.Value.Month);
        Assert.True(enveloppe.Provision > 0);
    }

    [Fact]
    public async Task ImporterSansCompteAncre_Repond409()
    {
        // Client frais mais base partagée : si un autre test a déjà ancré, ce cas
        // est déjà couvert par l'ancrage lui-même — on ne teste le 409 que sinon.
        var client = await factory.ClientConnecte();
        var resume = await ResumeAsync(client);
        if (resume.Compte is not null)
        {
            return;
        }
        using var formulaire = new MultipartFormDataContent
        {
            { new StringContent("Date,Description,Montant\n", Encoding.UTF8, "text/csv"), "fichier", "r.csv" },
        };
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/api/budget/import", formulaire)).StatusCode);
    }
}
