namespace HouseOs.Api.Domaine.Ephemerides;

public enum NomPhaseLunaire
{
    NouvelleLune,
    PremierCroissant,
    PremierQuartier,
    GibbeuseCroissante,
    PleineLune,
    GibbeuseDecroissante,
    DernierQuartier,
    DernierCroissant,
}

/// <summary>
/// <paramref name="Fraction"/> va de 0 (nouvelle lune) à 1 (la nouvelle lune
/// suivante) ; 0,5 est la pleine lune. <paramref name="Illumination"/> est la part
/// du disque éclairée, de 0 à 1.
/// </summary>
public sealed record PhaseLunaire(double Fraction, double Illumination, NomPhaseLunaire Nom, double AgeJours);

/// <summary>
/// La phase de la lune par la lunaison moyenne. C'est une approximation : la vraie
/// orbite est elliptique et perturbée, si bien que la date d'une nouvelle lune peut
/// s'écarter de quelques heures de ce calcul. Pour dire « pleine lune ce soir » sur
/// un mur, c'est très suffisant ; pour une éphéméride d'observation, non.
/// </summary>
public static class Lune
{
    /// <summary>Mois synodique moyen, en jours.</summary>
    public const double MoisSynodique = 29.530588853;

    /// <summary>
    /// Nouvelle lune de référence : 6 janvier 2000, 18 h 14 UTC. L'époque usuelle de
    /// ce calcul. Elle est tenue en date et non en jour julien : la constante
    /// 2451550,1 qui court les manuels vaut 14 h 24 et non 18 h 14, et les deux
    /// finissent par diverger de presque quatre heures sans qu'on sache laquelle est
    /// la bonne.
    /// </summary>
    public static readonly DateTime NouvelleLuneDeReference =
        new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);

    /// <summary>
    /// Une phase est dite « exacte » à moins d'une demi-journée de l'instant théorique.
    /// La fenêtre fait donc exactement un jour de large : en échantillonnant à midi,
    /// chaque pleine lune est nommée un jour et un seul. Plus large, « pleine lune »
    /// voudrait dire deux soirs de suite et ne surprendrait plus personne ; c'est aussi
    /// ce qui tient la promesse de rareté du fonds de tiroir (25 fois l'an pour la
    /// pleine et la nouvelle réunies).
    /// </summary>
    private const double FenetreExacte = 0.5 / MoisSynodique;

    public static PhaseLunaire Phase(DateTime instantUtc)
    {
        var fraction = Fraction(Soleil.JourJulien(instantUtc));
        return new PhaseLunaire(
            fraction,
            // Le disque éclairé suit un cosinus de l'angle de phase, pas la fraction
            // elle-même : au premier quartier (fraction 0,25) il est éclairé à moitié.
            (1 - Math.Cos(2 * Math.PI * fraction)) / 2,
            Nommer(fraction),
            fraction * MoisSynodique);
    }

    /// <summary>Le jour vu à midi local — une phase n'a pas besoin de plus de finesse.</summary>
    public static PhaseLunaire PhaseDuJour(DateOnly date, TimeSpan decalage) =>
        Phase(date.ToDateTime(new TimeOnly(12, 0)) - decalage);

    private static double Fraction(double jourJulien)
    {
        var cycles = (jourJulien - Soleil.JourJulien(NouvelleLuneDeReference)) / MoisSynodique;
        var fraction = cycles - Math.Floor(cycles);
        return fraction;
    }

    private static NomPhaseLunaire Nommer(double fraction)
    {
        if (fraction < FenetreExacte || fraction > 1 - FenetreExacte) return NomPhaseLunaire.NouvelleLune;
        if (Math.Abs(fraction - 0.25) < FenetreExacte) return NomPhaseLunaire.PremierQuartier;
        if (Math.Abs(fraction - 0.5) < FenetreExacte) return NomPhaseLunaire.PleineLune;
        if (Math.Abs(fraction - 0.75) < FenetreExacte) return NomPhaseLunaire.DernierQuartier;
        if (fraction < 0.25) return NomPhaseLunaire.PremierCroissant;
        if (fraction < 0.5) return NomPhaseLunaire.GibbeuseCroissante;
        if (fraction < 0.75) return NomPhaseLunaire.GibbeuseDecroissante;
        return NomPhaseLunaire.DernierCroissant;
    }
}
