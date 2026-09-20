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

    /// <summary>
    /// Délai entre deux réveils de l'appareil le jour. 15 min depuis 2026-09-20 : à
    /// 5 min (le choix initial du 2026-09-03), le reTerminal faisait ~198 réveils
    /// Wi-Fi par jour et perdait 0,25 V en 16 jours. Un tableau de bord mural ne
    /// gagne rien à être rafraîchi au quart d'heure près.
    /// </summary>
    public int CadenceJourSecondes { get; set; } = 900;

    /// <summary>La nuit, l'appareil dort jusqu'à NuitFin — sous PlafondSecondes.</summary>
    public TimeOnly NuitDebut { get; set; } = new(22, 0);
    public TimeOnly NuitFin { get; set; } = new(5, 30);

    /// <summary>
    /// Plus long sommeil qu'on ose demander au firmware. 3600 s est la seule valeur
    /// éprouvée sur le TRMNL 1.8.10 ; au-dessus, rien ne dit qu'il l'honore. C'est ce
    /// plafond qui fait que la nuit n'est pas un seul sommeil mais ~8 réveils.
    /// </summary>
    public int PlafondSecondes { get; set; } = 3600;

    /// <summary>Base des URL d'image renvoyées à l'appareil ; vide = déduite de la requête.</summary>
    public string? UrlBase { get; set; }
}
