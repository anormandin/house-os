import { afterEach, expect, test, vi } from 'vitest'
import type { FaitEcran, LigneEcran } from '@/lib/api'
import {
  capaciteListe,
  capaciteWidgets,
  dimensionsEcran,
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
  quandCeJour,
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

test("quandCeJour parle comme à la maison : aujourd'hui, demain, le jour, dans N jours", () => {
  const aujourdhui = '2026-09-03' // jeudi
  expect(quandCeJour('2026-09-03', aujourdhui)).toBe("aujourd'hui")
  expect(quandCeJour('2026-09-01', aujourdhui)).toBe("aujourd'hui")
  expect(quandCeJour('2026-09-04', aujourdhui)).toBe('demain')
  expect(quandCeJour('2026-09-07', aujourdhui)).toBe('lun')
  expect(quandCeJour('2026-09-10', aujourdhui)).toBe('dans 7 jours')
})

test('libelleDodos compte les nuits', () => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(2026, 8, 3, 14, 30))
  expect(libelleDodos('2026-10-06')).toBe('33 dodos')
  expect(libelleDodos('2026-09-04')).toBe('1 dodo')
  expect(libelleDodos('2026-09-03')).toBe("c'est aujourd'hui")
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

const ligne = (titre: string, joursDeRetard = 0, faite = false): LigneEcran => ({
  titre,
  assigne: null,
  faite,
  joursDeRetard,
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
  expect(repartitionColonnes(1, 4)).toEqual({ liste: 1, aparte: 2, bandeDePied: false })
  expect(repartitionColonnes(2, 4)).toEqual({ liste: 2, aparte: 1, bandeDePied: false })
  // Un fonds de tiroir maigre ne laisse pas une colonne vide au milieu du journal.
  expect(repartitionColonnes(1, 1)).toEqual({ liste: 1, aparte: 1, bandeDePied: false })
  expect(repartitionColonnes(0, 2)).toEqual({ liste: 0, aparte: 2, bandeDePied: false })
  // Sans rien à mettre à côté, la liste prend tout.
  expect(repartitionColonnes(1, 0)).toEqual({ liste: 3, aparte: 0, bandeDePied: false })
  expect(repartitionColonnes(3, 3)).toEqual({ liste: 3, aparte: 0, bandeDePied: true })
  // Rien à mettre dans le corps : pas de corps du tout, la manchette prend la page.
  // Sans ça, une installation neuve peignait un « Aujourd'hui · 0 à faire » sur trois
  // colonnes blanches.
  expect(repartitionColonnes(0, 0)).toEqual({ liste: 0, aparte: 0, bandeDePied: false })
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
  expect(capaciteWidgets({ liste: 0, aparte: 3, bandeDePied: false }, false)).toBe(
    3 * WIDGETS_PAR_COLONNE,
  )
  expect(capaciteWidgets({ liste: 0, aparte: 3, bandeDePied: false }, true)).toBe(
    3 * WIDGETS_PAR_COLONNE - 1,
  )

  // Une seule colonne d'aparté et un encadré : il ne reste qu'un widget.
  expect(capaciteWidgets({ liste: 2, aparte: 1, bandeDePied: false }, true)).toBe(1)
  // Et jamais un nombre négatif, même quand l'encadré coûte plus que la place.
  expect(capaciteWidgets({ liste: 3, aparte: 0, bandeDePied: false }, true)).toBe(0)

  // La bande de pied compte ses places à l'horizontale, pas par colonne.
  expect(capaciteWidgets({ liste: 3, aparte: 0, bandeDePied: true }, false)).toBe(
    PLACES_BANDE_DE_PIED,
  )
  expect(capaciteWidgets({ liste: 3, aparte: 0, bandeDePied: true }, true)).toBe(
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
    fait('maison.record', 'Maison'),
    fait('ciel.jour'),
  ]

  expect(faitsEnWidgets(faits).map((f) => f.cle)).toEqual(['maison.record', 'ciel.jour'])
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
