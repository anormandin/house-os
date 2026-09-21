using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HouseOs.Tests.Integration;

/// <summary>
/// Les normales climatiques en base : la clé des coordonnées et le remplacement. Sur
/// un vrai Postgres, parce que c'est l'index unique et la transaction qu'on vérifie
/// (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
///
/// <para>Le worker est retiré du host de test : rien ici ne parle à Open-Meteo, et les
/// journées d'archive sont fabriquées.</para>
/// </summary>
[Collection("integration")]
public class NormalesApiTests(HouseOsFactory factory) : IAsyncLifetime
{
    // Deux lieux quelconques : ce que le test raconte, c'est un déménagement, et rien
    // de ce dépôt public n'a à savoir lequel (vault : Distribution).
    private const string AvantLeDemenagement = "46.8100,-71.2100";
    private const string ApresLeDemenagement = "45.5000,-73.5000";

    // Les deux tables sont vidées autour de chaque test : la collection partage un
    // seul Postgres, et des normales laissées derrière feraient apparaître un fait de
    // climat dans l'écran d'un autre test — un fantôme difficile à lire au rapport.
    public Task InitializeAsync() => Vider();

    public Task DisposeAsync() => Vider();

    private async Task Vider()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await db.NormalesClimatiques.ExecuteDeleteAsync();
        await db.JoursDeClimat.ExecuteDeleteAsync();
    }

    private static List<JourDeClimat> Archive(string coordonnees, DateOnly debut, int jours) =>
        [.. Enumerable.Range(0, jours).Select(i => new JourDeClimat
        {
            Coordonnees = coordonnees,
            Date = debut.AddDays(i),
            TemperatureMinC = 4,
            TemperatureMaxC = 13,
            PrecipitationMm = 2,
        })];

    /// <summary>Matérialiser un tirage, comme le worker le ferait.</summary>
    private static Task Materialiser(
        HouseOsDbContext db, string coordonnees, IReadOnlyList<JourDeClimat> jours, DateTimeOffset maintenant) =>
        OperationsNormales.RemplacerAsync(
            db, jours, CalculDesNormales.Calculer(coordonnees, jours, maintenant));

    private async Task<T> AvecLaBase<T>(Func<HouseOsDbContext, Task<T>> travail)
    {
        using var scope = factory.Services.CreateScope();
        return await travail(scope.ServiceProvider.GetRequiredService<HouseOsDbContext>());
    }

    [Fact]
    public async Task Un_changement_de_coordonnees_recalcule_au_lieu_de_servir_les_anciennes()
    {
        // Un déménagement : le `.env` change de latitude, et les normales de l'ancienne
        // adresse ne doivent plus jamais sortir à la nouvelle. C'est la clé, et rien
        // d'autre, qui le garantit.
        var maintenant = DateTimeOffset.UtcNow;
        var archive = Archive(AvantLeDemenagement, new DateOnly(2020, 1, 1), 400);

        await AvecLaBase(async db =>
        {
            await Materialiser(db, AvantLeDemenagement, archive, maintenant);
            return 0;
        });

        var (avant, apres, restantes) = await AvecLaBase(async db => (
            await OperationsNormales.FautIlRecalculerAsync(db, AvantLeDemenagement, maintenant, 360),
            await OperationsNormales.FautIlRecalculerAsync(db, ApresLeDemenagement, maintenant, 360),
            await OperationsNormales.LireAsync(db, ApresLeDemenagement)));

        // Les normales fraîches du lieu configuré : rien à faire.
        Assert.False(avant);
        // Les mêmes, vues depuis les nouvelles coordonnées : à refaire, et surtout
        // pas à servir.
        Assert.True(apres);
        Assert.Null(restantes);
    }

    [Fact]
    public async Task Le_recalcul_emporte_l_archive_de_l_ancienne_adresse()
    {
        // Une seule localisation est configurée : garder les journées de l'ancienne
        // adresse ne servirait qu'à faire mentir « il a fait X° ce jour-là l'an
        // dernier » le lendemain de l'emménagement.
        var maintenant = DateTimeOffset.UtcNow;

        await AvecLaBase(async db =>
        {
            await Materialiser(
                db, AvantLeDemenagement, Archive(AvantLeDemenagement, new DateOnly(2020, 1, 1), 400), maintenant);
            await Materialiser(
                db, ApresLeDemenagement, Archive(ApresLeDemenagement, new DateOnly(2020, 1, 1), 400), maintenant);
            return 0;
        });

        var (ancienne, nouvelle, lignes) = await AvecLaBase(async db => (
            await OperationsNormales.LireAsync(db, AvantLeDemenagement),
            await OperationsNormales.LireAsync(db, ApresLeDemenagement),
            await db.JoursDeClimat.CountAsync(j => j.Coordonnees == AvantLeDemenagement)));

        Assert.Null(ancienne);
        Assert.NotNull(nouvelle);
        Assert.Equal(0, lignes);
    }

    [Fact]
    public async Task Des_normales_trop_vieilles_se_refont()
    {
        // Le tirage est annuel, mais il se déclenche à l'âge et non à date fixe : une
        // machine éteinte le jour de l'anniversaire ne doit pas coûter une année de
        // normales.
        var calculeesLe = DateTimeOffset.UtcNow.AddDays(-400);

        await AvecLaBase(async db =>
        {
            await Materialiser(
                db, ApresLeDemenagement, Archive(ApresLeDemenagement, new DateOnly(2020, 1, 1), 400), calculeesLe);
            return 0;
        });

        var (vieilles, encoreBonnes) = await AvecLaBase(async db => (
            await OperationsNormales.FautIlRecalculerAsync(db, ApresLeDemenagement, DateTimeOffset.UtcNow, 360),
            await OperationsNormales.FautIlRecalculerAsync(db, ApresLeDemenagement, DateTimeOffset.UtcNow, 500)));

        Assert.True(vieilles);
        Assert.False(encoreBonnes);
    }

    [Fact]
    public async Task Sans_normales_calculees_il_faut_recalculer()
    {
        // L'état d'une installation neuve : rien en base, donc tout à faire au premier
        // démarrage — et l'édition sort quand même entre-temps.
        var faire = await AvecLaBase(db =>
            OperationsNormales.FautIlRecalculerAsync(db, ApresLeDemenagement, DateTimeOffset.UtcNow, 360));

        Assert.True(faire);
    }
}
