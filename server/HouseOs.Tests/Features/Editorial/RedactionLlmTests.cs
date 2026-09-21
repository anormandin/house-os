using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.Editorial;

namespace HouseOs.Tests.Features.Editorial;

/// <summary>Le parse strict de l'éditorialiste — jamais de réseau dans les tests.</summary>
public class RedactionLlmTests
{
    private static readonly MatiereDEdition Chronique = new(
        new DateOnly(2026, 11, 8), RangEdition.Chronique, null, [], null, null,
        [new FaitPourEdition("ciel.jour", "Ciel", "Durée du jour", "9 h 58", "Le jour raccourcit.", true)],
        [], "17 rue de la Colline");

    private static readonly MatiereDEdition Sommaire = Chronique with
    {
        Rang = RangEdition.Sommaire,
        TachesDues =
        [
            new TachePourEdition("SAAQ — changement d'adresse", 0, false, "Alain"),
            new TachePourEdition("Hydro — transfert", 0, false, null),
            new TachePourEdition("Pneus d'hiver", 2, false, "Ariane"),
        ],
    };

    private const string Propre = """
        {"surtitre": "Première fin de semaine libre depuis le 26 août",
         "manchette": "La maison ne demande rien",
         "chapeau": "Aucune occurrence due · 33 jours dans la maison",
         "paragraphes": ["Onze semaines que la liste n'avait pas été vide un dimanche.", "Rien ne revient avant le 15."],
         "rubriques": []}
        """;

    [Fact]
    public void Extraire_JsonPropre()
    {
        var texte = RedactionLlm.Extraire(Propre, Chronique);
        Assert.NotNull(texte);
        Assert.Equal("La maison ne demande rien", texte.Manchette);
        Assert.Equal(2, texte.Paragraphes.Count);
        Assert.Empty(texte.Rubriques);
    }

    [Fact]
    public void Extraire_TolereLesCloturesDeCodeEtLaProse()
    {
        var texte = RedactionLlm.Extraire($"Voici l'édition {{demandée}} :\n```json\n{Propre}\n```\nBonne journée.", Chronique);
        Assert.NotNull(texte);
        Assert.Equal("La maison ne demande rien", texte.Manchette);
    }

    [Theory]
    [InlineData("pas du json")]
    [InlineData("""{"manchette": "Sans chapeau", "paragraphes": ["a", "b"]}""")]
    [InlineData("""{"surtitre": "", "manchette": "", "chapeau": "x", "paragraphes": ["a", "b"]}""")]
    [InlineData("""{"surtitre": "", "manchette": "Un seul paragraphe", "chapeau": "x", "paragraphes": ["a"]}""")]
    [InlineData("""{"surtitre": "", "manchette": "Trois paragraphes", "chapeau": "x", "paragraphes": ["a", "b", "c"]}""")]
    [InlineData("""{"surtitre": "", "manchette": "Paragraphe vide", "chapeau": "x", "paragraphes": ["a", "  "]}""")]
    [InlineData("""{"surtitre": 5, "manchette": "Non textuel", "chapeau": "x", "paragraphes": ["a", "b"]}""")]
    [InlineData("""{"surtitre": "", "manchette": ["tableau"], "chapeau": "x", "paragraphes": ["a", "b"]}""")]
    [InlineData("""{"surtitre": "", "manchette": "Coupé net", "chapeau": "x", "paragraphes": ["a", "b""")]
    [InlineData("""[{"manchette": "Un tableau à la racine"}]""")]
    public void Extraire_RejetteToutEcart(string texte)
    {
        Assert.Null(RedactionLlm.Extraire(texte, Chronique));
    }

    [Theory]
    [InlineData(60, 160, 240, true)]
    [InlineData(61, 160, 240, false)]
    [InlineData(60, 161, 240, false)]
    [InlineData(60, 160, 241, false)]
    public void Extraire_BornesDeLongueur(int manchette, int chapeau, int paragraphe, bool accepte)
    {
        var json = $$"""
            {"surtitre": "", "manchette": "{{new string('m', manchette)}}", "chapeau": "{{new string('c', chapeau)}}",
             "paragraphes": ["{{new string('p', paragraphe)}}", "b"]}
            """;
        Assert.Equal(accepte, RedactionLlm.Extraire(json, Chronique) is not null);
    }

    [Fact]
    public void Extraire_DitCeQuiAEteRefuse()
    {
        // Sans ça, « hors contrat » dans le journal ne dit pas s'il faut retoucher le
        // prompt ou la borne.
        Assert.Null(RedactionLlm.Extraire("pas du json", Chronique, out var ecart));
        Assert.Equal("aucun objet JSON", ecart);
        var tropLong = new string('p', 250);
        Assert.Null(RedactionLlm.Extraire(
            $$"""{"surtitre": "", "manchette": "Trop", "chapeau": "x", "paragraphes": ["{{tropLong}}", "b"]}""",
            Chronique, out ecart));
        Assert.Equal("paragraphe 1 : 250 signes, 240 au plus", ecart);
        Assert.Null(RedactionLlm.Extraire("""{"surtitre": 5, "manchette": "x", "chapeau": "x", "paragraphes": ["a", "b"]}""", Chronique, out ecart));
        Assert.Equal("surtitre : pas du texte", ecart);
        Assert.NotNull(RedactionLlm.Extraire(Propre, Chronique, out ecart));
        Assert.Null(ecart);
    }

    [Fact]
    public void Extraire_NormaliseLesBlancs()
    {
        var texte = RedactionLlm.Extraire("""
            {"surtitre": "  Deux   mots ", "manchette": "La\nmaison", "chapeau": "x", "paragraphes": ["a", "b"]}
            """, Chronique);
        Assert.Equal("Deux mots", texte!.Surtitre);
        Assert.Equal("La maison", texte.Manchette);
    }

    [Fact]
    public void Extraire_LesRubriquesNeGardentQueDesTachesReelles()
    {
        // Un titre inventé par le modèle est écarté, jamais affiché (D-2026-09-20
        // Regroupement Sans Catégorie De Tâche) ; un titre placé deux fois ne compte
        // qu'une fois ; une rubrique vidée disparaît.
        var texte = RedactionLlm.Extraire("""
            {"surtitre": "", "manchette": "Journée chargée", "chapeau": "x", "paragraphes": ["a", "b"],
             "rubriques": [
               {"nom": "Gouvernements", "taches": ["SAAQ — changement d'adresse", "Passeport (inventé)"]},
               {"nom": "Énergie", "taches": ["Hydro — transfert", "SAAQ — changement d'adresse"]},
               {"nom": "Fantômes", "taches": ["Rien de vrai"]}
             ]}
            """, Sommaire);
        Assert.NotNull(texte);
        Assert.Collection(texte.Rubriques,
            r =>
            {
                Assert.Equal("Gouvernements", r.Nom);
                Assert.Equal(["SAAQ — changement d'adresse"], r.Taches);
            },
            r =>
            {
                Assert.Equal("Énergie", r.Nom);
                Assert.Equal(["Hydro — transfert"], r.Taches);
            });
    }

    [Fact]
    public void Extraire_IgnoreLesRubriquesHorsDuSommaire()
    {
        var texte = RedactionLlm.Extraire("""
            {"surtitre": "", "manchette": "Calme", "chapeau": "x", "paragraphes": ["a", "b"],
             "rubriques": [{"nom": "Gouvernements", "taches": ["SAAQ — changement d'adresse"]}]}
            """, Chronique);
        Assert.NotNull(texte);
        Assert.Empty(texte.Rubriques);
    }

    [Fact]
    public void Extraire_UneRubriqueMalFormeeRejetteTout()
    {
        Assert.Null(RedactionLlm.Extraire("""
            {"surtitre": "", "manchette": "Journée chargée", "chapeau": "x", "paragraphes": ["a", "b"],
             "rubriques": [{"nom": 3, "taches": []}]}
            """, Sommaire));
        Assert.Null(RedactionLlm.Extraire("""
            {"surtitre": "", "manchette": "Journée chargée", "chapeau": "x", "paragraphes": ["a", "b"],
             "rubriques": ["pas un objet"]}
            """, Sommaire));
    }

    [Fact]
    public void SerialiserMatiere_ContientLesFaitsEtLaMemoire()
    {
        var matiere = Sommaire with
        {
            Plancher = new PlancherDuJour(RaisonDePlancher.Ferme, "Signer chez le notaire"),
            ProchainCompte = new CompteProcheDEdition("Le camion", 4),
            Meteo = new MeteoDEdition("nuageux", -1, 4),
            Precedentes = [new EditionPrecedente(new DateOnly(2026, 11, 7), "Hier", "La veille", "Chapeau d'hier")],
        };

        var json = RedactionLlm.SerialiserMatiere(matiere);

        Assert.Contains("\"rang\":\"Sommaire\"", json);
        Assert.Contains("\"raison\":\"Ferme\"", json);
        Assert.Contains("\"dodos\":4", json);
        Assert.Contains("\"publie\":true", json);
        Assert.Contains("nuageux", json);
        Assert.Contains("La veille", json);
        Assert.Contains("Pneus d", json);
        Assert.Contains("\"joursDeRetard\":2", json);
    }
}
