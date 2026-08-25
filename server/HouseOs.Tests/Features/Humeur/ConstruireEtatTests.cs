using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Features.Humeur;
using HouseOs.Tests.Features.Taches;

namespace HouseOs.Tests.Features.Humeur;

/// <summary>
/// L'état structuré (couche 1) : le signal météo doit être celui de la DATE de la
/// phrase — au rattrapage de 3 h du matin, la phrase du soir d'hier ne doit pas
/// annoncer les orages d'aujourd'hui.
/// </summary>
public class ConstruireEtatTests : TestAvecSqlite
{
    private static readonly DateOnly AujourdhuiFixe = new(2026, 9, 15);

    private void SemerOragePour(DateOnly date)
    {
        Db.PrevisionsQuotidiennes.Add(new PrevisionQuotidienne { Date = date });
        for (var h = 0; h < 24; h++)
        {
            Db.PrevisionsHoraires.Add(new PrevisionHoraire
            {
                Heure = date.ToDateTime(new TimeOnly(h, 0)),
                CodeMeteo = 95, // orage
            });
        }
        Db.SaveChanges();
    }

    [Fact]
    public async Task Sans_donnees_meteo_le_signal_est_null()
    {
        var etat = await ConstruireEtat.Construire(
            Db, AujourdhuiFixe, MomentJournee.Matin, CancellationToken.None);

        Assert.Null(etat.MeteoRemarquable);
    }

    [Fact]
    public async Task La_phrase_d_hier_n_utilise_pas_la_meteo_d_aujourdhui()
    {
        SemerOragePour(AujourdhuiFixe);

        var hier = await ConstruireEtat.Construire(
            Db, AujourdhuiFixe.AddDays(-1), MomentJournee.Soir, CancellationToken.None);
        var aujourdhui = await ConstruireEtat.Construire(
            Db, AujourdhuiFixe, MomentJournee.Soir, CancellationToken.None);

        Assert.Null(hier.MeteoRemarquable);
        Assert.NotNull(aujourdhui.MeteoRemarquable);
        Assert.Contains("orage", aujourdhui.MeteoRemarquable!.Description);
    }
}
