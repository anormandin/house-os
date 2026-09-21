// Helpers purs de la vue e-ink (pages/Ecran.tsx). La page met en forme ; ce qui se
// calcule se calcule ici, testé.

import type { FaitEcran, LigneEcran } from '@/lib/api'
import { dodosAvant } from '@/lib/format'

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
 * « 33 dodos », « 1 dodo », « aujourd'hui » — le vocabulaire des comptes à rebours.
 * `aujourdhuiIso` est la date composée par le serveur : un tirage d'essai daté d'un
 * autre jour doit compter ses dodos depuis ce jour-là, pas depuis l'horloge du
 * navigateur — sinon l'aperçu du 27 affichait les dodos du 21.
 */
export function libelleDodos(dateIso: string, aujourdhuiIso: string): string {
  const dodos = dodosAvant(dateIso, aujourdhuiIso)
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

/* ────────────────────────────────────────────────────────────────────────────
 * Le journal : rang, plancher, répartition des colonnes.
 * Une seule mise en page, six remplissages (vault : D-2026-09-20 Une Seule Mise
 * En Page À Rangs). Le rang est une fonction, pas un second gabarit.
 * ──────────────────────────────────────────────────────────────────────────── */

/** À partir de ce retard, une tâche ne peut plus être reléguée sous un widget. */
export const JOURS_RETARD_PLANCHER = 3

export type Plancher = { raison: 'compte' | 'retard' | 'ferme'; titre: string }

/**
 * Le plancher non négociable : un compte à rebours à zéro, une échéance ferme, ou une
 * tâche en retard de plus de trois jours prennent la manchette quoi qu'il arrive. Sans
 * lui, le jour où la lune passe devant « remettre les clés », l'écran perd sa
 * crédibilité. Même règle et même ordre que le serveur (`Domaine/Editorial/Plancher.cs`),
 * qui la réévalue à chaque rendu pour rééditer.
 *
 * L'échéance ferme est un booléen coché à la main sur la tâche, jamais déduit
 * (vault : D-2026-09-20 Échéance Ferme Explicite Sur La Tâche) ; une ligne du jour qui
 * le porte est due, donc « entrante » : elle prend la manchette.
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
  const ferme = lignes.find((l) => !l.faite && l.echeanceFerme)
  if (ferme !== undefined) {
    return { raison: 'ferme', titre: ferme.titre }
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

export type Repartition = { chronique: 0 | 1; liste: number; aparte: number; bandeDePied: boolean }

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
 *
 * La **chronique** — les deux paragraphes de l'éditorialiste — prend la première
 * colonne aux rangs où la manchette raconte (chronique, manchette : la liste tient sur
 * une colonne au plus), comme dans les maquettes. Aux rangs plus chargés, le corps
 * n'est pas montré : « titre court, sans lettrine ni chronique ».
 */
export function repartitionColonnes(
  colonnesListe: number,
  widgetsDisponibles: number,
  avecChronique = false,
): Repartition {
  const chronique: 0 | 1 = avecChronique && colonnesListe <= 1 ? 1 : 0
  if (chronique === 0 && colonnesListe <= 0 && widgetsDisponibles === 0) {
    return { chronique: 0, liste: 0, aparte: 0, bandeDePied: false }
  }
  if (widgetsDisponibles === 0) {
    // Sans widget, la liste prend ce que la chronique laisse ; sans liste non plus,
    // la chronique prend toute la page.
    return { chronique, liste: colonnesListe > 0 ? 3 - chronique : 0, aparte: 0, bandeDePied: false }
  }
  if (colonnesListe >= 3) {
    return { chronique: 0, liste: 3, aparte: 0, bandeDePied: true }
  }
  // Une colonne par widget au plus : un fonds de tiroir maigre ne doit pas laisser
  // une colonne vide au milieu du journal.
  const aparte = Math.min(3 - chronique - colonnesListe, widgetsDisponibles)
  return { chronique, liste: colonnesListe, aparte, bandeDePied: false }
}

/** Les rangs où la chronique a sa colonne : ceux dont la manchette porte une lettrine. */
export function chroniqueVisible(grille: Grille, paragraphes: string[]): boolean {
  return paragraphes.length > 0 && grille.lettrine && grille.colonnesListe <= 1
}

/**
 * Ce qu'une colonne d'aparté tient de widgets, et ce que la bande de pied tient de
 * places. Le pendant de `capaciteListe` pour l'autre moitié du corps : le budget du
 * rang dit ce que le journal **veut** montrer, la capacité dit ce que le papier
 * **tient**. Sans elle, le budget de sept du rang « chronique » n'est jamais
 * atteignable et le septième widget passe sous le pied.
 *
 * Mesuré au rendu 1872×1404 : un widget empilé fait ~300 px (l'étiquette, la valeur
 * sur deux lignes à 52 px, le texte sur deux lignes à 36 px) pour ~690 px de corps,
 * et l'encadré du compte à rebours coûte exactement une de ces places.
 */
export const WIDGETS_PAR_COLONNE = 2
export const PLACES_BANDE_DE_PIED = 3

export function capaciteWidgets(repartition: Repartition, avecEncadre: boolean): number {
  const places = repartition.bandeDePied
    ? PLACES_BANDE_DE_PIED
    : repartition.aparte * WIDGETS_PAR_COLONNE
  return Math.max(0, places - (avecEncadre ? 1 : 0))
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

/* ────────────────────────────────────────────────────────────────────────────
 * Le sommaire des journées chargées : la liste par rubrique, sur trois colonnes
 * (vault : D-2026-09-20 Regroupement Sans Catégorie De Tâche).
 * ──────────────────────────────────────────────────────────────────────────── */

export type GroupeDeLignes = { nom: string; lignes: LigneEcran[] }

/**
 * Les lignes rangées sous leurs rubriques, dans l'ordre du serveur. Le serveur ne sert
 * que des titres de lignes réelles, une place par ligne ; s'il en restait une sans
 * place, elle irait sous « Le reste » plutôt que de disparaître. Null quand il n'y a
 * rien à grouper — une seule rubrique (« Le reste » sur quatorze démarches sans zone,
 * le repli sans LLM) se lit mieux en liste plate, sans titre pour rien.
 */
export function groupesDeLaListe(
  lignes: LigneEcran[],
  rubriques: { nom: string; taches: string[] }[],
): GroupeDeLignes[] | null {
  if (rubriques.length <= 1) {
    return null
  }
  const restantes = [...lignes]
  const prendre = (titre: string): LigneEcran | undefined => {
    const i = restantes.findIndex((l) => l.titre === titre)
    return i === -1 ? undefined : restantes.splice(i, 1)[0]
  }
  const groupes: GroupeDeLignes[] = rubriques.map((r) => ({
    nom: r.nom,
    lignes: r.taches.map(prendre).filter((l): l is LigneEcran => l !== undefined),
  }))
  if (restantes.length > 0) {
    const reste = groupes.find((g) => g.nom === LE_RESTE)
    if (reste === undefined) {
      groupes.push({ nom: LE_RESTE, lignes: restantes })
    } else {
      reste.lignes.push(...restantes)
    }
  }
  return groupes.filter((g) => g.lignes.length > 0)
}

export const LE_RESTE = 'Le reste'

export type ItemDeColonne = { type: 'entete'; nom: string } | { type: 'ligne'; ligne: LigneEcran }

/**
 * Ce qu'un en-tête de rubrique coûte, en rangées serrées : mesuré au rendu 1872×1404,
 * l'étiquette et son filet font ~65 px pour ~88 px la rangée. On compte une rangée
 * entière — la capacité dit ce que le papier tient, pas ce qu'on espère y faire entrer.
 */
export const RANGEES_PAR_ENTETE = 1

/**
 * Les colonnes du sommaire, remplies à la main plutôt que par `column-count` : une
 * rubrique n'est jamais coiffée en bas d'une colonne avec ses lignes dans la suivante,
 * et ce qui ne tient pas est **annoncé**, jamais coupé en silence — la colonne cache
 * (`overflow-hidden`), et un clamp ne dit rien de ce qui passe sous le pied (leçon de
 * l'étape 7). Les faites cèdent leur place les premières : le compte des faites est
 * déjà dans la bande, et une ligne barrée dit moins qu'une ligne à faire.
 */
export function colonnesDuSommaire(
  groupes: GroupeDeLignes[],
  nombreDeColonnes: number,
  rangeesParColonne: number,
  elagueesParLeServeur = 0,
): { colonnes: ItemDeColonne[][]; enPlus: number } {
  let candidats = groupes.map((g) => ({ nom: g.nom, lignes: [...g.lignes] }))
  let resultat = auMieux(candidats, nombreDeColonnes, rangeesParColonne)
  // Tant que ça déborde et qu'il reste une faite, on retire la dernière faite.
  while (resultat.ecartees.length > 0) {
    const derniereFaite = derniereLigneFaite(candidats)
    if (derniereFaite === null) break
    candidats = candidats
      .map((g) => (g === derniereFaite.groupe ? { ...g, lignes: g.lignes.filter((l) => l !== derniereFaite.ligne) } : g))
      .filter((g) => g.lignes.length > 0)
    resultat = auMieux(candidats, nombreDeColonnes, rangeesParColonne)
  }
  // « + N autres » est une rangée de la dernière colonne : quand il y a de quoi
  // annoncer — ici, ou déjà chez le serveur, qui plafonne les lignes —, elle a besoin
  // de sa place, et c'est la dernière ligne posée qui la cède.
  const colonnes = resultat.colonnes
  const ecartees = [...resultat.ecartees]
  if (ecartees.length + elagueesParLeServeur > 0) {
    const derniere = colonnes[colonnes.length - 1]
    while (unites(derniere) > rangeesParColonne - 1) {
      const item = derniere.pop()
      if (item?.type === 'ligne') ecartees.unshift(item.ligne)
    }
    if (derniere.at(-1)?.type === 'entete') derniere.pop()
  }
  return { colonnes, enPlus: elagueesParLeServeur + ecartees.filter((l) => l.faite === false).length }
}

function unites(colonne: ItemDeColonne[]): number {
  return colonne.reduce((n, i) => n + (i.type === 'entete' ? RANGEES_PAR_ENTETE : 1), 0)
}

/**
 * Une rubrique entière par colonne quand la place le permet (une colonne par
 * rubrique, comme dans la maquette du 20 octobre) ; sinon, au fil des rangées, une
 * rubrique coupée reprenant sous son nom. Le journal préfère un blanc en bas de
 * colonne à une rubrique fendue — mais jamais une ligne écartée à un blanc.
 */
function auMieux(groupes: GroupeDeLignes[], nombreDeColonnes: number, rangeesParColonne: number) {
  const entier = remplir(groupes, nombreDeColonnes, rangeesParColonne, true)
  if (entier.ecartees.length === 0) return entier
  const auFil = remplir(groupes, nombreDeColonnes, rangeesParColonne, false)
  return auFil.ecartees.length < entier.ecartees.length ? auFil : entier
}

function derniereLigneFaite(groupes: GroupeDeLignes[]): { groupe: GroupeDeLignes; ligne: LigneEcran } | null {
  for (let g = groupes.length - 1; g >= 0; g--) {
    const lignes = groupes[g].lignes
    for (let i = lignes.length - 1; i >= 0; i--) {
      if (lignes[i].faite) return { groupe: groupes[g], ligne: lignes[i] }
    }
  }
  return null
}

function remplir(
  groupes: GroupeDeLignes[],
  nombreDeColonnes: number,
  rangeesParColonne: number,
  rubriquesEntieres: boolean,
): { colonnes: ItemDeColonne[][]; ecartees: LigneEcran[] } {
  const colonnes: ItemDeColonne[][] = Array.from({ length: nombreDeColonnes }, () => [])
  const ecartees: LigneEcran[] = []
  let c = 0
  let occupees = 0
  const place = (item: ItemDeColonne, cout: number): boolean => {
    while (c < nombreDeColonnes) {
      const reste = rangeesParColonne - occupees
      // Un en-tête a besoin de sa place et d'au moins une ligne sous lui.
      const besoin = item.type === 'entete' ? cout + 1 : cout
      if (reste >= besoin) {
        colonnes[c].push(item)
        occupees += cout
        return true
      }
      c++
      occupees = 0
    }
    return false
  }
  for (const groupe of groupes) {
    const hauteur = RANGEES_PAR_ENTETE + groupe.lignes.length
    if (rubriquesEntieres && occupees > 0 && hauteur > rangeesParColonne - occupees && hauteur <= rangeesParColonne) {
      c++
      occupees = 0
    }
    if (place({ type: 'entete', nom: groupe.nom }, RANGEES_PAR_ENTETE) === false) {
      ecartees.push(...groupe.lignes)
      continue
    }
    for (const [i, ligne] of groupe.lignes.entries()) {
      if (place({ type: 'ligne', ligne }, 1) === false) {
        ecartees.push(...groupe.lignes.slice(i))
        break
      }
      // Une colonne qui s'est remplie en plein groupe : la suite reprend sous un
      // rappel du nom, sinon la colonne suivante ouvre sur des lignes orphelines.
      if (occupees === rangeesParColonne && i < groupe.lignes.length - 1) {
        if (place({ type: 'entete', nom: groupe.nom }, RANGEES_PAR_ENTETE) === false) {
          ecartees.push(...groupe.lignes.slice(i + 1))
          break
        }
      }
    }
  }
  return { colonnes, ecartees }
}

/**
 * Ce que la liste plate montre, et ce qu'elle annonce. La mention « + N autres » est
 * une rangée de la liste — la dernière —, pas une ligne en dessous : posée sous le
 * bloc, elle lui volait ~50 px, et en colonnes CSS la septième rangée ne disparaissait
 * pas vers le bas mais dans une **quatrième colonne**, cachée à droite — trois lignes
 * perdues sans que la garde ne voie rien (trouvé à l'étape 8, 36 tâches un jeudi
 * d'essai). Le serveur fait pareil : « la mention occupe la dernière place ».
 */
export function listePlate(
  lignes: LigneEcran[],
  capacite: number,
  elagueesParLeServeur: number,
): { visibles: LigneEcran[]; enPlus: number } {
  if (lignes.length + elagueesParLeServeur <= capacite) {
    return { visibles: lignes, enPlus: 0 }
  }
  const visibles = lignes.slice(0, Math.max(0, capacite - 1))
  return { visibles, enPlus: resteAAnnoncer(lignes, visibles, elagueesParLeServeur) }
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

/**
 * Le compte de la bande du sommaire, par personne (« 4 Alain · 4 Ariane · 6 pour la
 * maison »), comme dans la maquette du 20 octobre : la dateline dit déjà « 14 choses
 * au programme », la bande dit qui les porte. Les porteurs viennent du serveur, comptés
 * sur **toutes** les ouvertes — les lignes servies sont plafonnées, et c'est justement
 * les jours chargés que le plafond mord. Sans personne d'assigné, le compte tout court,
 * et les faites devant, s'il y en a, comme l'en-tête de la liste les disait.
 */
export function compteDeLaBande(
  porteurs: { nom: string | null; ouvertes: number }[],
  ouvertes: number,
  faites: number,
): string {
  const parts: string[] = []
  if (faites > 0) parts.push(`${faites} faite${faites > 1 ? 's' : ''}`)
  if (porteurs.length === 0) {
    parts.push(`${ouvertes} à faire`)
  } else {
    for (const p of porteurs) parts.push(p.nom === null ? `${p.ouvertes} pour la maison` : `${p.ouvertes} ${p.nom}`)
  }
  return parts.join(' · ')
}

/**
 * La taille de la manchette dans la bande du sommaire : la plus grande des trois
 * (76, 60, 46 px) qui tient sur une ligne à côté du compte, sur les 1872 px du mur
 * moins les marges. Estimée en largeur de signe, mesurée au rendu 1872×1404 : la
 * titraille grasse fait ~0,39 em par signe en moyenne (« Une adresse, et tout le monde
 * à prévenir » : 1190 px à 76 px), comptée 0,46 ; le compte en capitales espacées
 * ~0,64 em à 34 px, compté 0,66.
 * Une manchette qui se plie sur deux lignes doublerait la bande, et c'est la liste en
 * dessous qui paierait.
 */
export const LARGEUR_BANDE = 1872 - 2 * 64 - 40
export const TAILLE_COMPTE_BANDE = 34

export function tailleDeLaBande(titre: string, compte: string): number {
  // De la marge sur les deux mesures : une manchette en capitales ou en lettres
  // larges déborde l'estimation, et la bande cache ce qui dépasse — la garde le voit.
  const largeurCompte = compte.length * TAILLE_COMPTE_BANDE * 0.66
  for (const taille of [76, 60, 46]) {
    if (titre.length * taille * 0.46 + largeurCompte <= LARGEUR_BANDE) return taille
  }
  return 46
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
      texte:
        aPlancher.raison === 'compte'
          ? "C'est aujourd'hui"
          : aPlancher.raison === 'ferme'
            ? 'Date ferme'
            : 'En retard',
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
 *
 * La prochaine collecte est dans le même cas depuis l'étape 6 : le journal lui garde
 * une place fixe — sortir le bac est le geste du soir, il ne doit pas dépendre d'un
 * classement — mais il l'écrit désormais **avec les mots du fait**, qui sait se taire
 * quand le calendrier n'est plus alimenté et quand la collecte est encore loin. Avant,
 * le widget lisait une colonne à part qui ne jugeait ni l'un ni l'autre.
 */
export const CLES_DEJA_AU_JOURNAL = ['calendrier.compte-a-rebours', 'ville.collecte']

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
