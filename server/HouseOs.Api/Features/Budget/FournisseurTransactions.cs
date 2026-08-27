using System.Globalization;
using System.Text.RegularExpressions;

namespace HouseOs.Api.Features.Budget;

/// <summary>Transaction lue d'une source externe, avant dédup et persistance.</summary>
public record TransactionImportee(DateOnly Date, decimal Montant, string Description, string? IdExterne);

/// <summary>
/// Abstraction d'entrée des transactions bancaires (D-2026-08-26 Import Manuel
/// D'abord Sync Ensuite). V1 : fichiers OFX/CSV téléversés ; SimpleFIN sera un
/// second fournisseur activé par configuration, sans refonte.
/// </summary>
public interface IFournisseurTransactions
{
    /// <summary>Lit un fichier exporté de la banque. Lève <see cref="FormatFichierException"/>
    /// si le contenu n'est ni un OFX ni un CSV reconnaissable.</summary>
    IReadOnlyList<TransactionImportee> Lire(string contenu, string nomFichier);
}

public class FormatFichierException(string message) : Exception(message);

/// <summary>Fournisseur v1 : fichiers OFX (FITID) et CSV AccWeb Desjardins.</summary>
public class FournisseurFichier : IFournisseurTransactions
{
    public IReadOnlyList<TransactionImportee> Lire(string contenu, string nomFichier)
    {
        if (contenu.Contains("<OFX", StringComparison.OrdinalIgnoreCase)
            || contenu.Contains("OFXHEADER", StringComparison.OrdinalIgnoreCase)
            || nomFichier.EndsWith(".ofx", StringComparison.OrdinalIgnoreCase))
        {
            return LecteurOfx.Lire(contenu);
        }
        return LecteurCsv.Lire(contenu);
    }
}

/// <summary>
/// Lecteur OFX tolérant : OFX 1.x est du SGML sans balises fermantes — on extrait
/// les blocs STMTTRN et leurs champs à la ligne, sans parseur XML.
/// </summary>
public static partial class LecteurOfx
{
    [GeneratedRegex(@"<STMTTRN>(.*?)(?=<STMTTRN>|</BANKTRANLIST>|</STMTTRN>)",
        RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BlocsTransaction();

    [GeneratedRegex(@"<(?<balise>DTPOSTED|TRNAMT|FITID|NAME|MEMO)>(?<valeur>[^<\r\n]*)",
        RegexOptions.IgnoreCase)]
    private static partial Regex Champs();

    public static IReadOnlyList<TransactionImportee> Lire(string contenu)
    {
        var transactions = new List<TransactionImportee>();
        foreach (Match bloc in BlocsTransaction().Matches(contenu))
        {
            string? dtPosted = null, trnAmt = null, fitId = null, nom = null, memo = null;
            foreach (Match champ in Champs().Matches(bloc.Groups[1].Value))
            {
                var valeur = champ.Groups["valeur"].Value.Trim();
                switch (champ.Groups["balise"].Value.ToUpperInvariant())
                {
                    case "DTPOSTED": dtPosted = valeur; break;
                    case "TRNAMT": trnAmt = valeur; break;
                    case "FITID": fitId = valeur; break;
                    case "NAME": nom = valeur; break;
                    case "MEMO": memo = valeur; break;
                }
            }
            if (dtPosted is null || trnAmt is null)
            {
                continue;
            }
            transactions.Add(new TransactionImportee(
                ParserDateOfx(dtPosted),
                ParserMontant(trnAmt),
                string.IsNullOrWhiteSpace(nom) ? memo ?? "(sans description)" : nom,
                string.IsNullOrWhiteSpace(fitId) ? null : fitId));
        }
        if (transactions.Count == 0)
        {
            throw new FormatFichierException("Aucune transaction trouvée dans le fichier OFX.");
        }
        return transactions;
    }

    /// <summary>DTPOSTED : YYYYMMDD, souvent suivi d'une heure et d'un fuseau.</summary>
    private static DateOnly ParserDateOfx(string valeur)
    {
        if (valeur.Length >= 8
            && DateOnly.TryParseExact(valeur[..8], "yyyyMMdd", out var date))
        {
            return date;
        }
        throw new FormatFichierException($"Date OFX illisible : « {valeur} ».");
    }

    internal static decimal ParserMontant(string valeur)
    {
        var normalise = valeur.Replace(" ", "").Replace(" ", "").Replace("$", "");
        // Virgule décimale québécoise quand il n'y a pas déjà de point.
        if (normalise.Contains('.') == false)
        {
            normalise = normalise.Replace(',', '.');
        }
        else
        {
            normalise = normalise.Replace(",", "");
        }
        if (decimal.TryParse(normalise, NumberStyles.Number, CultureInfo.InvariantCulture, out var montant))
        {
            return montant;
        }
        throw new FormatFichierException($"Montant illisible : « {valeur} ».");
    }
}

/// <summary>
/// Lecteur CSV visant l'export AccWeb Desjardins, tolérant sur ses variantes :
/// délimiteur , ou ;, colonnes Retrait/Dépôt séparées ou montant signé unique,
/// virgule décimale. Les lignes sans date reconnaissable (en-têtes) sont sautées.
/// </summary>
public static class LecteurCsv
{
    public static IReadOnlyList<TransactionImportee> Lire(string contenu)
    {
        var lignes = contenu.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lignes.Length == 0)
        {
            throw new FormatFichierException("Fichier vide.");
        }
        var delimiteur = ChoisirDelimiteur(lignes);
        var transactions = new List<TransactionImportee>();
        foreach (var ligne in lignes)
        {
            var champs = DecouperLigne(ligne, delimiteur);
            var indexDate = champs.FindIndex(EstDate);
            if (indexDate < 0)
            {
                continue; // en-tête ou ligne de garnissage
            }
            var date = ParserDate(champs[indexDate]);

            // Description : le champ texte non numérique le plus long après la date.
            var description = champs
                .Skip(indexDate + 1)
                .Where(c => string.IsNullOrWhiteSpace(c) == false && EstNumerique(c) == false && EstDate(c) == false)
                .OrderByDescending(c => c.Length)
                .FirstOrDefault() ?? "(sans description)";
            var indexDescription = champs.FindIndex(indexDate + 1, c => c == description);

            var montant = ExtraireMontant(champs, Math.Max(indexDate, indexDescription));
            if (montant is null || montant == 0)
            {
                continue;
            }
            transactions.Add(new TransactionImportee(date, montant.Value, description, IdExterne: null));
        }
        if (transactions.Count == 0)
        {
            throw new FormatFichierException(
                "Aucune transaction reconnue — le fichier est-il bien un export CSV de la banque ?");
        }
        return transactions;
    }

    /// <summary>
    /// Desjardins expose, après la description, Retrait puis Dépôt en colonnes séparées
    /// (montant = dépôt − retrait) suivies du solde — lecture positionnelle, colonne vide
    /// = 0. Une seule colonne après la description : montant signé unique.
    /// </summary>
    private static decimal? ExtraireMontant(List<string> champs, int apresIndex)
    {
        var positions = champs
            .Skip(apresIndex + 1)
            .Where(c => c.Length == 0 || EstNumerique(c))
            .Select(c => c.Length == 0 ? 0m : LecteurOfx.ParserMontant(c))
            .ToList();
        return positions.Count switch
        {
            0 => null,
            1 => positions[0],
            _ => positions[1] - positions[0],            // retrait, dépôt (puis solde, ignoré)
        };
    }

    private static char ChoisirDelimiteur(string[] lignes) =>
        lignes[0].Count(c => c == ';') > lignes[0].Count(c => c == ',') ? ';' : ',';

    /// <summary>Découpe une ligne CSV en respectant les guillemets.</summary>
    private static List<string> DecouperLigne(string ligne, char delimiteur)
    {
        var champs = new List<string>();
        var courant = new System.Text.StringBuilder();
        var entreGuillemets = false;
        foreach (var c in ligne)
        {
            if (c == '"')
            {
                entreGuillemets = entreGuillemets == false;
            }
            else if (c == delimiteur && entreGuillemets == false)
            {
                champs.Add(courant.ToString().Trim());
                courant.Clear();
            }
            else
            {
                courant.Append(c);
            }
        }
        champs.Add(courant.ToString().Trim());
        return champs;
    }

    private static bool EstDate(string valeur) => TryParserDate(valeur, out _);

    private static DateOnly ParserDate(string valeur) =>
        TryParserDate(valeur, out var date)
            ? date
            : throw new FormatFichierException($"Date illisible : « {valeur} ».");

    private static bool TryParserDate(string valeur, out DateOnly date)
    {
        string[] formats = ["yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "yyyyMMdd"];
        return DateOnly.TryParseExact(valeur.Trim(), formats, out date);
    }

    private static bool EstNumerique(string valeur)
    {
        var v = valeur.Replace(" ", "").Replace(" ", "").Replace("$", "");
        if (v.Length == 0)
        {
            return false;
        }
        return v.All(c => char.IsAsciiDigit(c) || c is '.' or ',' or '-' or '+');
    }
}
