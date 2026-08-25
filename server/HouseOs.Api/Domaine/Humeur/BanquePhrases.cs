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

        var suites = new[] { corps, PhraseProchaine(etat), PhraseMeteo(etat, graine) };
        return string.Join(" ", suites.Where(s => s is not null));
    }

    /// <summary>Sur une journée libre, pointer la prochaine tâche — quelque chose
    /// à anticiper plutôt qu'un simple « rien au programme ».</summary>
    private static string? PhraseProchaine(EtatMaison etat)
    {
        var journeeLibre = etat.Cle is CleEtat.ToutFait or CleEtat.RienAuProgramme;
        var prochaine = etat.ProchainesTaches.FirstOrDefault();
        if (journeeLibre == false || prochaine is null)
        {
            return null;
        }
        var horizon = prochaine.DansJours == 1 ? "demain" : $"dans {prochaine.DansJours} jours";
        return $"Prochaine affaire : {prochaine.Titre}, {horizon}.";
    }

    /// <summary>La météo seulement quand elle sort de l'ordinaire (D-2026-08-25
    /// Phrase Du Jour Axée Tâches) — et jamais pour alourdir une journée déjà
    /// chargée ou en retard.</summary>
    private static string? PhraseMeteo(EtatMaison etat, int graine)
    {
        if (etat.MeteoRemarquable is null || etat.Cle is CleEtat.Retard or CleEtat.JourneeChargee)
        {
            return null;
        }
        if (etat.MeteoRemarquable.Favorable)
        {
            return Choisir(
                ["Et il fait beau — sortez donc.", "Belle journée pour mettre le nez dehors."], graine);
        }
        return $"Pis dehors : {etat.MeteoRemarquable.Description}.";
    }

    private static string PhraseRetard(int enRetard) =>
        enRetard == 1 ? "une attend depuis hier" : $"{enRetard} attendent depuis un moment";

    private static string Choisir(string[] variantes, int graine) =>
        variantes[graine % variantes.Length];

    // Accorde chaque mot (« petite chose » → « petites choses »).
    private static string Pluriel(int n, string mot) =>
        n == 1 ? $"1 {mot}" : $"{n} {string.Join(" ", mot.Split(' ').Select(m => m + "s"))}";
}
