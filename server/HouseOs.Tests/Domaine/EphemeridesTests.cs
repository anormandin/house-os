using HouseOs.Api.Domaine.Ephemerides;

namespace HouseOs.Tests.Domaine;

/// <summary>
/// Les éphémérides sont vérifiées sur trois points du globe, et pas seulement sur
/// celui du mainteneur : le dépôt est public (vault : Distribution) et un calcul qui
/// suppose le Québec se casse chez quelqu'un d'autre sans qu'on le sache.
/// </summary>
public class EphemeridesTests
{
    // Région de Québec : hémisphère nord, latitude moyenne.
    private static readonly Lieu Quebec = new(46.85, -71.62);
    private static readonly TimeZoneInfo FuseauQuebec = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");

    // Hobart, Tasmanie : hémisphère sud, saisons inversées.
    private static readonly Lieu Hobart = new(-42.88, 147.32);
    private static readonly TimeZoneInfo FuseauHobart = TimeZoneInfo.FindSystemTimeZoneById("Australia/Hobart");

    // Longyearbyen, Svalbard : au-delà du cercle polaire, pas de lever certains jours.
    private static readonly Lieu Svalbard = new(78.22, 15.63);
    private static readonly TimeZoneInfo FuseauSvalbard = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    private static JourSolaire Jour(DateOnly date, Lieu lieu, TimeZoneInfo fuseau) =>
        Soleil.Jour(date, lieu, fuseau.GetUtcOffset(
            DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified)));

    [Fact]
    public void Le_lever_et_le_coucher_a_Quebec_tiennent_le_calcul_a_la_main()
    {
        // Repris à la main, sans le code : le 20 septembre 2026, à 71,62° ouest et en
        // UTC−4, le méridien du fuseau (60° ouest) est dépassé de 11,62°, soit
        // 46,5 min ; l'équation du temps avance le soleil de 6,6 min. Midi solaire
        // tombe donc à 12 h 40. La déclinaison vaut +1,2° trois jours avant
        // l'équinoxe, ce qui donne un angle horaire de 92,5°, soit 6 h 10 de part et
        // d'autre du midi.
        var jour = Jour(new DateOnly(2026, 9, 20), Quebec, FuseauQuebec);

        Assert.Equal(12 * 60 + 40, jour.MidiSolaire.ToTimeSpan().TotalMinutes, tolerance: 1);
        Assert.Equal(6 * 60 + 30, jour.Lever!.Value.ToTimeSpan().TotalMinutes, tolerance: 1);
        Assert.Equal(18 * 60 + 49, jour.Coucher!.Value.ToTimeSpan().TotalMinutes, tolerance: 1);
        Assert.Equal(12 * 60 + 19, jour.Duree!.Value.TotalMinutes, tolerance: 1);
    }

    [Fact]
    public void Le_midi_solaire_est_exactement_au_milieu_du_jour()
    {
        foreach (var (lieu, fuseau) in new[] { (Quebec, FuseauQuebec), (Hobart, FuseauHobart) })
        {
            var jour = Jour(new DateOnly(2026, 5, 4), lieu, fuseau);
            var milieu = jour.Lever!.Value.ToTimeSpan() + (jour.Coucher!.Value - jour.Lever.Value) / 2;
            Assert.Equal(jour.MidiSolaire.ToTimeSpan().TotalMinutes, milieu.TotalMinutes, tolerance: 0.5);
            Assert.Equal(jour.Duree!.Value.TotalMinutes, (jour.Coucher.Value - jour.Lever.Value).TotalMinutes, tolerance: 0.5);
        }
    }

    [Fact]
    public void A_l_equinoxe_le_jour_depasse_douze_heures_partout()
    {
        // La réfraction et le diamètre du disque donnent quelques minutes de plus que
        // douze heures, aux deux hémisphères — c'est ce que le zénith de 90°50' encode.
        foreach (var (lieu, fuseau) in new[] { (Quebec, FuseauQuebec), (Hobart, FuseauHobart) })
        {
            var duree = Jour(new DateOnly(2026, 9, 23), lieu, fuseau).Duree!.Value;
            Assert.InRange(duree.TotalMinutes, 720, 732);
        }
    }

    [Fact]
    public void Les_saisons_sont_inversees_dans_l_hemisphere_sud()
    {
        var juinQuebec = Jour(new DateOnly(2026, 6, 21), Quebec, FuseauQuebec).Duree!.Value;
        var decembreQuebec = Jour(new DateOnly(2026, 12, 21), Quebec, FuseauQuebec).Duree!.Value;
        var juinHobart = Jour(new DateOnly(2026, 6, 21), Hobart, FuseauHobart).Duree!.Value;
        var decembreHobart = Jour(new DateOnly(2026, 12, 21), Hobart, FuseauHobart).Duree!.Value;

        Assert.True(juinQuebec > decembreQuebec);
        Assert.True(decembreHobart > juinHobart);
        // Et la durée du plus long jour de l'un est celle du plus court de l'autre,
        // aux latitudes près : le calcul ne connaît pas de « bon » hémisphère.
        Assert.InRange(juinQuebec.TotalHours, 15.7, 16.0);
        Assert.InRange(decembreHobart.TotalHours, 15.1, 15.4);
    }

    [Fact]
    public void Au_dela_du_cercle_polaire_le_soleil_ne_se_leve_ni_ne_se_couche()
    {
        var nuit = Jour(new DateOnly(2026, 12, 21), Svalbard, FuseauSvalbard);
        Assert.True(nuit.NuitPolaire);
        Assert.Null(nuit.Lever);
        Assert.Null(nuit.Coucher);
        Assert.Equal(TimeSpan.Zero, nuit.Duree);

        var jour = Jour(new DateOnly(2026, 6, 21), Svalbard, FuseauSvalbard);
        Assert.True(jour.JourPolaire);
        Assert.Null(jour.Lever);
        Assert.Equal(TimeSpan.FromDays(1), jour.Duree);
    }

    [Fact]
    public void La_derive_quotidienne_change_de_signe_aux_solstices()
    {
        // Autour de l'équinoxe de septembre, au Québec, on perd environ trois minutes
        // par jour ; au solstice, la dérive passe par zéro.
        var septembre = Ciel.Calculer(new DateOnly(2026, 9, 23), Quebec, FuseauQuebec);
        Assert.InRange(septembre.DeriveQuotidienne!.Value.TotalMinutes, -4, -2.5);

        var solstice = Ciel.Calculer(new DateOnly(2026, 6, 21), Quebec, FuseauQuebec);
        Assert.InRange(solstice.DeriveQuotidienne!.Value.TotalSeconds, -20, 20);

        // Et elle est de l'autre signe au même moment dans l'hémisphère sud.
        var hobart = Ciel.Calculer(new DateOnly(2026, 9, 23), Hobart, FuseauHobart);
        Assert.InRange(hobart.DeriveQuotidienne!.Value.TotalMinutes, 2, 4);
    }

    [Fact]
    public void Les_equinoxes_et_solstices_tombent_sur_les_instants_publies()
    {
        // Le vrai ancrage extérieur de tout ce fichier : ces quatre instants sont
        // publiés, et ils valident d'un coup la longitude du soleil — donc aussi la
        // déclinaison dont dépendent tous les levers et couchers.
        //
        // Un quart d'heure de tolérance : la série employée ici est la série courte,
        // sans les perturbations planétaires. C'est son écart normal, et il est sans
        // conséquence pour dire « l'équinoxe, c'est lundi ».
        var publies = new (int Annee, Saison Saison, DateTime Instant)[]
        {
            (2024, Saison.SolsticeDeJuin, new DateTime(2024, 6, 20, 20, 51, 0)),
            (2024, Saison.EquinoxeDeSeptembre, new DateTime(2024, 9, 22, 12, 44, 0)),
            (2025, Saison.EquinoxeDeSeptembre, new DateTime(2025, 9, 22, 18, 19, 0)),
            (2025, Saison.SolsticeDeDecembre, new DateTime(2025, 12, 21, 15, 3, 0)),
        };

        foreach (var (annee, saison, instant) in publies)
        {
            var calcule = Saisons.DeLAnnee(annee).Single(e => e.Saison == saison).Instant.UtcDateTime;
            Assert.Equal(instant, calcule, TimeSpan.FromMinutes(15));
        }
    }

    [Fact]
    public void L_equinoxe_de_septembre_2026_tombe_la_veille_au_Quebec()
    {
        // Il arrive le 23 septembre à 0 h 09 UTC, donc le 22 au soir en heure de
        // l'Est. Un fait de date est toujours un fait de fuseau : c'est exactement le
        // genre d'erreur qu'un écran mural affiche pendant vingt-quatre heures.
        var equinoxe = Saisons.DeLAnnee(2026).Single(e => e.Saison == Saison.EquinoxeDeSeptembre);

        Assert.Equal(new DateOnly(2026, 9, 23), DateOnly.FromDateTime(equinoxe.Instant.UtcDateTime));
        Assert.Equal(
            new DateOnly(2026, 9, 22),
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(equinoxe.Instant, FuseauQuebec).DateTime));
    }

    [Fact]
    public void Le_prochain_evenement_saisonnier_franchit_l_annee()
    {
        var apresLeSolstice = new DateTimeOffset(2026, 12, 25, 0, 0, 0, TimeSpan.Zero);
        var prochain = Saisons.Prochain(apresLeSolstice);

        Assert.Equal(Saison.EquinoxeDeMars, prochain.Saison);
        Assert.Equal(2027, prochain.Instant.UtcDateTime.Year);
    }

    [Fact]
    public void La_lune_boucle_sur_sa_lunaison_depuis_la_nouvelle_lune_de_reference()
    {
        var epoque = Lune.NouvelleLuneDeReference;
        Assert.Equal(NomPhaseLunaire.NouvelleLune, Lune.Phase(epoque).Nom);
        Assert.Equal(0, Lune.Phase(epoque).Illumination, tolerance: 0.001);

        var pleine = Lune.Phase(epoque.AddDays(Lune.MoisSynodique / 2));
        Assert.Equal(NomPhaseLunaire.PleineLune, pleine.Nom);
        Assert.Equal(1, pleine.Illumination, tolerance: 0.001);

        var quartier = Lune.Phase(epoque.AddDays(Lune.MoisSynodique / 4));
        Assert.Equal(NomPhaseLunaire.PremierQuartier, quartier.Nom);
        Assert.Equal(0.5, quartier.Illumination, tolerance: 0.001);

        // Une lunaison plus tard, on est revenu au même point.
        Assert.Equal(
            Lune.Phase(epoque.AddDays(3)).Fraction,
            Lune.Phase(epoque.AddDays(3 + Lune.MoisSynodique)).Fraction,
            tolerance: 0.0001);
    }

    [Fact]
    public void Le_changement_d_heure_se_lit_dans_les_regles_du_systeme()
    {
        // Amérique du Nord : retour à l'heure normale le premier dimanche de novembre.
        var automne = ChangementHeure.Prochain(FuseauQuebec, new DateOnly(2026, 9, 20));
        Assert.Equal(new DateOnly(2026, 11, 1), automne!.Date);
        Assert.False(automne.Avance);
        Assert.Equal(TimeSpan.FromHours(-1), automne.Ecart);

        // Hémisphère sud : la Tasmanie avance en octobre, quand le Québec recule.
        var hobart = ChangementHeure.Prochain(FuseauHobart, new DateOnly(2026, 9, 20));
        Assert.True(hobart!.Avance);

        // Et un fuseau sans heure avancée ne donne rien du tout, sans planter.
        var sansBascule = TimeZoneInfo.FindSystemTimeZoneById("America/Phoenix");
        Assert.Null(ChangementHeure.Prochain(sansBascule, new DateOnly(2026, 9, 20)));
    }
}
