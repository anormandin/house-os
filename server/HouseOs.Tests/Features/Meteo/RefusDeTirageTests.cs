using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Features.Meteo;

namespace HouseOs.Tests.Features.Meteo;

/// <summary>
/// Quand un tirage de l'archive ne vaut <b>pas</b> la peine de remplacer ce qu'on a.
/// Sans base et sans réseau : c'est une règle, pas une requête.
///
/// <para>Le danger n'est pas la panne, qui se voit et se réessaie : c'est le 200
/// maigre. Il remplacerait dix ans d'archive par trois mois <b>et</b> poserait un
/// horodatage tout neuf, qui interdit de réessayer avant trois cent soixante jours —
/// une année de silence pour la famille « le climat », avec un log d'information.
/// Trouvé en revue de code, étape 5.</para>
/// </summary>
public class RefusDeTirageTests
{
    private static NormalesClimatiques Normales(int saisons) =>
        new() { Coordonnees = "46.8100,-71.2100", SaisonsCompletes = saisons };

    [Fact]
    public void Un_tirage_complet_remplace_ce_qu_on_avait()
    {
        Assert.Null(OperationsNormales.PourquoiRefuser(3652, 3652, Normales(9), Normales(9)));
    }

    [Fact]
    public void Un_premier_tirage_complet_est_accepte_meme_sans_rien_en_base()
    {
        // L'installation neuve : il n'y a rien à protéger, et tout à poser.
        Assert.Null(OperationsNormales.PourquoiRefuser(3652, 3652, Normales(9), null));
    }

    [Fact]
    public void Un_tirage_maigre_est_ecarte_meme_s_il_n_est_pas_vide()
    {
        // Une réponse tronquée, une fenêtre rabotée par l'API, un NormalesAnnees baissé
        // par erreur : trois mois ne remplacent pas dix ans.
        var refus = OperationsNormales.PourquoiRefuser(90, 3652, Normales(0), Normales(9));

        Assert.NotNull(refus);
        Assert.Contains("90", refus);
        // Et il est écarté même sur une installation neuve : mieux vaut se taire
        // quelques heures de plus que se figer un an sur trois mois d'archive.
        Assert.NotNull(OperationsNormales.PourquoiRefuser(90, 3652, Normales(0), null));
    }

    [Fact]
    public void Un_tirage_qui_perd_des_saisons_est_ecarte()
    {
        // La couverture en journées peut être bonne et le résultat moins bon quand
        // même — un trou au milieu casse des saisons. On ne troque pas neuf saisons
        // contre quatre.
        var refus = OperationsNormales.PourquoiRefuser(3500, 3652, Normales(4), Normales(9));

        Assert.NotNull(refus);
        Assert.Contains("4", refus);
    }

    [Fact]
    public void Un_tirage_qui_gagne_une_saison_passe()
    {
        // Le cas normal du tirage annuel : une saison de plus qu'il y a un an.
        Assert.Null(OperationsNormales.PourquoiRefuser(3652, 3652, Normales(10), Normales(9)));
    }
}
