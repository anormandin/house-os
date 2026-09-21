using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Humeur;
using HouseOs.Tests.Features.Editorial;
using HouseOs.Tests.Features.Taches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.Affichage;

/// <summary>
/// Le tirage demandé à la main : réécrire la phrase du créneau, puis tirer l'image par
/// le chemin de l'appareil. Sans clé API, la banque de gabarits écrit — ce qui est le
/// fonctionnement normal d'une installation sans clé, et ce qui rend ces tests
/// hermétiques.
/// </summary>
public class TirageDuMurTests : TestAvecSqlite
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 20);
    private static readonly HumeurOptions Humeur = new();
    private readonly RenduEcranFictif _rendu = new();
    private readonly RedacteurFictif _redacteur = new();
    private readonly CacheImages _cache = new();

    private Task<TirageDuMurDto> Regenerer(MomentJournee moment, DateTime? horloge = null) =>
        TirageDuMur.RegenererAsync(
            Db, Humeur, _redacteur, null, null, null, _rendu, _cache, NullLogger.Instance, Aujourdhui, moment,
            horloge ?? Aujourdhui.ToDateTime(new TimeOnly(7, 0)), Aujourdhui.ToDateTime(new TimeOnly(7, 0)),
            CancellationToken.None);

    [Fact]
    public void Le_creneau_se_deduit_de_l_heure_comme_pour_le_service_de_fond()
    {
        // Avant l'heure du matin on est encore sur le SOIR DE LA VEILLE : c'est ce que
        // le service de fond fait, et un tirage à la main qui en déciderait autrement
        // écraserait la mauvaise phrase.
        var nuit = Aujourdhui.ToDateTime(new TimeOnly(3, 0));
        Assert.True(TirageDuMur.LireMoment(null, nuit, Humeur, out var date, out var moment));
        Assert.Equal(Aujourdhui.AddDays(-1), date);
        Assert.Equal(MomentJournee.Soir, moment);

        var matin = Aujourdhui.ToDateTime(new TimeOnly(9, 0));
        Assert.True(TirageDuMur.LireMoment(null, matin, Humeur, out _, out moment));
        Assert.Equal(MomentJournee.Matin, moment);

        var soir = Aujourdhui.ToDateTime(new TimeOnly(18, 0));
        Assert.True(TirageDuMur.LireMoment(null, soir, Humeur, out _, out moment));
        Assert.Equal(MomentJournee.Soir, moment);

        // Et les deux mots forcent la main, quelle que soit l'heure.
        Assert.True(TirageDuMur.LireMoment("soir", matin, Humeur, out _, out moment));
        Assert.Equal(MomentJournee.Soir, moment);
        Assert.True(TirageDuMur.LireMoment("MATIN", soir, Humeur, out _, out moment));
        Assert.Equal(MomentJournee.Matin, moment);
    }

    [Fact]
    public void Un_moment_inconnu_est_refuse_plutot_que_devine()
    {
        // « matun » veut dire le matin ; lui servir le soir en silence lui ferait tirer
        // la mauvaise conclusion de son essai.
        Assert.False(TirageDuMur.LireMoment("matun", DateTime.Now, Humeur, out _, out _));
        Assert.False(TirageDuMur.LireMoment("demain", DateTime.Now, Humeur, out _, out _));
    }

    [Fact]
    public async Task Regenerer_reecrit_la_phrase_meme_quand_elle_existe_deja()
    {
        // C'est TOUTE la différence avec le service de fond, qui passe son tour dès
        // qu'une phrase existe. Sans ça, « régénérer » ne régénérerait rien.
        Db.PhrasesDuJour.Add(new PhraseDuJour
        {
            Date = Aujourdhui,
            Moment = MomentJournee.Matin,
            Titre = "Une vieille manchette",
            SousTitre = "Écrite il y a longtemps",
            Source = SourcePhrase.Gabarit,
            GenereLe = DateTimeOffset.UtcNow.AddHours(-6),
        });
        await Db.SaveChangesAsync();

        var tirage = await Regenerer(MomentJournee.Matin);

        Assert.True(tirage.PhraseReecrite);
        Assert.NotEqual("Une vieille manchette", tirage.Titre);
        // Une seule phrase par créneau : on remplace, on n'empile pas.
        var phrases = await Db.PhrasesDuJour
            .Where(p => p.Date == Aujourdhui && p.Moment == MomentJournee.Matin)
            .ToListAsync();
        Assert.Single(phrases);
        Assert.Equal(tirage.Titre, phrases[0].Titre);
    }

    [Fact]
    public async Task Le_service_de_fond_lui_ne_reecrit_pas_ce_qui_existe()
    {
        // Le pendant du test précédent, sur la même couture : le réveil de fond repasse
        // sur le même créneau à chaque tour et ne doit pas rappeler le LLM pour rien.
        Db.PhrasesDuJour.Add(new PhraseDuJour
        {
            Date = Aujourdhui,
            Moment = MomentJournee.Soir,
            Titre = "Déjà écrite",
            SousTitre = "Et elle reste",
            Source = SourcePhrase.Llm,
            GenereLe = DateTimeOffset.UtcNow,
        });
        await Db.SaveChangesAsync();

        var (phrase, generee) = await GenerationHumeur.GenererAsync(
            Db, Humeur, NullLogger.Instance, Aujourdhui, MomentJournee.Soir,
            remplacer: false, CancellationToken.None);

        Assert.False(generee);
        Assert.Equal("Déjà écrite", phrase.Titre);
    }

    [Fact]
    public async Task L_image_est_tiree_par_le_chemin_de_l_appareil()
    {
        // Tirer à une autre taille ne prouverait rien du mur : c'est la taille annoncée
        // par l'appareil enrôlé qui compte, et le seuillage 1-bit avec.
        Db.AppareilsAffichage.Add(new HouseOs.Api.Domaine.AppareilAffichage
        {
            Id = Guid.NewGuid(),
            AdresseMac = "44:1B:F6:C0:02:94",
            Identifiant = "B73199",
            Cle = "x",
            Largeur = 1872,
            Hauteur = 1404,
            EnroleLe = DateTimeOffset.UtcNow,
            DernierFichier = "unautrefichier",
        });
        await Db.SaveChangesAsync();

        var tirage = await Regenerer(MomentJournee.Matin);

        Assert.Equal("B73199", tirage.Appareil);
        Assert.Equal(1872, tirage.Largeur);
        Assert.Equal(1404, tirage.Hauteur);
        Assert.Equal(1872, _rendu.DerniereDemande!.Largeur);
        Assert.True(tirage.Octets > 0);
        Assert.True(tirage.DifferentDuDernier);
    }

    [Fact]
    public async Task L_horloge_du_creneau_traverse_la_capture()
    {
        // C'est ainsi qu'un tirage « du soir » porte le surtitre du soir : la page
        // reçoit l'heure dans son URL et la repasse au serveur.
        var soir = Aujourdhui.ToDateTime(new TimeOnly(19, 0));
        await Regenerer(MomentJournee.Soir, soir);

        Assert.Equal(soir, _rendu.DerniereDemande!.Moment);
        Assert.Contains("maintenant=2026-09-20T19%3A00%3A00", _rendu.DerniereDemande.Requete);
    }

    [Fact]
    public async Task Sans_appareil_enrole_le_tirage_sort_quand_meme()
    {
        // Une installation neuve n'a pas encore d'écran : on tire à la taille de repli
        // plutôt que de refuser, sinon on ne pourrait rien vérifier avant l'enrôlement.
        var tirage = await Regenerer(MomentJournee.Matin);

        Assert.Null(tirage.Appareil);
        Assert.Equal(AffichageOptions.LargeurParDefaut, tirage.Largeur);
        Assert.True(tirage.DifferentDuDernier);
    }

    [Fact]
    public async Task La_phrase_servie_est_celle_du_creneau_compose()
    {
        // Le défaut trouvé au rendu : régénérer « le matin » en soirée écrivait bien la
        // phrase du matin, mais le journal continuait d'afficher celle du soir — « la
        // plus récente » l'emportait sur « celle du créneau », et le tirage ne montrait
        // donc pas ce qu'on venait de faire écrire.
        Db.PhrasesDuJour.AddRange(
            new PhraseDuJour
            {
                Date = Aujourdhui, Moment = MomentJournee.Matin,
                Titre = "Celle du matin", SousTitre = "…", Source = SourcePhrase.Llm,
                GenereLe = DateTimeOffset.UtcNow,
            },
            new PhraseDuJour
            {
                Date = Aujourdhui, Moment = MomentJournee.Soir,
                Titre = "Celle du soir", SousTitre = "…", Source = SourcePhrase.Llm,
                GenereLe = DateTimeOffset.UtcNow,
            });
        await Db.SaveChangesAsync();

        var matin = await HumeurEndpoints.PhraseCouranteAsync(Db, Aujourdhui, MomentJournee.Matin);
        Assert.Equal("Celle du matin", matin!.Titre);

        var soir = await HumeurEndpoints.PhraseCouranteAsync(Db, Aujourdhui, MomentJournee.Soir);
        Assert.Equal("Celle du soir", soir!.Titre);
    }

    [Fact]
    public async Task Un_matin_pas_encore_ecrit_retombe_sur_le_soir_de_la_veille()
    {
        // Le repli normal à six heures du matin : la phrase d'hier soir tient encore.
        // Ce qu'on ne veut pas, c'est aller chercher le soir du MÊME jour.
        Db.PhrasesDuJour.Add(new PhraseDuJour
        {
            Date = Aujourdhui.AddDays(-1), Moment = MomentJournee.Soir,
            Titre = "Hier soir", SousTitre = "…", Source = SourcePhrase.Gabarit,
            GenereLe = DateTimeOffset.UtcNow.AddHours(-13),
        });
        await Db.SaveChangesAsync();

        var phrase = await HumeurEndpoints.PhraseCouranteAsync(Db, Aujourdhui, MomentJournee.Matin);
        Assert.Equal("Hier soir", phrase!.Titre);
    }

    [Fact]
    public async Task L_heure_vraie_est_gardee_pour_le_creneau_qu_on_vit()
    {
        // Le pied du journal dit quand il a été imprimé : lui coller 7 h pile alors
        // qu'il est 9 h 12 mentirait sur la seule chose qu'il a à dire.
        var maintenant = Aujourdhui.ToDateTime(new TimeOnly(9, 12));
        Assert.Equal(
            maintenant,
            AffichageEndpoints.HorlogeDuCreneau(Aujourdhui, MomentJournee.Matin, Humeur, maintenant));

        // Mais un créneau qu'on ne vit pas est daté de son heure à lui.
        Assert.Equal(
            Aujourdhui.ToDateTime(MomentDEssai.Soir),
            AffichageEndpoints.HorlogeDuCreneau(Aujourdhui, MomentJournee.Soir, Humeur, maintenant));
    }
}
