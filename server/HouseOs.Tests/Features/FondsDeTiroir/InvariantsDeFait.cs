using HouseOs.Api.Features.FondsDeTiroir;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// Les deux invariants de forme du fonds de tiroir, appris au rendu et en revue de
/// code à l'étape 2 (vault : Fonds De Tiroir). Ils valent pour <b>toutes</b> les
/// familles, et chaque famille les balaie sur une année entière.
///
/// <para>Rien ici ne parle de pixel : ce sont des règles d'écriture, pas de mise en
/// page (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).</para>
/// </summary>
internal static class InvariantsDeFait
{
    /// <summary>
    /// Au-delà, la forme courte se fait couper dans une colonne de widget —
    /// « Équinoxe de septembre dans 2 jours » coupé au milieu, au mur, le 2026-09-20.
    /// </summary>
    private const int ValeurMaximale = 30;

    internal static void Verifier(FaitDeTiroir fait)
    {
        Assert.NotEmpty(fait.Etiquette);
        Assert.NotEmpty(fait.Texte);

        // L'étiquette porte le sujet, la valeur porte le chiffre.
        Assert.InRange(fait.Valeur.Length, 1, ValeurMaximale);

        // Le texte long doit AJOUTER quelque chose : « LE JOUR RACCOURCIT / 3 min par
        // jour / Le jour raccourcit d'environ 3 minutes par jour » coûtait trois lignes
        // de mur pour une seule idée.
        Assert.DoesNotContain(fait.Etiquette, fait.Texte, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fait.Valeur, fait.Texte, StringComparison.OrdinalIgnoreCase);
    }

    internal static void Verifier(IEnumerable<FaitDeTiroir> faits)
    {
        foreach (var fait in faits)
        {
            Verifier(fait);
        }
    }
}
