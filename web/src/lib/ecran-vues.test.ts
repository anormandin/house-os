import { afterEach, expect, test, vi } from 'vitest'
import type { LigneEcran } from '@/lib/api'
import {
  capaciteListe,
  dimensionsEcran,
  grilleDuJour,
  initiales,
  ITEMS_PAR_COLONNE_LARGE,
  ITEMS_PAR_COLONNE_SERREE,
  JOURS_RETARD_PLANCHER,
  libelleDodos,
  pileAnnoncee,
  plancher,
  quandCeJour,
  rangeeSerree,
  repartitionColonnes,
  resteAAnnoncer,
  surtitreEdition,
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

test('« + N autres » ne compte que ce qui reste à faire', () => {
  const lignes = [ligne('À faire 1'), ligne('À faire 2'), ligne('Faite 1', 0, true), ligne('Faite 2', 0, true)]
  // Les deux ouvertes sont montrées : les faites qui manquent ne sont pas un reste.
  expect(resteAAnnoncer(lignes, lignes.slice(0, 2), 0)).toBe(0)
  // Une ouverte n'a pas trouvé de place : elle, on l'annonce.
  expect(resteAAnnoncer(lignes, lignes.slice(0, 1), 0)).toBe(1)
  // Et l'élagage du serveur s'ajoute (il élague les ouvertes en dernier).
  expect(resteAAnnoncer(lignes, lignes.slice(0, 2), 3)).toBe(3)
})
