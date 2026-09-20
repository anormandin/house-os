using System.Globalization;
using HouseOs.Api.Domaine.Ephemerides;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// La famille « le ciel » : sept items tirés d'un calcul local, sans réseau et sans
/// base (vault : Fonds De Tiroir). Les raretés sont celles de la table des maquettes —
/// la durée du jour peut sortir tous les jours, l'équinoxe quatre fois l'an.
///
/// <para>Un fait dont la source manque <b>ne sort pas</b> : au-delà des cercles
/// polaires il n'y a ni lever ni coucher certains jours, et beaucoup de fuseaux n'ont
/// pas de changement d'heure. Ce sont des absences normales, jamais des erreurs.</para>
///
/// <para><b>Le texte long doit ajouter quelque chose.</b> L'étiquette et la valeur
/// disent déjà l'essentiel ; un texte qui les répète fait perdre trois lignes au mur
/// pour rien (« LE JOUR RACCOURCIT / 3 min par jour / Le jour raccourcit d'environ
/// 3 minutes par jour »). Un test balaie l'année et refuse tout texte long qui contient
/// l'étiquette ou la valeur.</para>
/// </summary>
public static class FaitsDuCiel
{
    /// <summary>Au-delà, le prochain équinoxe ou solstice est trop loin pour se dire.</summary>
    private const int JoursDApprocheSaison = 10;

    /// <summary>Idem pour le changement d'heure — deux semaines de préavis suffisent.</summary>
    private const int JoursDApprocheChangementHeure = 14;

    public static IEnumerable<FaitDeTiroir> Produire(ContexteDuJour contexte)
    {
        var ciel = Ciel.Calculer(contexte.Date, contexte.Lieu, contexte.Fuseau);

        var faits = new List<FaitDeTiroir?>
        {
            LeverEtCoucher(ciel),
            Derive(ciel),
            PhaseDeLune(ciel),
            ProchaineSaison(ciel, contexte),
            Equilibre(ciel, contexte),
            BasculeHoraire(ciel, contexte.Date),
            Noirceur(ciel, contexte),
        };
        return faits.OfType<FaitDeTiroir>();
    }

    private static FaitDeTiroir? LeverEtCoucher(CielDuJour ciel)
    {
        var soleil = ciel.Soleil;
        if (soleil.Lever is not { } lever || soleil.Coucher is not { } coucher)
        {
            // Nuit ou jour polaire : c'est autrement plus remarquable qu'un lever, et
            // ça ne dure qu'une partie de l'année.
            return new FaitDeTiroir(
                "ciel.jour",
                FamilleDeFait.Ciel,
                "Le soleil",
                soleil.NuitPolaire ? "Il ne se lève pas" : "Il ne se couche pas",
                soleil.NuitPolaire
                    ? "Il ne franchira pas l'horizon de la journée."
                    : "Il restera au-dessus de l'horizon toute la journée.",
                new ScoreDeFait(Rarete.ParAn(60), 1, 1.5));
        }

        return new FaitDeTiroir(
            "ciel.jour",
            FamilleDeFait.Ciel,
            "Le soleil",
            $"{Heure(lever)} → {Heure(coucher)}",
            // La durée, et seulement elle : redire les deux heures qui sont déjà
            // au-dessus ne vaut pas une ligne de journal.
            $"Cela fait {Duree(ciel.Soleil.Duree!.Value)} de clarté.",
            new ScoreDeFait(Rarete.Quotidien, 1, 1));
    }

    private static FaitDeTiroir? Derive(CielDuJour ciel)
    {
        // Le texte long passe à la semaine : trois minutes par jour ne se sentent pas,
        // vingt et une d'un coup, oui — c'est l'écart qu'on remarque en sortant du
        // travail. Aux latitudes où la semaine n'a pas de sens, on retombe sur le jour.

        if (ciel.DeriveQuotidienne is not { } derive)
        {
            return null;
        }
        // Sous la minute, la dérive n'est plus une nouvelle : c'est ce qui se passe
        // autour des solstices, et c'est le fait « saison » qui prend le relais.
        var minutes = (int)Math.Round(Math.Abs(derive.TotalMinutes));
        if (minutes < 1)
        {
            return null;
        }

        // L'étiquette dit le sens et la valeur dit le chiffre, comme dans les
        // maquettes (« On perd » / « 2 min / jour ») : c'est ce qui permet au fait de
        // tenir sur une seule rangée de tableau au lieu d'y être coupé.
        var sallonge = derive > TimeSpan.Zero;
        var surLaSemaine = ciel.DeriveHebdomadaire is { } semaine
            ? (int)Math.Round(Math.Abs(semaine.TotalMinutes))
            : 0;
        var texte = surLaSemaine >= 1
            ? $"{surLaSemaine} minute{(surLaSemaine > 1 ? "s" : "")} {(sallonge ? "de plus" : "de moins")} qu'il y a une semaine."
            : $"Environ {minutes} minute{(minutes > 1 ? "s" : "")} d'écart chaque matin.";

        return new FaitDeTiroir(
            "ciel.derive",
            FamilleDeFait.Ciel,
            sallonge ? "Le jour s'allonge" : "Le jour raccourcit",
            $"{minutes} min par jour",
            texte,
            new ScoreDeFait(Rarete.Quotidien, 1, 1));
    }

    private static FaitDeTiroir? PhaseDeLune(CielDuJour ciel)
    {
        // Vingt-cinq fois par an : la pleine et la nouvelle lune, rien d'autre. Un
        // croissant ordinaire n'est pas une nouvelle.
        var nom = ciel.Lune.Nom switch
        {
            NomPhaseLunaire.PleineLune => "Pleine lune",
            NomPhaseLunaire.NouvelleLune => "Nouvelle lune",
            _ => null,
        };
        if (nom is null)
        {
            return null;
        }

        return new FaitDeTiroir(
            "ciel.lune",
            FamilleDeFait.Ciel,
            "La lune",
            nom,
            ciel.Lune.Nom == NomPhaseLunaire.PleineLune
                ? "Elle se lève au coucher du soleil et brille jusqu'au matin."
                : "Le ciel sera au plus noir cette nuit.",
            new ScoreDeFait(Rarete.ParAn(25), 1, 1));
    }

    private static FaitDeTiroir? ProchaineSaison(CielDuJour ciel, ContexteDuJour contexte)
    {
        var evenement = ciel.ProchainEvenementSaisonnier;
        // Le fuseau du foyer, jamais celui de la machine : l'équinoxe de septembre 2026
        // tombe le 23 à Greenwich et le 22 au soir au Québec, et un conteneur en UTC
        // afficherait la mauvaise date toute la journée.
        var chezNous = TimeZoneInfo.ConvertTime(evenement.Instant, contexte.Fuseau);
        var jourLocal = DateOnly.FromDateTime(chezNous.DateTime);
        var jours = jourLocal.DayNumber - contexte.Date.DayNumber;
        var nom = Nommer(evenement.Saison);

        if (jours == 0)
        {
            return new FaitDeTiroir(
                "ciel.saison",
                FamilleDeFait.Ciel,
                "Aujourd'hui",
                nom,
                $"À {Heure(TimeOnly.FromDateTime(chezNous.DateTime))}, heure d'ici.",
                new ScoreDeFait(Rarete.ParAn(4), 1, 2));
        }
        if (jours < 0 || jours > JoursDApprocheSaison)
        {
            return null;
        }

        // L'étiquette porte le sujet et la valeur porte le chiffre — comme l'encadré
        // du compte à rebours. Une valeur de quarante caractères n'est pas une valeur
        // courte : elle se fait couper dans une colonne de widget.
        return new FaitDeTiroir(
            "ciel.saison-approche",
            FamilleDeFait.Ciel,
            nom,
            $"Dans {jours} jour{(jours > 1 ? "s" : "")}",
            $"Le {DateLongue(jourLocal)}, à {Heure(TimeOnly.FromDateTime(chezNous.DateTime))}.",
            new ScoreDeFait(Rarete.ParAn(4 * JoursDApprocheSaison), 1, 1));
    }

    private static FaitDeTiroir? Equilibre(CielDuJour ciel, ContexteDuJour contexte)
    {
        if (ciel.Soleil.Duree is not { } aujourdhui)
        {
            return null;
        }
        var demain = Soleil.Jour(
            contexte.Date.AddDays(1),
            contexte.Lieu,
            contexte.Fuseau.GetUtcOffset(DateTime.SpecifyKind(
                contexte.Date.AddDays(1).ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified)));
        if (demain.Duree is not { } lendemain)
        {
            return null;
        }

        // Le jour où la durée du jour franchit douze heures. Ce n'est pas l'équinoxe :
        // la réfraction et le diamètre du disque donnent quelques minutes de jour en
        // plus, si bien que la bascule arrive quelques jours après l'équinoxe de
        // septembre et quelques jours avant celui de mars.
        var douzeHeures = TimeSpan.FromHours(12);
        if (aujourdhui > douzeHeures && lendemain <= douzeHeures)
        {
            return new FaitDeTiroir(
                "ciel.equilibre",
                FamilleDeFait.Ciel,
                "Ce soir",
                "La nuit passe devant le jour",
                "À partir de demain, la nuit dure plus longtemps que le jour.",
                new ScoreDeFait(Rarete.ParAn(2), 1, 1.5));
        }
        if (aujourdhui <= douzeHeures && lendemain > douzeHeures)
        {
            return new FaitDeTiroir(
                "ciel.equilibre",
                FamilleDeFait.Ciel,
                "Ce soir",
                "Le jour repasse devant la nuit",
                "À partir de demain, le jour dure plus longtemps que la nuit.",
                new ScoreDeFait(Rarete.ParAn(2), 1, 1.5));
        }
        return null;
    }

    private static FaitDeTiroir? BasculeHoraire(CielDuJour ciel, DateOnly aujourdhui)
    {
        if (ciel.ProchainChangementDHeure is not { } changement)
        {
            return null;
        }
        var jours = changement.Date.DayNumber - aujourdhui.DayNumber;
        if (jours > JoursDApprocheChangementHeure)
        {
            return null;
        }

        // La bascule a lieu au petit matin : la veille, c'est « cette nuit » ; le jour
        // même, c'est déjà fait, et le dire au futur serait faux.
        var sens = changement.Avance ? "avancé" : "reculé";
        var valeur = jours switch
        {
            0 => "C'était cette nuit",
            1 => "C'est cette nuit",
            _ => $"Dans {jours} jours",
        };
        var texte = jours == 0
            ? $"On a {sens} d'une heure cette nuit."
            : $"Le {DateLongue(changement.Date)}, on aura {sens} d'une heure.";

        return new FaitDeTiroir(
            "ciel.changement-heure",
            FamilleDeFait.Ciel,
            "Le changement d'heure",
            valeur,
            texte,
            new ScoreDeFait(Rarete.ParAn(2 * JoursDApprocheChangementHeure), 1, jours <= 2 ? 2 : 1));
    }

    private static FaitDeTiroir? Noirceur(CielDuJour ciel, ContexteDuJour contexte)
    {
        // « Il fera noir à 18 h 25 » est de la décoration un mardi ordinaire et une
        // consigne le jour où il y a du travail dehors. Sans tâche extérieure ouverte,
        // le fait ne sort pas du tout.
        if (contexte.TachesDehors == false || ciel.Soleil.Coucher is not { } coucher)
        {
            return null;
        }

        // L'étiquette n'est pas « Dehors » : le verdict météo du journal porte déjà ce
        // titre, et les deux sortent le même jour par construction — le fait ne se
        // déclenche que s'il y a du travail dehors, ce qui est exactement le jour où un
        // « bonne journée pour tondre » a le plus de chances de tomber.
        return new FaitDeTiroir(
            "ciel.noirceur",
            FamilleDeFait.Ciel,
            "La noirceur",
            $"À {Heure(coucher)}",
            "Il y a du travail dehors aujourd'hui.",
            new ScoreDeFait(Rarete.Quotidien, 1, 2));
    }

    private static string Nommer(Saison saison) => saison switch
    {
        // Le nom du mois, pas celui de la saison : « équinoxe de printemps » est faux
        // dans l'hémisphère sud, et le dépôt est public (vault : Distribution).
        Saison.EquinoxeDeMars => "Équinoxe de mars",
        Saison.SolsticeDeJuin => "Solstice de juin",
        Saison.EquinoxeDeSeptembre => "Équinoxe de septembre",
        _ => "Solstice de décembre",
    };

    /// <summary>« 6 h 30 » — l'heure à la québécoise (OQLF), comme dans le reste de l'app.</summary>
    private static string Heure(TimeOnly heure) => $"{heure.Hour} h {heure.Minute:00}";

    private static string Duree(TimeSpan duree) => $"{(int)duree.TotalHours} h {duree.Minutes:00}";

    /// <summary>« 1er novembre », « 22 septembre » — le premier du mois est ordinal.</summary>
    private static string DateLongue(DateOnly date) =>
        date.Day == 1
            ? $"1er {date.ToString("MMMM", Francais)}"
            : date.ToString("d MMMM", Francais);

    private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-CA");
}
