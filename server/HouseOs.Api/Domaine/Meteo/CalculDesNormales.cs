namespace HouseOs.Api.Domaine.Meteo;

/// <summary>
/// Les normales, déduites de l'archive. Du calcul pur sur des lignes déjà normalisées :
/// aucune base, aucun réseau, aucune horloge — la règle reste du C# testable
/// (vault : D-2026-08-23 Pas De N8n Dans Le Cœur).
///
/// <para><b>La saison, pas l'année civile.</b> « Le premier gel » du 3 janvier et celui
/// du 5 octobre appartiennent au même hiver, et une moyenne par année civile les
/// mélangerait en une date de juin qui n'existe nulle part. Les saisons se comptent
/// donc à partir du <b>mois qui suit le plus chaud</b> — déduit de l'archive, pas
/// supposé : le dépôt est public et l'hémisphère sud a son été en janvier.</para>
///
/// <para><b>Une normale qui n'en est pas ne sort pas.</b> Trop peu de saisons, un
/// événement qui n'arrive qu'une année sur trois, une douceur qui ne s'arrête jamais
/// (les tropiques) : dans tous ces cas la normale vaut <c>null</c>, et le fait se tait.
/// C'est la même règle que le lever du soleil au-delà du cercle polaire — une absence
/// normale, jamais une erreur.</para>
/// </summary>
public static class CalculDesNormales
{
    /// <summary>Seuils éditoriaux — ce qui mérite d'être annoncé, pas ce qui est mesurable.</summary>
    ///
    /// <remarks>
    /// <b>Trois degrés, et non zéro.</b> Le gel qu'on annonce est celui qui blanchit le
    /// gazon et tue le basilic ; celui que la réanalyse mesure est la moyenne d'une
    /// maille de neuf kilomètres à deux mètres du sol, qui ne voit ni le rayonnement
    /// nocturne d'un jardin ni l'air froid qui s'y accumule. Les avertissements de gel
    /// au sol s'émettent d'ailleurs partout à deux ou quatre degrés annoncés, pour cette
    /// raison exacte.
    ///
    /// <para>Relevé au premier tirage réel (2026-09-20, région de Québec) : à zéro
    /// degré, la médiane des neuf saisons tombait au <b>27 octobre</b>, trois semaines
    /// après le gel que tout le monde ici connaît. À trois degrés, elle tombe au
    /// <b>3 octobre</b>, ce que disent les normales publiées de la station. Le seuil
    /// n'est pas un ajustement québécois : c'est la différence, vraie partout, entre la
    /// température de l'abri et celle du sol.</para>
    /// </remarks>
    private const double GelAuSolC = 3;

    /// <summary>
    /// La première neige qu'on annonce est celle qui <b>tient</b> : sous le centimètre,
    /// l'archive voit tomber de la neige que personne ne voit au sol. Le seuil ne
    /// déplace guère la date (sept des dix dernières saisons donnent le même jour à
    /// un demi-centimètre près, relevé 2026-09-20) — il rend la phrase vraie.
    /// </summary>
    private const double NeigeQuiTientCm = 1;

    private const double DouceurC = 20;

    /// <summary>
    /// La dernière douceur se cherche dans la <b>première moitié</b> de la saison :
    /// au-delà, on retrouverait l'été suivant, qui n'a rien à voir avec l'automne
    /// qu'on annonce.
    /// </summary>
    private const int DemiSaisonJours = 182;

    /// <summary>
    /// Une normale qui tombe au bord de la fenêtre de recherche n'est pas une normale,
    /// c'est le bord de la fenêtre : là où la douceur ne s'arrête jamais, mieux vaut
    /// se taire que d'annoncer une fin d'été le dernier jour de janvier.
    /// </summary>
    private const int MargeDuBordJours = 7;

    private const int SaisonsMinimales = 3;

    /// <summary>
    /// Ce que le mois le plus arrosé doit dépasser le plus sec pour qu'il y ait quelque
    /// chose à dire. Un quart : sous cet écart, ce qu'on classerait est la longueur des
    /// mois (trois jours de différence entre février et janvier font déjà 11 %), pas le
    /// climat.
    /// </summary>
    private const double EcartMinimalEntreMois = 0.25;

    /// <summary>
    /// Part des saisons qui doivent avoir connu l'événement. Une neige qui tombe une
    /// année sur trois n'a pas de date normale.
    /// </summary>
    private const double PartDesSaisonsMinimale = 0.6;

    /// <summary>
    /// Année non bissextile, et la suivante non plus : une normale ne doit jamais
    /// tomber un 29 février, qui n'existe pas trois ans sur quatre.
    /// </summary>
    private const int AnneeDeReference = 2001;

    public static NormalesClimatiques Calculer(
        string coordonnees, IReadOnlyList<JourDeClimat> archive, DateTimeOffset calculeesLe)
    {
        var normales = new NormalesClimatiques { Coordonnees = coordonnees, CalculeesLe = calculeesLe };
        if (archive.Count == 0)
        {
            return normales;
        }

        var jours = archive.OrderBy(j => j.Date).ToList();
        normales.DebutArchive = jours[0].Date;
        normales.FinArchive = jours[^1].Date;

        var ancreMois = MoisLePlusChaud(jours) % 12 + 1;
        var saisons = Saisons(jours, ancreMois);
        normales.SaisonsCompletes = saisons.Count;

        var gels = Offsets(saisons, (saison, _) => saison.FirstOrDefault(j => j.TemperatureMinC <= GelAuSolC));
        var neiges = Offsets(saisons, (saison, _) => saison.FirstOrDefault(j => j.NeigeCm >= NeigeQuiTientCm));
        var douceurs = Offsets(saisons, (saison, ancre) => saison
            .Where(j => j.Date.DayNumber - ancre.DayNumber < DemiSaisonJours)
            .LastOrDefault(j => j.TemperatureMaxC >= DouceurC));

        if (Normale(gels, saisons.Count, ancreMois) is { } gel)
        {
            (normales.PremierGelMois, normales.PremierGelJour, normales.PremierGelEcartJours) =
                (gel.Mois, gel.Jour, gel.EcartJours);
        }
        if (Normale(neiges, saisons.Count, ancreMois) is { } neige)
        {
            (normales.PremiereNeigeMois, normales.PremiereNeigeJour, normales.PremiereNeigeEcartJours) =
                (neige.Mois, neige.Jour, neige.EcartJours);
        }
        // La douceur, et seulement elle, se juge aussi sur le bord de sa fenêtre.
        if (Normale(douceurs, saisons.Count, ancreMois, DemiSaisonJours - MargeDuBordJours) is { } douceur)
        {
            (normales.DerniereDouceurMois, normales.DerniereDouceurJour, normales.DerniereDouceurEcartJours) =
                (douceur.Mois, douceur.Jour, douceur.EcartJours);
        }

        var (sec, pluvieux) = MoisExtremes(jours);
        normales.MoisLePlusSec = sec?.Mois;
        normales.MoisLePlusSecMm = sec?.PrecipitationMm;
        normales.MoisLePlusPluvieux = pluvieux?.Mois;
        normales.MoisLePlusPluvieuxMm = pluvieux?.PrecipitationMm;

        return normales;
    }

    /// <summary>
    /// Le cœur de l'été, déduit de l'archive : le mois dont les maximums sont les plus
    /// hauts. C'est lui qui place l'ancre des saisons, et c'est ce qui rend le calcul
    /// vrai partout plutôt qu'au nord seulement.
    /// </summary>
    private static int MoisLePlusChaud(IReadOnlyList<JourDeClimat> jours) =>
        jours
            .GroupBy(j => j.Date.Month)
            .OrderByDescending(g => g.Average(j => j.TemperatureMaxC))
            .ThenBy(g => g.Key)
            .First()
            .Key;

    /// <summary>
    /// Les saisons <b>complètes</b> de l'archive, chacune du premier jour de l'ancre au
    /// dernier jour avant l'ancre suivante. Une saison entamée par le bord de l'archive
    /// est écartée : elle n'aurait pas eu l'occasion de geler.
    /// </summary>
    private static List<List<JourDeClimat>> Saisons(IReadOnlyList<JourDeClimat> jours, int ancreMois)
    {
        var debut = jours[0].Date;
        var fin = jours[^1].Date;
        var saisons = new List<List<JourDeClimat>>();

        for (var annee = debut.Year; annee <= fin.Year; annee++)
        {
            var ancre = new DateOnly(annee, ancreMois, 1);
            var suivante = ancre.AddYears(1);
            if (ancre < debut || suivante.AddDays(-1) > fin)
            {
                continue;
            }
            var saison = jours.Where(j => j.Date >= ancre && j.Date < suivante).ToList();
            if (saison.Count > 0)
            {
                saisons.Add(saison);
            }
        }
        return saisons;
    }

    /// <summary>L'événement de chaque saison, compté en jours depuis l'ancre.</summary>
    private static List<int> Offsets(
        List<List<JourDeClimat>> saisons, Func<List<JourDeClimat>, DateOnly, JourDeClimat?> trouver)
    {
        var offsets = new List<int>();
        foreach (var saison in saisons)
        {
            // L'ancre est le premier jour du mois, pas le premier jour observé : deux
            // saisons doivent se compter depuis la même origine, sinon la médiane
            // additionne des jours qui ne partent pas du même endroit.
            var ancre = new DateOnly(saison[0].Date.Year, saison[0].Date.Month, 1);
            if (trouver(saison, ancre) is { } jour)
            {
                offsets.Add(jour.Date.DayNumber - ancre.DayNumber);
            }
        }
        return offsets;
    }

    /// <summary>
    /// La médiane des offsets, rendue en date du calendrier — ou <c>null</c> si les
    /// gardes ne sont pas franchies. La médiane, et pas la moyenne : une seule année à
    /// gel tardif ne doit pas déplacer la normale de deux semaines.
    /// </summary>
    private static DateNormale? Normale(
        IReadOnlyList<int> offsets, int saisons, int ancreMois, int? offsetMaximal = null)
    {
        if (saisons < SaisonsMinimales || offsets.Count < saisons * PartDesSaisonsMinimale || offsets.Count == 0)
        {
            return null;
        }

        var mediane = Mediane(offsets);
        if (offsetMaximal is { } maximum && mediane >= maximum)
        {
            return null;
        }

        // La date se prend à la médiane, l'écart à la moyenne, et ce n'est pas une
        // inattention : une année à gel très tardif ne doit pas déplacer la date —
        // c'est tout l'intérêt de la médiane — mais elle doit se voir dans
        // l'imprécision annoncée. Un écart médian l'aurait effacée, et le journal
        // aurait promis « à un jour près » un climat qui varie de six semaines.
        var ecart = Math.Max(1, (int)Math.Round(
            offsets.Average(o => Math.Abs(o - mediane)), MidpointRounding.AwayFromZero));
        var date = new DateOnly(AnneeDeReference, ancreMois, 1).AddDays(mediane);
        return new DateNormale(date.Month, date.Day, ecart);
    }

    private static int Mediane(IReadOnlyList<int> valeurs)
    {
        var tries = valeurs.Order().ToList();
        var milieu = tries.Count / 2;
        return tries.Count % 2 == 1
            ? tries[milieu]
            : (int)Math.Round((tries[milieu - 1] + tries[milieu]) / 2.0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Le mois le plus sec et le plus pluvieux, en moyenne. Seuls les mois dont
    /// <b>toutes les journées</b> sont là comptent — et c'est un compte de jours, pas
    /// un encadrement de dates : un septembre commencé le 12 ferait passer le mois le
    /// plus arrosé de l'année pour le plus sec, et un mois troué au milieu par une
    /// journée que l'archive n'a pas rendue ferait la même chose en plus discret.
    /// </summary>
    private static (MoisNormal? Sec, MoisNormal? Pluvieux) MoisExtremes(IReadOnlyList<JourDeClimat> jours)
    {
        var parMois = jours
            .GroupBy(j => (j.Date.Year, j.Date.Month))
            .Where(g => g.Count() == DateTime.DaysInMonth(g.Key.Year, g.Key.Month))
            .GroupBy(g => g.Key.Month)
            .Select(g => new MoisNormal(g.Key, g.Average(mois => mois.Sum(j => j.PrecipitationMm))))
            .ToList();

        // Les douze mois, ou rien : comparer un août complet à un février qui manque
        // donnerait un « mois le plus sec » qui n'est que le mois le moins observé.
        if (parMois.Count < 12)
        {
            return (null, null);
        }

        var sec = parMois.OrderBy(m => m.PrecipitationMm).ThenBy(m => m.Mois).First();
        var pluvieux = parMois.OrderByDescending(m => m.PrecipitationMm).ThenBy(m => m.Mois).First();

        // Sans écart marqué, le « mois le plus sec » n'est que le mois le plus court :
        // février bat janvier de trois jours de pluie, et le journal publierait un
        // artefact du calendrier comme une statistique du climat. Une normale qui n'en
        // est pas ne sort pas. Trouvé en revue de code, étape 5.
        return pluvieux.PrecipitationMm < sec.PrecipitationMm * (1 + EcartMinimalEntreMois)
            ? (null, null)
            : (sec, pluvieux);
    }
}
