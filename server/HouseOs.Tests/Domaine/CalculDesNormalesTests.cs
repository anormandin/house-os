using HouseOs.Api.Domaine.Meteo;

namespace HouseOs.Tests.Domaine;

/// <summary>
/// Les normales, calculées sur des archives fabriquées dont on connaît la réponse.
/// Sans base, sans réseau : la règle est du C# testable
/// (vault : D-2026-08-23 Pas De N8n Dans Le Cœur).
///
/// <para>Les climats de test sont <b>lisses</b> — une sinusoïde par année — pour que la
/// date attendue se calcule à la main plutôt que de se relire dans la sortie. Les
/// irrégularités sont ajoutées une à une, là où un test veut prouver qu'elles ne
/// dérangent rien.</para>
/// </summary>
public class CalculDesNormalesTests
{
    private const string Ici = "46.8500,-71.6200";

    /// <summary>Un climat continental de l'hémisphère nord : juillet au plus chaud.</summary>
    private static double MaxDuNord(DateOnly date) =>
        13 - 15 * Math.Cos(2 * Math.PI * (date.DayOfYear - 20) / 365.0);

    private static List<JourDeClimat> Archive(
        DateOnly debut, DateOnly fin, Func<DateOnly, double> max, Func<DateOnly, double>? pluie = null)
    {
        var jours = new List<JourDeClimat>();
        for (var date = debut; date <= fin; date = date.AddDays(1))
        {
            var maximum = max(date);
            jours.Add(new JourDeClimat
            {
                Coordonnees = Ici,
                Date = date,
                TemperatureMaxC = maximum,
                // Neuf degrés d'amplitude : le gel de la nuit précède de plusieurs
                // semaines celui de la journée, comme dans la vraie vie.
                TemperatureMinC = maximum - 9,
                PrecipitationMm = pluie?.Invoke(date) ?? 2,
                NeigeCm = maximum < -1 ? 2 : 0,
            });
        }
        return jours;
    }

    private static NormalesClimatiques Calculer(IReadOnlyList<JourDeClimat> archive) =>
        CalculDesNormales.Calculer(Ici, archive, new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));

    private static NormalesClimatiques DixAnsAuNord() =>
        Calculer(Archive(new DateOnly(2016, 1, 1), new DateOnly(2025, 12, 31), MaxDuNord));

    [Fact]
    public void Une_archive_vide_ne_casse_rien_et_ne_promet_rien()
    {
        // Une installation neuve dont le réseau n'a rien rendu : aucune normale, et
        // surtout aucune exception — la famille se taira, l'édition sortira.
        var normales = Calculer([]);

        Assert.Equal(0, normales.SaisonsCompletes);
        Assert.Null(normales.PremierGel);
        Assert.Null(normales.PremiereNeige);
        Assert.Null(normales.DerniereDouceur);
        Assert.Null(normales.MoisSec);
        Assert.Null(normales.MoisPluvieux);
    }

    [Fact]
    public void Les_trois_dates_tombent_a_l_automne_et_dans_le_bon_ordre()
    {
        var normales = DixAnsAuNord();

        // Neuf saisons complètes : la première (janvier à juillet 2016) n'a pas
        // d'ancre dans l'archive, et c'est voulu — elle n'aurait pas eu l'occasion
        // de geler.
        Assert.Equal(9, normales.SaisonsCompletes);

        var gel = normales.PremierGel!;
        var neige = normales.PremiereNeige!;
        var douceur = normales.DerniereDouceur!;

        // Le mercure atteint vingt degrés une dernière fois, puis les nuits passent
        // sous zéro, puis la neige tient : trois dates, dans cet ordre, à l'automne.
        Assert.InRange(douceur.Mois, 9, 10);
        Assert.InRange(gel.Mois, 9, 11);
        Assert.InRange(neige.Mois, 11, 12);
        Assert.True(Rang(douceur) < Rang(gel));
        Assert.True(Rang(gel) < Rang(neige));

        // Un climat parfaitement régulier ne donne pas une normale au jour près :
        // l'écart plancher à un jour dit l'imprécision qu'une normale porte toujours.
        Assert.Equal(1, gel.EcartJours);
    }

    [Fact]
    public void Le_gel_annonce_est_celui_du_sol_et_non_celui_de_l_abri()
    {
        // Une maille de neuf kilomètres à deux mètres du sol ne voit ni le rayonnement
        // nocturne d'un jardin ni l'air froid qui s'y accumule : la gelée qui tue le
        // basilic arrive pendant que la réanalyse lit encore deux ou trois degrés.
        // Ici, un automne qui plafonne à deux degrés pendant tout octobre avant de
        // plonger le 1er novembre — le gel à annoncer est celui du 1er octobre.
        var jours = new List<JourDeClimat>();
        for (var date = new DateOnly(2016, 1, 1); date.Year <= 2025; date = date.AddDays(1))
        {
            var minimum = date.Month switch
            {
                10 => 2.0,
                11 or 12 or 1 or 2 or 3 => -5.0,
                _ => 10.0,
            };
            jours.Add(new JourDeClimat
            {
                Coordonnees = Ici,
                Date = date,
                // Le maximum reste au-dessus de la neige et sous les vingt degrés :
                // seul le premier gel est en jeu dans ce test.
                TemperatureMaxC = date.Month is 7 or 8 ? 25 : 12,
                TemperatureMinC = minimum,
                PrecipitationMm = 2,
            });
        }

        var gel = Calculer(jours).PremierGel!;

        Assert.Equal(10, gel.Mois);
        Assert.Equal(1, gel.Jour);
    }

    [Fact]
    public void La_mediane_resiste_a_une_annee_aberrante()
    {
        // Une seule année à gel très tardif ne doit pas déplacer la normale : c'est
        // toute la raison de prendre la médiane plutôt que la moyenne.
        var normal = DixAnsAuNord().PremierGel!;

        var archive = Archive(new DateOnly(2016, 1, 1), new DateOnly(2025, 12, 31), MaxDuNord);
        foreach (var jour in archive.Where(j => j.Date.Year == 2022 && j.Date.Month is 8 or 9 or 10 or 11))
        {
            // Un automne 2022 qui ne gèle pas avant décembre.
            jour.TemperatureMinC = 5;
        }
        var avecAberration = Calculer(archive).PremierGel!;

        Assert.Equal(normal.Mois, avecAberration.Mois);
        Assert.Equal(normal.Jour, avecAberration.Jour);
        // Elle se voit là où elle doit se voir : dans l'imprécision annoncée.
        Assert.True(avecAberration.EcartJours > normal.EcartJours);
    }

    [Fact]
    public void Au_sud_de_l_equateur_la_saison_froide_commence_en_fevrier()
    {
        // Le dépôt est public : l'ancre des saisons se déduit du mois le plus chaud de
        // l'archive, jamais d'un août supposé. En Tasmanie, l'hiver est en juillet et
        // le premier gel tombe au milieu de l'année.
        double MaxDuSud(DateOnly date) => 13 - 15 * Math.Cos(2 * Math.PI * (date.DayOfYear - 203) / 365.0);

        var normales = Calculer(Archive(new DateOnly(2016, 1, 1), new DateOnly(2025, 12, 31), MaxDuSud));

        Assert.InRange(normales.PremierGel!.Mois, 3, 6);
        Assert.InRange(normales.DerniereDouceur!.Mois, 3, 5);
        // Et l'ordre tient : la douceur s'en va avant que ça gèle, là aussi.
        Assert.True(Rang(normales.DerniereDouceur) < Rang(normales.PremierGel));
    }

    [Fact]
    public void Une_archive_trop_courte_ne_donne_aucune_normale_datee()
    {
        // Deux saisons ne font pas une normale. Mieux vaut se taire que d'annoncer le
        // premier gel d'après deux hivers.
        var normales = Calculer(Archive(new DateOnly(2022, 1, 1), new DateOnly(2024, 6, 30), MaxDuNord));

        Assert.True(normales.SaisonsCompletes < 3);
        Assert.Null(normales.PremierGel);
        Assert.Null(normales.PremiereNeige);
        Assert.Null(normales.DerniereDouceur);
    }

    [Fact]
    public void Un_climat_sans_gel_ni_fin_de_douceur_se_tait()
    {
        // Sous les tropiques il ne gèle pas, il ne neige pas, et la douceur ne
        // s'arrête jamais : les trois dates sont absentes. Une normale qui tombe au
        // bord de la fenêtre de recherche n'est pas une normale.
        var normales = Calculer(Archive(
            new DateOnly(2016, 1, 1), new DateOnly(2025, 12, 31),
            date => 29 - 2 * Math.Cos(2 * Math.PI * (date.DayOfYear - 20) / 365.0)));

        Assert.True(normales.SaisonsCompletes >= 3);
        Assert.Null(normales.PremierGel);
        Assert.Null(normales.PremiereNeige);
        Assert.Null(normales.DerniereDouceur);
    }

    [Fact]
    public void Le_mois_le_plus_sec_et_le_plus_pluvieux_se_lisent_sur_les_mois_entiers()
    {
        // Novembre reçoit le double, février la moitié : c'est ce que le calcul doit
        // retrouver, et rien d'autre.
        double Pluie(DateOnly date) => date.Month switch { 11 => 6, 2 => 0.5, _ => 2 };

        var normales = Calculer(Archive(
            new DateOnly(2016, 1, 1), new DateOnly(2025, 12, 31), MaxDuNord, Pluie));

        Assert.Equal(11, normales.MoisPluvieux!.Mois);
        Assert.Equal(2, normales.MoisSec!.Mois);
        Assert.Equal(180, normales.MoisPluvieux.PrecipitationMm, tolerance: 0.001);
    }

    [Fact]
    public void Un_mois_entame_par_le_bord_de_l_archive_ne_fausse_pas_la_moyenne()
    {
        // L'archive commence le 12 novembre : ce novembre-là n'a que dix-neuf jours de
        // pluie, et il ferait passer le mois le plus arrosé de l'année pour le plus
        // sec s'il comptait dans la moyenne.
        double Pluie(DateOnly date) => date.Month switch { 11 => 6, 2 => 0.5, _ => 2 };

        var normales = Calculer(Archive(
            new DateOnly(2015, 11, 12), new DateOnly(2025, 12, 31), MaxDuNord, Pluie));

        Assert.Equal(11, normales.MoisPluvieux!.Mois);
        Assert.Equal(180, normales.MoisPluvieux.PrecipitationMm, tolerance: 0.001);
    }

    [Fact]
    public void Un_climat_sans_contraste_de_pluie_ne_designe_aucun_mois()
    {
        // Douze mois qui reçoivent tous autant : le seul écart qui reste est celui du
        // calendrier — février est plus sec que janvier de trois jours de pluie. Le
        // journal publierait la longueur des mois comme une statistique du climat.
        // Une normale qui n'en est pas ne sort pas.
        var normales = Calculer(Archive(
            new DateOnly(2016, 1, 1), new DateOnly(2025, 12, 31), MaxDuNord, _ => 2));

        Assert.Null(normales.MoisSec);
        Assert.Null(normales.MoisPluvieux);
    }

    [Fact]
    public void Un_mois_troue_au_milieu_de_l_archive_ne_passe_pas_pour_le_plus_sec()
    {
        // La lecture de l'archive écarte les journées sans température : un trou de
        // dix jours au milieu d'un juillet — le mois le plus arrosé — le ferait passer
        // pour un mois ordinaire, et personne ne verrait rien. Un mois compte quand
        // toutes ses journées sont là, pas quand ses bords tombent dans la fenêtre.
        double Pluie(DateOnly date) => date.Month switch { 7 => 6, 2 => 0.5, _ => 2 };
        var archive = Archive(new DateOnly(2016, 1, 1), new DateOnly(2025, 12, 31), MaxDuNord, Pluie);
        archive.RemoveAll(j => j.Date.Year == 2020 && j.Date.Month == 7 && j.Date.Day <= 10);

        var normales = Calculer(archive);

        Assert.Equal(7, normales.MoisPluvieux!.Mois);
        // Neuf juillets entiers, pas dix : la moyenne ne porte que sur ce qui est
        // complet, et le juillet troué n'a pas tiré le chiffre vers le bas.
        Assert.Equal(186, normales.MoisPluvieux.PrecipitationMm, tolerance: 0.001);
    }

    /// <summary>Le rang d'une date normale dans l'automne, pour comparer deux dates.</summary>
    private static int Rang(DateNormale date) => date.Mois * 100 + date.Jour;
}
