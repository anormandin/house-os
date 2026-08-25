using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Features.Humeur;

namespace HouseOs.Tests.Features.Humeur;

public class PolissageLlmTests
{
    // --- Extraction de la réponse LLM (jamais de réseau dans les tests) ---

    [Fact]
    public void Extraire_JsonPropre()
    {
        var resultat = PolissageLlm.Extraire("""{"titre": "On y est presque.", "sousTitre": "3 dodos avant le camion."}""");
        Assert.Equal(("On y est presque.", "3 dodos avant le camion."), resultat);
    }

    [Fact]
    public void Extraire_TolereLesCloturesDeCode()
    {
        var resultat = PolissageLlm.Extraire("""
            ```json
            {"titre": "Tout doux.", "sousTitre": "Rien au programme."}
            ```
            """);
        Assert.Equal(("Tout doux.", "Rien au programme."), resultat);
    }

    [Theory]
    [InlineData("pas du json")]
    [InlineData("""{"titre": "Sans sous-titre"}""")]
    [InlineData("""{"titre": "", "sousTitre": "vide"}""")]
    public void Extraire_RejetteLesReponsesInvalides(string texte)
    {
        Assert.Null(PolissageLlm.Extraire(texte));
    }

    [Fact]
    public void Extraire_RejetteUnTitreTropLong()
    {
        var tropLong = new string('a', 80);
        Assert.Null(PolissageLlm.Extraire($$"""{"titre": "{{tropLong}}", "sousTitre": "ok"}"""));
    }

    [Theory]
    [InlineData("""{"titre": 5, "sousTitre": "ok"}""")]
    [InlineData("""{"titre": "ok", "sousTitre": {"texte": "imbriqué"}}""")]
    [InlineData("""{"titre": ["tableau"], "sousTitre": "ok"}""")]
    [InlineData("""{"titre": true, "sousTitre": "ok"}""")]
    public void Extraire_ChampNonTextuel_RendNull(string texte)
    {
        // Contrat « tout écart → null » : jamais d'exception qui sortirait du parse.
        Assert.Null(PolissageLlm.Extraire(texte));
    }

    [Fact]
    public void Extraire_TolereLaProseAvecAccolades_AutourDuJson()
    {
        var resultat = PolissageLlm.Extraire(
            """Voici l'objet {titre, sousTitre} demandé : {"titre": "Bon matin.", "sousTitre": "Deux affaires au programme."} — bonne journée!""");

        Assert.Equal(("Bon matin.", "Deux affaires au programme."), resultat);
    }

    [Fact]
    public void Extraire_JsonTronque_RendNull()
    {
        // MaxTokens peut couper la réponse en plein vol.
        Assert.Null(PolissageLlm.Extraire("""{"titre": "Coupé net.", "sousTi"""));
    }

    [Theory]
    [InlineData(60, 180, true)]   // bornes exactes acceptées
    [InlineData(61, 180, false)]  // titre juste trop long
    [InlineData(60, 181, false)]  // sous-titre juste trop long
    public void Extraire_BornesDeLongueur(int longueurTitre, int longueurSousTitre, bool accepte)
    {
        var titre = new string('t', longueurTitre);
        var sousTitre = new string('s', longueurSousTitre);

        var resultat = PolissageLlm.Extraire(
            $$"""{"titre": "{{titre}}", "sousTitre": "{{sousTitre}}"}""");

        Assert.Equal(accepte, resultat is not null);
    }

    // --- Sérialisation de l'état (le contrat vu par le LLM) ---

    [Fact]
    public void SerialiserEtat_ContientLesFaitsEtRienDAutre()
    {
        var etat = new EtatMaison(
            new DateOnly(2026, 10, 3), MomentJournee.Matin,
            Ouvertes: 4, EnRetard: 1, FaitesAujourdhui: 2,
            ComptesProches: [new CompteProche("Déménagement", 3)],
            TachesDuJour: ["Vider le garage"],
            ProchainesTaches: [new TacheAVenir("Changer les filtres", 2)],
            MeteoRemarquable: new SignalMeteoRemarquable("des orages attendus aujourd'hui", Favorable: false));

        var json = PolissageLlm.SerialiserEtat(etat);

        Assert.Contains("\"moment\":\"matin\"", json);
        Assert.Contains("\"tachesOuvertes\":4", json);
        Assert.Contains("\"tachesEnRetard\":1", json);
        Assert.Contains("\"dodos\":3", json);
        Assert.Contains("D\\u00E9m\\u00E9nagement", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Vider le garage", json);
        Assert.Contains("\"dansJours\":2", json);
        Assert.Contains("orages", json);
    }

    [Fact]
    public void SerialiserEtat_MeteoOrdinaireAbsente()
    {
        var etat = new EtatMaison(
            new DateOnly(2026, 8, 24), MomentJournee.Soir,
            Ouvertes: 0, EnRetard: 0, FaitesAujourdhui: 5,
            ComptesProches: [], TachesDuJour: [], ProchainesTaches: [],
            MeteoRemarquable: null);

        var json = PolissageLlm.SerialiserEtat(etat);

        Assert.Contains("\"moment\":\"soir\"", json);
        Assert.Contains("\"meteoRemarquable\":null", json);
        Assert.Contains("ToutFait", json);
    }
}
