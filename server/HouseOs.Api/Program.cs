using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.Budget;
using HouseOs.Api.Features.ComptesARebours;
using HouseOs.Api.Features.Courriel;
using HouseOs.Api.Features.Documents;
using HouseOs.Api.Features.Equipements;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.FluxIcal;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Journalisation;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Features.Sante;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Features.Zones;
using System.Threading.RateLimiting;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Journal d'amorçage : sans lui, une panne avant la fin du câblage (config absente,
// migration refusée) sort en texte brut et n'atteint jamais Seq. Remplacé par le
// logger complet dès AddSerilog.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Secrets locaux hors git (clé Anthropic…) — prime sur appsettings*.json.
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

builder.AjouterJournalisation();

// Le défaut Kestrel (30 Mo) est sous la limite documents de 50 Mo : sans ceci, un
// scan PDF de 40 Mo meurt en 413 opaque avant même d'atteindre le handler.
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = DocumentsEndpoints.TailleMax + 1024 * 1024);

// EnableDynamicJson : requis pour mapper Dictionary<string,string> (specs) en jsonb.
var sourceDonnees = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("HouseOs"))
    .EnableDynamicJson()
    .Build();
// L'intercepteur de synchro diffuse un événement par module touché après chaque
// sauvegarde : c'est ce qui rend la fraîcheur des UI ouvertes automatique, y compris
// pour les écritures MCP et les services d'arrière-plan.
builder.Services.AddSingleton<IDiffuseurSynchro, DiffuseurSynchro>();
builder.Services.AddSingleton<IntercepteurSynchro>();
builder.Services.AddDbContext<HouseOsDbContext>((sp, options) => options
    .UseNpgsql(sourceDonnees)
    .AddInterceptors(sp.GetRequiredService<IntercepteurSynchro>()));

// Les clés de protection des données chiffrent le cookie de session. Par défaut
// elles vivent dans le profil utilisateur — donc dans la couche éphémère du
// conteneur : chaque `docker compose up -d --build` en générait de nouvelles et
// déconnectait tout le monde, malgré un cookie prévu pour 180 jours. Constaté en
// prod le 2026-08-29, le jour où les logs ont commencé à le dire.
// Chemin vide (dev sur le Mac) = comportement par défaut, déjà persistant.
var cheminCles = builder.Configuration["Securite:CheminCles"];
if (string.IsNullOrWhiteSpace(cheminCles) == false)
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(Directory.CreateDirectory(cheminCles))
        // Fixe : le défaut dérive du chemin du content root, qu'un changement
        // d'image suffirait à faire bouger.
        .SetApplicationName("HouseOs");
}

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "houseos_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Secure quand la requête arrive en HTTPS (via NPM, grâce aux ForwardedHeaders) ;
        // l'accès http direct sur le LAN reste possible — risque résiduel accepté
        // (foyer de 2, Tailscale chiffre déjà le reste).
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(180);
        options.SlidingExpiration = true;
        // API : 401/403 plutôt que redirections vers une page de login
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AuthentificationCleApiHandler>(
        AuthentificationCleApiHandler.NomScheme, null);

// X-Forwarded-* honoré seulement depuis les proxys déclarés (Reseau:ProxiesConnus —
// IPs ou CIDR, séparés par des virgules : le NPM du LAN, le réseau du compose au
// besoin) au lieu du wildcard d'ASPNETCORE_FORWARDEDHEADERS_ENABLED.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var proxys = builder.Configuration["Reseau:ProxiesConnus"] ?? "";
    foreach (var entree in proxys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (entree.Contains('/'))
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(entree));
        }
        else
        {
            options.KnownProxies.Add(System.Net.IPAddress.Parse(entree));
        }
    }
});

// Fenêtre fixe par IP sur la connexion : 5/min suffit largement à deux humains,
// pas à un brute-force du LAN/tailnet.
var tentativesConnexion = builder.Configuration.GetValue("Auth:LimiteConnexion:Tentatives", 5);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (contexte, ct) =>
    {
        // Un brute-force du LAN doit laisser une trace : c'est le seul signal
        // sécurité que ce système produit.
        contexte.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("HouseOs.Auth")
            .LogWarning(
                "Limite de tentatives atteinte — {Chemin} depuis {AdresseClient}.",
                contexte.HttpContext.Request.Path.Value,
                contexte.HttpContext.Connection.RemoteIpAddress?.ToString());
        await contexte.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Trop de tentatives de connexion — réessayez dans une minute.",
        }, ct);
    };
    options.AddPolicy(AuthEndpoints.PolitiqueLimiteConnexion, contexte =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexte.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = tentativesConnexion,
                Window = TimeSpan.FromMinutes(1),
            }));
    // Journal du navigateur : anonyme par nécessité (l'écran de connexion doit pouvoir
    // se plaindre), donc borné. Deux onglets vidant leur tampon toutes les 5 s font
    // 24 lots/min — 120 laisse de la marge sans ouvrir un robinet.
    options.AddPolicy(JournalClientEndpoints.PolitiqueLimite, contexte =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexte.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
            }));
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
    // /mcp : clé API au lieu du cookie (la policy remplace la FallbackPolicy sur ce endpoint).
    options.AddPolicy(McpEndpoints.PolicyCleApi, policy => policy
        .AddAuthenticationSchemes(AuthentificationCleApiHandler.NomScheme)
        .RequireAuthenticatedUser());
});

builder.Services.AjouterMcp();

builder.Services.AddSingleton<IFournisseurTransactions, FournisseurFichier>();

builder.Services.AddAntiforgery();
builder.Services.AddSignalR();
builder.Services.AddHostedService<RolloverService>();

builder.Services.Configure<MeteoOptions>(builder.Configuration.GetSection("Meteo"));
builder.Services.AddHttpClient();
builder.Services.AddHostedService<MeteoIngestionService>();

// Téléchargement des ICS externes : plafond mémoire (un flux qui streame ferait un
// OOM du conteneur), timeout, redirections suivies à la main par la garde SSRF —
// partagé entre le worker et la validation à la création.
builder.Services.AddHttpClient(FluxExternesRafraichissement.NomClientHttp, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.MaxResponseContentBufferSize = FluxExternesRafraichissement.TailleMaxIcs;
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
    .AddHttpMessageHandler(() => new GardeSsrfHandler());
builder.Services.AddHostedService<FluxExternesRafraichissement>();

builder.Services.Configure<HumeurOptions>(builder.Configuration.GetSection("Humeur"));
// La clé peut vivre à plat (« ANTHROPIC_API_KEY » dans appsettings.local.json ou en env).
builder.Services.PostConfigure<HumeurOptions>(o =>
{
    if (string.IsNullOrWhiteSpace(o.CleApi))
    {
        o.CleApi = builder.Configuration["ANTHROPIC_API_KEY"] ?? "";
    }
});
builder.Services.AddHostedService<HumeurService>();

// Courriel entrant : le Worker Cloudflare dépose les .eml dans R2, l'app les relève
// (D-2026-09-02 Courriel Entrant Par Cloudflare Et R2). Config absente = dépôt inactif,
// service idle — le dev n'a pas besoin d'un bucket.
builder.Services.Configure<CourrielOptions>(builder.Configuration.GetSection("Courriel"));
builder.Services.AddSingleton<IDepotCourriels>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CourrielOptions>>().Value;
    return options.Actif ? new DepotCourrielsR2(options) : new DepotCourrielsInactif();
});
builder.Services.AddSingleton<IEnrichisseurCourriel, EnrichisseurAnthropic>();
builder.Services.AddSingleton<CourrielEntrantService>();
builder.Services.AddHostedService<CourrielEntrantHote>();

// Vue e-ink (D-2026-09-03 Rendu E-ink Par Chromium Headless) : le jeton que le
// serveur donne à son propre navigateur, et ce navigateur, préchauffé au démarrage.
builder.Services.Configure<AffichageOptions>(builder.Configuration.GetSection("Affichage"));
builder.Services.AddSingleton<JetonRendu>();
builder.Services.AddSingleton<RenduEcranPlaywright>();
builder.Services.AddSingleton<IRenduEcran>(sp => sp.GetRequiredService<RenduEcranPlaywright>());
builder.Services.AddHostedService<HoteRenduEcran>();
builder.Services.AddSingleton<CacheImages>();

var app = builder.Build();

// Avant tout le reste : le schéma/IP vus par l'app (cookie Secure, partition du
// rate limiter) doivent être ceux du client, pas ceux du proxy.
app.UseForwardedHeaders();

// Juste après : l'IP et le schéma journalisés doivent être ceux du client.
app.UtiliserJournalisation();

app.Use(async (contexte, suivant) =>
{
    // Jamais de reniflage MIME par le navigateur (documents téléversés inclus).
    contexte.Response.Headers.XContentTypeOptions = "nosniff";
    await suivant();
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapSante();
app.MapJournalClient();
app.MapAuth();
app.MapTaches();
app.MapZones();
app.MapComptesARebours();
app.MapEquipements();
app.MapDocuments();
app.MapCourriel();
app.MapBudget();
app.MapImportTransactions();
app.MapIcal();
app.MapMeteo();
app.MapHumeur();
app.MapFluxExternes();
app.MapAffichage();
app.MapMcpHouseOs();
// Avant le fallback SPA, sinon index.html avalerait la route du hub.
app.MapHub<SynchroHub>(SynchroHub.Chemin);

// PWA : toute route non-API retombe sur l'app React
app.MapFallbackToFile("index.html").AllowAnonymous();

await app.MigrerEtAmorcer();

app.Run();

// Rend la classe Program visible à WebApplicationFactory<Program> (tests d'intégration).
public partial class Program;
