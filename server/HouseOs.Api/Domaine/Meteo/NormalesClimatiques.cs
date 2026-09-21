namespace HouseOs.Api.Domaine.Meteo;

/// <summary>
/// Une date normale, avec son imprécision. ERA5 est une grille d'environ neuf
/// kilomètres et dix saisons ne font pas une certitude : le journal dit « vers le
/// 8 octobre », jamais « le 8 octobre »
/// (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
/// </summary>
/// <param name="EcartJours">L'écart typique à cette date, en jours, sur les saisons
/// observées. Jamais zéro : une normale au jour près n'existe pas.</param>
public sealed record DateNormale(int Mois, int Jour, int EcartJours);

/// <summary>Un mois et ce qu'il reçoit d'eau, en moyenne sur l'archive.</summary>
public sealed record MoisNormal(int Mois, double PrecipitationMm);

/// <summary>
/// Les normales du lieu, <b>matérialisées</b> : calculées une fois l'an depuis
/// l'archive et relues telles quelles, jamais recalculées dans le chemin de requête
/// (même principe que les tables de prévisions).
///
/// <para>La ligne <b>porte les coordonnées du calcul</b>. C'est tout l'enjeu : le jour
/// où le `.env` change de latitude, ces normales décrivent un autre endroit, et le
/// journal annoncerait le gel de l'ancienne rue à la nouvelle maison. Une clé qui ne
/// correspond plus déclenche le recalcul.</para>
///
/// <para>Chaque normale est facultative : un lieu sans gel, sans neige ou sans archive
/// assez longue n'en a pas, et le fait correspondant se tait — une absence normale,
/// jamais une panne.</para>
/// </summary>
public class NormalesClimatiques
{
    public int Id { get; set; }

    /// <summary>Les coordonnées du calcul (<c>MeteoOptions.CleCoordonnees</c>).</summary>
    public string Coordonnees { get; set; } = "";

    public DateTimeOffset CalculeesLe { get; set; }

    /// <summary>La fenêtre de l'archive lue, pour que le journal puisse dire « sur dix ans ».</summary>
    public DateOnly DebutArchive { get; set; }
    public DateOnly FinArchive { get; set; }

    /// <summary>
    /// Combien de saisons froides <b>complètes</b> l'archive couvre. C'est le nombre
    /// que le journal cite, et pas le nombre d'années demandées : une archive tronquée
    /// ne doit pas se vanter de dix ans.
    /// </summary>
    public int SaisonsCompletes { get; set; }

    public int? PremierGelMois { get; set; }
    public int? PremierGelJour { get; set; }
    public int? PremierGelEcartJours { get; set; }

    public int? PremiereNeigeMois { get; set; }
    public int? PremiereNeigeJour { get; set; }
    public int? PremiereNeigeEcartJours { get; set; }

    public int? DerniereDouceurMois { get; set; }
    public int? DerniereDouceurJour { get; set; }
    public int? DerniereDouceurEcartJours { get; set; }

    public int? MoisLePlusSec { get; set; }
    public double? MoisLePlusSecMm { get; set; }
    public int? MoisLePlusPluvieux { get; set; }
    public double? MoisLePlusPluvieuxMm { get; set; }

    /// <summary>
    /// La première gelée <b>au sol</b> après l'été — pas la première nuit sous zéro à
    /// l'abri : voir le seuil dans <see cref="CalculDesNormales"/>, qui explique
    /// l'écart et ce qu'il a corrigé.
    /// </summary>
    public DateNormale? PremierGel =>
        Composer(PremierGelMois, PremierGelJour, PremierGelEcartJours);

    /// <summary>La première neige qui tient — un centimètre au sol.</summary>
    public DateNormale? PremiereNeige =>
        Composer(PremiereNeigeMois, PremiereNeigeJour, PremiereNeigeEcartJours);

    /// <summary>La dernière journée à vingt degrés avant l'hiver.</summary>
    public DateNormale? DerniereDouceur =>
        Composer(DerniereDouceurMois, DerniereDouceurJour, DerniereDouceurEcartJours);

    public MoisNormal? MoisSec =>
        MoisLePlusSec is { } mois && MoisLePlusSecMm is { } mm ? new MoisNormal(mois, mm) : null;

    public MoisNormal? MoisPluvieux =>
        MoisLePlusPluvieux is { } mois && MoisLePlusPluvieuxMm is { } mm ? new MoisNormal(mois, mm) : null;

    private static DateNormale? Composer(int? mois, int? jour, int? ecart) =>
        mois is { } m && jour is { } j ? new DateNormale(m, j, ecart ?? 1) : null;
}
