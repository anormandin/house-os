import type { EnveloppeBudget, ResumeBudget, SortiePrevue, TypeEnveloppe } from '@/lib/api'

// Logique pure de la page Budget (patron taches-vues) : palette des segments,
// partition du compte, géométrie du diagramme « Flux tracé » — voir
// vault/Decisions/D-2026-08-26 Page Budget Flux Raffiné Et Bascule Flux Tracé.md.

/** Pâles dérivés des couleurs de chips (recette des fonds d'avatars) ; la seconde
 * teinte sert à distinguer deux enveloppes du même type dans une barre empilée. */
export const PALES: Record<TypeEnveloppe, [string, string]> = {
  Taxes: ['#ded4e8', '#ebe5f2'],
  Equipement: ['#cfe0d8', '#e0ebe4'],
  Projet: ['#cdd9ec', '#dfe8f4'],
  Reserve: ['#e8e0bd', '#f0e9cf'],
}

/** Couleur stable par enveloppe : alternance des deux teintes du type. */
export function couleursEnveloppes(enveloppes: EnveloppeBudget[]): Map<string, string> {
  const compteurs = new Map<TypeEnveloppe, number>()
  const couleurs = new Map<string, string>()
  for (const enveloppe of enveloppes) {
    const rang = compteurs.get(enveloppe.type) ?? 0
    compteurs.set(enveloppe.type, rang + 1)
    couleurs.set(enveloppe.id, PALES[enveloppe.type][rang % 2])
  }
  return couleurs
}

export type Segment = {
  id: string | null
  nom: string
  montant: number
  pct: number
  couleur: string | null // null = non affecté (motif hachuré)
}

export type Partition = {
  segments: Segment[]
  surAllocation: boolean
  /** Sur-allocation : position du marqueur « solde réel » en % de la barre. */
  marqueurSoldePct: number | null
  /** Sur-allocation : largeur du dépassement hachuré rouge en % de la barre. */
  depassementPct: number | null
}

/**
 * La barre empilée du compte. Cas normal : base = solde du compte, segments =
 * enveloppes à solde positif + non affecté. Sur-allocation (non affecté négatif) :
 * la barre représente Σ enveloppes, un marqueur situe le solde réel et la zone
 * au-delà est le dépassement (le trou est visible dans la barre elle-même).
 */
export function construirePartition(resume: ResumeBudget): Partition {
  const couleurs = couleursEnveloppes(resume.enveloppes)
  const positives = resume.enveloppes.filter((e) => e.solde > 0)
  const totalPositives = positives.reduce((somme, e) => somme + e.solde, 0)
  const surAllocation = resume.nonAffecte < 0

  const base = surAllocation ? totalPositives : totalPositives + Math.max(0, resume.nonAffecte)
  if (base <= 0) {
    return { segments: [], surAllocation, marqueurSoldePct: null, depassementPct: null }
  }

  const segments: Segment[] = positives.map((e) => ({
    id: e.id,
    nom: e.nom,
    montant: e.solde,
    pct: (e.solde / base) * 100,
    couleur: couleurs.get(e.id) ?? null,
  }))
  if (surAllocation === false && resume.nonAffecte > 0) {
    segments.push({
      id: null,
      nom: 'Non affecté',
      montant: resume.nonAffecte,
      pct: (resume.nonAffecte / base) * 100,
      couleur: null,
    })
  }
  return {
    segments,
    surAllocation,
    marqueurSoldePct: surAllocation ? (resume.soldeCourant / base) * 100 : null,
    depassementPct: surAllocation ? (-resume.nonAffecte / base) * 100 : null,
  }
}

// ——— Flux tracé (vue E2) — colonnes fixes, desktop seulement ———

export const FLUX_LARGEUR = 1148
const X_SOURCE_FIN = 190
const X_NOEUD = 430
const X_NOEUD_FIN = 640
const X_SORTIE = 880
const Y_DEPART = 40
const ECART = 14
const HAUTEUR_NOEUD_MIN = 46
const EPAISSEUR_ENTREES = 150
const EPAISSEUR_SORTIES = 110

export type NoeudFlux = {
  enveloppe: EnveloppeBudget
  y: number
  h: number
  couleur: string
  sansFlux: boolean
}

export type RubanFlux = { path: string; couleur: string }
export type EtiquetteFlux = { x: number; y: number; texte: string }

export type SortieFlux = {
  y: number
  h: number
  date: string
  nom: string
  montant: number
  /** Total des sorties de l'enveloppe sur les 12 prochains mois. */
  annuel: number
  detail: string | null
}

export type LointainFlux = { path: string; y: number; sortie: SortiePrevue }

export type Flux = {
  hauteur: number
  source: { y: number; h: number }
  noeuds: NoeudFlux[]
  rubansEntree: RubanFlux[]
  etiquettes: EtiquetteFlux[]
  sorties: SortieFlux[]
  rubansSortie: RubanFlux[]
  lointains: LointainFlux[]
}

/** Ruban plein entre deux tranches verticales (béziers symétriques). */
function ruban(x0: number, haut0: number, bas0: number, x1: number, haut1: number, bas1: number): string {
  const xm = (x0 + x1) / 2
  return (
    `M ${x0},${haut0} C ${xm},${haut0} ${xm},${haut1} ${x1},${haut1} ` +
    `L ${x1},${bas1} C ${xm},${bas1} ${xm},${bas0} ${x0},${bas0} Z`
  )
}

/** Une date tombe-t-elle dans les 12 prochains mois (borne incluse)? */
export function dansDouzeMois(date: string, aujourdhuiIso: string): boolean {
  const [annee, mois, jour] = aujourdhuiIso.split('-').map(Number)
  const borne = `${annee + 1}-${String(mois).padStart(2, '0')}-${String(jour).padStart(2, '0')}`
  return date <= borne
}

/** Géométrie complète du diagramme : nœuds empilés, rubans d'entrée proportionnels
 * aux provisions, sorties des 12 prochains mois à droite, lointaines en pointillé. */
export function construireFlux(resume: ResumeBudget, aujourdhuiIso: string): Flux {
  const couleurs = couleursEnveloppes(resume.enveloppes)
  const actives = resume.enveloppes.filter((e) => e.statut === 'Active')
  const ordonnees = [...actives].sort((a, b) => b.provision - a.provision)
  const totalProvision = resume.virementSuggere

  // Sorties des 12 prochains mois groupées par enveloppe (la plus proche d'abord).
  const sortiesProches = new Map<string, SortiePrevue[]>()
  const lointainesParEnveloppe = new Map<string, SortiePrevue>()
  for (const sortie of resume.sorties) {
    if (dansDouzeMois(sortie.date, aujourdhuiIso)) {
      sortiesProches.set(sortie.enveloppeId, [
        ...(sortiesProches.get(sortie.enveloppeId) ?? []),
        sortie,
      ])
    } else if (
      sortiesProches.has(sortie.enveloppeId) === false &&
      lointainesParEnveloppe.has(sortie.enveloppeId) === false
    ) {
      lointainesParEnveloppe.set(sortie.enveloppeId, sortie)
    }
  }

  // Nœuds empilés : épaisseur d'entrée ∝ provision.
  const noeuds: NoeudFlux[] = []
  const epaisseurs = new Map<string, number>()
  let y = Y_DEPART
  for (const enveloppe of ordonnees) {
    const epaisseur =
      totalProvision > 0 && enveloppe.provision > 0
        ? Math.max(6, (enveloppe.provision / totalProvision) * EPAISSEUR_ENTREES)
        : 0
    epaisseurs.set(enveloppe.id, epaisseur)
    const h = Math.max(HAUTEUR_NOEUD_MIN, epaisseur + 10)
    noeuds.push({
      enveloppe,
      y,
      h,
      couleur: couleurs.get(enveloppe.id) ?? PALES.Reserve[0],
      sansFlux: epaisseur === 0,
    })
    y += h + ECART
  }
  const hauteurPile = y - ECART + Y_DEPART

  // Rubans d'entrée : empilés à la sortie de la source, centrés à l'entrée du nœud.
  const totalEpaisseur = [...epaisseurs.values()].reduce((somme, e) => somme + e, 0)
  const sourceH = Math.max(120, totalEpaisseur + 24)
  const sourceY = Math.max(Y_DEPART, (hauteurPile - sourceH) / 2)
  const rubansEntree: RubanFlux[] = []
  const etiquettes: EtiquetteFlux[] = []
  let sourceCurseur = sourceY + (sourceH - totalEpaisseur) / 2
  for (const noeud of noeuds) {
    const epaisseur = epaisseurs.get(noeud.enveloppe.id) ?? 0
    if (epaisseur === 0) {
      continue
    }
    const noeudHaut = noeud.y + (noeud.h - epaisseur) / 2
    rubansEntree.push({
      path: ruban(
        X_SOURCE_FIN, sourceCurseur, sourceCurseur + epaisseur,
        X_NOEUD, noeudHaut, noeudHaut + epaisseur),
      couleur: noeud.couleur,
    })
    etiquettes.push({
      x: X_SOURCE_FIN + 34,
      y: (sourceCurseur + noeudHaut) / 2 + epaisseur / 2,
      texte: `+ ${Math.round(noeud.enveloppe.provision)} $`,
    })
    sourceCurseur += epaisseur
  }

  // Boîtes de sortie (12 prochains mois) et rubans vers elles.
  const sorties: SortieFlux[] = []
  const rubansSortie: RubanFlux[] = []
  const totalAnnuel = [...sortiesProches.values()]
    .flat()
    .reduce((somme, s) => somme + s.montant, 0)
  let ySortie = Y_DEPART
  for (const noeud of noeuds) {
    const proches = sortiesProches.get(noeud.enveloppe.id)
    if (proches === undefined) {
      continue
    }
    const annuel = proches.reduce((somme, s) => somme + s.montant, 0)
    const epaisseur =
      totalAnnuel > 0 ? Math.max(6, (annuel / totalAnnuel) * EPAISSEUR_SORTIES) : 6
    const h = Math.max(HAUTEUR_NOEUD_MIN + 12, epaisseur + 26)
    const prochaine = proches[0]
    sorties.push({
      y: ySortie,
      h,
      date: prochaine.date,
      nom: prochaine.nom,
      montant: prochaine.montant,
      annuel,
      detail:
        proches.length > 1 ? `${proches.length} versements sur 12 mois` : null,
    })
    const noeudHaut = noeud.y + (noeud.h - epaisseur) / 2
    const sortieHaut = ySortie + (h - epaisseur) / 2
    rubansSortie.push({
      path: ruban(
        X_NOEUD_FIN, noeudHaut, noeudHaut + epaisseur,
        X_SORTIE, sortieHaut, sortieHaut + epaisseur),
      couleur: noeud.couleur,
    })
    ySortie += h + ECART
  }

  // Sorties lointaines (> 12 mois) : trait pointillé + note.
  const lointains: LointainFlux[] = []
  for (const noeud of noeuds) {
    const lointaine = lointainesParEnveloppe.get(noeud.enveloppe.id)
    if (lointaine === undefined) {
      continue
    }
    const yNote = Math.max(noeud.y + noeud.h / 2, ySortie)
    lointains.push({
      path: `M ${X_NOEUD_FIN},${noeud.y + noeud.h / 2} C ${(X_NOEUD_FIN + X_SORTIE) / 2},${noeud.y + noeud.h / 2} ${(X_NOEUD_FIN + X_SORTIE) / 2},${yNote} ${X_SORTIE},${yNote}`,
      y: yNote,
      sortie: lointaine,
    })
    ySortie = yNote + 30
  }

  return {
    hauteur: Math.max(hauteurPile, ySortie, sourceY + sourceH + Y_DEPART),
    source: { y: sourceY, h: sourceH },
    noeuds,
    rubansEntree,
    etiquettes,
    sorties,
    rubansSortie,
    lointains,
  }
}

/** Largeur de jauge (%) bornée à [0, 100] — un solde négatif reste à 0. */
export function pctJauge(solde: number, cible: number | null): number | null {
  if (cible === null || cible <= 0) {
    return null
  }
  return Math.min(100, Math.max(0, (solde / cible) * 100))
}

// ——— Ventilation d'un dépôt (LierModal) : arithmétique au cent ———

/** Arrondi au cent — les sommes de flottants dérivent (0.1 + 0.2 ≠ 0.3). */
export function arrondirCents(montant: number): number {
  const cents = Math.round(montant * 100) / 100
  // Jamais −0 : il s'afficherait « −0,00 $ ».
  return cents === 0 ? 0 : cents
}

/** Pré-remplissage de la ventilation : les provisions suggérées (plus grosses
 * d'abord), écrêtées au montant du dépôt et arrondies à 2 décimales — jamais
 * « 108.33333333333334 » dans un champ. */
export function preRemplirVentilation(
  montant: number,
  enveloppes: { id: string; provision: number }[],
): Record<string, string> {
  let reste = montant
  const parts: Record<string, string> = {}
  for (const enveloppe of [...enveloppes].sort((a, b) => b.provision - a.provision)) {
    const part = arrondirCents(Math.min(enveloppe.provision, reste))
    parts[enveloppe.id] = part > 0 ? String(part) : ''
    reste = arrondirCents(reste - part)
  }
  return parts
}

/** Total saisi et reste en non affecté, arrondis au cent : un dépôt réparti au
 * complet donne reste = 0, jamais −1e-13 (donc pas de faux « dépasse le dépôt »). */
export function bilanVentilation(
  montant: number,
  parts: number[],
): { total: number; reste: number } {
  const total = arrondirCents(parts.reduce((somme, part) => somme + part, 0))
  return { total, reste: arrondirCents(montant - total) }
}
