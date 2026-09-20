namespace HouseOs.Api.Domaine.Ephemerides;

/// <summary>
/// L'état du ciel un jour donné, pour un lieu donné : tout ce que la famille « le
/// ciel » du fonds de tiroir a besoin de savoir, et rien de plus.
/// </summary>
/// <param name="DeriveQuotidienne">Ce que la journée a gagné ou perdu depuis hier.
/// Nul aux pôles, où la notion n'a pas de sens une partie de l'année.</param>
/// <param name="DeriveHebdomadaire">Le même écart sur sept jours. À trois minutes par
/// jour, c'est le chiffre qui se sent vraiment — celui qu'on remarque en sortant du
/// travail.</param>
public sealed record CielDuJour(
    JourSolaire Soleil,
    TimeSpan? DeriveQuotidienne,
    TimeSpan? DeriveHebdomadaire,
    PhaseLunaire Lune,
    EvenementSaisonnier ProchainEvenementSaisonnier,
    ChangementDHeure? ProchainChangementDHeure);

/// <summary>
/// Le calcul du ciel, assemblé en une lecture. Pur : aucune base, aucun réseau,
/// aucune horloge cachée — tout entre par les paramètres
/// (vault : Fonds De Tiroir, D-2026-08-23 Pas De N8n Dans Le Cœur).
/// </summary>
public static class Ciel
{
    public static CielDuJour Calculer(DateOnly date, Lieu lieu, TimeZoneInfo fuseau)
    {
        var decalage = DecalageDuJour(fuseau, date);
        var aujourdhui = Soleil.Jour(date, lieu, decalage);
        var hier = Jour(date.AddDays(-1), lieu, fuseau);
        var semaineDerniere = Jour(date.AddDays(-7), lieu, fuseau);

        return new CielDuJour(
            aujourdhui,
            aujourdhui.Duree is { } a && hier.Duree is { } h ? a - h : null,
            aujourdhui.Duree is { } b && semaineDerniere.Duree is { } s ? b - s : null,
            Lune.PhaseDuJour(date, decalage),
            Saisons.Prochain(new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), decalage)),
            ChangementHeure.Prochain(fuseau, date));
    }

    private static JourSolaire Jour(DateOnly date, Lieu lieu, TimeZoneInfo fuseau) =>
        Soleil.Jour(date, lieu, DecalageDuJour(fuseau, date));

    private static TimeSpan DecalageDuJour(TimeZoneInfo fuseau, DateOnly jour) =>
        fuseau.GetUtcOffset(DateTime.SpecifyKind(jour.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified));
}
