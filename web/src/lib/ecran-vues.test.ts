import { afterEach, expect, test, vi } from 'vitest'
import type { FaitEcran, LigneEcran } from '@/lib/api'
import {
  capaciteListe,
  capaciteWidgets,
  chroniqueVisible,
  colonnesDuSommaire,
  compteDeLaBande,
  dimensionsEcran,
  tailleDeLaBande,
  groupesDeLaListe,
  LE_RESTE,
  listePlate,
  RANGEES_PAR_ENTETE,
  grilleDuJour,
  initiales,
  ITEMS_PAR_COLONNE_LARGE,
  ITEMS_PAR_COLONNE_SERREE,
  PLACES_BANDE_DE_PIED,
  WIDGETS_PAR_COLONNE,
  JOURS_RETARD_PLANCHER,
  libelleDodos,
  pileAnnoncee,
  plancher,
  rangeeSerree,
  repartitionColonnes,
  resteAAnnoncer,
  surtitreEdition,
  etatDuJour,
  faitAvecTexteLong,
  faitsDeLaFamille,
  faitsEnWidgets,
  formeDuCiel,
  placesDuFonds,
  LONGUEUR_RANGEE_CIEL,
  MAX_RANGEES_CIEL,
  rangeesDuCiel,
} from '@/lib/ecran-vues'

afterEach(() => vi.useRealTimers())

test('initiales prend deux lettres en majuscules (Alain et Ariane se distinguent), vide sans nom', () => {
  expect(initiales('alain')).toBe('AL')
  expect(initiales(' Ariane ')).toBe('AR')
  expect(initiales('éloi')).toBe('ÉL')
  expect(initiales('A')).toBe('A')
  expect(initiales(null)).toBe('')
  expect(initiales('  ')).toBe('')
})

test('libelleDodos compte les nuits', () => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(2026, 8, 3, 14, 30))
  // Toujours depuis la date composée par le serveur, jamais depuis l'horloge du
  // navigateur : un tirage d'essai daté d'un autre jour compte ses dodos de là.
  expect(libelleDodos('2026-10-06', '2026-09-03')).toBe('33 dodos')
  expect(libelleDodos('2026-09-04', '2026-09-03')).toBe('1 dodo')
  expect(libelleDodos('2026-09-03', '2026-09-03')).toBe("c'est aujourd'hui")
  expect(libelleDodos('2026-10-06', '2026-09-27')).toBe('9 dodos')
})

test("dimensionsEcran retombe sur l'E1003 en paysage quand la requête est absente ou farfelue", () => {
  expect(dimensionsEcran(new URLSearchParams(''))).toEqual({ largeur: 1872, hauteur: 1404 })
  expect(dimensionsEcran(new URLSearchParams('largeur=800&hauteur=480'))).toEqual({
    largeur: 800,
    hauteur: 480,
  })
  expect(dimensionsEcran(new URLSearchParams('largeur=abc&hauteur=99999'))).toEqual({
    largeur: 1872,
    hauteur: 1404,
  })
})

test('pileAnnoncee est bornée à 0–100 et absente sans paramètre', () => {
  expect(pileAnnoncee(new URLSearchParams(''))).toBeNull()
  expect(pileAnnoncee(new URLSearchParams('pile=87.4'))).toBe(87)
  expect(pileAnnoncee(new URLSearchParams('pile=140'))).toBe(100)
  expect(pileAnnoncee(new URLSearchParams('pile=oui'))).toBeNull()
})

const ligne = (titre: string, joursDeRetard = 0, faite = false, echeanceFerme = false): LigneEcran => ({
  titre,
  assigne: null,
  faite,
  joursDeRetard,
  echeanceFerme,
})

test('le plancher : un compte à rebours à zéro passe devant tout', () => {
  expect(plancher([], { titre: 'Déménagement', dateCible: '2026-10-06' }, '2026-10-06')).toEqual({
    raison: 'compte',
    titre: 'Déménagement',
  })
  // Passé, aussi : un compte à rebours qu'on n'a pas retiré reste un événement.
  expect(plancher([], { titre: 'Déménagement', dateCible: '2026-10-06' }, '2026-10-07')?.raison).toBe(
    'compte',
  )
  expect(plancher([], { titre: 'Déménagement', dateCible: '2026-10-07' }, '2026-10-06')).toBeNull()
})

test("le plancher : un retard de plus de trois jours ne peut pas être relégué", () => {
  expect(plancher([ligne('Remettre les clés', 4)], null, '2026-10-09')).toEqual({
    raison: 'retard',
    titre: 'Remettre les clés',
  })
  expect(plancher([ligne('Boîtes', JOURS_RETARD_PLANCHER)], null, '2026-10-09')).toBeNull()
  // Une tâche faite ne déclenche rien, même si elle a traîné.
  expect(plancher([ligne('Boîtes', 9, true)], null, '2026-10-09')).toBeNull()
  expect(plancher([], null, '2026-10-09')).toBeNull()
})

test('le plancher : le compte à rebours passe avant le retard', () => {
  const p = plancher([ligne('Remettre les clés', 9)], { titre: 'Le camion', dateCible: '2026-10-06' }, '2026-10-06')
  expect(p).toEqual({ raison: 'compte', titre: 'Le camion' })
})

test("le plancher : une échéance ferme du jour prend la manchette, entre le compte et le retard", () => {
  // Cochée à la main sur la tâche, jamais déduite (D-2026-09-20 Échéance Ferme
  // Explicite Sur La Tâche) : le notaire ne passe pas sous un widget.
  const notaire = ligne('Signer chez le notaire', 0, false, true)
  expect(plancher([ligne('Boîtes'), notaire], null, '2026-10-02')).toEqual({
    raison: 'ferme',
    titre: 'Signer chez le notaire',
  })
  // Avant un retard, même long ; après un compte à rebours à zéro.
  expect(plancher([ligne('Boîtes', 9), notaire], null, '2026-10-02')?.raison).toBe('ferme')
  expect(
    plancher([notaire], { titre: 'Le camion', dateCible: '2026-10-02' }, '2026-10-02')?.raison,
  ).toBe('compte')
  // Une ferme déjà faite ne déclenche rien.
  expect(plancher([ligne('Signer chez le notaire', 0, true, true)], null, '2026-10-02')).toBeNull()
  // Et la dateline le dit en inversé, avec ses propres mots.
  expect(etatDuJour({ raison: 'ferme', titre: 'Signer chez le notaire' }, 3, 0)).toEqual({
    texte: 'Date ferme',
    urgent: true,
  })
})

test('les six rangs du tableau de bascule', () => {
  expect(grilleDuJour(0, false).rang).toBe('chronique')
  expect(grilleDuJour(1, false).rang).toBe('manchette')
  expect(grilleDuJour(2, false).rang).toBe('manchette')
  expect(grilleDuJour(3, false).rang).toBe('resserre')
  expect(grilleDuJour(5, false).rang).toBe('resserre')
  expect(grilleDuJour(6, false).rang).toBe('court')
  expect(grilleDuJour(9, false).rang).toBe('court')
  expect(grilleDuJour(10, false).rang).toBe('sommaire')
  expect(grilleDuJour(14, false).rang).toBe('sommaire')
  // Le plancher prend la manchette quelle que soit la charge — même à 14 tâches.
  expect(grilleDuJour(14, true).rang).toBe('evenement')
  expect(grilleDuJour(0, true).rang).toBe('evenement')
})

test('la manchette part avant la place : lettrine puis chapeau, et le budget rétrécit', () => {
  expect(grilleDuJour(0, false)).toMatchObject({ lettrine: true, chapeau: true, widgets: 7 })
  expect(grilleDuJour(2, false)).toMatchObject({ lettrine: true, chapeau: true, widgets: 5 })
  expect(grilleDuJour(4, false)).toMatchObject({ lettrine: false, chapeau: true, widgets: 4 })
  expect(grilleDuJour(7, false)).toMatchObject({ lettrine: false, chapeau: false, widgets: 3 })
  // Au rang « événement », un seul widget, et il doit servir.
  expect(grilleDuJour(2, true).widgets).toBe(1)
})

test('la liste prend plus de colonnes à mesure que la journée se charge', () => {
  expect(grilleDuJour(0, false).colonnesListe).toBe(0)
  expect(grilleDuJour(4, false).colonnesListe).toBe(1)
  expect(grilleDuJour(8, false).colonnesListe).toBe(2)
  expect(grilleDuJour(14, false).colonnesListe).toBe(3)
})

test('répartition : jamais de colonne vide, et le sommaire renvoie les widgets au pied', () => {
  expect(repartitionColonnes(1, 4)).toEqual({ chronique: 0, liste: 1, aparte: 2, bandeDePied: false })
  expect(repartitionColonnes(2, 4)).toEqual({ chronique: 0, liste: 2, aparte: 1, bandeDePied: false })
  // Un fonds de tiroir maigre ne laisse pas une colonne vide au milieu du journal.
  expect(repartitionColonnes(1, 1)).toEqual({ chronique: 0, liste: 1, aparte: 1, bandeDePied: false })
  expect(repartitionColonnes(0, 2)).toEqual({ chronique: 0, liste: 0, aparte: 2, bandeDePied: false })
  // Sans rien à mettre à côté, la liste prend tout.
  expect(repartitionColonnes(1, 0)).toEqual({ chronique: 0, liste: 3, aparte: 0, bandeDePied: false })
  expect(repartitionColonnes(3, 3)).toEqual({ chronique: 0, liste: 3, aparte: 0, bandeDePied: true })
  // Rien à mettre dans le corps : pas de corps du tout, la manchette prend la page.
  // Sans ça, une installation neuve peignait un « Aujourd'hui · 0 à faire » sur trois
  // colonnes blanches.
  expect(repartitionColonnes(0, 0)).toEqual({ chronique: 0, liste: 0, aparte: 0, bandeDePied: false })
})

test('la chronique prend la première colonne aux rangs qui racontent, et jamais ailleurs', () => {
  // Rang « chronique » : la prose, puis deux colonnes d'aparté (maquette du 8 novembre).
  expect(repartitionColonnes(0, 4, true)).toEqual({ chronique: 1, liste: 0, aparte: 2, bandeDePied: false })
  // Rang « manchette » : la prose, la liste, une colonne d'aparté (maquette du 20 septembre).
  expect(repartitionColonnes(1, 4, true)).toEqual({ chronique: 1, liste: 1, aparte: 1, bandeDePied: false })
  // Dès que la liste veut deux colonnes, le corps n'est plus montré.
  expect(repartitionColonnes(2, 4, true)).toEqual({ chronique: 0, liste: 2, aparte: 1, bandeDePied: false })
  expect(repartitionColonnes(3, 3, true)).toEqual({ chronique: 0, liste: 3, aparte: 0, bandeDePied: true })
  // Sans widget : la chronique et la liste se partagent la page ; sans liste, la
  // chronique la prend toute — jamais de colonne blanche.
  expect(repartitionColonnes(1, 0, true)).toEqual({ chronique: 1, liste: 2, aparte: 0, bandeDePied: false })
  expect(repartitionColonnes(0, 0, true)).toEqual({ chronique: 1, liste: 0, aparte: 0, bandeDePied: false })
  // Et elle ne s'affiche que si l'éditorialiste a écrit : le gabarit n'a pas de corps.
  expect(chroniqueVisible(grilleDuJour(0, false), ['Un paragraphe', 'Un autre'])).toBe(true)
  expect(chroniqueVisible(grilleDuJour(0, false), [])).toBe(false)
  expect(chroniqueVisible(grilleDuJour(4, false), ['Un paragraphe', 'Un autre'])).toBe(false)
  // Le plancher garde la lettrine et une colonne de liste : la chronique reste.
  expect(chroniqueVisible(grilleDuJour(1, true), ['Un paragraphe', 'Un autre'])).toBe(true)
})

test("le surtitre suit l'heure du tirage", () => {
  expect(surtitreEdition(new Date(2026, 8, 20, 6, 5).toISOString())).toBe('Édition du matin')
  expect(surtitreEdition(new Date(2026, 8, 20, 14, 30).toISOString())).toBe('Édition du soir')
})

test('la rangée ne se serre que quand la colonne en a besoin', () => {
  // Quatre rangées larges par colonne : une journée à quatre tâches garde ses
  // titres entiers, une journée à neuf sur deux colonnes doit se serrer.
  expect(rangeeSerree(4, 1)).toBe(false)
  expect(rangeeSerree(5, 1)).toBe(true)
  expect(rangeeSerree(8, 2)).toBe(false)
  expect(rangeeSerree(9, 2)).toBe(true)
  expect(rangeeSerree(14, 3)).toBe(true)
  expect(rangeeSerree(5, 0)).toBe(false)
})

test('la capacité de la liste suit la rangée et le chapeau, et le reste est annoncé', () => {
  expect(capaciteListe(1, false, false)).toBe(ITEMS_PAR_COLONNE_LARGE)
  expect(capaciteListe(3, true, false)).toBe(3 * ITEMS_PAR_COLONNE_SERREE)
  expect(capaciteListe(0, true, false)).toBe(0)
  // Le chapeau coûte une rangée par colonne.
  expect(capaciteListe(1, true, true)).toBe(ITEMS_PAR_COLONNE_SERREE - 1)
  expect(capaciteListe(3, false, true)).toBe(3 * (ITEMS_PAR_COLONNE_LARGE - 1))
  // Le sommaire n'a pas de chapeau : 23 lignes sur trois colonnes, 21 montrées.
  const sommaire = grilleDuJour(14, false)
  expect(capaciteListe(3, rangeeSerree(23, 3), sommaire.chapeau)).toBe(21)
})

test("la capacité de l'aparté borne le budget du rang, et l'encadré coûte une place", () => {
  // Le budget du rang dit ce que le journal veut ; la capacité dit ce que le papier
  // tient. Sans elle, le septième widget du rang « chronique » passe sous le pied —
  // 90 px de débordement, mesurés à l'étape 4.
  const chronique = grilleDuJour(0, false)
  expect(chronique.widgets).toBe(7)
  expect(capaciteWidgets({ chronique: 0, liste: 0, aparte: 3, bandeDePied: false }, false)).toBe(
    3 * WIDGETS_PAR_COLONNE,
  )
  expect(capaciteWidgets({ chronique: 0, liste: 0, aparte: 3, bandeDePied: false }, true)).toBe(
    3 * WIDGETS_PAR_COLONNE - 1,
  )

  // Une seule colonne d'aparté et un encadré : il ne reste qu'un widget.
  expect(capaciteWidgets({ chronique: 0, liste: 2, aparte: 1, bandeDePied: false }, true)).toBe(1)
  // Et jamais un nombre négatif, même quand l'encadré coûte plus que la place.
  expect(capaciteWidgets({ chronique: 0, liste: 3, aparte: 0, bandeDePied: false }, true)).toBe(0)

  // La bande de pied compte ses places à l'horizontale, pas par colonne.
  expect(capaciteWidgets({ chronique: 0, liste: 3, aparte: 0, bandeDePied: true }, false)).toBe(
    PLACES_BANDE_DE_PIED,
  )
  expect(capaciteWidgets({ chronique: 0, liste: 3, aparte: 0, bandeDePied: true }, true)).toBe(
    PLACES_BANDE_DE_PIED - 1,
  )
})

test('« + N autres » ne compte que ce qui reste à faire', () => {
  const lignes = [ligne('À faire 1'), ligne('À faire 2'), ligne('Faite 1', 0, true), ligne('Faite 2', 0, true)]
  // Les deux ouvertes sont montrées : les faites qui manquent ne sont pas un reste.
  expect(resteAAnnoncer(lignes, lignes.slice(0, 2), 0)).toBe(0)
  // Une ouverte n'a pas trouvé de place : elle, on l'annonce.
  expect(resteAAnnoncer(lignes, lignes.slice(0, 1), 0)).toBe(1)
  // Et l'élagage du serveur s'ajoute (il élague les ouvertes en dernier).
  expect(resteAAnnoncer(lignes, lignes.slice(0, 2), 3)).toBe(3)
})

test('la dateline dit le compte du jour, et le plancher le dit en inversé', () => {
  expect(etatDuJour(null, 0, 0)).toEqual({ texte: 'Rien au programme', urgent: false })
  // Une journée vide qui ne l'était pas ce matin n'est pas « rien au programme ».
  expect(etatDuJour(null, 0, 1)).toEqual({ texte: 'Tout est fait — 1 réglée', urgent: false })
  expect(etatDuJour(null, 0, 9)).toEqual({ texte: 'Tout est fait — 9 réglées', urgent: false })
  expect(etatDuJour(null, 1, 0)).toEqual({ texte: 'Une seule chose au programme', urgent: false })
  expect(etatDuJour(null, 14, 9)).toEqual({ texte: '14 choses au programme', urgent: false })
  // Le plancher prend la place, quel que soit le compte.
  expect(etatDuJour({ raison: 'compte', titre: 'Le camion' }, 14, 0)).toEqual({
    texte: "C'est aujourd'hui",
    urgent: true,
  })
  expect(etatDuJour({ raison: 'retard', titre: 'Remettre les clés' }, 3, 0)).toEqual({
    texte: 'En retard',
    urgent: true,
  })
})

test("l'état urgent et la bande du sommaire ne peuvent jamais sortir ensemble", () => {
  // La règle d'une seule bande inversée tient parce que le plancher force le rang
  // « événement » : le sommaire, qui a sa propre bande, ne peut pas coexister.
  expect(grilleDuJour(14, true).rang).toBe('evenement')
  expect(etatDuJour({ raison: 'compte', titre: 'Le camion' }, 14, 0).urgent).toBe(true)
  expect(etatDuJour(null, 14, 0).urgent).toBe(false)
})

const fait = (cle: string, famille: FaitEcran['famille'] = 'Ciel'): FaitEcran => ({
  cle,
  famille,
  etiquette: 'Le soleil',
  valeur: '6 h 30 → 18 h 49',
  texte: 'Le jour dure 12 h 19.',
})

test('le ciel rétrécit avant de disparaître', () => {
  // Rang « chronique » : sept widgets, de quoi déployer le tableau.
  expect(formeDuCiel(4, 7)).toBe('tableau')
  // Assez de place mais pas assez de matière : un tableau à deux rangées se lit
  // moins bien qu'une phrase.
  expect(formeDuCiel(2, 7)).toBe('phrase')
  // Rangs « resserré » et « court » : la phrase, avec son texte long.
  expect(formeDuCiel(4, 4)).toBe('phrase')
  expect(formeDuCiel(4, 2)).toBe('phrase')
  // Rang « événement » : un seul widget, et c'est celui qui sert — le ciel n'a plus
  // droit qu'à sa valeur courte.
  expect(formeDuCiel(4, 1)).toBe('demi-phrase')
  expect(formeDuCiel(0, 1)).toBe('demi-phrase')
})

test('le tableau du ciel ne dépasse jamais quatre rangées', () => {
  // Sept faits du ciel un jour faste : le tableau en prend quatre, les autres
  // deviennent des widgets ordinaires.
  const sept = Array.from({ length: 7 }, (_, i) => fait(`ciel.${i}`))
  expect(formeDuCiel(sept.length, 7)).toBe('tableau')
  expect(sept.slice(0, MAX_RANGEES_CIEL)).toHaveLength(4)
})

test('le journal ne prend que la famille qu’il demande', () => {
  const faits = [fait('ciel.jour'), fait('ville.collecte', 'Ville'), fait('ciel.lune')]
  // L'ordre du fonds de tiroir est celui du score : le journal ne le retrie jamais.
  expect(faitsDeLaFamille(faits, 'Ciel').map((f) => f.cle)).toEqual(['ciel.jour', 'ciel.lune'])
  expect(faitsDeLaFamille(faits, 'Maison')).toEqual([])
})

test('une rangée de tableau n’accepte que ce qui tient sur une ligne', () => {
  const court = { ...fait('ciel.jour'), etiquette: 'Le soleil', valeur: '6 h 28 → 18 h 47' }
  const long = { ...fait('ciel.equilibre'), etiquette: 'Ce soir', valeur: 'La nuit passe devant le jour' }

  expect(court.etiquette.length + court.valeur.length).toBeLessThanOrEqual(LONGUEUR_RANGEE_CIEL)
  expect(long.etiquette.length + long.valeur.length).toBeGreaterThan(LONGUEUR_RANGEE_CIEL)
  // Le fait trop long n'est pas coupé : il sort du tableau et garde sa place de
  // widget empilé, où il a deux lignes pour se dire.
  expect(rangeesDuCiel([court, long]).map((f) => f.cle)).toEqual(['ciel.jour'])
})

test('un ciel qui ne tient pas en rangées renonce au tableau', () => {
  const longs = Array.from({ length: 5 }, (_, i) => ({
    ...fait(`ciel.${i}`),
    etiquette: 'Ce soir',
    valeur: 'La nuit passe devant le jour',
  }))
  // De la place, mais pas de matière : trois rangées, sinon la phrase.
  expect(formeDuCiel(rangeesDuCiel(longs).length, 7)).toBe('phrase')
})

test("le compte à rebours ne se dit pas deux fois : l'encadré le montre déjà", () => {
  // Le fonds de tiroir ne sait pas qu'un encadré existe, et c'est voulu — la lettre
  // du matin n'en aura pas. C'est donc le journal qui écarte le doublon, et lui seul.
  const faits = [
    fait('calendrier.compte-a-rebours', 'Calendrier'),
    // La collecte est dans le même cas depuis l'étape 6 : le journal lui dessine son
    // propre widget (« Dans la ville »), le fonds la produit pour la lettre du matin.
    fait('ville.collecte', 'Ville'),
    fait('maison.record', 'Maison'),
    fait('ciel.jour'),
  ]

  expect(faitsEnWidgets(faits).map((f) => f.cle)).toEqual(['maison.record', 'ciel.jour'])
  // Les autres faits de la ville, eux, prennent bien leur place au mur.
  expect(faitsEnWidgets([fait('ville.evenement', 'Ville')]).map((f) => f.cle)).toEqual([
    'ville.evenement',
  ])
  // Et l'ordre du score est intact : le journal écarte, il ne retrie pas.
  expect(faitsEnWidgets([fait('ciel.jour')]).map((f) => f.cle)).toEqual(['ciel.jour'])
})

test('un fait perd son texte long au rang le plus serré, comme le ciel', () => {
  // Rang « événement » : un seul widget, et c'est celui qui sert.
  expect(faitAvecTexteLong(1)).toBe(false)
  expect(faitAvecTexteLong(2)).toBe(true)
  expect(faitAvecTexteLong(7)).toBe(true)
})

test('le bloc du ciel prend une place, pas toute sa famille', () => {
  // Un jour d'équinoxe au rang « resserré » : quatre widgets, le ciel en « phrase ».
  // Le bloc n'absorbe que le meilleur fait du ciel ; les autres retournent au
  // classement et sortent à leur propre score. Sans ça, la durée du jour — le fait le
  // plus banal du fonds — passait devant une garantie soixante fois mieux classée.
  const faits = [
    fait('ciel.saison'),
    fait('maison.record', 'Maison'),
    fait('calendrier.expiration', 'Calendrier'),
    fait('ciel.derive'),
    fait('ciel.jour'),
  ]

  const places = placesDuFonds(faits, new Set(['ciel.saison']))

  expect(places.map((p) => (p.type === 'fait' ? p.fait.cle : 'bloc-ciel'))).toEqual([
    'bloc-ciel',
    'maison.record',
    'calendrier.expiration',
    'ciel.derive',
    'ciel.jour',
  ])
})

test('le tableau du ciel absorbe ses rangées et les retire du classement, une seule fois', () => {
  const faits = [
    fait('ciel.saison'),
    fait('maison.record', 'Maison'),
    fait('ciel.derive'),
    fait('ciel.jour'),
  ]

  // En forme « tableau », trois faits du ciel deviennent des rangées : le bloc les
  // remplace tous les trois, à la place du mieux classé d'entre eux.
  const places = placesDuFonds(faits, new Set(['ciel.saison', 'ciel.derive', 'ciel.jour']))

  expect(places.map((p) => (p.type === 'fait' ? p.fait.cle : 'bloc-ciel'))).toEqual([
    'bloc-ciel',
    'maison.record',
  ])
})

test('sans ciel, le fonds garde exactement son ordre', () => {
  const faits = [fait('maison.record', 'Maison'), fait('calendrier.expiration', 'Calendrier')]

  expect(placesDuFonds(faits, new Set()).map((p) => (p.type === 'fait' ? p.fait.cle : 'bloc-ciel'))).toEqual([
    'maison.record',
    'calendrier.expiration',
  ])
})

/* ── Le sommaire des journées chargées ─────────────────────────────────────── */

const demarches = (n: number, faites = 0): LigneEcran[] => [
  ...Array.from({ length: n }, (_, i) => ligne(`Démarche ${i + 1}`)),
  ...Array.from({ length: faites }, (_, i) => ligne(`Faite ${i + 1}`, 0, true)),
]

test('les lignes se rangent sous leurs rubriques, par titre, et rien ne se perd', () => {
  const lignes = [ligne('SAAQ'), ligne('Hydro'), ligne('Banques'), ligne('Pneus'), ligne('Hydro', 0, true)]
  const groupes = groupesDeLaListe(lignes, [
    { nom: 'Gouvernements', taches: ['SAAQ', 'Hydro', 'Hydro'] },
    { nom: 'Argent', taches: ['Banques', 'Passeport (inventé)'] },
    { nom: LE_RESTE, taches: ['Pneus'] },
  ])
  expect(groupes).toEqual([
    { nom: 'Gouvernements', lignes: [lignes[0], lignes[1], lignes[4]] },
    { nom: 'Argent', lignes: [lignes[2]] },
    { nom: LE_RESTE, lignes: [lignes[3]] },
  ])
  // Une ligne que le serveur n'aurait placée nulle part tombe dans « Le reste »
  // plutôt que de disparaître ; un titre inventé ne fait pas de ligne.
  const orpheline = groupesDeLaListe([ligne('A'), ligne('B'), ligne('C')], [
    { nom: 'X', taches: ['A'] },
    { nom: 'Y', taches: ['B'] },
  ])
  expect(orpheline?.map((g) => g.nom)).toEqual(['X', 'Y', LE_RESTE])
  expect(orpheline?.[2].lignes.map((l) => l.titre)).toEqual(['C'])
})

test("une seule rubrique n'est pas un sommaire : liste plate", () => {
  expect(groupesDeLaListe(demarches(14), [{ nom: LE_RESTE, taches: [] }])).toBeNull()
  expect(groupesDeLaListe(demarches(14), [])).toBeNull()
  // Une rubrique vidée par le filtrage disparaît.
  const groupes = groupesDeLaListe([ligne('A')], [
    { nom: 'X', taches: ['A'] },
    { nom: 'Y', taches: ['Inventée'] },
  ])
  expect(groupes?.map((g) => g.nom)).toEqual(['X'])
})

test('la journée la plus chargée de la prod tient sur trois colonnes, rubriques comprises', () => {
  // 14 démarches en quatre rubriques : 14 rangées + 4 en-têtes = 18 unités pour 21.
  const groupes = groupesDeLaListe(demarches(14), [
    { nom: 'Gouvernements', taches: ['Démarche 1', 'Démarche 2', 'Démarche 3', 'Démarche 4'] },
    { nom: 'Argent', taches: ['Démarche 5', 'Démarche 6', 'Démarche 7', 'Démarche 8', 'Démarche 9'] },
    { nom: 'Santé', taches: ['Démarche 10', 'Démarche 11'] },
    { nom: LE_RESTE, taches: ['Démarche 12', 'Démarche 13', 'Démarche 14'] },
  ])!
  const { colonnes, enPlus } = colonnesDuSommaire(groupes, 3, ITEMS_PAR_COLONNE_SERREE)

  expect(enPlus).toBe(0)
  expect(colonnes).toHaveLength(3)
  expect(colonnes.flat().filter((i) => i.type === 'ligne')).toHaveLength(14)
  for (const colonne of colonnes) {
    const unites = colonne.reduce((n, i) => n + (i.type === 'entete' ? RANGEES_PAR_ENTETE : 1), 0)
    expect(unites).toBeLessThanOrEqual(ITEMS_PAR_COLONNE_SERREE)
    // Jamais un en-tête seul en bas de colonne.
    expect(colonne.at(-1)?.type).toBe('ligne')
  }
})

test("un en-tête ne reste jamais orphelin en bas de colonne, et un groupe coupé reprend sous son nom", () => {
  // Deux colonnes de trois : « A » prend en-tête + 2 lignes ; « B » n'a plus la place
  // d'un en-tête et d'une ligne dans la première colonne, il ouvre la seconde.
  const groupes = groupesDeLaListe([ligne('a1'), ligne('a2'), ligne('b1'), ligne('b2')], [
    { nom: 'A', taches: ['a1', 'a2'] },
    { nom: 'B', taches: ['b1', 'b2'] },
  ])!
  const { colonnes } = colonnesDuSommaire(groupes, 2, 3)
  expect(colonnes[0].map((i) => (i.type === 'entete' ? `#${i.nom}` : i.ligne.titre))).toEqual(['#A', 'a1', 'a2'])
  expect(colonnes[1].map((i) => (i.type === 'entete' ? `#${i.nom}` : i.ligne.titre))).toEqual(['#B', 'b1', 'b2'])

  // Quand la place le permet, une rubrique entière par colonne : « B » attend la
  // seconde colonne plutôt que de se fendre (2 colonnes de 5, A = 3 unités, B = 4).
  const entiers = groupesDeLaListe([ligne('a1'), ligne('a2'), ligne('b1'), ligne('b2'), ligne('b3')], [
    { nom: 'A', taches: ['a1', 'a2'] },
    { nom: 'B', taches: ['b1', 'b2', 'b3'] },
  ])!
  const parColonne = colonnesDuSommaire(entiers, 2, 5)
  expect(parColonne.colonnes[0].map((i) => (i.type === 'entete' ? `#${i.nom}` : i.ligne.titre))).toEqual(['#A', 'a1', 'a2'])
  expect(parColonne.colonnes[1].map((i) => (i.type === 'entete' ? `#${i.nom}` : i.ligne.titre))).toEqual(['#B', 'b1', 'b2', 'b3'])
  expect(parColonne.enPlus).toBe(0)

  // Un groupe plus long qu'une colonne continue dans la suivante, coiffé d'un rappel.
  const long = groupesDeLaListe([ligne('a1'), ligne('a2'), ligne('a3'), ligne('a4'), ligne('b1')], [
    { nom: 'A', taches: ['a1', 'a2', 'a3', 'a4'] },
    { nom: 'B', taches: ['b1'] },
  ])!
  const suite = colonnesDuSommaire(long, 3, 3)
  expect(suite.colonnes[0].map((i) => (i.type === 'entete' ? `#${i.nom}` : i.ligne.titre))).toEqual(['#A', 'a1', 'a2'])
  expect(suite.colonnes[1].map((i) => (i.type === 'entete' ? `#${i.nom}` : i.ligne.titre))).toEqual(['#A', 'a3', 'a4'])
  expect(suite.colonnes[2].map((i) => (i.type === 'entete' ? `#${i.nom}` : i.ligne.titre))).toEqual(['#B', 'b1'])
  expect(suite.enPlus).toBe(0)
})

test('ce qui ne tient pas est annoncé, et les faites cèdent leur place les premières', () => {
  // 20 lignes à faire + 3 faites en trois rubriques, sur 21 unités : les faites
  // partent d'abord, puis les dernières à faire sont annoncées, jamais coupées.
  const lignes = demarches(20, 3)
  const groupes = groupesDeLaListe(lignes, [
    { nom: 'A', taches: lignes.slice(0, 7).map((l) => l.titre) },
    { nom: 'B', taches: [...lignes.slice(7, 14).map((l) => l.titre), 'Faite 1', 'Faite 2'] },
    { nom: LE_RESTE, taches: [...lignes.slice(14, 20).map((l) => l.titre), 'Faite 3'] },
  ])!
  const { colonnes, enPlus } = colonnesDuSommaire(groupes, 3, ITEMS_PAR_COLONNE_SERREE)
  const montrees = colonnes.flat().filter((i) => i.type === 'ligne')
  expect(montrees.some((i) => i.type === 'ligne' && i.ligne.faite)).toBe(false)
  // 20 à faire + 3 en-têtes = 23 unités pour 21 : il en manque, et on le dit.
  expect(montrees.length + enPlus).toBe(20)
  expect(enPlus).toBeGreaterThan(0)
  for (const colonne of colonnes) {
    expect(colonne.at(-1)?.type).toBe('ligne')
  }
})

test('la bande du sommaire compte par personne, et tout court quand personne ne porte rien', () => {
  // Les porteurs viennent du serveur, sur toutes les ouvertes : 36 tâches un jeudi
  // chargé, dont 26 servies — la bande dit 36, pas 26.
  const porteurs = [
    { nom: 'Alain', ouvertes: 12 },
    { nom: 'Ariane', ouvertes: 12 },
    { nom: null, ouvertes: 12 },
  ]
  expect(compteDeLaBande(porteurs, 36, 0)).toBe('12 Alain · 12 Ariane · 12 pour la maison')
  expect(compteDeLaBande([], 36, 0)).toBe('36 à faire')
  // Les faites passent devant.
  expect(compteDeLaBande([{ nom: 'Alain', ouvertes: 1 }], 1, 3)).toBe('3 faites · 1 Alain')
  expect(compteDeLaBande([], 1, 1)).toBe('1 faite · 1 à faire')
})

test('la manchette de la bande rétrécit pour tenir sur une ligne à côté du compte', () => {
  const compteCourt = '14 à faire'
  const compteLong = '3 Alain · 3 Ariane · 8 pour la maison'
  expect(tailleDeLaBande('Journée chargée', compteCourt)).toBe(76)
  // Quarante signes mesurent 1190 px à 76 px ; comptés avec de la marge, ils tiennent
  // encore à côté d'un compte court, plus à côté d'un compte par personne (~800 px)
  // — la manchette cède, jamais la ligne.
  expect(tailleDeLaBande('Une adresse, et tout le monde à prévenir', compteCourt)).toBe(76)
  expect(tailleDeLaBande('Une adresse, et tout le monde à prévenir', compteLong)).toBe(46)
  expect(tailleDeLaBande('Quatorze fois la même adresse', compteLong)).toBe(60)
  // Soixante signes, la borne du contrat : 46 px, et jamais moins.
  expect(tailleDeLaBande('m'.repeat(60), compteLong)).toBe(46)
})

test("« + N autres » est une rangée de la liste : elle prend la dernière place quand il y en a à annoncer", () => {
  const lignes = demarches(21)
  // Tout tient : rien d'annoncé, rien de cédé.
  expect(listePlate(lignes, 21, 0)).toEqual({ visibles: lignes, enPlus: 0 })
  // Une de trop chez le serveur : la dernière rangée devient l'annonce.
  expect(listePlate(lignes, 21, 1)).toEqual({ visibles: lignes.slice(0, 20), enPlus: 2 })
  expect(listePlate(demarches(26), 21, 10)).toEqual({ visibles: demarches(26).slice(0, 20), enPlus: 16 })
  expect(listePlate([], 0, 0)).toEqual({ visibles: [], enPlus: 0 })
})

test("au sommaire aussi, l'annonce a sa rangée dans la dernière colonne", () => {
  // 21 unités pleines (3 rubriques de 6) et deux lignes de plus : la dernière colonne
  // cède une ligne à l'annonce, et l'annonce compte cette ligne.
  const lignes = demarches(20)
  const groupes = groupesDeLaListe(lignes, [
    { nom: 'A', taches: lignes.slice(0, 6).map((l) => l.titre) },
    { nom: 'B', taches: lignes.slice(6, 12).map((l) => l.titre) },
    { nom: LE_RESTE, taches: lignes.slice(12, 20).map((l) => l.titre) },
  ])!
  const { colonnes, enPlus } = colonnesDuSommaire(groupes, 3, ITEMS_PAR_COLONNE_SERREE)
  const montrees = colonnes.flat().filter((i) => i.type === 'ligne').length
  expect(montrees + enPlus).toBe(20)
  expect(enPlus).toBe(3)
  expect(colonnes[2].reduce((n, i) => n + (i.type === 'entete' ? RANGEES_PAR_ENTETE : 1), 0)).toBe(
    ITEMS_PAR_COLONNE_SERREE - 1,
  )

  // Le serveur a déjà élagué : même quand tout ce qui est servi tient, l'annonce a
  // besoin de sa rangée, et elle compte ce que le serveur a retenu.
  const justes = demarches(18)
  const pleines = groupesDeLaListe(justes, [
    { nom: 'A', taches: justes.slice(0, 6).map((l) => l.titre) },
    { nom: 'B', taches: justes.slice(6, 12).map((l) => l.titre) },
    { nom: LE_RESTE, taches: justes.slice(12, 18).map((l) => l.titre) },
  ])!
  expect(colonnesDuSommaire(pleines, 3, ITEMS_PAR_COLONNE_SERREE, 0).enPlus).toBe(0)
  const avecElagage = colonnesDuSommaire(pleines, 3, ITEMS_PAR_COLONNE_SERREE, 4)
  expect(avecElagage.enPlus).toBe(5)
  expect(avecElagage.colonnes[2].reduce((n, i) => n + (i.type === 'entete' ? RANGEES_PAR_ENTETE : 1), 0)).toBe(
    ITEMS_PAR_COLONNE_SERREE - 1,
  )
})
