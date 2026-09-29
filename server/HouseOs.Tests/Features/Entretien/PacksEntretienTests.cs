using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Entretien;
using HouseOs.Api.Features.Taches;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.Entretien;

/// <summary>
/// Le fichier de packs livré et sa lecture (vault : D-2026-09-28 Packs D'entretien En
/// Fichier De Données). Le fichier est de la donnée d'édition : chaque item doit faire
/// une récurrence que le moteur accepte, et un item boiteux ne coûte jamais le démarrage.
/// </summary>
public class PacksEntretienTests : IDisposable
{
    private readonly string _dossier = Directory.CreateTempSubdirectory("packs-entretien").FullName;
    private static readonly DateOnly Aujourdhui = new(2026, 9, 29);

    public void Dispose() => Directory.Delete(_dossier, recursive: true);

    private string Ecrire(string contenu)
    {
        var chemin = Path.Combine(_dossier, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(chemin, contenu);
        return chemin;
    }

    private static PacksEntretien Lire(string? chemin) =>
        LecturePacks.Lire(chemin, AppContext.BaseDirectory, NullLogger.Instance);

    [Fact]
    public void Chaque_item_du_pack_livre_est_une_recurrence_valide()
    {
        var packs = Lire(null);

        Assert.False(packs.EstVide);
        Assert.NotEmpty(packs.Maison);
        // Toutes les catégories qui ont un pack, et jamais « Autre » (D-2026-09-28).
        Assert.DoesNotContain(CategorieEquipement.Autre, packs.ParCategorie.Keys);
        Assert.Contains(CategorieEquipement.Chauffage, packs.ParCategorie.Keys);
        Assert.Contains(CategorieEquipement.Vehicule, packs.ParCategorie.Keys);

        // La lecture écarte ce qui ne passe pas : si le fichier livré perdait un item
        // en route, ce test ne le verrait pas. On rejoue donc la conversion sur le brut.
        var brut = System.Text.Json.JsonSerializer.Deserialize<FichierDePacks>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, LecturePacks.NomDuFichierParDefaut)),
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
            {
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            })!;
        var tous = brut.Maison.Concat(brut.ParCategorie.Values.SelectMany(v => v)).ToList();
        Assert.Equal(tous.Count, packs.Nombre);
        foreach (var item in tous)
        {
            var (spec, erreur) = OperationsTaches.ConvertirRecurrence(item.Recurrence, Aujourdhui);
            Assert.True(erreur is null, $"{item.Cle} : {erreur}");
            Assert.NotEqual(ModeRecurrence.Ponctuelle, spec.Mode);
        }
    }

    [Fact]
    public void Un_item_boiteux_est_ecarte_sans_couter_le_reste()
    {
        var chemin = Ecrire("""
            {
              "maison": [
                { "cle": "ok", "titre": "Nettoyer les gouttières",
                  "recurrence": { "mode": "Fixe", "fixeType": "Annuelle", "moisAnnuel": 11, "jourAnnuel": 1 } },
                { "cle": "ponctuelle", "titre": "Une fois", "recurrence": { "mode": "Ponctuelle" } },
                { "cle": "sans-recurrence", "titre": "Rien" },
                { "cle": "ok", "titre": "Clé en double",
                  "recurrence": { "mode": "Intervalle", "intervalleJours": 30 } },
                { "cle": "fenetre-impossible", "titre": "Fenêtre du 31 février",
                  "recurrence": { "mode": "Intervalle", "intervalleJours": 30,
                    "fenetreDebutMois": 2, "fenetreDebutJour": 31, "fenetreFinMois": 3, "fenetreFinJour": 1 } },
              ],
              "parCategorie": {
                "Autre": [ { "cle": "autre.x", "titre": "X", "recurrence": { "mode": "Intervalle", "intervalleJours": 7 } } ],
                "Inconnue": [ { "cle": "inc.x", "titre": "X", "recurrence": { "mode": "Intervalle", "intervalleJours": 7 } } ],
                "eauchaude": [ { "cle": "eau.vidange", "titre": "Vidanger",
                  "recurrence": { "mode": "Intervalle", "intervalleJours": 365 }, "strategie": "alternance" } ],
              }
            }
            """);

        var packs = Lire(chemin);

        Assert.Equal("ok", Assert.Single(packs.Maison).Cle);
        var eau = Assert.Single(packs.ParCategorie);
        Assert.Equal(CategorieEquipement.EauChaude, eau.Key); // casse tolérée
        Assert.Equal("eau.vidange", Assert.Single(eau.Value).Cle);
        Assert.NotNull(packs.Trouver("eau.vidange"));
        Assert.Null(packs.Trouver("ponctuelle"));
    }

    [Fact]
    public void Un_chemin_configure_mais_introuvable_ne_retombe_pas_sur_les_packs_livres()
    {
        var packs = Lire(Path.Combine(_dossier, "absent.json"));

        Assert.True(packs.EstVide);
    }

    [Fact]
    public void Un_fichier_illisible_donne_des_packs_vides()
    {
        var packs = Lire(Ecrire("{ pas du json"));

        Assert.True(packs.EstVide);
    }
}
