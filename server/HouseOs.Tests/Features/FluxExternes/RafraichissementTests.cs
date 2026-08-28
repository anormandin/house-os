using System.Net;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Infrastructure;
using HouseOs.Tests.Features.Taches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.FluxExternes;

/// <summary>
/// Le rafraîchissement d'un flux ICS face aux échecs : l'erreur est notée sans rien
/// détruire, et un flux qui casse (même supprimé pendant le passage) ne doit jamais
/// laisser le contexte sale ni avorter les flux suivants.
/// </summary>
public class RafraichissementTests : TestAvecSqlite
{
    // Daté de demain : la fenêtre d'ingestion est hier → +60 jours.
    private static readonly string IcsValide = $"""
        BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//test//FR
        BEGIN:VEVENT
        UID:frais@test
        DTSTART;VALUE=DATE:{DateTime.Now.AddDays(1):yyyyMMdd}
        SUMMARY:Événement frais
        END:VEVENT
        END:VCALENDAR
        """;

    private sealed class ReponseFixe(Func<HttpResponseMessage> fabrique) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage requete, CancellationToken ct) => Task.FromResult(fabrique());
    }

    private static HttpClient Client(HttpStatusCode statut, string? corps = null) =>
        new(new ReponseFixe(() => new HttpResponseMessage(statut)
        {
            Content = new StringContent(corps ?? ""),
        }));

    private FluxExterne SemerFlux(bool avecEvenement = true)
    {
        var flux = new FluxExterne { Id = Guid.NewGuid(), Nom = "Collectes", Url = "https://exemple.test/c.ics" };
        Db.FluxExternes.Add(flux);
        if (avecEvenement)
        {
            Db.EvenementsExternes.Add(new EvenementExterne
            {
                FluxExterneId = flux.Id,
                Uid = "ancien:2026-09-01",
                Titre = "Ancien événement",
                Date = new DateOnly(2026, 9, 1),
            });
        }
        Db.SaveChanges();
        return flux;
    }

    [Fact]
    public async Task Un_flux_en_erreur_note_l_erreur_et_garde_les_evenements()
    {
        var flux = SemerFlux();

        await FluxExternesRafraichissement.Rafraichir(
            Db, flux, Client(HttpStatusCode.NotFound), CancellationToken.None);

        // L'appelant (validation à la création) lit l'instance en mémoire…
        Assert.NotNull(flux.DerniereErreur);
        // …et la base est à jour aussi, sans avoir touché aux derniers événements connus.
        Db.ChangeTracker.Clear();
        Assert.NotNull(Db.FluxExternes.Single().DerniereErreur);
        Assert.Single(Db.EvenementsExternes);
    }

    [Fact]
    public async Task Un_flux_supprime_pendant_le_passage_ne_fait_pas_tomber_le_passage()
    {
        var flux = SemerFlux();
        // Suppression concurrente (autre requête HTTP) pendant que ce passage tient
        // encore l'instance trackée.
        await Db.FluxExternes.Where(f => f.Id == flux.Id).ExecuteDeleteAsync();

        // Sans purge du change tracker, la sauvegarde de l'erreur lèverait et
        // l'exception avorterait tous les flux suivants pendant 6 heures.
        await FluxExternesRafraichissement.Rafraichir(
            Db, flux, Client(HttpStatusCode.OK, IcsValide), CancellationToken.None);

        Db.ChangeTracker.Clear();
        Assert.Empty(Db.FluxExternes);
        Assert.Empty(Db.EvenementsExternes.Where(e => e.Titre == "Événement frais"));
        // Le contexte reste propre : un SaveChanges ultérieur ne flushe rien de fantôme.
        await Db.SaveChangesAsync();
    }

    private sealed class ReponseParHote(Func<string, HttpResponseMessage> fabrique) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage requete, CancellationToken ct) =>
            Task.FromResult(fabrique(requete.RequestUri!.Host));
    }

    private sealed class FabriqueClientFixe(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    [Fact]
    public async Task Un_flux_en_erreur_ne_gele_pas_le_statut_des_flux_suivants()
    {
        // Ordre alphabétique du Guid non garanti — deux flux, A traité avant B parce
        // que le passage suit l'ordre de la liste chargée (insertion ici).
        var fluxMort = new FluxExterne { Id = Guid.NewGuid(), Nom = "A mort", Url = "https://mort.exemple.test/a.ics" };
        var fluxSain = new FluxExterne { Id = Guid.NewGuid(), Nom = "B sain", Url = "https://sain.exemple.test/b.ics" };
        Db.FluxExternes.AddRange(fluxMort, fluxSain);
        Db.SaveChanges();

        var client = new HttpClient(new ReponseParHote(hote =>
            hote.StartsWith("mort")
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(IcsValide) }));

        // Le vrai passage : un scope (donc un DbContext) par flux, sur la même base
        // sqlite que Db — c'est ce découpage que le bug d'origine violait (un seul
        // contexte partagé, Clear() du flux mort détachait le flux sain).
        var options = new DbContextOptionsBuilder<HouseOsDbContext>().UseSqlite(Connexion).Options;
        var services = new ServiceCollection();
        services.AddScoped<HouseOsDbContext>(_ => new HouseOsDbContextSqlite(options));
        await using var fournisseur = services.BuildServiceProvider();
        var service = new FluxExternesRafraichissement(
            fournisseur.GetRequiredService<IServiceScopeFactory>(),
            new FabriqueClientFixe(client),
            NullLogger<FluxExternesRafraichissement>.Instance);

        await service.RafraichirTous(CancellationToken.None);

        Db.ChangeTracker.Clear();
        var sain = Db.FluxExternes.Single(f => f.Id == fluxSain.Id);
        // Le flux sain traité APRÈS l'échec doit avoir son horodatage ET zéro erreur.
        Assert.NotNull(sain.DernierRafraichissementLe);
        Assert.Null(sain.DerniereErreur);
        Assert.Single(Db.EvenementsExternes.Where(e => e.FluxExterneId == fluxSain.Id));
        var mort = Db.FluxExternes.Single(f => f.Id == fluxMort.Id);
        Assert.NotNull(mort.DerniereErreur);
        Assert.Null(mort.DernierRafraichissementLe);
    }

    [Fact]
    public async Task Un_rafraichissement_reussi_remplace_les_evenements_du_flux()
    {
        var flux = SemerFlux();

        await FluxExternesRafraichissement.Rafraichir(
            Db, flux, Client(HttpStatusCode.OK, IcsValide), CancellationToken.None);

        Assert.Null(flux.DerniereErreur);
        Assert.NotNull(flux.DernierRafraichissementLe);
        var evenement = Assert.Single(Db.EvenementsExternes);
        Assert.Equal("Événement frais", evenement.Titre);
    }
}
