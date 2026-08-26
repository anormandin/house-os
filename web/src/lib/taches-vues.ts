// Logique pure des deux vues de la page Tâches (D-2026-08-26 Page Tâches Rythmes
// Et Année) : groupement par rythme pour la vue Liste, géométrie 12 mois pour la
// vue Année. Même patron que ruban.ts — tout est testable sans DOM.

import type { Recurrence, TacheResume } from '@/lib/api'

const JOURS_COURTS = ['dim', 'lun', 'mar', 'mer', 'jeu', 'ven', 'sam']
const MOIS_COURTS = [
  'janv',
  'févr',
  'mars',
  'avr',
  'mai',
  'juin',
  'juil',
  'août',
  'sept',
  'oct',
  'nov',
  'déc',
]

function jourOrdinal(jour: number): string {
  return jour === 1 ? '1ᵉʳ' : String(jour)
}

export type ClasseChip = 'hebdo' | 'intervalle' | 'mois' | 'annuelle' | 'saison'

export type ChipRecurrence = { libelle: string; classe: ClasseChip }

/** Les chips « ↻ hebdo · dim », « ↻ aux 4 jours », « ☀ mai→oct »… d'une récurrence. */
export function chipsRecurrence(rec: Recurrence): ChipRecurrence[] {
  const chips: ChipRecurrence[] = []
  if (rec.mode === 'Fixe' && rec.fixeType === 'JoursSemaine') {
    const jours = (rec.joursSemaine ?? []).map((j) => JOURS_COURTS[j]).join(' · ')
    chips.push({ libelle: `↻ hebdo · ${jours}`, classe: 'hebdo' })
  }
  if (rec.mode === 'Fixe' && rec.fixeType === 'JourDuMois' && rec.jourDuMois != null) {
    chips.push({ libelle: `↻ ${jourOrdinal(rec.jourDuMois)} du mois`, classe: 'mois' })
  }
  if (rec.mode === 'Fixe' && rec.fixeType === 'Annuelle' && rec.moisAnnuel != null) {
    chips.push({
      libelle: `↻ annuelle · ${jourOrdinal(rec.jourAnnuel ?? 1)} ${MOIS_COURTS[rec.moisAnnuel - 1]}`,
      classe: 'annuelle',
    })
  }
  if (rec.mode === 'Intervalle' && rec.intervalleJours != null) {
    chips.push({
      libelle: rec.intervalleJours === 1 ? '↻ chaque jour' : `↻ aux ${rec.intervalleJours} jours`,
      classe: 'intervalle',
    })
  }
  if (aFenetre(rec)) {
    chips.push({
      libelle: `☀ ${MOIS_COURTS[rec.fenetreDebutMois! - 1]}→${MOIS_COURTS[rec.fenetreFinMois! - 1]}`,
      classe: 'saison',
    })
  }
  return chips
}

function aFenetre(rec: Recurrence): boolean {
  return rec.fenetreDebutMois != null && rec.fenetreFinMois != null
}

// ————— Vue Liste : groupes par rythme —————

export type CleGroupe = 'semaine' | 'intervalle' | 'mois' | 'annuel' | 'ponctuelles'

export type GroupeRythme = {
  cle: CleGroupe
  titre: string
  taches: TacheResume[]
  enRetard: number
}

export type VueListe = {
  groupes: GroupeRythme[]
  /** Progression des ponctuelles, faites incluses (la liste ne montre que les à-faire). */
  progression: { faites: number; total: number }
}

const TITRES_GROUPES: Record<CleGroupe, string> = {
  semaine: 'Chaque semaine',
  intervalle: 'Aux quelques jours',
  mois: 'Chaque mois',
  annuel: 'Chaque année & au fil des saisons',
  ponctuelles: 'Ponctuelles',
}

function cleGroupe(rec: Recurrence): CleGroupe {
  if (rec.mode === 'Ponctuelle') {
    return 'ponctuelles'
  }
  if (rec.mode === 'Intervalle') {
    return 'intervalle'
  }
  if (rec.fixeType === 'JoursSemaine') {
    return 'semaine'
  }
  if (rec.fixeType === 'JourDuMois') {
    return 'mois'
  }
  return 'annuel'
}

/** Groupe les définitions par rythme ; les groupes vides sont omis. */
export function grouperParRythme(taches: TacheResume[], aujourdhui: string): VueListe {
  const ponctuelles = taches.filter((t) => t.recurrence.mode === 'Ponctuelle')
  const groupes = (['semaine', 'intervalle', 'mois', 'annuel', 'ponctuelles'] as const)
    .map((cle) => {
      const membres = taches.filter((t) => cleGroupe(t.recurrence) === cle && t.completee === false)
      return {
        cle,
        titre: TITRES_GROUPES[cle],
        taches: membres,
        enRetard: membres.filter((t) => t.echeance !== null && t.echeance < aujourdhui).length,
      }
    })
    .filter((g) => g.taches.length > 0)
  return {
    groupes,
    progression: {
      faites: ponctuelles.filter((t) => t.completee).length,
      total: ponctuelles.length,
    },
  }
}

/** Ton d'affichage d'une échéance : retard (rouge), proche (≤ 3 jours, doré), normal. */
export function tonEcheance(
  echeance: string | null,
  aujourdhui: string,
): 'retard' | 'proche' | 'normal' | 'aucune' {
  if (echeance === null) {
    return 'aucune'
  }
  if (echeance < aujourdhui) {
    return 'retard'
  }
  const [a, m, j] = aujourdhui.split('-').map(Number)
  const limite = new Date(a, m - 1, j + 3)
  const [ae, me, je] = echeance.split('-').map(Number)
  return new Date(ae, me - 1, je) <= limite ? 'proche' : 'normal'
}

// ————— Vue Année : géométrie 12 mois —————

/** Position 0–100 d'une date locale dans son année (indépendante de l'année bissextile). */
export function pctDansAnnee(dateIso: string): number {
  const [annee, mois, jour] = dateIso.split('-').map(Number)
  const debut = new Date(annee, 0, 1)
  const jours =
    (new Date(annee, mois - 1, jour).getTime() - debut.getTime()) / 86_400_000
  const total = (new Date(annee + 1, 0, 1).getTime() - debut.getTime()) / 86_400_000
  return (jours / total) * 100
}

function pctMoisJour(annee: number, mois: number, jour: number): number {
  const clampe = Math.min(jour, new Date(annee, mois, 0).getDate())
  return pctDansAnnee(
    `${annee}-${String(mois).padStart(2, '0')}-${String(clampe).padStart(2, '0')}`,
  )
}

export type LigneAnnee =
  | { type: 'mensuelle'; tache: TacheResume; points: number[] }
  | {
      type: 'fenetre'
      tache: TacheResume
      segments: { debut: number; fin: number }[]
      enCours: boolean
      libelle: string
    }
  | { type: 'annuelle'; tache: TacheResume; position: number; libelle: string }

export type GrappePonctuelle = { date: string; position: number; taches: TacheResume[] }

export type VueAnnee = {
  lignes: LigneAnnee[]
  ponctuelles: GrappePonctuelle[]
  /** Trop fréquentes pour l'échelle de l'année : hebdos et intervalles sans fenêtre. */
  tempoCourt: TacheResume[]
  aujourdhuiPct: number
}

function dansFenetre(rec: Recurrence, mois: number, jour: number): boolean {
  const debut = rec.fenetreDebutMois! * 100 + (rec.fenetreDebutJour ?? 1)
  const fin = rec.fenetreFinMois! * 100 + (rec.fenetreFinJour ?? 31)
  const date = mois * 100 + jour
  // Une fenêtre peut chevaucher l'an (nov→mars) : l'intervalle est alors inversé.
  return debut <= fin ? date >= debut && date <= fin : date >= debut || date <= fin
}

function segmentsFenetre(rec: Recurrence, annee: number): { debut: number; fin: number }[] {
  const debut = pctMoisJour(annee, rec.fenetreDebutMois!, rec.fenetreDebutJour ?? 1)
  const fin = pctMoisJour(annee, rec.fenetreFinMois!, rec.fenetreFinJour ?? 31)
  return debut <= fin
    ? [{ debut, fin }]
    : [
        { debut: 0, fin },
        { debut, fin: 100 },
      ]
}

/** Construit la chronologie 12 mois de l'année d'aujourd'hui. */
export function construireAnnee(taches: TacheResume[], aujourdhui: string): VueAnnee {
  const [annee, moisAuj, jourAuj] = aujourdhui.split('-').map(Number)
  const lignes: LigneAnnee[] = []
  const tempoCourt: TacheResume[] = []
  const parDate = new Map<string, TacheResume[]>()

  for (const tache of taches) {
    const rec = tache.recurrence
    if (rec.mode === 'Ponctuelle') {
      if (tache.completee === false && tache.echeance?.startsWith(`${annee}-`)) {
        parDate.set(tache.echeance, [...(parDate.get(tache.echeance) ?? []), tache])
      }
      continue
    }
    if (aFenetre(rec)) {
      lignes.push({
        type: 'fenetre',
        tache,
        segments: segmentsFenetre(rec, annee),
        enCours: dansFenetre(rec, moisAuj, jourAuj),
        libelle:
          rec.mode === 'Intervalle'
            ? `↻ aux ${rec.intervalleJours} jours pendant la saison`
            : rec.fixeType === 'JoursSemaine'
              ? '↻ hebdo pendant la saison'
              : 'à faire dans la fenêtre',
      })
      continue
    }
    if (rec.fixeType === 'JourDuMois' && rec.jourDuMois != null) {
      lignes.push({
        type: 'mensuelle',
        tache,
        points: Array.from({ length: 12 }, (_, m) => pctMoisJour(annee, m + 1, rec.jourDuMois!)),
      })
      continue
    }
    if (rec.fixeType === 'Annuelle' && rec.moisAnnuel != null) {
      lignes.push({
        type: 'annuelle',
        tache,
        position: pctMoisJour(annee, rec.moisAnnuel, rec.jourAnnuel ?? 1),
        libelle: `${jourOrdinal(rec.jourAnnuel ?? 1)} ${MOIS_COURTS[rec.moisAnnuel - 1]}`,
      })
      continue
    }
    tempoCourt.push(tache)
  }

  // L'ordre de lecture de la maquette : mensuelles, fenêtres, annuelles.
  const rang = { mensuelle: 0, fenetre: 1, annuelle: 2 }
  lignes.sort((a, b) => rang[a.type] - rang[b.type])

  // À l'échelle de l'année, deux dates à quelques jours d'écart occupent le même
  // pixel : les grappes plus proches que la largeur d'un point fusionnent (le
  // chiffre reste honnête, l'infobulle liste tout).
  const grappes: GrappePonctuelle[] = []
  for (const [date, membres] of [...parDate.entries()].sort(([a], [b]) => (a < b ? -1 : 1))) {
    const position = pctDansAnnee(date)
    const derniere = grappes.at(-1)
    if (derniere !== undefined && position - derniere.position < 1.5) {
      derniere.taches.push(...membres)
    } else {
      grappes.push({ date, position, taches: membres })
    }
  }

  return {
    lignes,
    ponctuelles: grappes,
    tempoCourt,
    aujourdhuiPct: pctDansAnnee(aujourdhui),
  }
}
