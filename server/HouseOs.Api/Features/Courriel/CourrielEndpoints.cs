using HouseOs.Api.Features.Documents;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using MimeKit;

namespace HouseOs.Api.Features.Courriel;

public record PieceJointeDto(string NomFichier, string TypeMime, long Taille);

public record CourrielDocumentDto(
    string De,
    string A,
    DateTimeOffset Date,
    string Sujet,
    string Texte,
    IReadOnlyList<PieceJointeDto> PiecesJointes);

public static class CourrielEndpoints
{
    /// <summary>Plafond de l'aperçu texte servi au tiroir.</summary>
    public const int LongueurMaxApercu = 20_000;

    public static IEndpointRouteBuilder MapCourriel(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("Courriel");

        app.MapPost("/api/documents/relever-courriels", async (
            CourrielEntrantService service, CancellationToken ct) =>
        {
            var rapport = await service.ReleverAsync(ct);
            if (rapport.Actif == false)
            {
                journal.LogWarning("Relevé demandé alors que le dépôt de courriels n'est pas configuré.");
                return Results.Json(new { message = "Relevé du courriel non configuré." },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            if (rapport.DejaEnCours)
            {
                return Results.Json(new { message = "Un relevé est déjà en cours." },
                    statusCode: StatusCodes.Status409Conflict);
            }
            return Results.Ok(new
            {
                actif = true,
                nbCourriels = rapport.NbCourriels,
                nbDocuments = rapport.NbDocuments,
                nbIgnores = rapport.NbIgnores,
                erreurs = rapport.Erreurs,
            });
        });

        // Le .eml est archivé tel quel ; l'aperçu en extrait du texte à la volée —
        // jamais de HTML servi au navigateur.
        app.MapGet("/api/documents/{id:guid}/courriel", async (
            Guid id, HouseOsDbContext db, IConfiguration config, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var document = await db.Documents.FindAsync([id], ct);
            if (document is null || document.TypeMime != EnregistrementDocument.TypeMimeCourriel)
            {
                return Results.NotFound();
            }
            var chemin = Path.Combine(DocumentsEndpoints.DossierFichiers(config, env), document.CheminDisque);
            if (File.Exists(chemin) == false)
            {
                return Results.NotFound();
            }
            MimeMessage message;
            await using (var flux = File.OpenRead(chemin))
            {
                message = await MimeMessage.LoadAsync(flux, ct);
            }
            var lu = LectureCourriel.Lire(message);
            var texte = lu.Texte.Length <= LongueurMaxApercu ? lu.Texte : lu.Texte[..LongueurMaxApercu] + "…";
            return Results.Ok(new CourrielDocumentDto(
                message.From.ToString(),
                message.To.ToString(),
                lu.Date,
                lu.Sujet,
                texte,
                lu.PiecesJointes.Select(p => new PieceJointeDto(p.NomFichier, p.TypeMime, p.Contenu.Length)).ToList()));
        });

        return app;
    }
}
