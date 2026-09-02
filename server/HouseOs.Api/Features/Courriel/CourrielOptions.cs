namespace HouseOs.Api.Features.Courriel;

/// <summary>Relevé du dépôt de courriels (D-2026-09-02 Courriel Entrant Par Cloudflare
/// Et R2). Sans les quatre champs R2, le relevé est simplement désactivé.</summary>
public class CourrielOptions
{
    public R2Options R2 { get; set; } = new();

    /// <summary>Cadence du relevé ; plancher d'une minute dans la boucle.</summary>
    public int CadenceMinutes { get; set; } = 2;

    /// <summary>Au-delà, l'objet est marqué en erreur et effacé (Cloudflare coupe déjà
    /// à 25 MiB en amont).</summary>
    public long TailleMaxOctets { get; set; } = 25 * 1024 * 1024;

    public bool Actif =>
        string.IsNullOrWhiteSpace(R2.Endpoint) == false
        && string.IsNullOrWhiteSpace(R2.Bucket) == false
        && string.IsNullOrWhiteSpace(R2.CleAcces) == false
        && string.IsNullOrWhiteSpace(R2.CleSecrete) == false;
}

public class R2Options
{
    /// <summary>https://&lt;account_id&gt;.r2.cloudflarestorage.com</summary>
    public string Endpoint { get; set; } = "";
    public string Bucket { get; set; } = "";
    public string CleAcces { get; set; } = "";
    public string CleSecrete { get; set; } = "";
    /// <summary>Préfixe des objets déposés par le Worker.</summary>
    public string Prefixe { get; set; } = "entrants/";
}
