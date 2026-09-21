using HouseOs.Api.Features.FondsDeTiroir;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// La lecture du fichier de banque. Un fichier absent, illisible ou à moitié faux ne
/// doit jamais coûter le journal du matin : la famille se tait, et tout le reste sort
/// (vault : Fonds De Tiroir — « un fait dont la source manque ne sort pas »).
/// </summary>
public class LectureDeLaBanqueTests : IDisposable
{
    private readonly string _dossier =
        Directory.CreateTempSubdirectory("banque-du-hasard").FullName;

    public void Dispose() => Directory.Delete(_dossier, recursive: true);

    private string Ecrire(string contenu)
    {
        var chemin = Path.Combine(_dossier, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(chemin, contenu);
        return chemin;
    }

    private static BanqueDuHasard Lire(string? chemin) =>
        LectureDeLaBanque.Lire(chemin, AppContext.BaseDirectory, NullLogger.Instance);

    [Fact]
    public void Sans_chemin_configure_c_est_la_banque_livree_avec_l_app()
    {
        var banque = Lire(null);

        Assert.False(banque.EstVide);
        Assert.Equal(12, banque.Dictons.Select(d => d.Mois).Distinct().Count());
        Assert.Contains(banque.Fetes, f => f.Nom == "La Saint-Jean" && f.Ferie);
    }

    [Fact]
    public void Un_chemin_configure_remplace_la_banque_livree()
    {
        // C'est tout l'intérêt : un foyer ailleurs échange le fichier et garde le code
        // (vault : Distribution).
        var chemin = Ecrire("""
            { "dictons": [ { "mois": 3, "texte": "Märzenstaub bringt Gras und Laub." } ],
              "fetes": [] }
            """);

        var banque = Lire(chemin);
        var dicton = Assert.Single(banque.Dictons);
        Assert.Equal("Märzenstaub bringt Gras und Laub.", dicton.Texte);
        Assert.Empty(banque.Fetes);
    }

    [Fact]
    public void Un_chemin_configure_et_introuvable_ne_retombe_pas_sur_la_banque_livree()
    {
        // Servir des jours fériés québécois à un foyer qui a justement demandé les
        // siens serait pire que le silence.
        Assert.True(Lire(Path.Combine(_dossier, "rien.json")).EstVide);
    }

    [Fact]
    public void Un_fichier_illisible_fait_taire_la_famille_sans_faire_tomber_l_app()
    {
        Assert.True(Lire(Ecrire("{ ceci n'est pas du JSON")).EstVide);
        Assert.True(Lire(Ecrire("null")).EstVide);
    }

    [Fact]
    public void Les_commentaires_et_la_virgule_finale_sont_toleres()
    {
        // Ce fichier se modifie à la main, à côté du `.env` : une virgule de trop ne
        // doit pas coûter le journal du matin.
        var banque = Lire(Ecrire("""
            {
              // Le dicton de mars.
              "dictons": [ { "mois": 3, "texte": "Mars venteux, verger pompeux." }, ],
              "fetes": [],
            }
            """));

        Assert.Single(banque.Dictons);
    }

    [Fact]
    public void Une_entree_incomplete_est_ecartee_sans_couter_les_autres()
    {
        // Une faute dans une ligne ne doit pas coûter la banque entière.
        var banque = Lire(Ecrire("""
            {
              "dictons": [
                { "mois": 3, "texte": "Mars venteux, verger pompeux." },
                { "mois": 13, "texte": "Un treizième mois." },
                { "mois": 4, "texte": "   " }
              ],
              "fetes": [
                { "nom": "La Saint-Jean", "texte": "Feux et défilés.", "ferie": true,
                  "quand": { "mois": 6, "jour": 24 } },
                { "nom": "Sans quand", "texte": "Rien pour la dater." },
                { "nom": "", "texte": "Sans nom.", "quand": { "mois": 1, "jour": 1 } }
              ]
            }
            """));

        Assert.Single(banque.Dictons);
        Assert.Single(banque.Fetes);
    }

    [Fact]
    public void Une_banque_sans_section_se_lit_quand_meme()
    {
        // Un fichier qui n'a que des fêtes est un choix légitime : un foyer peut ne
        // pas vouloir de dictons.
        var banque = Lire(Ecrire("""{ "fetes": [] }"""));

        Assert.True(banque.EstVide);
        Assert.Empty(banque.Dictons);
    }
}
