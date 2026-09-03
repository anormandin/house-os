using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Courriel;
using HouseOs.Api.Features.Documents;

namespace HouseOs.Tests.Features.Courriel;

/// <summary>
/// Le vocabulaire fermé et le repli : le LLM propose, le code dispose — rien qui sorte
/// des catégories, des dossiers ou des équipements existants n'atteint la base.
/// </summary>
public class EnrichissementCourrielTests
{
    private static readonly Guid Thermopompe = Guid.NewGuid();

    private static ContexteEnrichissement Contexte(string sujet = "Votre reçu IKEA", string texte = "Merci.") =>
        new("camille@exemple.com", sujet, new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.FromHours(-4)),
            texte, ["Facture_48211.pdf"], ["12 rue des Érables"],
            [new EquipementRef(Thermopompe, "Thermopompe", "Fujitsu")]);

    [Fact]
    public void Extraire_lit_un_objet_json_meme_entoure_de_prose()
    {
        var texte = "Voici :\n```json\n{\"titre\": \"Facture IKEA\", \"categorie\": \"Facture\", " +
            "\"dossier\": null, \"equipementId\": \"" + Thermopompe + "\", \"dateDocument\": \"2026-08-30\", " +
            "\"montant\": \"129,95 $\", \"resume\": \"Bibliothèque BILLY.\"}\n```";

        var proposition = EnrichissementCourriel.Extraire(texte);

        Assert.NotNull(proposition);
        Assert.Equal("Facture IKEA", proposition.Titre);
        Assert.Equal("Facture", proposition.Categorie);
        Assert.Null(proposition.Dossier);
        Assert.Equal(Thermopompe, proposition.EquipementId);
        Assert.Equal("2026-08-30", proposition.DateDocument);
        Assert.Equal("129,95 $", proposition.Montant);
    }

    [Fact]
    public void Extraire_rend_null_sans_objet_valide()
    {
        Assert.Null(EnrichissementCourriel.Extraire("Je ne peux pas."));
        Assert.Null(EnrichissementCourriel.Extraire("{ pas du json"));
    }

    [Fact]
    public void Appliquer_garde_ce_qui_est_dans_le_vocabulaire()
    {
        var proposition = new PropositionLlm("Facture IKEA — BILLY", "facture", "12 rue des Érables",
            Thermopompe, "2026-08-30", "129,95 $", "Bibliothèque BILLY blanche.");

        var meta = EnrichissementCourriel.Appliquer(proposition, Contexte(), "Facture_48211.pdf", "application/pdf");

        Assert.Equal("Facture IKEA — BILLY", meta.Titre);
        Assert.Equal(CategorieDocument.Facture, meta.Categorie);
        Assert.Equal("12 rue des Érables", meta.Dossier);
        Assert.Equal(Thermopompe, meta.EquipementId);
        Assert.Equal(new DateOnly(2026, 8, 30), meta.DateDocument);
        Assert.Equal(
            "Bibliothèque BILLY blanche.\nMontant : 129,95 $\n" +
            "Reçu par courriel de camille@exemple.com le 2026-09-01 — Votre reçu IKEA",
            meta.Notes);
    }

    [Fact]
    public void Appliquer_rejette_ce_qui_sort_du_vocabulaire_sans_jeter_le_reste()
    {
        var proposition = new PropositionLlm(new string('x', 201), "Reçu", "Nouveau dossier",
            Guid.NewGuid(), "30/08/2026", null, null);

        var meta = EnrichissementCourriel.Appliquer(proposition, Contexte(), "Facture_48211.pdf", "application/pdf");

        Assert.Equal("Facture_48211", meta.Titre);                // titre trop long → nom de pièce
        Assert.Equal(CategorieDocument.Facture, meta.Categorie);  // catégorie inconnue → mots-clés
        Assert.Null(meta.Dossier);                                // dossier inventé → null
        Assert.Null(meta.EquipementId);                           // équipement inconnu → null
        Assert.Equal(new DateOnly(2026, 9, 1), meta.DateDocument); // date illisible → date du courriel
        Assert.Equal("Reçu par courriel de camille@exemple.com le 2026-09-01 — Votre reçu IKEA", meta.Notes);
    }

    [Fact]
    public void Appliquer_null_est_le_repli()
    {
        Assert.Equal(
            EnrichissementCourriel.Repli(Contexte(), null, EnregistrementDocument.TypeMimeCourriel),
            EnrichissementCourriel.Appliquer(null, Contexte(), null, EnregistrementDocument.TypeMimeCourriel));
    }

    [Fact]
    public void Les_notes_gardent_toujours_la_provenance_en_fin()
    {
        var proposition = new PropositionLlm("T", "Facture", null, null, null, null, new string('r', 5000));

        var meta = EnrichissementCourriel.Appliquer(proposition, Contexte(), null, "application/pdf");

        Assert.True(meta.Notes.Length <= 2000);
        Assert.EndsWith("— Votre reçu IKEA", meta.Notes);
    }

    [Theory]
    [InlineData("Votre reçu Amazon", "x.pdf", "application/pdf", CategorieDocument.Facture)]
    [InlineData("Warranty registration", "w.pdf", "application/pdf", CategorieDocument.Garantie)]
    [InlineData("Police d'assurance habitation", null, "message/rfc822", CategorieDocument.Assurance)]
    [InlineData("Bonjour", null, "message/rfc822", CategorieDocument.Autre)]
    [InlineData("Bonjour", "guide.pdf", "application/pdf", CategorieDocument.Manuel)]
    [InlineData("Bonjour", "photo.jpg", "image/jpeg", CategorieDocument.Photo)]
    public void Repli_devine_la_categorie_par_mots_cles_puis_par_type(
        string sujet, string? piece, string typeMime, CategorieDocument attendue)
    {
        var meta = EnrichissementCourriel.Repli(Contexte(sujet), piece, typeMime);

        Assert.Equal(attendue, meta.Categorie);
    }

    [Fact]
    public void Repli_titre_le_courriel_par_son_sujet_ou_sa_date()
    {
        Assert.Equal("Votre reçu IKEA",
            EnrichissementCourriel.Repli(Contexte(), null, EnregistrementDocument.TypeMimeCourriel).Titre);
        Assert.Equal("Courriel du 2026-09-01",
            EnrichissementCourriel.Repli(Contexte(sujet: ""), null, EnregistrementDocument.TypeMimeCourriel).Titre);
    }
}
