namespace HouseOs.Api.Features.Affichage;

/// <summary>Réglages de la vue e-ink (section « Affichage » ; tous facultatifs).</summary>
public sealed class AffichageOptions
{
    /// <summary>Largeur × hauteur de repli : le reTerminal E1003 en paysage.</summary>
    public const int LargeurParDefaut = 1872;
    public const int HauteurParDefaut = 1404;

    /// <summary>
    /// La page que le navigateur de rendu capture. En prod, l'app se sert elle-même
    /// (port interne du conteneur) ; en dev, Vite (appsettings.Development.json).
    /// </summary>
    public string UrlEcran { get; set; } = "http://localhost:8080/ecran";

    /// <summary>Délai entre deux réveils de l'appareil le jour (choix 2026-09-03 : 5 min).</summary>
    public int CadenceJourSecondes { get; set; } = 300;

    /// <summary>La nuit, l'appareil dort d'un trait jusqu'à NuitFin.</summary>
    public TimeOnly NuitDebut { get; set; } = new(22, 0);
    public TimeOnly NuitFin { get; set; } = new(5, 30);

    /// <summary>Plus long sommeil qu'on ose demander au firmware (à confirmer sur l'appareil).</summary>
    public int PlafondSecondes { get; set; } = 3600;

    /// <summary>Base des URL d'image renvoyées à l'appareil ; vide = déduite de la requête.</summary>
    public string? UrlBase { get; set; }
}
