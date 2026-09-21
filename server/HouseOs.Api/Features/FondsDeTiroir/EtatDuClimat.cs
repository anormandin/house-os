using HouseOs.Api.Domaine.Meteo;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La journée d'il y a un an, telle que l'archive l'a gardée. Elle ne porte pas sa
/// date : c'est l'étiquette du fait qui dit « ce jour-là l'an dernier », et le journal
/// affiche déjà celle d'aujourd'hui en manchette.
/// </summary>
public sealed record JourneePassee(double MinC, double MaxC);

/// <summary>
/// Les normales du lieu et la journée d'il y a un an, réduites à ce que la famille
/// « le climat » a besoin de savoir (vault : Fonds De Tiroir). Aucune lecture
/// d'horloge, aucune base : ce qui arrive ici est déjà arrêté.
///
/// <para>Chaque normale est facultative, comme la famille entière : un lieu sans gel
/// ni neige, ou une installation qui n'a pas encore vu l'archive, laisse simplement le
/// journal parler d'autre chose.</para>
/// </summary>
/// <param name="SaisonsObservees">Ce que le journal a le droit de dire — « sur dix
/// saisons » se compte sur l'archive réellement tirée, pas sur celle qu'on a
/// demandée.</param>
/// <param name="AnDernier">La même date, un an plus tôt. Elle vient de l'archive et
/// non des tables de prévisions, qui sont remplacées à chaque heure et ne se
/// souviennent de rien.</param>
/// <param name="MaxDAujourdhuiC">Le maximum prévu aujourd'hui, quand il est connu :
/// c'est lui qui transforme une température de l'an dernier en comparaison.</param>
public sealed record EtatDuClimat(
    int SaisonsObservees,
    DateNormale? PremierGel,
    DateNormale? PremiereNeige,
    DateNormale? DerniereDouceur,
    MoisNormal? MoisLePlusSec,
    MoisNormal? MoisLePlusPluvieux,
    JourneePassee? AnDernier = null,
    double? MaxDAujourdhuiC = null)
{
    /// <summary>Les normales matérialisées, telles que la famille les lit.</summary>
    public static EtatDuClimat Depuis(NormalesClimatiques normales, JourDeClimat? anDernier) =>
        new(
            normales.SaisonsCompletes,
            normales.PremierGel,
            normales.PremiereNeige,
            normales.DerniereDouceur,
            normales.MoisSec,
            normales.MoisPluvieux,
            anDernier is null
                ? null
                : new JourneePassee(anDernier.TemperatureMinC, anDernier.TemperatureMaxC));
}
