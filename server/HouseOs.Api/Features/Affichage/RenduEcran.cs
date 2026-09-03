using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace HouseOs.Api.Features.Affichage;

/// <summary>Ce qu'on demande à la page /ecran : sa taille, la pile à afficher, ou
/// l'écran d'accueil d'un appareil fraîchement enrôlé (son identifiant).</summary>
public record DemandeCapture(int Largeur, int Hauteur, int? Pile = null, string? Accueil = null)
{
    public string Requete =>
        $"largeur={Largeur}&hauteur={Hauteur}"
        + (Pile is { } p ? $"&pile={p}" : "")
        + (Accueil is { } a ? $"&accueil={Uri.EscapeDataString(a)}" : "");
}

/// <summary>Capture la page /ecran en PNG brut (encore anti-aliasé) à la taille demandée.</summary>
public interface IRenduEcran
{
    Task<byte[]> CapturerAsync(DemandeCapture demande, CancellationToken ct);
}

public sealed class RenduEcranException(string message, Exception? interne = null)
    : Exception(message, interne);

/// <summary>
/// Chromium headless dans le serveur (D-2026-09-03 Rendu E-ink Par Chromium
/// Headless). Un seul navigateur, lancé au premier besoin et gardé ; un contexte
/// neuf par capture (viewport exact, jeton de rendu en en-tête). Les captures sont
/// sérialisées : un écran toutes les cinq minutes n'a pas besoin de parallélisme.
/// </summary>
public sealed class RenduEcranPlaywright(
    IOptions<AffichageOptions> options,
    JetonRendu jeton,
    ILogger<RenduEcranPlaywright> journal) : IRenduEcran, IAsyncDisposable
{
    private static readonly string[] ArgumentsChromium =
    [
        // Pas de sous-pixel ni de hinting : le seuillage 1-bit veut des contours
        // francs, pas des franges colorées.
        "--disable-lcd-text",
        "--font-render-hinting=none",
    ];

    private readonly SemaphoreSlim _verrou = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _navigateur;

    /// <summary>Lance le navigateur d'avance pour que la première capture ne paie pas le démarrage.</summary>
    public async Task PrechaufferAsync(CancellationToken ct)
    {
        try
        {
            await ObtenirNavigateurAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Le reste de House OS ne dépend pas de l'écran : on avertit, on continue.
            journal.LogWarning(ex,
                "Chromium indisponible au démarrage — la vue e-ink échouera jusqu'à ce que « playwright install chromium » soit fait.");
        }
    }

    public async Task<byte[]> CapturerAsync(DemandeCapture demande, CancellationToken ct)
    {
        var (largeur, hauteur) = (demande.Largeur, demande.Hauteur);
        await _verrou.WaitAsync(ct);
        try
        {
            var navigateur = await ObtenirNavigateurAsync(ct);
            await using var contexte = await navigateur.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = largeur, Height = hauteur },
                DeviceScaleFactor = 1,
                ReducedMotion = ReducedMotion.Reduce,
                ExtraHTTPHeaders = new Dictionary<string, string> { [JetonRendu.NomEntete] = jeton.Valeur },
            });
            var page = await contexte.NewPageAsync();
            var url = $"{options.Value.UrlEcran}?{demande.Requete}";

            await page.GotoAsync(url);
            // La page lève ce drapeau quand polices et données sont là (pages/Ecran.tsx).
            await page.WaitForSelectorAsync("html[data-pret='1']",
                new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 20_000 });
            if (await page.EvaluateAsync<bool>("() => document.documentElement.dataset.erreur === '1'"))
            {
                throw new RenduEcranException("La page /ecran n'a pas pu charger ses données.");
            }

            return await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                Animations = ScreenshotAnimations.Disabled,
                Clip = new Clip { X = 0, Y = 0, Width = largeur, Height = hauteur },
            });
        }
        catch (Exception ex) when (ex is not (RenduEcranException or OperationCanceledException))
        {
            // PlaywrightException, mais aussi le pilote qui ne démarre pas (node absent,
            // permissions) : pour l'appelant c'est la même chose, l'écran n'est pas rendu.
            throw new RenduEcranException($"Capture de {options.Value.UrlEcran} impossible : {ex.Message}", ex);
        }
        finally
        {
            _verrou.Release();
        }
    }

    private async Task<IBrowser> ObtenirNavigateurAsync(CancellationToken ct)
    {
        if (_navigateur is { IsConnected: true })
        {
            return _navigateur;
        }
        ct.ThrowIfCancellationRequested();
        _playwright ??= await Playwright.CreateAsync();
        _navigateur = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ArgumentsChromium,
        });
        journal.LogInformation("Chromium {Version} lancé pour la vue e-ink.", _navigateur.Version);
        return _navigateur;
    }

    public async ValueTask DisposeAsync()
    {
        if (_navigateur is not null)
        {
            await _navigateur.CloseAsync();
        }
        _playwright?.Dispose();
        _verrou.Dispose();
    }
}

/// <summary>Préchauffe le navigateur au démarrage et le ferme à l'arrêt.</summary>
public sealed class HoteRenduEcran(RenduEcranPlaywright rendu) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => rendu.PrechaufferAsync(ct);

    public async Task StopAsync(CancellationToken ct) => await rendu.DisposeAsync();
}
