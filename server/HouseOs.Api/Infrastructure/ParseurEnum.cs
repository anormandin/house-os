namespace HouseOs.Api.Infrastructure;

/// <summary>
/// Lire un nom d'enum venu du dehors — d'un client LLM comme d'un corps JSON.
///
/// <para><b>Pourquoi pas <c>Enum.TryParse</c> tel quel :</b> il accepte les chaînes
/// numériques et les valeurs qui ne sont définies nulle part. <c>{"source": "5"}</c>
/// passait, et donnait un flux externe <i>mort-vivant</i> — la passe de
/// rafraîchissement l'ignore (ce n'est pas <c>Ics</c>), la poussée le refuse (ce n'est
/// pas <c>Poussee</c>), et l'UI le montre comme un calendrier ordinaire. Trouvé en
/// revue de code à l'étape 6 du chantier du journal.</para>
///
/// <para>La casse est tolérée des deux côtés : un aller-retour de correction coûte
/// cher à un client LLM, et « poussee » veut dire ce qu'on croit.</para>
/// </summary>
public static class ParseurEnum
{
    public static bool Lire<T>(string valeur, out T resultat) where T : struct, Enum =>
        Enum.TryParse(valeur, ignoreCase: true, out resultat)
        && string.IsNullOrWhiteSpace(valeur) == false
        && char.IsDigit(valeur.Trim()[0]) == false
        && Enum.IsDefined(resultat);
}
