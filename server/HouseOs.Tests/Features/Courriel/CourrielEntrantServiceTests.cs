using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Courriel;
using HouseOs.Api.Features.Documents;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Infrastructure;
using HouseOs.Tests.Features.Synchro;
using HouseOs.Tests.Features.Taches;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using static HouseOs.Tests.Features.Courriel.FabriqueCourriels;

namespace HouseOs.Tests.Features.Courriel;

/// <summary>
/// Le passage de relève de bout en bout, sur un dépôt en mémoire : documents à classer
/// créés, fichiers écrits, objets effacés, doublons ignorés, erreurs isolées, toast
/// diffusé — et la parité MCP sur ces mêmes documents.
/// </summary>
public sealed class CourrielEntrantServiceTests : TestAvecSqlite
{
    private readonly string _dossier = Directory.CreateTempSubdirectory("houseos-courriel-").FullName;
    private readonly DepotCourrielsFictif _depot = new();
    private readonly EnrichisseurFictif _enrichisseur = new();
    private readonly DiffuseurMouchard _synchro = new();
    private readonly ServiceProvider _fournisseur;

    public CourrielEntrantServiceTests()
    {
        var options = new DbContextOptionsBuilder<HouseOsDbContext>().UseSqlite(Connexion).Options;
        var services = new ServiceCollection();
        services.AddScoped<HouseOsDbContext>(_ => new HouseOsDbContextSqlite(options));
        _fournisseur = services.BuildServiceProvider();
    }

    private CourrielEntrantService Service(long tailleMax = 25 * 1024 * 1024) => new(
        _fournisseur.GetRequiredService<IServiceScopeFactory>(),
        _depot,
        Options.Create(new CourrielOptions { TailleMaxOctets = tailleMax }),
        _enrichisseur,
        _synchro,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Fichiers:Chemin"] = _dossier }).Build(),
        new EnvironnementFictif(),
        NullLogger<CourrielEntrantService>.Instance);

    private List<Document> Documents()
    {
        Db.ChangeTracker.Clear();
        return Db.Documents.OrderBy(d => d.Titre).ToList();
    }

    [Fact]
    public async Task Un_pdf_joint_devient_un_document_a_classer_et_l_objet_est_efface()
    {
        var cle = _depot.Deposer(AvecPdf());
        _enrichisseur.Proposition = new PropositionLlm(
            "Facture IKEA — BILLY", "Facture", null, null, "2026-08-30", "129,95 $", "Bibliothèque BILLY.");

        var rapport = await Service().ReleverAsync(CancellationToken.None);

        Assert.True(rapport.Actif);
        Assert.Equal(1, rapport.NbCourriels);
        Assert.Equal(1, rapport.NbDocuments);
        Assert.Empty(rapport.Erreurs);

        var document = Assert.Single(Documents());
        Assert.True(document.AClasser);
        Assert.Equal("Facture IKEA — BILLY", document.Titre);
        Assert.Equal(CategorieDocument.Facture, document.Categorie);
        Assert.Equal(new DateOnly(2026, 8, 30), document.DateDocument);
        Assert.Contains("Montant : 129,95 $", document.Notes);
        Assert.Equal("Facture_48211.pdf", document.NomFichier);
        Assert.Equal(PetitPdf(), File.ReadAllBytes(Path.Combine(_dossier, document.CheminDisque)));

        var import = Db.ImportsCourriel.Single();
        Assert.Equal(document.ImportCourrielId, import.Id);
        Assert.Equal(StatutImportCourriel.Importe, import.Statut);
        Assert.Equal("recu-1@exemple.test", import.MessageId);
        Assert.Equal(1, import.NbDocuments);
        Assert.Equal([cle], _depot.Supprimes);
        Assert.Empty(_depot.Cles);

        // Un seul appel LLM par courriel, qui a vu les pièces et le vocabulaire.
        Assert.Equal(1, _enrichisseur.Appels);
        Assert.Equal(["Facture_48211.pdf"], _enrichisseur.DernierContexte!.NomsPiecesJointes);

        var evenement = Assert.Single(_synchro.Fins);
        Assert.Equal(EvenementSynchro.GenreDocumentsRecus, evenement.Genre);
        Assert.Equal(EvenementSynchro.SourceCourriel, evenement.Source);
        Assert.Equal(1, evenement.Nombre);
        Assert.Equal("Facture IKEA — BILLY", evenement.Libelle);
    }

    [Fact]
    public async Task Sans_piece_le_courriel_entier_devient_un_eml()
    {
        _depot.Deposer(HtmlSeul());

        await Service().ReleverAsync(CancellationToken.None);

        var document = Assert.Single(Documents());
        Assert.Equal(EnregistrementDocument.TypeMimeCourriel, document.TypeMime);
        Assert.Equal("Confirmation de commande.eml", document.NomFichier);
        Assert.EndsWith(".eml", document.CheminDisque);
        Assert.Equal("Confirmation de commande", document.Titre); // repli : le sujet
        Assert.Equal(CategorieDocument.Facture, document.Categorie); // « commande » → Facture
        Assert.True(File.Exists(Path.Combine(_dossier, document.CheminDisque)));
    }

    [Fact]
    public async Task Le_meme_message_id_deux_fois_est_ignore_sans_doublon()
    {
        _depot.Deposer(AvecPdf(messageId: "meme@exemple.test"));
        _depot.Deposer(AvecPdf(messageId: "meme@exemple.test"));

        var rapport = await Service().ReleverAsync(CancellationToken.None);

        Assert.Equal(2, rapport.NbCourriels);
        Assert.Equal(1, rapport.NbDocuments);
        Assert.Equal(1, rapport.NbIgnores);
        Assert.Single(Documents());
        Assert.Empty(_depot.Cles);
        Db.ChangeTracker.Clear();
        var ignore = Db.ImportsCourriel.Single(i => i.Statut == StatutImportCourriel.Ignore);
        Assert.Null(ignore.MessageId);
        Assert.Contains("meme@exemple.test", ignore.Erreur);
    }

    [Fact]
    public async Task Un_objet_deja_traite_dont_l_effacement_avait_rate_est_efface_sans_reimport()
    {
        var cle = _depot.Deposer(AvecPdf());
        _depot.EchouerSuppression = true;
        await Service().ReleverAsync(CancellationToken.None);
        Assert.Single(Documents());
        Assert.Contains(cle, _depot.Cles);

        _depot.EchouerSuppression = false;
        var rapport = await Service().ReleverAsync(CancellationToken.None);

        Assert.Equal(1, rapport.NbIgnores);
        Assert.Single(Documents());
        Assert.Empty(_depot.Cles);
    }

    [Fact]
    public async Task Un_courriel_illisible_ou_trop_gros_est_marque_en_erreur_et_efface()
    {
        _depot.Deposer("ceci n'est pas du MIME"u8.ToArray(), "entrants/00-illisible.eml");
        _depot.Deposer(AvecPdf(messageId: "gros@exemple.test"), "entrants/01-gros.eml");

        // Plafond minuscule : le second dépasse, le premier ne parse pas.
        var rapport = await Service(tailleMax: 30).ReleverAsync(CancellationToken.None);

        Assert.Equal(2, rapport.Erreurs.Count);
        Assert.Empty(Documents());
        Assert.Empty(_depot.Cles);
        Db.ChangeTracker.Clear();
        Assert.All(Db.ImportsCourriel, i => Assert.Equal(StatutImportCourriel.Erreur, i.Statut));
    }

    [Fact]
    public async Task Une_piece_dont_le_contenu_ment_sur_son_type_est_ecartee_le_reste_entre()
    {
        var message = Message(corps: b =>
        {
            b.Attachments.Add("vrai.pdf", PetitPdf(), MimeKit.ContentType.Parse("application/pdf"));
            b.Attachments.Add("faux.png", "pas un png"u8.ToArray(), MimeKit.ContentType.Parse("image/png"));
        });
        _depot.Deposer(Octets(message));

        var rapport = await Service().ReleverAsync(CancellationToken.None);

        Assert.Equal(1, rapport.NbDocuments);
        Assert.Equal("vrai.pdf", Assert.Single(Documents()).NomFichier);
    }

    [Fact]
    public async Task Depot_inactif_ou_passage_concurrent_ne_font_rien()
    {
        _depot.Actif = false;
        Assert.False((await Service().ReleverAsync(CancellationToken.None)).Actif);

        _depot.Actif = true;
        var service = Service();
        var verrou = typeof(CourrielEntrantService)
            .GetField("_verrou", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var semaphore = (SemaphoreSlim)verrou.GetValue(service)!;
        await semaphore.WaitAsync();
        try
        {
            Assert.True((await service.ReleverAsync(CancellationToken.None)).DejaEnCours);
        }
        finally
        {
            semaphore.Release();
        }
    }

    [Fact]
    public async Task Parite_mcp_lister_a_classer_et_classer()
    {
        _depot.Deposer(AvecPdf());
        var rapport = await OutilsMaison.ReleverCourriels(Service(), CancellationToken.None);
        Assert.Contains("nbDocuments = 1", rapport.ToString());

        var aClasser = await OutilsMaison.ListerDocuments(Db, aClasser: true);
        var id = Assert.Single(aClasser).Id;
        Assert.Empty(await OutilsMaison.ListerDocuments(Db, aClasser: false));

        await OutilsMaison.GererDocument(Db, null!, null!, "classer", id);

        Assert.Empty(await OutilsMaison.ListerDocuments(Db, aClasser: true));
        Assert.Single(await OutilsMaison.ListerDocuments(Db, aClasser: false));
    }

    [Fact]
    public async Task Relever_courriels_mcp_refuse_un_depot_inactif()
    {
        _depot.Actif = false;
        await Assert.ThrowsAsync<McpException>(() => OutilsMaison.ReleverCourriels(Service(), CancellationToken.None));
    }

    private sealed class EnvironnementFictif : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "HouseOs.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
    }
}
