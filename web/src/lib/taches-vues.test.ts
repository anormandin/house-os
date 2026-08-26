import { describe, expect, test } from 'vitest'
import type { Recurrence, TacheResume } from '@/lib/api'
import {
  chipsRecurrence,
  construireAnnee,
  grouperParRythme,
  pctDansAnnee,
  tonEcheance,
} from '@/lib/taches-vues'

const AUJOURDHUI = '2026-08-26'

function tache(partiel: Partial<TacheResume> & { id: string; titre: string }): TacheResume {
  return {
    description: null,
    echeance: null,
    assigneA: null,
    zoneId: null,
    equipementId: null,
    strategie: 'Fixe',
    recurrence: { mode: 'Ponctuelle' },
    nbDocuments: 0,
    completee: false,
    ...partiel,
  }
}

const HEBDO: Recurrence = { mode: 'Fixe', fixeType: 'JoursSemaine', joursSemaine: [0, 6] }
const INTERVALLE: Recurrence = { mode: 'Intervalle', intervalleJours: 4 }
const MENSUELLE: Recurrence = { mode: 'Fixe', fixeType: 'JourDuMois', jourDuMois: 1 }
const ANNUELLE: Recurrence = { mode: 'Fixe', fixeType: 'Annuelle', moisAnnuel: 9, jourAnnuel: 15 }
const TONTE: Recurrence = {
  mode: 'Intervalle',
  intervalleJours: 7,
  fenetreDebutMois: 5,
  fenetreDebutJour: 1,
  fenetreFinMois: 10,
  fenetreFinJour: 31,
}

describe('chipsRecurrence', () => {
  test('libellés français par mode, fenêtre en chip séparée', () => {
    expect(chipsRecurrence(HEBDO)).toEqual([{ libelle: '↻ hebdo · dim · sam', classe: 'hebdo' }])
    expect(chipsRecurrence(INTERVALLE)).toEqual([
      { libelle: '↻ aux 4 jours', classe: 'intervalle' },
    ])
    expect(chipsRecurrence({ mode: 'Intervalle', intervalleJours: 1 })).toEqual([
      { libelle: '↻ chaque jour', classe: 'intervalle' },
    ])
    expect(chipsRecurrence(MENSUELLE)).toEqual([{ libelle: '↻ 1ᵉʳ du mois', classe: 'mois' }])
    expect(chipsRecurrence(ANNUELLE)).toEqual([
      { libelle: '↻ annuelle · 15 sept', classe: 'annuelle' },
    ])
    expect(chipsRecurrence(TONTE)).toEqual([
      { libelle: '↻ aux 7 jours', classe: 'intervalle' },
      { libelle: '☀ mai→oct', classe: 'saison' },
    ])
    expect(chipsRecurrence({ mode: 'Ponctuelle' })).toEqual([])
  })
})

describe('grouperParRythme', () => {
  test('cinq rythmes, groupes vides omis, retards comptés, progression des ponctuelles', () => {
    const { groupes, progression } = grouperParRythme(
      [
        tache({ id: 'h', titre: 'Draps', recurrence: HEBDO, echeance: '2026-08-30' }),
        tache({ id: 'i', titre: 'Aspirateur', recurrence: INTERVALLE, echeance: '2026-08-25' }),
        tache({ id: 't', titre: 'Tondre', recurrence: TONTE, echeance: '2026-08-28' }),
        tache({ id: 'a', titre: 'Ramoner', recurrence: ANNUELLE, echeance: '2026-09-15' }),
        tache({ id: 'p1', titre: 'Notaire', echeance: '2026-08-31' }),
        tache({ id: 'p2', titre: 'SQCA', echeance: null, completee: true }),
      ],
      AUJOURDHUI,
    )

    // Pas de mensuelle : le groupe « Chaque mois » est absent.
    expect(groupes.map((g) => g.cle)).toEqual(['semaine', 'intervalle', 'annuel', 'ponctuelles'])
    const intervalle = groupes.find((g) => g.cle === 'intervalle')!
    // Tondre (intervalle + fenêtre) vit dans le même groupe que l'aspirateur.
    expect(intervalle.taches.map((t) => t.id)).toEqual(['i', 't'])
    expect(intervalle.enRetard).toBe(1)
    // La ponctuelle faite sort de la liste mais compte dans la progression.
    expect(groupes.find((g) => g.cle === 'ponctuelles')!.taches.map((t) => t.id)).toEqual(['p1'])
    expect(progression).toEqual({ faites: 1, total: 2 })
  })
})

describe('tonEcheance', () => {
  test('retard hier, proche jusqu’à trois jours, normal ensuite', () => {
    expect(tonEcheance('2026-08-25', AUJOURDHUI)).toBe('retard')
    expect(tonEcheance('2026-08-26', AUJOURDHUI)).toBe('proche')
    expect(tonEcheance('2026-08-29', AUJOURDHUI)).toBe('proche')
    expect(tonEcheance('2026-08-30', AUJOURDHUI)).toBe('normal')
    expect(tonEcheance(null, AUJOURDHUI)).toBe('aucune')
  })
})

describe('construireAnnee', () => {
  test('chaque mode trouve sa géométrie et le tempo court récupère le reste', () => {
    const annee = construireAnnee(
      [
        tache({ id: 'm', titre: 'Filtre', recurrence: MENSUELLE }),
        tache({ id: 't', titre: 'Tondre', recurrence: TONTE }),
        tache({ id: 'a', titre: 'Ramoner', recurrence: ANNUELLE }),
        tache({ id: 'h', titre: 'Draps', recurrence: HEBDO }),
        tache({ id: 'i', titre: 'Litière', recurrence: { mode: 'Intervalle', intervalleJours: 2 } }),
        tache({ id: 'p1', titre: 'Notaire', echeance: '2026-09-08' }),
        tache({ id: 'p2', titre: 'SQCA', echeance: '2026-09-08' }),
        tache({ id: 'p2b', titre: 'Postes Canada', echeance: '2026-09-10' }),
        tache({ id: 'p3', titre: 'Hydro', echeance: '2026-09-22' }),
        tache({ id: 'fait', titre: 'Déjà fait', echeance: '2026-09-08', completee: true }),
        tache({ id: 'vieux', titre: 'Hors année', echeance: '2025-12-01' }),
      ],
      AUJOURDHUI,
    )

    // Ordre de lecture : mensuelles, fenêtres, annuelles.
    expect(annee.lignes.map((l) => l.type)).toEqual(['mensuelle', 'fenetre', 'annuelle'])

    const [mensuelle, fenetre, annuelle] = annee.lignes
    expect(mensuelle.type === 'mensuelle' && mensuelle.points).toHaveLength(12)
    if (fenetre.type === 'fenetre') {
      expect(fenetre.segments).toHaveLength(1)
      expect(fenetre.enCours).toBe(true) // le 26 août est dans mai→oct
      expect(fenetre.libelle).toBe('↻ aux 7 jours pendant la saison')
    }
    if (annuelle.type === 'annuelle') {
      expect(annuelle.libelle).toBe('15 sept')
      expect(annuelle.position).toBeCloseTo(pctDansAnnee('2026-09-15'))
    }

    // Grappes : le 10 sept fusionne avec le 8 (même pixel à l'échelle de l'année),
    // le 22 sept reste distinct ; la faite et celle hors année sont ignorées.
    expect(annee.ponctuelles.map((g) => [g.date, g.taches.length])).toEqual([
      ['2026-09-08', 3],
      ['2026-09-22', 1],
    ])
    expect(annee.tempoCourt.map((t) => t.id)).toEqual(['h', 'i'])
    expect(annee.aujourdhuiPct).toBeCloseTo(pctDansAnnee(AUJOURDHUI))
  })

  test('une fenêtre qui chevauche l’an devient deux segments', () => {
    const annee = construireAnnee(
      [
        tache({
          id: 'd',
          titre: 'Déneiger',
          recurrence: {
            mode: 'Intervalle',
            intervalleJours: 3,
            fenetreDebutMois: 11,
            fenetreDebutJour: 15,
            fenetreFinMois: 3,
            fenetreFinJour: 31,
          },
        }),
      ],
      AUJOURDHUI,
    )

    const ligne = annee.lignes[0]
    if (ligne.type === 'fenetre') {
      expect(ligne.segments).toHaveLength(2)
      expect(ligne.segments[0].debut).toBe(0)
      expect(ligne.segments[1].fin).toBe(100)
      expect(ligne.enCours).toBe(false) // août est hors nov→mars
    } else {
      expect.unreachable('la fenêtre doit produire une ligne fenetre')
    }
  })
})
