namespace HouseOs.Api.Domaine.Humeur;

/// <summary>
/// Couche 2 du titre d'humeur : la banque de gabarits français, rotation
/// ensemencée par la date. Zéro coût, testable, repli permanent du polissage LLM.
/// Les faits viennent d'<see cref="EtatMaison"/> — jamais de chiffre inventé,
/// jamais de culpabilisation.
/// </summary>
public static class BanquePhrases
{
    private static readonly string[] TitresCompteARebours =
        ["On y est presque.", "Bientôt le grand jour.", "Le compte à rebours est parti.", "Ça s'en vient pour vrai."];

    private static readonly string[] TitresCalmes =
        ["La maison respire.", "Tout doux aujourd'hui.", "Rien qui presse.", "On se la coule douce."];

    private static readonly string[] TitresActifs =
        ["On avance, tranquillement.", "Une chose à la fois.", "La maison s'occupe de nous.", "Petit train va loin."];

    public static (string Titre, string SousTitre) Generer(EtatMaison etat)
    {
        // Même graine que l'intérim client (web/src/lib/humeur.ts) : stable pour
        // la journée, décalée le soir pour que la phrase tourne.
        var graine = etat.Date.Year * 366 + etat.Date.DayOfYear
            + (etat.Moment == MomentJournee.Soir ? 7 : 0);

        var compteProche = etat.ComptesProches.FirstOrDefault(c => c.Dodos > 0 && c.Dodos <= 60);
        var titres = etat.Cle switch
        {
            CleEtat.ToutFait or CleEtat.RienAuProgramme =>
                compteProche is null ? TitresCalmes : TitresCompteARebours,
            _ => compteProche is null ? TitresActifs : TitresCompteARebours,
        };

        return (Choisir(titres, graine), SousTitre(etat, graine));
    }

    private static string SousTitre(EtatMaison etat, int graine)
    {
        var corps = etat.Cle switch
        {
            CleEtat.ToutFait => Choisir(
                [
                    $"Tout est fait — {Pluriel(etat.FaitesAujourdhui, "chose")} de réglée{(etat.FaitesAujourdhui > 1 ? "s" : "")} aujourd'hui. Bravo l'équipe.",
                    "Rien ne reste sur la liste. Profitez de la soirée.",
                ], graine),
            CleEtat.RienAuProgramme => Choisir(
                [
                    "Rien au programme aujourd'hui.",
                    "Journée libre — la maison ne demande rien.",
                ], graine),
            CleEtat.Retard =>
                $"{Pluriel(etat.Ouvertes, "petite chose")} aujourd'hui — {PhraseRetard(etat.EnRetard)}, le reste est sous contrôle.",
            CleEtat.JourneeChargee => Choisir(
                [
                    $"Grosse journée : {etat.Ouvertes} choses au programme — une à la fois.",
                    $"{etat.Ouvertes} choses sur la liste, pas besoin de courir.",
                ], graine),
            _ => Choisir(
                [
                    $"{Pluriel(etat.Ouvertes, "petite chose")} aujourd'hui — rien ne presse.",
                    $"{Pluriel(etat.Ouvertes, "chose")} au programme, à votre rythme.",
                ], graine),
        };

        var meteo = PhraseMeteo(etat, graine);
        return meteo is null ? corps : $"{corps} {meteo}";
    }

    /// <summary>Une touche météo-consciente sur les journées sans retard —
    /// jamais pour alourdir une journée déjà chargée.</summary>
    private static string? PhraseMeteo(EtatMaison etat, int graine)
    {
        if (etat.Meteo is null || etat.Cle is CleEtat.Retard or CleEtat.JourneeChargee)
        {
            return null;
        }
        if (etat.Meteo.VerdictsFavorables.Contains("Être dehors"))
        {
            return Choisir(
                ["Et il fait beau — sortez donc.", "Belle journée pour mettre le nez dehors."], graine);
        }
        if (etat.Meteo.ProbabilitePrecipitationPct >= 60)
        {
            return Choisir(
                ["Parapluie pas loin, on dirait.", "Journée parfaite pour rester au chaud."], graine);
        }
        return null;
    }

    private static string PhraseRetard(int enRetard) =>
        enRetard == 1 ? "une attend depuis hier" : $"{enRetard} attendent depuis un moment";

    private static string Choisir(string[] variantes, int graine) =>
        variantes[graine % variantes.Length];

    // Accorde chaque mot (« petite chose » → « petites choses »).
    private static string Pluriel(int n, string mot) =>
        n == 1 ? $"1 {mot}" : $"{n} {string.Join(" ", mot.Split(' ').Select(m => m + "s"))}";
}
