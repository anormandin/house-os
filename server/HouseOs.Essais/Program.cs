using System.Diagnostics;
using System.Text.Json;
using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Lettre;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Domaine.Lettre;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

// L'atelier de l'éditorialiste : rejouer une matière conservée contre un prompt, sans
// toucher au code ni à la base. Jamais dans l'image Docker — un outil de la ronde de
// réglage (vault : Journal De La Maison, « L'atelier du prompt »).

const string Aide = """
    Atelier de l'éditorialiste — rejouer une journée contre un prompt.

      dotnet run --project server/HouseOs.Essais -- prompt
          Imprime le prompt système en vigueur (à rediriger dans un fichier, puis à retoucher).

      dotnet run --project server/HouseOs.Essais -- matiere <YYYY-MM-DD> [--base <connexion>]
          Imprime la matière conservée sur l'édition de cette date (la base de dev par défaut,
          ou ConnectionStrings__HouseOs, ou --base). À rediriger dans un fichier.

      dotnet run --project server/HouseOs.Essais -- rediger <matiere.json> [--prompt <fichier>]
                                                    [--modele <id>] [--fois <n>] [--brut]
          Envoie la matière au modèle avec le prompt du fichier (le défaut sans --prompt), n fois
          (1 par défaut), et imprime chaque réponse avec ses longueurs — ou l'écart qui l'a fait
          refuser, avec le texte brut. --brut imprime le texte brut même quand il est accepté.
          La clé : ANTHROPIC_API_KEY, ou « ANTHROPIC_API_KEY » / « Humeur:CleApi » dans
          server/HouseOs.Api/appsettings.local.json.

    La lettre du matin (vault : Lettre Du Matin), mêmes règles, ses propres commandes :

      dotnet run --project server/HouseOs.Essais -- prompt-lettre
      dotnet run --project server/HouseOs.Essais -- matiere-lettre <YYYY-MM-DD> [--base <connexion>] [--composer]
          Imprime la matière conservée sur la lettre de cette date. --composer la COMPOSE depuis
          la base (tâches, journal, fonds de tiroir, avec la météo et le lieu de appsettings),
          sans rien écrire : pour rejouer une journée qui n'a pas encore de lettre.
      dotnet run --project server/HouseOs.Essais -- rediger-lettre <matiere.json> [--prompt <fichier>]
                                                    [--modele <id>] [--fois <n>] [--brut]
    """;

const string BaseDeDev = "Host=localhost;Port=5433;Database=houseos;Username=houseos;Password=houseos-dev";

if (args.Length == 0 || args[0] is "-h" or "--help" or "aide")
{
    Console.WriteLine(Aide);
    return 0;
}

try
{
    return args[0] switch
    {
        "prompt" => Prompt(),
        "matiere" => await Matiere(args),
        "rediger" => await Rediger(args),
        "prompt-lettre" => PromptLettre(),
        "matiere-lettre" => await MatiereLettre(args),
        "rediger-lettre" => await RedigerLettre(args),
        _ => Erreur($"commande inconnue : {args[0]}"),
    };
}
catch (Exception ex)
{
    return Erreur(ex.Message);
}

static int Prompt()
{
    Console.Write(RedactionLlm.PromptParDefaut);
    return 0;
}

static async Task<int> Matiere(string[] args)
{
    if (args.Length < 2 || DateOnly.TryParseExact(args[1], "yyyy-MM-dd", out var date) == false)
    {
        return Erreur("matiere : il faut une date YYYY-MM-DD.");
    }
    var connexion = Option(args, "--base")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__HouseOs")
        ?? BaseDeDev;
    var options = new DbContextOptionsBuilder<HouseOsDbContext>().UseNpgsql(connexion).Options;
    await using var db = new HouseOsDbContext(options);
    // La seule colonne qu'on veut : un contexte nu, sans le JSON dynamique de l'app,
    // ne saurait pas relire les listes jsonb de l'édition.
    var edition = await db.Editions.AsNoTracking()
        .Where(e => e.Date == date)
        .Select(e => new { e.Matiere })
        .SingleOrDefaultAsync();
    if (edition is null)
    {
        return Erreur($"aucune édition le {date:yyyy-MM-dd}.");
    }
    if (edition.Matiere is null)
    {
        return Erreur($"l'édition du {date:yyyy-MM-dd} n'a pas de matière (gabarit posé par le rendu, " +
            "ou écrite avant que la matière soit conservée).");
    }
    Console.WriteLine(edition.Matiere);
    return 0;
}

static async Task<int> Rediger(string[] args)
{
    if (args.Length < 2 || File.Exists(args[1]) == false)
    {
        return Erreur("rediger : il faut le chemin d'un fichier de matière.");
    }
    var matiere = RedactionLlm.DeserialiserMatiere(await File.ReadAllTextAsync(args[1]));
    if (matiere is null)
    {
        return Erreur($"{args[1]} n'est pas une matière d'édition.");
    }
    var prompt = Option(args, "--prompt") is { } fichier ? await File.ReadAllTextAsync(fichier) : null;
    var modele = Option(args, "--modele") ?? new EditionOptions().Modele;
    var fois = int.TryParse(Option(args, "--fois"), out var n) && n > 0 ? n : 1;
    var brut = args.Contains("--brut");
    var cle = CleApi();
    if (cle is null)
    {
        return Erreur("aucune clé Anthropic (ANTHROPIC_API_KEY, ou appsettings.local.json).");
    }

    Console.WriteLine($"matière du {matiere.Date:yyyy-MM-dd} — rang {matiere.Rang}, " +
        $"{matiere.TachesDues.Count} tâche(s), {matiere.Faits.Count} fait(s), " +
        $"{matiere.Precedentes.Count} précédente(s) — prompt {(prompt is null ? "par défaut" : Option(args, "--prompt"))}, " +
        $"modèle {modele}, {fois} fois");

    for (var i = 1; i <= fois; i++)
    {
        var chrono = Stopwatch.StartNew();
        var reponse = await RedactionLlm.RedigerAvecEcart(matiere, cle, modele, CancellationToken.None, prompt);
        chrono.Stop();
        Console.WriteLine();
        Console.WriteLine($"— essai {i}/{fois} ({chrono.Elapsed.TotalSeconds:0} s) —");
        if (reponse.Texte is { } texte)
        {
            Imprimer(texte);
            if (brut)
            {
                Console.WriteLine();
                Console.WriteLine(reponse.Brut);
            }
        }
        else
        {
            Console.WriteLine($"REFUSÉ : {reponse.Ecart}");
            Console.WriteLine(reponse.Brut ?? "(aucun texte)");
        }
    }
    return 0;
}

static int PromptLettre()
{
    Console.Write(RedactionLettre.PromptParDefaut);
    return 0;
}

static async Task<int> MatiereLettre(string[] args)
{
    if (args.Length < 2 || DateOnly.TryParseExact(args[1], "yyyy-MM-dd", out var date) == false)
    {
        return Erreur("matiere-lettre : il faut une date YYYY-MM-DD.");
    }
    var connexion = Option(args, "--base")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__HouseOs")
        ?? BaseDeDev;

    if (args.Contains("--composer"))
    {
        // La même journée que le service composerait : un vrai contexte (jsonb
        // dynamique), la météo et le lieu de appsettings, la banque du hasard de l'app.
        var dossierApi = DossierApi();
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(dossierApi, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(dossierApi, "appsettings.local.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();
        var meteo = config.GetSection("Meteo").Get<MeteoOptions>() ?? new MeteoOptions();
        var lieu = config["Affichage:Lieu"];
        var banque = LectureDeLaBanque.Lire(config["Hasard:Fichier"] ?? "", dossierApi, NullLogger.Instance);
        var source = new Npgsql.NpgsqlDataSourceBuilder(connexion).EnableDynamicJson().Build();
        var optionsComplet = new DbContextOptionsBuilder<HouseOsDbContext>().UseNpgsql(source).Options;
        await using var dbComplet = new HouseOsDbContext(optionsComplet);
        var composee = await GenerationLettre.MatiereAsync(
            dbComplet, meteo, banque, lieu, date, DateTime.Now, CancellationToken.None);
        Console.WriteLine(RedactionLettre.SerialiserMatiere(composee));
        return 0;
    }

    var options = new DbContextOptionsBuilder<HouseOsDbContext>().UseNpgsql(connexion).Options;
    await using var db = new HouseOsDbContext(options);
    var lettre = await db.Lettres.AsNoTracking()
        .Where(l => l.Date == date)
        .Select(l => new { l.Matiere })
        .SingleOrDefaultAsync();
    if (lettre is null)
    {
        return Erreur($"aucune lettre le {date:yyyy-MM-dd} (--composer pour la composer depuis la base).");
    }
    if (lettre.Matiere is null)
    {
        return Erreur($"la lettre du {date:yyyy-MM-dd} n'a pas de matière.");
    }
    Console.WriteLine(lettre.Matiere);
    return 0;
}

static async Task<int> RedigerLettre(string[] args)
{
    if (args.Length < 2 || File.Exists(args[1]) == false)
    {
        return Erreur("rediger-lettre : il faut le chemin d'un fichier de matière.");
    }
    var matiere = RedactionLettre.DeserialiserMatiere(await File.ReadAllTextAsync(args[1]));
    if (matiere is null)
    {
        return Erreur($"{args[1]} n'est pas une matière de lettre.");
    }
    var prompt = Option(args, "--prompt") is { } fichier ? await File.ReadAllTextAsync(fichier) : null;
    var modele = Option(args, "--modele") ?? new EditionOptions().Modele;
    var fois = int.TryParse(Option(args, "--fois"), out var n) && n > 0 ? n : 1;
    var brut = args.Contains("--brut");
    var cle = CleApi();
    if (cle is null)
    {
        return Erreur("aucune clé Anthropic (ANTHROPIC_API_KEY, ou appsettings.local.json).");
    }

    var e = matiere.Edition;
    Console.WriteLine($"matière du {e.Date:yyyy-MM-dd} — rang {e.Rang}, {e.TachesDues.Count} tâche(s), " +
        $"{e.Faits.Count} fait(s), {matiere.SemaineDevant.Count} devant, {matiere.FaitesDepuisLaDerniere.Count} faite(s), " +
        $"{matiere.Precedentes.Count} précédente(s) — prompt {(prompt is null ? "par défaut" : Option(args, "--prompt"))}, " +
        $"modèle {modele}, {fois} fois");

    for (var i = 1; i <= fois; i++)
    {
        var chrono = Stopwatch.StartNew();
        var reponse = await RedactionLettre.RedigerAvecEcart(matiere, cle, modele, CancellationToken.None, prompt);
        chrono.Stop();
        Console.WriteLine();
        Console.WriteLine($"— essai {i}/{fois} ({chrono.Elapsed.TotalSeconds:0} s) —");
        if (reponse.Texte is { } texte)
        {
            ImprimerLettre(texte);
            if (brut)
            {
                Console.WriteLine();
                Console.WriteLine(reponse.Brut);
            }
        }
        else
        {
            Console.WriteLine($"REFUSÉ : {reponse.Ecart}");
            Console.WriteLine(reponse.Brut ?? "(aucun texte)");
        }
    }
    return 0;
}

static void ImprimerLettre(TexteDeLettre texte)
{
    Ligne("sujet", texte.Sujet, 55);
    for (var i = 0; i < texte.Paragraphes.Count; i++)
    {
        Ligne($"§ {i + 1}", texte.Paragraphes[i], 250);
    }
    var total = texte.Paragraphes.Sum(p => p.Length);
    Console.WriteLine($"total      ({total,4}{(total > 1200 ? " > 1200" : "")})");
}

/// <summary>Le dossier de HouseOs.Api, cherché en remontant depuis le dossier courant.</summary>
static string DossierApi()
{
    for (var dossier = new DirectoryInfo(Directory.GetCurrentDirectory()); dossier is not null; dossier = dossier.Parent)
    {
        foreach (var candidat in new[]
        {
            Path.Combine(dossier.FullName, "HouseOs.Api"),
            Path.Combine(dossier.FullName, "server", "HouseOs.Api"),
        })
        {
            if (File.Exists(Path.Combine(candidat, "appsettings.json")))
            {
                return candidat;
            }
        }
    }
    throw new InvalidOperationException("HouseOs.Api introuvable depuis le dossier courant.");
}

static void Imprimer(TexteDEdition texte)
{
    Ligne("surtitre", texte.Surtitre, RedactionLlm.LongueurMaxSurtitre);
    Ligne("manchette", texte.Manchette, RedactionLlm.LongueurMaxManchette);
    Ligne("chapeau", texte.Chapeau, RedactionLlm.LongueurMaxChapeau);
    for (var i = 0; i < texte.Paragraphes.Count; i++)
    {
        Ligne($"§ {i + 1}", texte.Paragraphes[i], RedactionLlm.LongueurViseeParagraphe);
    }
    if (texte.Rubriques.Count > 0)
    {
        Console.WriteLine("rubriques :");
        foreach (var r in texte.Rubriques)
        {
            Console.WriteLine($"  {r.Nom} ({r.Taches.Count}) : {string.Join(" · ", r.Taches)}");
        }
    }
}

/// <summary>La longueur à côté de chaque champ, et un repère quand elle passe la cible.</summary>
static void Ligne(string nom, string valeur, int cible)
{
    var repere = valeur.Length > cible ? $" > {cible}" : "";
    Console.WriteLine($"{nom,-10} ({valeur.Length,3}{repere}) : {valeur}");
}

static string? Option(string[] args, string nom)
{
    var i = Array.IndexOf(args, nom);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

/// <summary>La clé du titre d'humeur, là où le dev la met : la variable d'environnement,
/// sinon appsettings.local.json (cherché en remontant depuis le dossier courant).</summary>
static string? CleApi()
{
    if (Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is { Length: > 0 } env)
    {
        return env;
    }
    for (var dossier = new DirectoryInfo(Directory.GetCurrentDirectory()); dossier is not null; dossier = dossier.Parent)
    {
        foreach (var chemin in new[]
        {
            Path.Combine(dossier.FullName, "appsettings.local.json"),
            Path.Combine(dossier.FullName, "HouseOs.Api", "appsettings.local.json"),
            Path.Combine(dossier.FullName, "server", "HouseOs.Api", "appsettings.local.json"),
        })
        {
            if (File.Exists(chemin) == false)
            {
                continue;
            }
            using var document = JsonDocument.Parse(File.ReadAllText(chemin));
            var racine = document.RootElement;
            if (racine.TryGetProperty("ANTHROPIC_API_KEY", out var directe) && directe.GetString() is { Length: > 0 } d)
            {
                return d;
            }
            if (racine.TryGetProperty("Humeur", out var humeur)
                && humeur.TryGetProperty("CleApi", out var cleApi) && cleApi.GetString() is { Length: > 0 } c)
            {
                return c;
            }
        }
    }
    return null;
}

static int Erreur(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
