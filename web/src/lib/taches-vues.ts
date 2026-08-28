// Logique pure des deux vues de la page Tâches (D-2026-08-26 Page Tâches Rythmes
// Et Année) : groupement par rythme pour la vue Liste, géométrie 12 mois pour la
// vue Année. Même patron que ruban.ts — tout est testable sans DOM.

import type { Recurrence, TacheResume } from '@/lib/api'
import { dateCourte, jourCourt } from '@/lib/format'

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

function iso(annee: number, mois: number, jour: number): string {
  const clampe = Math.min(jour, new Date(annee, mois, 0).getDate())
  return `${annee}-${String(mois).padStart(2, '0')}-${String(clampe).padStart(2, '0')}`
}

function enDate(dateIso: string): Date {
  const [a, m, j] = dateIso.split('-').map(Number)
  return new Date(a, m - 1, j)
}

/** Écart en jours entre deux dates locales (a − b). */
export function diffJours(a: string, b: string): number {
  return Math.round((enDate(a).getTime() - enDate(b).getTime()) / 86_400_000)
}

function plusJours(dateIso: string, jours: number): string {
  const d = enDate(dateIso)
  d.setDate(d.getDate() + jours)
  return d.toLocaleDateString('fr-CA')
}

/** « mar 1ᵉʳ » (mensuelle) ou « jeu 15 oct » (avec le mois). */
function libellePoint(dateIso: string, avecMois: boolean): string {
  const [, mois, jour] = dateIso.split('-').map(Number)
  const base = `${jourCourt(dateIso)} ${jourOrdinal(jour)}`
  return avecMois ? `${base} ${MOIS_COURTS[mois - 1]}` : base
}

function dansFenetre(rec: Recurrence, mois: number, jour: number): boolean {
  const debut = rec.fenetreDebutMois! * 100 + (rec.fenetreDebutJour ?? 1)
  const fin = rec.fenetreFinMois! * 100 + (rec.fenetreFinJour ?? 31)
  const date = mois * 100 + jour
  // Une fenêtre peut chevaucher l'an (nov→mars) : l'intervalle est alors inversé.
  return debut <= fin ? date >= debut && date <= fin : date >= debut || date <= fin
}

export type PointRuban = { offset: number; libelle: string; passe: boolean }

export type LigneRuban =
  | { type: 'mensuelle'; tache: TacheResume; points: PointRuban[] }
  | {
      type: 'fenetre'
      tache: TacheResume
      segments: { debut: number; fin: number }[]
      enCours: boolean
      libelle: string
      vise: PointRuban | null
    }
  | { type: 'annuelle'; tache: TacheResume; point: PointRuban | null; note: string | null }
  | { type: 'intervalle'; tache: TacheResume; point: PointRuban | null; note: string | null }

export type GrappeJour = {
  date: string
  offset: number
  libelle: string
  /** 1 = décalée vers le bas quand la voisine de gauche est trop proche. */
  voie: 0 | 1
  taches: TacheResume[]
}

export type SemainePonctuelles = { lundi: string; libelle: string; taches: TacheResume[] }

export type Ruban = {
  /** 1ᵉʳ du mois courant — le ruban ne remonte pas dans le passé. */
  debutDomaine: string
  /** Longueur du domaine en jours (jusqu'au 31 décembre inclus). */
  jours: number
  moisDomaine: { libelle: string; offset: number }[]
  aujourdhuiOffset: number
  lignes: LigneRuban[]
  grappes: GrappeJour[]
  /** Ponctuelles échues avant aujourd'hui (bloc « En retard » de la bande). */
  enRetard: TacheResume[]
  /** Semaines (lundi) à venir, semaine courante incluse — l'an prochain aussi. */
  semaines: SemainePonctuelles[]
  /** « prochaine · … → » quand des ponctuelles échoient après le 31 décembre. */
  noteAnProchain: string | null
  totalPonctuelles: number
  /** Cadences ≤ 15 jours sans fenêtre : illisibles même à l'échelle du ruban. */
  tempoCourt: TacheResume[]
}

const MOIS_TITRES = [
  'Janv',
  'Févr',
  'Mars',
  'Avr',
  'Mai',
  'Juin',
  'Juil',
  'Août',
  'Sept',
  'Oct',
  'Nov',
  'Déc',
]

/** Lundi de la semaine locale d'une date. */
export function lundiDe(dateIso: string): string {
  const d = enDate(dateIso)
  d.setDate(d.getDate() - ((d.getDay() + 6) % 7))
  return d.toLocaleDateString('fr-CA')
}

/**
 * Le ruban défilant de la vue Année (D-2026-08-26 Vue Année Défilante) : domaine
 * du 1ᵉʳ du mois courant au 31 décembre, positions en jours — la page multiplie
 * par son échelle en px/jour.
 */
export function construireRuban(taches: TacheResume[], aujourdhui: string): Ruban {
  const [annee, moisAuj, jourAuj] = aujourdhui.split('-').map(Number)
  const debutDomaine = iso(annee, moisAuj, 1)
  const finDomaine = iso(annee, 12, 31)
  const jours = diffJours(finDomaine, debutDomaine) + 1
  const offsetDe = (dateIso: string) => diffJours(dateIso, debutDomaine)
  const dansDomaine = (dateIso: string) => dateIso >= debutDomaine && dateIso <= finDomaine

  const lignes: LigneRuban[] = []
  const tempoCourt: TacheResume[] = []
  const parDate = new Map<string, TacheResume[]>()
  const enRetard: TacheResume[] = []
  const apresDomaine: TacheResume[] = []

  for (const tache of taches) {
    const rec = tache.recurrence
    if (rec.mode === 'Ponctuelle') {
      if (tache.completee || tache.echeance === null) {
        continue
      }
      if (tache.echeance < aujourdhui) {
        enRetard.push(tache)
      }
      if (dansDomaine(tache.echeance)) {
        parDate.set(tache.echeance, [...(parDate.get(tache.echeance) ?? []), tache])
      } else if (tache.echeance > finDomaine) {
        // Échue après le 31 décembre : hors du ruban mais pas invisible — note
        // « prochaine → » sur la rangée et bloc dans la bande, comme les annuelles.
        apresDomaine.push(tache)
      }
      continue
    }
    if (aFenetre(rec)) {
      const debut = iso(annee, rec.fenetreDebutMois!, rec.fenetreDebutJour ?? 1)
      const fin = iso(annee, rec.fenetreFinMois!, rec.fenetreFinJour ?? 31)
      // Fenêtre chevauchant l'an (nov→mars) : deux morceaux dans l'année civile.
      const morceaux =
        debut <= fin ? [[debut, fin]] : [[iso(annee, 1, 1), fin], [debut, finDomaine]]
      const segments = morceaux
        .map(([d, f]) => ({ debut: Math.max(0, offsetDe(d)), fin: Math.min(jours, offsetDe(f) + 1) }))
        .filter((s) => s.fin > s.debut)
      lignes.push({
        type: 'fenetre',
        tache,
        segments,
        enCours: dansFenetre(rec, moisAuj, jourAuj),
        libelle: `☀ fenêtre du ${jourOrdinal(rec.fenetreDebutJour ?? 1)} ${MOIS_COURTS[rec.fenetreDebutMois! - 1]} au ${jourOrdinal(rec.fenetreFinJour ?? 31)} ${MOIS_COURTS[rec.fenetreFinMois! - 1]}`,
        vise:
          tache.echeance !== null && dansDomaine(tache.echeance)
            ? {
                offset: offsetDe(tache.echeance),
                libelle: `visé · ${libellePoint(tache.echeance, true)}`,
                passe: tache.echeance < aujourdhui,
              }
            : null,
      })
      continue
    }
    if (rec.fixeType === 'JourDuMois' && rec.jourDuMois != null) {
      const points: PointRuban[] = []
      for (let m = moisAuj; m <= 12; m += 1) {
        const date = iso(annee, m, rec.jourDuMois)
        const passe = date < aujourdhui
        // Un passage n'est « fait » que si l'échéance en attente est plus tard ;
        // sinon l'occurrence traîne encore et le point est en retard.
        const fait = tache.echeance === null || tache.echeance > date
        points.push({
          offset: offsetDe(date),
          libelle: passe ? (fait ? 'fait' : 'retard') : libellePoint(date, false),
          passe,
        })
      }
      lignes.push({ type: 'mensuelle', tache, points })
      continue
    }
    if (rec.fixeType === 'Annuelle' && rec.moisAnnuel != null) {
      // L'échéance réelle (glissée par rollover/fenêtre) prime sur la date théorique.
      const date =
        tache.echeance !== null && dansDomaine(tache.echeance)
          ? tache.echeance
          : iso(annee, rec.moisAnnuel, rec.jourAnnuel ?? 1)
      lignes.push({
        type: 'annuelle',
        tache,
        point: dansDomaine(date)
          ? { offset: offsetDe(date), libelle: libellePoint(date, true), passe: date < aujourdhui }
          : null,
        note:
          dansDomaine(date) || tache.echeance === null
            ? null
            : `prochaine · ${libellePoint(tache.echeance, true)} ${tache.echeance.slice(0, 4)} →`,
      })
      continue
    }
    if (rec.mode === 'Intervalle' && (rec.intervalleJours ?? 0) > 15) {
      const echeance = tache.echeance
      let point: PointRuban | null = null
      let note: string | null = null
      if (echeance !== null && echeance <= finDomaine) {
        const date = echeance < debutDomaine ? debutDomaine : echeance
        point = {
          offset: offsetDe(date),
          libelle:
            echeance < aujourdhui ? `retard · ${libellePoint(echeance, true)}` : libellePoint(echeance, true),
          passe: echeance < aujourdhui,
        }
        const suivante = plusJours(echeance, rec.intervalleJours!)
        if (suivante > finDomaine) {
          note = `ensuite ≈ ${libellePoint(suivante, true)} ${suivante.slice(0, 4)} →`
        }
      } else if (echeance !== null) {
        note = `prochaine · ${libellePoint(echeance, true)} ${echeance.slice(0, 4)} →`
      }
      lignes.push({ type: 'intervalle', tache, point, note })
      continue
    }
    tempoCourt.push(tache)
  }

  // L'ordre de lecture de la maquette : mensuelles, fenêtres, annuelles, intervalles.
  const rang = { mensuelle: 0, fenetre: 1, annuelle: 2, intervalle: 3 }
  lignes.sort((a, b) => rang[a.type] - rang[b.type])

  // Grappes par jour ; une voisine à moins de ~2,5 jours descend sur la voie basse
  // pour que chiffre et date restent lisibles.
  const grappes: GrappeJour[] = []
  let moisPrecedent = 0
  for (const [date, membres] of [...parDate.entries()].sort(([a], [b]) => (a < b ? -1 : 1))) {
    const offset = offsetDe(date)
    const [, mois, jour] = date.split('-').map(Number)
    const precedente = grappes.at(-1)
    grappes.push({
      date,
      offset,
      libelle:
        mois === moisPrecedent ? String(jour) : `${jour} ${MOIS_COURTS[mois - 1]}`,
      voie: precedente !== undefined && offset - precedente.offset < 2.5 && precedente.voie === 0 ? 1 : 0,
      taches: membres,
    })
    moisPrecedent = mois
  }

  // Bande semaine-par-semaine : les à-venir groupées par lundi local.
  const parLundi = new Map<string, TacheResume[]>()
  for (const [date, membres] of parDate) {
    if (date < aujourdhui) {
      continue
    }
    const lundi = lundiDe(date)
    parLundi.set(lundi, [...(parLundi.get(lundi) ?? []), ...membres])
  }
  apresDomaine.sort((a, b) => (a.echeance! < b.echeance! ? -1 : 1))
  for (const tache of apresDomaine) {
    const lundi = lundiDe(tache.echeance!)
    parLundi.set(lundi, [...(parLundi.get(lundi) ?? []), tache])
  }
  const semaines = [...parLundi.entries()]
    .sort(([a], [b]) => (a < b ? -1 : 1))
    .map(([lundi, membres]) => ({
      lundi,
      libelle: `Semaine du ${dateCourte(lundi)}`,
      taches: membres.sort((a, b) => (a.echeance! < b.echeance! ? -1 : 1)),
    }))

  return {
    debutDomaine,
    jours,
    moisDomaine: Array.from({ length: 13 - moisAuj }, (_, i) => ({
      libelle: MOIS_TITRES[moisAuj - 1 + i],
      offset: offsetDe(iso(annee, moisAuj + i, 1)),
    })),
    aujourdhuiOffset: offsetDe(aujourdhui),
    lignes,
    grappes,
    enRetard,
    semaines,
    noteAnProchain:
      apresDomaine.length === 0
        ? null
        : `prochaine · ${libellePoint(apresDomaine[0].echeance!, true)} ${apresDomaine[0].echeance!.slice(0, 4)} →`,
    totalPonctuelles: enRetard.length + semaines.reduce((n, s) => n + s.taches.length, 0),
    tempoCourt,
  }
}
