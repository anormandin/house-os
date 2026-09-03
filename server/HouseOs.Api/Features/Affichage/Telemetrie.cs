using System.Globalization;
using HouseOs.Api.Domaine;

namespace HouseOs.Api.Features.Affichage;

/// <summary>Lecture des en-têtes que le firmware TRMNL envoie à chaque réveil.</summary>
public static class Telemetrie
{
    /// <summary>Tension d'une cellule Li-ion : ~4,2 V pleine, ~3,3 V à plat sous charge.</summary>
    private const double TensionPleine = 4.2;
    private const double TensionVide = 3.3;

    public static void Appliquer(AppareilAffichage appareil, IHeaderDictionary entetes, DateTimeOffset maintenant)
    {
        appareil.DernierContact = maintenant;
        if (Entier(entetes["Width"]) is { } largeur && largeur > 0)
        {
            appareil.Largeur = largeur;
        }
        if (Entier(entetes["Height"]) is { } hauteur && hauteur > 0)
        {
            appareil.Hauteur = hauteur;
        }
        if (Decimal(entetes["Battery-Voltage"]) is { } tension)
        {
            appareil.TensionPile = tension;
        }
        if (Entier(entetes["RSSI"]) is { } rssi)
        {
            appareil.Rssi = rssi;
        }
        if (Texte(entetes["FW-Version"]) is { } version)
        {
            appareil.VersionFirmware = version;
        }
        if (Texte(entetes["Model"]) is { } modele)
        {
            appareil.Modele = modele;
        }
    }

    /// <summary>Estimation linéaire : indicative, pas une jauge de précision.</summary>
    public static int? PileEnPourcent(double? tension)
    {
        if (tension is not { } v)
        {
            return null;
        }
        var ratio = (v - TensionVide) / (TensionPleine - TensionVide);
        return (int)Math.Round(Math.Clamp(ratio, 0, 1) * 100);
    }

    private static int? Entier(string? valeur) =>
        int.TryParse(valeur, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;

    private static double? Decimal(string? valeur) =>
        double.TryParse(valeur, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static string? Texte(string? valeur)
    {
        var propre = valeur?.Trim();
        return string.IsNullOrEmpty(propre) ? null : propre[..Math.Min(propre.Length, 50)];
    }
}
