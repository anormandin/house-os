using System.Text.Json;
using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Lettre;

namespace HouseOs.Tests.Features.Lettre;

/// <summary>Le contrat de sortie de la lettre, écart par écart (vault : Prompt De La
/// Lettre). Le modèle n'est jamais appelé ici.</summary>
public class RedactionLettreTests
{
    private static string P(int longueur, char c = 'a') =>
        string.Join(" ", Enumerable.Repeat(new string(c, 9), longueur / 10)) + ".";

    private static string Lettre(string sujet = "Demain, le camion", params string[] paragraphes) =>
        JsonSerializer.Serialize(new { sujet, paragraphes = paragraphes.Length == 0 ? [P(150), P(200), P(180)] : paragraphes });

    [Fact]
    public void Une_lettre_conforme_est_acceptee_meme_entouree_de_prose()
    {
        var texte = RedactionLettre.Extraire("Voici la lettre :\n```json\n" + Lettre() + "\n```", out var ecart);
        Assert.NotNull(texte);
        Assert.Null(ecart);
        Assert.Equal("Demain, le camion", texte.Sujet);
        Assert.Equal(3, texte.Paragraphes.Count);
    }

    [Theory]
    [InlineData("Un sujet beaucoup trop long pour tenir dans les soixante signes permis", "sujet : ")]
    [InlineData("Demain, le camion.", "sujet : point final")]
    [InlineData("Demain, le camion !", "sujet : point d'exclamation")]
    [InlineData("", "sujet : vide")]
    public void Un_sujet_hors_contrat_est_refuse(string sujet, string ecartAttendu)
    {
        Assert.Null(RedactionLettre.Extraire(Lettre(sujet), out var ecart));
        Assert.StartsWith(ecartAttendu, ecart);
    }

    [Fact]
    public void Deux_paragraphes_ou_six_sont_refuses()
    {
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), P(150)), out var deux));
        Assert.StartsWith("paragraphes", deux);
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), P(100), P(100), P(100), P(100), P(100)), out var six));
        Assert.StartsWith("paragraphes", six);
        Assert.NotNull(RedactionLettre.Extraire(Lettre("S", P(150), P(100), P(100), P(100), P(100))));
    }

    [Fact]
    public void Un_paragraphe_trop_long_ou_un_premier_trop_court_est_refuse()
    {
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), P(380), P(100)), out var long_));
        Assert.Equal("paragraphe 2 : 380 signes, 360 au plus", long_);
        Assert.NotNull(RedactionLettre.Extraire(Lettre("S", P(150), P(340), P(100))));
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(80), P(100), P(100)), out var court));
        Assert.StartsWith("paragraphe 1 : 80 signes, 100 au moins", court);
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), P(40), P(100)), out var moignon));
        Assert.StartsWith("paragraphe 2 : 40 signes, 60 au moins", moignon);
    }

    [Fact]
    public void Le_plafond_total_mord_meme_quand_chaque_paragraphe_passe()
    {
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(300), P(300), P(300), P(300), P(300)), out var ecart));
        Assert.StartsWith("total : 1500 signes", ecart);
    }

    [Theory]
    [InlineData("Bonjour vous deux, il ne reste qu'une chose aujourd'hui et c'est la même que dimanche dernier, les boîtes du bureau.", "salutation")]
    [InlineData("Salut, il ne reste qu'une chose aujourd'hui et c'est la même que dimanche dernier, les boîtes du bureau et rien d'autre.", "salutation")]
    public void La_salutation_redonnee_par_reflexe_est_refusee(string premier, string mot)
    {
        Assert.Null(RedactionLettre.Extraire(Lettre("S", premier, P(100), P(100)), out var ecart));
        Assert.Contains(mot, ecart);
    }

    [Fact]
    public void La_signature_et_l_adieu_sont_au_gabarit()
    {
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), P(100), P(100)[..90] + " — la maison"), out var signature));
        Assert.Contains("signature", signature);
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), P(100), P(100)[..85] + " Bonne journée."), out var adieu));
        Assert.Contains("Bonne journée", adieu);
    }

    [Theory]
    [InlineData("- ")]
    [InlineData("• ")]
    [InlineData("1. ")]
    [InlineData("# ")]
    public void Une_liste_deguisee_est_refusee(string amorce)
    {
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), amorce + P(100), P(100)), out var ecart));
        Assert.Contains("liste", ecart);
    }

    [Fact]
    public void Un_point_d_exclamation_fait_basculer_dans_l_infolettre()
    {
        Assert.Null(RedactionLettre.Extraire(Lettre("S", P(150), P(100)[..95] + " oui !", P(100)), out var ecart));
        Assert.Contains("exclamation", ecart);
    }

    [Fact]
    public void Un_champ_non_textuel_ou_un_json_absent_rendent_null()
    {
        Assert.Null(RedactionLettre.Extraire("""{"sujet": 12, "paragraphes": []}""", out var nombre));
        Assert.Equal("sujet : pas du texte", nombre);
        Assert.Null(RedactionLettre.Extraire("Je préfère ne rien écrire ce matin.", out var rien));
        Assert.Equal("aucun objet JSON", rien);
    }

    [Fact]
    public void La_matiere_se_conserve_et_se_relit_a_l_identique()
    {
        var date = new DateOnly(2026, 9, 20);
        var matiere = new MatiereDeLettre(
            new MatiereDEdition(date, RangEdition.Manchette,
                new PlancherDuJour(RaisonDePlancher.Ferme, "Garde de l'animal"),
                [new("Emballer le bureau", 0, false, "Alain", "Bureau"), new("Garde de l'animal", 0, true, "Ariane")],
                new("Déménagement", 16), new("ciel dégagé", 9.1, 19.4),
                [new("ciel.equinoxe", "Ciel", "ÉQUINOXE", "mardi", "L'équinoxe est mardi soir.", true)],
                [], "17 rue de la Colline"),
            [new(date.AddDays(2), "Prêteur", "Ariane", true)],
            [new(date.AddDays(-1), "Changer les draps", "Alain")],
            new Dictionary<string, int> { ["Emballer le bureau"] = 4 },
            [new(date.AddDays(-7), "Le sous-sol, et rien d'autre", "Une seule chose aujourd'hui : emballer le sous-sol.")]);

        var json = RedactionLettre.SerialiserMatiere(matiere);
        Assert.Contains("\"serie\":4", json);
        Assert.Contains("\"semaineDevant\"", json);
        Assert.Contains("\"faitesDepuisLaDerniere\"", json);
        Assert.Contains("\"premiereLigne\"", json);

        var relue = RedactionLettre.DeserialiserMatiere(json);
        Assert.NotNull(relue);
        Assert.Equal(matiere.Edition.Date, relue.Edition.Date);
        Assert.Equal(matiere.Edition.Rang, relue.Edition.Rang);
        Assert.Equal(matiere.Edition.Plancher, relue.Edition.Plancher);
        Assert.Equal(matiere.Edition.TachesDues, relue.Edition.TachesDues);
        Assert.Equal(matiere.Edition.ProchainCompte, relue.Edition.ProchainCompte);
        Assert.Equal(matiere.Edition.Meteo, relue.Edition.Meteo);
        Assert.Equal(matiere.Edition.Faits, relue.Edition.Faits);
        Assert.Empty(relue.Edition.Precedentes);
        Assert.Equal(matiere.Edition.Lieu, relue.Edition.Lieu);
        Assert.Equal(matiere.SemaineDevant, relue.SemaineDevant);
        Assert.Equal(matiere.FaitesDepuisLaDerniere, relue.FaitesDepuisLaDerniere);
        Assert.Equal(matiere.Precedentes, relue.Precedentes);
        Assert.Equal(4, relue.Serie("Emballer le bureau"));
        Assert.Equal(0, relue.Serie("Garde de l'animal"));
        Assert.Null(RedactionLettre.DeserialiserMatiere("pas du json"));
    }

    [Fact]
    public void Le_prompt_de_la_lettre_ne_partage_rien_avec_celui_du_mur()
    {
        Assert.NotEqual(HouseOs.Api.Features.Editorial.RedactionLlm.PromptParDefaut, RedactionLettre.PromptParDefaut);
        Assert.Contains("« serie »", RedactionLettre.PromptParDefaut);
        Assert.Contains("faitesDepuisLaDerniere", RedactionLettre.PromptParDefaut);
    }
}
