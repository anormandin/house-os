namespace HouseOs.Api.Domaine;

public enum TypeFixe
{
    JoursSemaine,
    JourDuMois,
    Annuelle,
}

/// <summary>
/// Régime de répétition d'une tâche : type + paramètres, jamais de chaîne RRULE/cron
/// (voir D-2026-08-23 Moteur De Récurrence Trois Modes). Objet possédé par Tache,
/// mappé sur les colonnes de la même table.
/// </summary>
public class SpecRecurrence
{
    public ModeRecurrence Mode { get; set; } = ModeRecurrence.Ponctuelle;

    // Mode fixe
    public TypeFixe? FixeType { get; set; }

    /// <summary>Masque de bits par DayOfWeek (dimanche = bit 0 … samedi = bit 6).</summary>
    public int? JoursSemaineMasque { get; set; }

    /// <summary>1–31 ; clampé au dernier jour des mois courts.</summary>
    public int? JourDuMois { get; set; }

    public int? MoisAnnuel { get; set; }
    public int? JourAnnuel { get; set; }

    // Mode intervalle
    public int? IntervalleJours { get; set; }

    // Fenêtre saisonnière (mois-jour, peut chevaucher l'an : nov → mars)
    public int? FenetreDebutMois { get; set; }
    public int? FenetreDebutJour { get; set; }
    public int? FenetreFinMois { get; set; }
    public int? FenetreFinJour { get; set; }

    /// <summary>Une occurrence manquée glisse au lieu de s'empiler (tâches fixes).</summary>
    public bool Rollover { get; set; } = true;

    public bool AFenetre => FenetreDebutMois is not null;

    public static SpecRecurrence Ponctuelle() => new() { Mode = ModeRecurrence.Ponctuelle };

    public static int MasqueDe(params DayOfWeek[] jours) =>
        jours.Aggregate(0, (masque, jour) => masque | (1 << (int)jour));

    public bool JourSemainePlanifie(DayOfWeek jour) =>
        ((JoursSemaineMasque ?? 0) >> (int)jour & 1) == 1;

    /// <summary>
    /// La fenêtre saisonnière vue comme deux dates plutôt que comme quatre nombres :
    /// celle qui est ouverte à <paramref name="reference"/>, sinon la prochaine à
    /// s'ouvrir. Null quand la tâche n'a pas de fenêtre.
    ///
    /// <para>Une fenêtre qui chevauche le jour de l'An (novembre → mars) appartient à
    /// deux années civiles : on essaie donc l'ancrage de l'an dernier avant celui de
    /// cette année, sans quoi le 15 janvier tomberait « hors saison ».</para>
    /// </summary>
    public (DateOnly Debut, DateOnly Fin)? FenetreAutour(DateOnly reference)
    {
        if (AFenetre == false)
        {
            return null;
        }
        for (var annee = reference.Year - 1; annee <= reference.Year + 1; annee++)
        {
            var debut = JourBorne(annee, FenetreDebutMois!.Value, FenetreDebutJour!.Value);
            var fin = JourBorne(annee, FenetreFinMois!.Value, FenetreFinJour!.Value);
            if (fin < debut)
            {
                fin = JourBorne(annee + 1, FenetreFinMois!.Value, FenetreFinJour!.Value);
            }
            if (reference <= fin)
            {
                return (debut, fin);
            }
        }
        return null;
    }

    /// <summary>Le 31 d'un mois court est le dernier jour du mois, comme pour JourDuMois.</summary>
    private static DateOnly JourBorne(int annee, int mois, int jour) =>
        new(annee, mois, Math.Min(jour, DateTime.DaysInMonth(annee, mois)));

    /// <summary>La date tombe-t-elle dans la fenêtre saisonnière (bornes incluses)?</summary>
    public bool DansFenetre(DateOnly date)
    {
        if (AFenetre == false)
        {
            return true;
        }
        var md = date.Month * 100 + date.Day;
        var debut = FenetreDebutMois!.Value * 100 + FenetreDebutJour!.Value;
        var fin = FenetreFinMois!.Value * 100 + FenetreFinJour!.Value;
        return debut <= fin
            ? md >= debut && md <= fin
            : md >= debut || md <= fin; // chevauche le changement d'année
    }
}
