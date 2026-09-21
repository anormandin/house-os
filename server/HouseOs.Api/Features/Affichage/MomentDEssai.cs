using System.Globalization;

namespace HouseOs.Api.Features.Affichage;

/// <summary>
/// L'horloge d'un <b>tirage d'essai</b> : « montre-moi le mur du matin » sans attendre
/// demain matin. Le journal change de visage selon l'heure — surtitre d'édition,
/// heure d'impression, titre d'humeur, météo du moment — et l'attendre pour le voir
/// coûte une demi-journée par essai.
///
/// <para><b>Jamais sur le chemin de l'appareil.</b> `/api/display` dit toujours
/// l'heure vraie : un mur qui se ferait dater du matin à sept heures du soir mentirait
/// à la seule personne qui le lit de loin. L'horloge d'essai n'existe que sur l'aperçu
/// et sur le tirage demandé à la main (vault : Affichage E-ink).</para>
/// </summary>
public static class MomentDEssai
{
    /// <summary>
    /// Les deux moments qu'un essai veut voir, et les seuls mots à retenir : le mur au
    /// réveil et le mur au souper, de part et d'autre de midi — c'est midi qui fait
    /// basculer le surtitre de « Édition du matin » à « Édition du soir ».
    /// </summary>
    public static readonly TimeOnly Matin = new(7, 0);
    public static readonly TimeOnly Soir = new(19, 0);

    /// <summary>
    /// Lit la demande : vide ou « maintenant » pour l'heure vraie, « matin » et
    /// « soir » pour les deux moments du jour courant, sinon une date et heure
    /// complète (« 2026-12-25T07:30 »).
    ///
    /// <para>Rend <c>false</c> quand c'est illisible, plutôt que de retomber en
    /// silence sur l'heure vraie : quelqu'un qui écrit « matun » veut voir le matin,
    /// et lui servir le soir sans rien dire lui ferait tirer la mauvaise conclusion de
    /// son essai.</para>
    /// </summary>
    public static bool Lire(string? demande, DateTime maintenant, out DateTime moment)
    {
        moment = maintenant;
        if (string.IsNullOrWhiteSpace(demande))
        {
            return true;
        }

        switch (demande.Trim().ToLowerInvariant())
        {
            case "maintenant":
                return true;
            case "matin":
                moment = maintenant.Date.Add(Matin.ToTimeSpan());
                return true;
            case "soir":
                moment = maintenant.Date.Add(Soir.ToTimeSpan());
                return true;
        }

        // Un instant complet. En invariant et sans conversion de fuseau : la valeur
        // est écrite par un humain dans l'heure du foyer, et le serveur y vit déjà.
        if (DateTime.TryParse(demande, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var lu))
        {
            moment = DateTime.SpecifyKind(lu, DateTimeKind.Unspecified);
            return true;
        }
        return false;
    }

    /// <summary>Ce qu'on répond à qui se trompe — la liste des mots, pas un reproche.</summary>
    public const string Erreur =
        "Moment illisible : « matin », « soir », « maintenant », ou une date et heure complète "
        + "(2026-12-25T07:30).";
}
