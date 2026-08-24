using HouseOs.Api.Domaine.Humeur;

namespace HouseOs.Tests.Domaine;

public class BanquePhrasesTests
{
    private static readonly DateOnly Date = new(2026, 8, 24);

    private static EtatMaison Etat(
        int ouvertes = 0,
        int enRetard = 0,
        int faites = 0,
        MomentJournee moment = MomentJournee.Matin,
        IReadOnlyList<CompteProche>? comptes = null,
        MeteoDuJour? meteo = null) =>
        new(Date, moment, ouvertes, enRetard, faites, comptes ?? [], meteo);

    // --- Clé d'état ---

    [Theory]
    [InlineData(0, 0, 3, CleEtat.ToutFait)]
    [InlineData(0, 0, 0, CleEtat.RienAuProgramme)]
    [InlineData(3, 1, 0, CleEtat.Retard)]
    [InlineData(6, 0, 0, CleEtat.JourneeChargee)]
    [InlineData(2, 0, 1, CleEtat.Calme)]
    public void CleEtat_DeriveeDesFaits(int ouvertes, int enRetard, int faites, CleEtat attendue)
    {
        Assert.Equal(attendue, Etat(ouvertes, enRetard, faites).Cle);
    }

    // --- Banque ---

    [Fact]
    public void ToutFait_FeliciteSansCulpabiliser()
    {
        var (titre, sousTitre) = BanquePhrases.Generer(Etat(faites: 3));
        Assert.False(string.IsNullOrWhiteSpace(titre));
        Assert.Contains("3 choses", sousTitre);
    }

    [Fact]
    public void Retard_ResteRassurant()
    {
        var (_, sousTitre) = BanquePhrases.Generer(Etat(ouvertes: 4, enRetard: 2));
        Assert.Contains("4 petites choses", sousTitre);
        Assert.Contains("2 attendent depuis un moment", sousTitre);
        Assert.Contains("sous contrôle", sousTitre);
    }

    [Fact]
    public void RetardSingulier_AttendDepuisHier()
    {
        var (_, sousTitre) = BanquePhrases.Generer(Etat(ouvertes: 1, enRetard: 1));
        Assert.Contains("1 petite chose", sousTitre);
        Assert.Contains("une attend depuis hier", sousTitre);
    }

    [Fact]
    public void CompteARebours_ProcheTeinteLeTitre()
    {
        var proche = new CompteProche("Déménagement", 12);
        var (titre, _) = BanquePhrases.Generer(Etat(ouvertes: 2, comptes: [proche]));
        var titresAttendus = new[]
        {
            "On y est presque.", "Bientôt le grand jour.",
            "Le compte à rebours est parti.", "Ça s'en vient pour vrai.",
        };
        Assert.Contains(titre, titresAttendus);
    }

    [Fact]
    public void CompteARebours_LointainNInfluencePas()
    {
        var lointain = new CompteProche("Noël", 120);
        var (titre, _) = BanquePhrases.Generer(Etat(ouvertes: 2, comptes: [lointain]));
        Assert.DoesNotContain(titre, new[] { "On y est presque.", "Bientôt le grand jour." });
    }

    [Fact]
    public void BeauTemps_AjouteUneToucheMeteo()
    {
        var meteo = new MeteoDuJour(12, 24, 10, ["Être dehors"]);
        var (_, sousTitre) = BanquePhrases.Generer(Etat(ouvertes: 2, meteo: meteo));
        var touches = new[] { "il fait beau", "nez dehors" };
        Assert.Contains(touches, t => sousTitre.Contains(t));
    }

    [Fact]
    public void JourDePluie_MentionneLaPluie()
    {
        var meteo = new MeteoDuJour(10, 15, 80, []);
        var (_, sousTitre) = BanquePhrases.Generer(Etat(ouvertes: 2, meteo: meteo));
        var touches = new[] { "Parapluie", "au chaud" };
        Assert.Contains(touches, t => sousTitre.Contains(t));
    }

    [Fact]
    public void JourneeChargee_PasDeToucheMeteo()
    {
        var meteo = new MeteoDuJour(12, 24, 10, ["Être dehors"]);
        var (_, sousTitre) = BanquePhrases.Generer(Etat(ouvertes: 7, meteo: meteo));
        Assert.DoesNotContain("il fait beau", sousTitre);
    }

    [Fact]
    public void Rotation_StableLeJourEtDifferenteLeSoir()
    {
        var matin1 = BanquePhrases.Generer(Etat(ouvertes: 2));
        var matin2 = BanquePhrases.Generer(Etat(ouvertes: 2));
        var soir = BanquePhrases.Generer(Etat(ouvertes: 2, moment: MomentJournee.Soir));
        Assert.Equal(matin1, matin2);
        Assert.NotEqual(matin1.Titre, soir.Titre);
    }
}
