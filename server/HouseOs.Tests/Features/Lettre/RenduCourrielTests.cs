using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Lettre;

namespace HouseOs.Tests.Features.Lettre;

/// <summary>Le gabarit du courriel : ce qu'il pose autour des paragraphes, en texte et
/// en HTML, et le lien qui n'existe que si l'URL est réglée.</summary>
public class RenduCourrielTests
{
    private static readonly LettreDuMatin Lettre = new()
    {
        Date = new DateOnly(2026, 9, 20), Sujet = "Demain, le camion",
        Paragraphes = ["Un dodo. Demain matin, le camion.", "Dehors, trois degrés & du soleil."],
        Source = SourceLettre.Llm, ComposeeLe = new DateTimeOffset(2026, 9, 20, 6, 28, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 9, 20))),
    };

    [Fact]
    public void Le_texte_pose_la_date_la_salutation_l_adieu_la_signature_et_le_pied()
    {
        var texte = RenduCourriel.Texte(Lettre, new LettreOptions());
        Assert.StartsWith("Dimanche 20 septembre 2026\n\nBonjour vous deux.\n\nUn dodo.", texte);
        Assert.Contains("\n\nBonne journée.\n\n— la maison\n\nHouse OS · écrite à 6 h 28\n", texte);
        Assert.DoesNotContain("Ouvrir la journée", texte);
    }

    [Fact]
    public void Le_html_echappe_et_n_appelle_rien_de_distant()
    {
        var html = RenduCourriel.Html(Lettre, new LettreOptions { UrlDeLApp = "https://maison.exemple.tld/" });
        Assert.Contains("trois degrés &amp; du soleil", html);
        Assert.Contains("<a href=\"https://maison.exemple.tld/\"", html);
        Assert.DoesNotContain("<img", html);
        Assert.DoesNotContain("<link", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void Le_courriel_porte_le_sujet_ecrit_et_les_deux_corps()
    {
        var courriel = RenduCourriel.Composer(Lettre, [new("Alain", "alain@exemple.tld")], new LettreOptions());
        Assert.Equal("Demain, le camion", courriel.Sujet);
        Assert.Contains("Bonjour vous deux.", courriel.Texte);
        Assert.Contains("Bonjour vous deux.", courriel.Html);
    }
}
