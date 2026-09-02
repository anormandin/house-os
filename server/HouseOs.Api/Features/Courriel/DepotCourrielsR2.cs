using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace HouseOs.Api.Features.Courriel;

/// <summary>
/// Le bucket R2 par son API S3. Client unique (thread-safe) ; jeton scopé au bucket
/// « Object Read &amp; Write ». Les sommes de contrôle sont laissées à « quand requis » :
/// R2 ne parle pas les en-têtes de checksum que le SDK v4 ajoute par défaut.
/// </summary>
public sealed class DepotCourrielsR2 : IDepotCourriels, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _prefixe;

    public DepotCourrielsR2(CourrielOptions options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.R2.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            Timeout = TimeSpan.FromSeconds(60),
        };
        _client = new AmazonS3Client(new BasicAWSCredentials(options.R2.CleAcces, options.R2.CleSecrete), config);
        _bucket = options.R2.Bucket;
        _prefixe = options.R2.Prefixe;
    }

    public bool Actif => true;

    public async Task<IReadOnlyList<CourrielEnDepot>> ListerAsync(CancellationToken ct)
    {
        var objets = new List<CourrielEnDepot>();
        string? suite = null;
        do
        {
            var reponse = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = _prefixe,
                ContinuationToken = suite,
            }, ct);
            objets.AddRange((reponse.S3Objects ?? [])
                .Where(o => o.Key.EndsWith(".eml", StringComparison.OrdinalIgnoreCase))
                .Select(o => new CourrielEnDepot(o.Key, o.Size ?? 0)));
            suite = reponse.IsTruncated == true ? reponse.NextContinuationToken : null;
        }
        while (suite is not null);
        return objets.OrderBy(o => o.Cle, StringComparer.Ordinal).ToList();
    }

    public async Task<byte[]> TelechargerAsync(string cle, CancellationToken ct)
    {
        using var reponse = await _client.GetObjectAsync(_bucket, cle, ct);
        using var memoire = new MemoryStream();
        await reponse.ResponseStream.CopyToAsync(memoire, ct);
        return memoire.ToArray();
    }

    public Task SupprimerAsync(string cle, CancellationToken ct) =>
        _client.DeleteObjectAsync(_bucket, cle, ct);

    public void Dispose() => _client.Dispose();
}
