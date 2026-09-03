using System.Security.Cryptography;

namespace HouseOs.Api.Features.Affichage;

/// <summary>
/// Le secret que le serveur donne à son propre navigateur headless pour lire les
/// données de l'écran sans cookie de session. Aléatoire par processus : il ne se
/// configure pas, ne se journalise pas, et meurt avec l'app.
/// </summary>
public sealed class JetonRendu
{
    public const string NomEntete = "X-Rendu-Jeton";

    public string Valeur { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>Un humain connecté ou le navigateur de rendu — personne d'autre.</summary>
    public bool Autorise(HttpContext contexte)
    {
        if (contexte.User.Identity?.IsAuthenticated == true)
        {
            return true;
        }
        var fourni = contexte.Request.Headers[NomEntete].ToString();
        return fourni.Length == Valeur.Length
            && CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(fourni),
                System.Text.Encoding.ASCII.GetBytes(Valeur));
    }
}
