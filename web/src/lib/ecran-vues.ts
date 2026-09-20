// Helpers purs de la vue e-ink (pages/Ecran.tsx). La page met en forme ; ce qui se
// calcule se calcule ici, testé.

import { dateLocaleIso, type FaitEcran, type LigneEcran } from '@/lib/api'
import { dodosAvant, jourCourt } from '@/lib/format'

/**
 * « AL » pour Alain, « AR » pour Ariane — la couleur par personne n'existe pas en
 * 1-bit, et une seule lettre ne distingue pas deux prénoms qui commencent pareil.
 */
export function initiales(nomAffichage: string | null): string {
  if (nomAffichage === null || nomAffichage.trim() === '') {
    return ''
  }
  return [...nomAffichage.trim()].slice(0, 2).join('').toLocaleUpperCase('fr-CA')
}

/**
 * Quand une date arrive, en mots de tous les jours : « aujourd'hui », « demain »,
 * le jour de la semaine cette semaine, sinon « dans N jours ».
 */
export function quandCeJour(dateIso: string, aujourdhui = dateLocaleIso()): string {
  const jours = joursEntre(aujourdhui, dateIso)
  if (jours <= 0) return "aujourd'hui"
  if (jours === 1) return 'demain'
  if (jours < 7) return jourCourt(dateIso)
  return `dans ${jours} jours`
}

/** « 33 dodos », « 1 dodo », « aujourd'hui » — le vocabulaire des comptes à rebours. */
export function libelleDodos(dateIso: string): string {
  const dodos = dodosAvant(dateIso)
  if (dodos <= 0) return "c'est aujourd'hui"
  return dodos === 1 ? '1 dodo' : `${dodos} dodos`
}

/** Taille du cadre : les dimensions de l'appareil, ou celles de l'E1003 en paysage. */
export function dimensionsEcran(recherche: URLSearchParams): { largeur: number; hauteur: number } {
  return {
    largeur: entierBorne(recherche.get('largeur'), 1872),
    hauteur: entierBorne(recherche.get('hauteur'), 1404),
  }
}

/** Le niveau de pile que l'appareil a annoncé, si le serveur nous l'a transmis. */
export function pileAnnoncee(recherche: URLSearchParams): number | null {
  const valeur = Number(recherche.get('pile'))
  if (recherche.get('pile') === null || Number.isNaN(valeur)) {
    return null
  }
  return Math.min(100, Math.max(0, Math.round(valeur)))
}

function entierBorne(valeur: string | null, defaut: number): number {
  const n = Number(valeur)
  if (valeur === null || !Number.isInteger(n) || n < 200 || n > 4000) {
    return defaut
  }
  return n
}

function joursEntre(deIso: string, aIso: string): number {
  return Math.round(
    (new Date(`${aIso}T00:00:00`).getTime() - new Date(`${deIso}T00:00:00`).getTime()) / 86_400_000,
  )
}

/* ────────────────────────────────────────────────────────────────────────────
 * Le journal : rang, plancher, répartition des colonnes.
 * Une seule mise en page, six remplissages (vault : D-2026-09-20 Une Seule Mise
 * En Page À Rangs). Le rang est une fonction, pas un second gabarit.
 * ──────────────────────────────────────────────────────────────────────────── */

/** À partir de ce retard, une tâche ne peut plus être reléguée sous un widget. */
export const JOURS_RETARD_PLANCHER = 3

export type Plancher = { raison: 'compte' | 'retard'; titre: string }

/**
 * Le plancher non négociable : un compte à rebours à zéro ou une tâche en retard de
 * plus de trois jours prennent la manchette quoi qu'il arrive. Sans lui, le jour où
 * la lune passe devant « remettre les clés », l'écran perd sa crédibilité.
 *
 * Les lignes arrivent triées par échéance (les retards d'abord), donc la plus en
 * retard survit toujours à l'élagage du serveur.
 */
export function plancher(
  lignes: LigneEcran[],
  prochainCompte: { titre: string; dateCible: string } | null,
  aujourdhui: string,
): Plancher | null {
  if (prochainCompte !== null && prochainCompte.dateCible <= aujourdhui) {
    return { raison: 'compte', titre: prochainCompte.titre }
  }
  const tresEnRetard = lignes.find((l) => !l.faite && l.joursDeRetard > JOURS_RETARD_PLANCHER)
  if (tresEnRetard !== undefined) {
    return { raison: 'retard', titre: tresEnRetard.titre }
  }
  return null
}

export type Rang = 'evenement' | 'chronique' | 'manchette' | 'resserre' | 'court' | 'sommaire'

export type Grille = {
  rang: Rang
  /** La manchette porte une lettrine. */
  lettrine: boolean
  /** Le chapeau (sous-titre de la manchette) est affiché. */
  chapeau: boolean
  /** Colonnes prises par la liste du jour, sur les trois du corps. */
  colonnesListe: 0 | 1 | 2 | 3
  /** Budget de widgets — les widgets rétrécissent avant de disparaître. */
  widgets: number
}

/**
 * Le rang du jour. Les nombres viennent des maquettes, mesurés et non estimés
 * (`design/maquettes/une-editorialiste.html`) : la contrainte n'est pas la place —
 * trois colonnes tiennent ~27 items — c'est la manchette, qui n'a plus de sens
 * passé six tâches.
 */
export function grilleDuJour(tachesDues: number, plancherDeclenche: boolean): Grille {
  const base = rangDeCharge(tachesDues)
  if (plancherDeclenche === false) {
    return base
  }
  // Le plancher prend la manchette et ne garde qu'un widget, celui qui sert. Mais il
  // ne fait pas disparaître la journée : la liste garde les colonnes que sa charge
  // lui donne (au moins une, sinon les tâches du jour n'auraient nulle part où aller).
  return {
    rang: 'evenement',
    lettrine: true,
    // Le chapeau suit la charge : le plancher impose la manchette, il ne rend pas
    // au journal la place que neuf tâches lui avaient déjà prise.
    chapeau: base.chapeau,
    colonnesListe: Math.max(1, base.colonnesListe) as 1 | 2 | 3,
    widgets: 1,
  }
}

function rangDeCharge(tachesDues: number): Grille {
  if (tachesDues === 0) {
    return { rang: 'chronique', lettrine: true, chapeau: true, colonnesListe: 0, widgets: 7 }
  }
  if (tachesDues <= 2) {
    return { rang: 'manchette', lettrine: true, chapeau: true, colonnesListe: 1, widgets: 5 }
  }
  if (tachesDues <= 5) {
    return { rang: 'resserre', lettrine: false, chapeau: true, colonnesListe: 1, widgets: 4 }
  }
  if (tachesDues <= 9) {
    return { rang: 'court', lettrine: false, chapeau: false, colonnesListe: 2, widgets: 3 }
  }
  return { rang: 'sommaire', lettrine: false, chapeau: false, colonnesListe: 3, widgets: 3 }
}

/**
 * La taille de la manchette tient compte de sa longueur, pas seulement du rang.
 * Tant que l'éditorialiste n'écrit pas (étape 7), la manchette peut être un titre
 * de tâche de soixante caractères : à 116 px il mangerait les deux tiers du mur.
 * Et une lettrine sur une phrase longue n'est plus une lettrine, c'est un accident.
 */
export function manchetteDuJour(titre: string, grille: Grille): { taille: number; lettrine: boolean } {
  const parRang = grille.rang === 'court' ? 68 : grille.rang === 'resserre' ? 88 : 116
  const parLongueur = titre.length > 80 ? 54 : titre.length > 55 ? 68 : titre.length > 30 ? 88 : 116
  const taille = Math.min(parRang, parLongueur)
  return { taille, lettrine: grille.lettrine && titre.length <= 55 }
}

export type Repartition = { liste: number; aparte: number; bandeDePied: boolean }

/**
 * Comment les trois colonnes du corps se partagent. Trois cas tordent la grille
 * théorique : quand il n'y a **rien** à mettre dans le corps, il n'y a pas de corps du
 * tout et la manchette prend la page ; sans widget à montrer, la liste prend tout
 * (jamais une colonne vide) ; et au sommaire, la liste prend les trois colonnes et les
 * widgets descendent en bande de pied.
 *
 * Le premier cas est celui d'une installation neuve — aucune tâche, aucune météo
 * relevée, aucun fait au fonds de tiroir. Sans lui, l'écran peignait une grille à trois
 * colonnes coiffée d'un « Aujourd'hui · 0 à faire » et de deux colonnes blanches
 * (défaut de l'étape 1, trouvé en revue à l'étape 2).
 */
export function repartitionColonnes(colonnesListe: number, widgetsDisponibles: number): Repartition {
  if (colonnesListe <= 0 && widgetsDisponibles === 0) {
    return { liste: 0, aparte: 0, bandeDePied: false }
  }
  if (widgetsDisponibles === 0) {
    return { liste: 3, aparte: 0, bandeDePied: false }
  }
  if (colonnesListe >= 3) {
    return { liste: 3, aparte: 0, bandeDePied: true }
  }
  // Une colonne par widget au plus : un fonds de tiroir maigre ne doit pas laisser
  // une colonne vide au milieu du journal.
  const aparte = Math.min(3 - colonnesListe, widgetsDisponibles)
  return { liste: colonnesListe, aparte, bandeDePied: false }
}

/**
 * La rangée serrée (une ligne, corps réduit) ne sert que lorsque la colonne en a
 * vraiment besoin : compresser une liste qui tient déjà, c'est tronquer des titres
 * pour de la place qu'on n'utilise pas.
 *
 * Le seuil est un nombre de rangées LARGES, et une rangée large fait deux lignes
 * dès que le titre dépasse la largeur d'une colonne — ce qui est le cas ordinaire
 * (« Internet — vérifier la couverture + réserver le technicien »). Mesuré au rendu
 * 1872×1404 : ~160 px la rangée large contre ~88 px la serrée, pour ~700 px de corps.
 */
export const ITEMS_PAR_COLONNE_LARGE = 4
export const ITEMS_PAR_COLONNE_SERREE = 7

export function rangeeSerree(nombreDeLignes: number, colonnesListe: number): boolean {
  if (colonnesListe <= 0) {
    return false
  }
  return nombreDeLignes / colonnesListe > ITEMS_PAR_COLONNE_LARGE
}

/**
 * Combien de rangées la liste peut montrer. Ce que la colonne ne peut pas montrer
 * doit être **annoncé**, jamais coupé en silence : le plafond du serveur (27) est
 * celui de la donnée, celui-ci est celui du papier.
 *
 * Le chapeau coûte une rangée : mesuré au rendu 1872×1404, une colonne serrée tient
 * sept rangées sans chapeau (rangs « court » et « sommaire ») et six avec. Sans ça,
 * la dernière rangée est coupée en deux par la ligne « + N autres ».
 */
export function capaciteListe(colonnesListe: number, serree: boolean, chapeau: boolean): number {
  if (colonnesListe <= 0) {
    return 0
  }
  const parColonne = serree ? ITEMS_PAR_COLONNE_SERREE : ITEMS_PAR_COLONNE_LARGE
  return colonnesListe * (chapeau ? parColonne - 1 : parColonne)
}

/**
 * Ce que la mention « + N autres » annonce : les tâches **à faire** qui n'ont pas
 * trouvé de place, jamais des tâches faites. Une journée à quatre choses ne dit pas
 * « + 17 autres » parce que dix-sept ont été cochées — le compte des faites est déjà
 * dans l'en-tête de la colonne.
 */
export function resteAAnnoncer(
  lignes: LigneEcran[],
  visibles: LigneEcran[],
  elagueesParLeServeur: number,
): number {
  const ouvertes = (l: LigneEcran[]) => l.filter((x) => x.faite === false).length
  return elagueesParLeServeur + ouvertes(lignes) - ouvertes(visibles)
}

/** « Édition du matin » avant midi, « Édition du soir » ensuite. */
export function surtitreEdition(renduLe: string): string {
  return new Date(renduLe).getHours() < 12 ? 'Édition du matin' : 'Édition du soir'
}

export type EtatDuJour = { texte: string; urgent: boolean }

/**
 * Ce que la dateline dit du jour, entre la date et la météo — la place qu'un
 * quotidien donne à sa ligne de sous-titre (maquettes, `une-editorialiste.html`).
 * C'est un état, pas une manchette : la manchette porte l'éditorial, la dateline
 * porte le compte.
 *
 * Le plancher y met une mention inversée. Elle ne peut jamais coexister avec la
 * bande du sommaire : dès que le plancher se déclenche, le rang devient
 * « événement » (voir `grilleDuJour`), donc la règle d'une seule bande inversée
 * tient (vault : Affichage Mural Et E-ink).
 */
export function etatDuJour(
  aPlancher: Plancher | null,
  ouvertes: number,
  faites: number,
): EtatDuJour {
  if (aPlancher !== null) {
    return {
      texte: aPlancher.raison === 'compte' ? "C'est aujourd'hui" : 'En retard',
      urgent: true,
    }
  }
  if (ouvertes === 0) {
    const regle = `Tout est fait — ${faites} réglée${faites > 1 ? 's' : ''}`
    return { texte: faites > 0 ? regle : 'Rien au programme', urgent: false }
  }
  if (ouvertes === 1) {
    return { texte: 'Une seule chose au programme', urgent: false }
  }
  return { texte: `${ouvertes} choses au programme`, urgent: false }
}

/* ────────────────────────────────────────────────────────────────────────────
 * Le fonds de tiroir vu du journal. Le fonds rend des faits sans mise en forme
 * (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal) ; c'est ici qu'on
 * décide de la place qu'on leur donne.
 * ──────────────────────────────────────────────────────────────────────────── */

/** Au-delà, le tableau du ciel devient une liste et perd sa lisibilité de loin. */
export const MAX_RANGEES_CIEL = 4

/**
 * Étiquette et valeur réunies, en caractères : au-delà, la rangée ne tient plus sur
 * une ligne dans une colonne de widget, et un tableau qui coupe ses valeurs (« Dans
 * 2… ») ment sur le peu qu'il avait à dire. Mesuré au rendu 1872×1404 : ~34 signes à
 * 28 px dans une colonne du tiers.
 */
export const LONGUEUR_RANGEE_CIEL = 34

/**
 * Les faits qui peuvent devenir une rangée de tableau. Les autres gardent leur place
 * de widget empilé, où ils ont deux lignes pour se dire — c'est le même principe que
 * partout ailleurs : élaguer, pas rapetisser.
 */
export function rangeesDuCiel(faits: FaitEcran[]): FaitEcran[] {
  return faits.filter((f) => f.etiquette.length + f.valeur.length <= LONGUEUR_RANGEE_CIEL)
}

export type FormeDuCiel = 'tableau' | 'phrase' | 'demi-phrase'

/**
 * « Les widgets rétrécissent avant de disparaître » : le tableau du ciel devient une
 * phrase, puis une demi-phrase (vault : D-2026-09-20 Une Seule Mise En Page À Rangs).
 * Le ciel occupe **une seule place** de widget dans les trois formes — ce qui change,
 * c'est la densité, jamais le nombre de colonnes.
 *
 * Le tableau demande deux choses : de la place (le rang doit accorder au moins cinq
 * widgets) et de la matière — trois faits **qui tiennent sur une rangée**
 * (`rangeesDuCiel`), sinon c'est un tableau à deux rangées, ce qui se lit moins bien
 * qu'une phrase.
 */
export function formeDuCiel(rangeesPossibles: number, budgetDeWidgets: number): FormeDuCiel {
  if (budgetDeWidgets >= 5 && rangeesPossibles >= 3) {
    return 'tableau'
  }
  return budgetDeWidgets >= 2 ? 'phrase' : 'demi-phrase'
}

/**
 * Les clés que le journal montre **déjà ailleurs** qu'en widget. Le fonds de tiroir ne
 * sait pas qu'un encadré de compte à rebours existe, et c'est voulu : la lettre du
 * matin n'en aura pas et voudra le fait (vault : D-2026-09-20 Fonds De Tiroir Séparé
 * Du Journal). C'est donc ici, chez le consommateur, qu'on évite de le dire deux fois —
 * le défaut « deux colonnes voisines coiffées DEHORS » de l'étape 2, en pire : le même
 * titre et le même chiffre, à dix centimètres l'un de l'autre.
 */
export const CLES_DEJA_AU_JOURNAL = ['calendrier.compte-a-rebours']

/** Le fonds de tiroir, moins ce que le journal dessine autrement. */
export function faitsEnWidgets(faits: FaitEcran[]): FaitEcran[] {
  return faits.filter((f) => CLES_DEJA_AU_JOURNAL.includes(f.cle) === false)
}

/**
 * Un fait a-t-il droit à son texte long ? Même règle que pour le ciel, qui perd sa
 * phrase avant de disparaître : au rang le plus serré (un seul widget), il ne reste
 * que l'étiquette et la valeur.
 */
export function faitAvecTexteLong(budgetDeWidgets: number): boolean {
  return budgetDeWidgets >= 2
}

/**
 * Une place dans la bande de widgets : soit un fait, soit le bloc du ciel — qui en
 * absorbe plusieurs et n'en occupe qu'une.
 */
export type PlaceDuFonds = { type: 'fait'; fait: FaitEcran } | { type: 'bloc-ciel' }

/**
 * Les places de la bande, **dans l'ordre du score** : le journal ne réordonne rien, il
 * coupe à la fin. Le bloc du ciel prend une seule place, celle du meilleur fait qu'il
 * absorbe ; les faits du ciel restés dehors gardent la leur et sortent à leur propre
 * score, comme n'importe quel autre fait.
 *
 * Trouvé en revue de code (2026-09-20) : en rendant le bloc **et** toute sa famille
 * d'un coup, un jour d'équinoxe au rang « resserré » la bande devenait
 * `[équinoxe, dérive du jour, durée du jour, record]` — la durée du jour, le fait le
 * plus banal du fonds, passait devant une garantie qui expire soixante fois mieux
 * classée, et se faisait couper à la troncature du budget.
 */
export function placesDuFonds(faits: FaitEcran[], prisesDuCiel: ReadonlySet<string>): PlaceDuFonds[] {
  const places: PlaceDuFonds[] = []
  let blocPlace = false

  for (const fait of faits) {
    if (prisesDuCiel.has(fait.cle)) {
      if (blocPlace === false) {
        blocPlace = true
        places.push({ type: 'bloc-ciel' })
      }
      continue
    }
    places.push({ type: 'fait', fait })
  }
  return places
}

/** Les faits d'une famille, dans l'ordre où le fonds de tiroir les a classés. */
export function faitsDeLaFamille(faits: FaitEcran[], famille: FaitEcran['famille']): FaitEcran[] {
  return faits.filter((f) => f.famille === famille)
}
