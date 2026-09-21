namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>Réglages de la famille « le hasard » (section « Hasard »).</summary>
public sealed class HasardOptions
{
    /// <summary>
    /// Le chemin d'un fichier de banque qui remplace celui de l'app. Vide — le cas
    /// ordinaire — la banque livrée avec l'image sert
    /// (<see cref="LectureDeLaBanque.NomDuFichierParDefaut"/>). `.env` : HASARD_FICHIER.
    ///
    /// <para>Un chemin réglé mais illisible ne retombe <b>pas</b> sur la banque livrée :
    /// servir des jours fériés québécois à un foyer qui a justement demandé les siens
    /// serait pire que le silence.</para>
    /// </summary>
    public string Fichier { get; set; } = "";
}
