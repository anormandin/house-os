using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.Budget;
using HouseOs.Api.Features.ComptesARebours;
using HouseOs.Api.Features.Documents;
using HouseOs.Api.Features.Equipements;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.FluxIcal;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Features.Sante;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Features.Zones;
using System.Threading.RateLimiting;
using HouseOs.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Secrets locaux hors git (clé Anthropic…) — prime sur appsettings*.json.
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

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
        await contexte.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Trop de tentatives de connexion — réessayez dans une minute.",
        }, ct);
    options.AddPolicy(AuthEndpoints.PolitiqueLimiteConnexion, contexte =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexte.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = tentativesConnexion,
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

var app = builder.Build();

// Avant tout le reste : le schéma/IP vus par l'app (cookie Secure, partition du
// rate limiter) doivent être ceux du client, pas ceux du proxy.
app.UseForwardedHeaders();

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
app.MapAuth();
app.MapTaches();
app.MapZones();
app.MapComptesARebours();
app.MapEquipements();
app.MapDocuments();
app.MapBudget();
app.MapImportTransactions();
app.MapIcal();
app.MapMeteo();
app.MapHumeur();
app.MapFluxExternes();
app.MapMcpHouseOs();
// Avant le fallback SPA, sinon index.html avalerait la route du hub.
app.MapHub<SynchroHub>(SynchroHub.Chemin);

// PWA : toute route non-API retombe sur l'app React
app.MapFallbackToFile("index.html").AllowAnonymous();

await app.MigrerEtAmorcer();

app.Run();

// Rend la classe Program visible à WebApplicationFactory<Program> (tests d'intégration).
public partial class Program;
