using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.ComptesARebours;
using HouseOs.Api.Features.Equipements;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.FluxIcal;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Features.Sante;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Features.Zones;
using HouseOs.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Secrets locaux hors git (clé Anthropic…) — prime sur appsettings*.json.
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

// EnableDynamicJson : requis pour mapper Dictionary<string,string> (specs) en jsonb.
var sourceDonnees = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("HouseOs"))
    .EnableDynamicJson()
    .Build();
builder.Services.AddDbContext<HouseOsDbContext>(options => options.UseNpgsql(sourceDonnees));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "houseos_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
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

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
    // /mcp : clé API au lieu du cookie (la policy remplace la FallbackPolicy sur ce endpoint).
    options.AddPolicy(McpEndpoints.PolicyCleApi, policy => policy
        .AddAuthenticationSchemes(AuthentificationCleApiHandler.NomScheme)
        .RequireAuthenticatedUser());
});

builder.Services.AjouterMcp();

builder.Services.AddAntiforgery();
builder.Services.AddHostedService<RolloverService>();

builder.Services.Configure<MeteoOptions>(builder.Configuration.GetSection("Meteo"));
builder.Services.AddHttpClient();
builder.Services.AddHostedService<MeteoIngestionService>();

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

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapSante();
app.MapAuth();
app.MapTaches();
app.MapZones();
app.MapComptesARebours();
app.MapEquipements();
app.MapIcal();
app.MapMeteo();
app.MapHumeur();
app.MapFluxExternes();
app.MapMcpHouseOs();

// PWA : toute route non-API retombe sur l'app React
app.MapFallbackToFile("index.html").AllowAnonymous();

await app.MigrerEtAmorcer();

app.Run();
