using System.Globalization;

namespace HouseOs.Api.Domaine.Lettre;

/// <summary>
/// La note C3 : sujet + quatre lignes composées sans modèle, depuis la matière — ce qui
/// part quand le modèle s'est tu deux fois, ou tout de suite quand il n'y a pas de clé
/// (D-2026-09-21 Lettre Écrite À Part Sur La Même Matière). Jamais vide : le fonds de
/// tiroir bouche les trous.
/// </summary>
public static class NoteDeRepli
{
    public const int Lignes = 4;
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-CA");

    public static TexteDeLettre Composer(MatiereDeLettre matiere)
    {
        var e = matiere.Edition;
        var lignes = new List<string> { LigneDesTaches(matiere) };

        if (e.Meteo is { } m)
        {
            lignes.Add($"{Majuscule(m.Description)}, de {Degres(m.TempMin)} à {Degres(m.TempMax)} °C.");
        }
        if (matiere.SemaineDevant.Count > 0)
        {
            var p = matiere.SemaineDevant[0];
            var quand = p.Date.ToString("dddd d MMMM", Fr);
            lignes.Add(p.Assigne is null
                ? $"{Majuscule(quand)} : {p.Titre}."
                : $"{Majuscule(quand)} : {p.Titre} ({p.Assigne}).");
        }
        if (e.ProchainCompte is { } c)
        {
            lignes.Add(c.Dodos switch
            {
                0 => $"C'est aujourd'hui : {c.Titre}.",
                1 => $"Un dodo avant {c.Titre}.",
                _ => $"{c.Dodos} dodos avant {c.Titre}.",
            });
        }
        foreach (var fait in e.Faits)
        {
            if (lignes.Count >= Lignes)
            {
                break;
            }
            lignes.Add(fait.Texte);
        }
        return new TexteDeLettre(Sujet(matiere), lignes.Take(Lignes).ToList());
    }

    private static string LigneDesTaches(MatiereDeLettre matiere)
    {
        var e = matiere.Edition;
        if (e.Plancher is { } plancher)
        {
            return $"{plancher.Titre} : ça ne se relègue pas.";
        }
        return e.TachesDues.Count switch
        {
            0 => "Rien n'est dû aujourd'hui.",
            1 => $"Une seule chose aujourd'hui : {e.TachesDues[0].Titre}.",
            2 => $"Deux choses aujourd'hui : {e.TachesDues[0].Titre} et {e.TachesDues[1].Titre}.",
            var n => $"{n} choses aujourd'hui, dont {e.TachesDues[0].Titre} et {e.TachesDues[1].Titre}.",
        };
    }

    private static string Sujet(MatiereDeLettre matiere)
    {
        var e = matiere.Edition;
        var sujet = e.Plancher is { } p
            ? p.Titre
            : e.TachesDues.Count switch
            {
                0 => "Rien à faire aujourd'hui",
                1 => $"Une seule chose : {e.TachesDues[0].Titre}",
                var n => $"{n} choses à faire aujourd'hui",
            };
        return sujet.Length <= 60 ? sujet : sujet[..59].TrimEnd() + "…";
    }

    private static string Majuscule(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Fr) + s[1..];

    private static string Degres(double t) => Math.Round(t).ToString("0", Fr).Replace("-", "−");
}
