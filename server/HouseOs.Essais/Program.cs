using System.Diagnostics;
using System.Text.Json;
using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.Editorial;
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
