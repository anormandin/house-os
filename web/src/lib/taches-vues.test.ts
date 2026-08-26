import { describe, expect, test } from 'vitest'
import type { Recurrence, TacheResume } from '@/lib/api'
import {
  chipsRecurrence,
  construireRuban,
  diffJours,
  grouperParRythme,
  lundiDe,
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

describe('construireRuban', () => {
  test('domaine du 1ᵉʳ du mois courant au 31 décembre, chaque mode à sa place', () => {
    const ruban = construireRuban(
      [
        tache({ id: 'm', titre: 'Filtre', recurrence: MENSUELLE }),
        tache({ id: 't', titre: 'Tondre', recurrence: TONTE, echeance: '2026-08-28' }),
        tache({ id: 'a', titre: 'Ramoner', recurrence: ANNUELLE, echeance: '2026-09-15' }),
        tache({ id: 'h', titre: 'Draps', recurrence: HEBDO }),
        tache({ id: 'i', titre: 'Litière', recurrence: { mode: 'Intervalle', intervalleJours: 2 } }),
        tache({
          id: 'long',
          titre: 'Bassin',
          recurrence: { mode: 'Intervalle', intervalleJours: 182 },
          echeance: '2026-09-12',
        }),
      ],
      AUJOURDHUI,
    )

    expect(ruban.debutDomaine).toBe('2026-08-01')
    expect(ruban.jours).toBe(153) // août → décembre
    expect(ruban.aujourdhuiOffset).toBe(25)
    expect(ruban.moisDomaine.map((m) => m.libelle)).toEqual(['Août', 'Sept', 'Oct', 'Nov', 'Déc'])
    expect(ruban.moisDomaine[1]).toEqual({ libelle: 'Sept', offset: 31 })

    // Ordre : mensuelle, fenêtre, annuelle, long intervalle ; tempo court pour le reste.
    expect(ruban.lignes.map((l) => l.type)).toEqual([
      'mensuelle',
      'fenetre',
      'annuelle',
      'intervalle',
    ])
    expect(ruban.tempoCourt.map((t) => t.id)).toEqual(['h', 'i'])

    const [mensuelle, fenetre, annuelle, long] = ruban.lignes
    // Mensuelle : 5 points (août→déc), le 1ᵉʳ août déjà passé est estompé.
    if (mensuelle.type === 'mensuelle') {
      expect(mensuelle.points).toHaveLength(5)
      expect(mensuelle.points[0]).toEqual({ offset: 0, libelle: 'fait', passe: true })
      expect(mensuelle.points[1].libelle).toBe('mar 1ᵉʳ')
    }
    // Fenêtre mai→oct : clampée au domaine (débute à 0), en cours le 26 août.
    if (fenetre.type === 'fenetre') {
      expect(fenetre.segments).toEqual([{ debut: 0, fin: diffJours('2026-10-31', '2026-08-01') + 1 }])
      expect(fenetre.enCours).toBe(true)
      expect(fenetre.libelle).toBe('☀ fenêtre du 1ᵉʳ mai au 31 oct')
      expect(fenetre.vise).toEqual({ offset: 27, libelle: 'visé · ven 28 août', passe: false })
    }
    if (annuelle.type === 'annuelle') {
      expect(annuelle.point).toEqual({ offset: 45, libelle: 'mar 15 sept', passe: false })
    }
    // Long intervalle : pastille au 12 sept, la suivante (mars 2027) déborde → note.
    if (long.type === 'intervalle') {
      expect(long.point?.offset).toBe(42)
      expect(long.note).toContain('2027')
    }
  })

  test('grappes par jour avec voie basse pour les voisines, semaines et retards', () => {
    const ruban = construireRuban(
      [
        tache({ id: 'r1', titre: 'Ménage frigo', echeance: '2026-08-23' }),
        tache({ id: 'p1', titre: 'Notaire', echeance: '2026-08-31' }),
        tache({ id: 'p2', titre: 'SQCA', echeance: '2026-09-01' }),
        tache({ id: 'p3', titre: 'Hydro', echeance: '2026-09-08' }),
        tache({ id: 'p4', titre: 'Postes', echeance: '2026-09-08' }),
        tache({ id: 'fait', titre: 'Déjà fait', echeance: '2026-09-08', completee: true }),
        tache({ id: 'sans', titre: 'Sans échéance' }),
      ],
      AUJOURDHUI,
    )

    // Le 1ᵉʳ sept est à 1 jour du 31 août : voie basse ; le mois change → libellé avec mois.
    expect(ruban.grappes.map((g) => [g.date, g.taches.length, g.voie, g.libelle])).toEqual([
      ['2026-08-23', 1, 0, '23 août'], // la première grappe porte son mois
      ['2026-08-31', 1, 0, '31'],
      ['2026-09-01', 1, 1, '1 sept'],
      ['2026-09-08', 2, 0, '8'],
    ])

    // Bande : le retard sort des semaines ; la faite et la sans-échéance sont ignorées.
    expect(ruban.enRetard.map((t) => t.id)).toEqual(['r1'])
    expect(ruban.semaines.map((s) => [s.lundi, s.taches.length])).toEqual([
      ['2026-08-31', 2],
      ['2026-09-07', 2],
    ])
    expect(ruban.semaines[0].libelle).toBe('Semaine du 31 août')
    expect(ruban.totalPonctuelles).toBe(5)
  })

  test('une fenêtre qui chevauche l’an est clampée au domaine en deux morceaux visibles', () => {
    const ruban = construireRuban(
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

    const ligne = ruban.lignes[0]
    if (ligne.type === 'fenetre') {
      // Le morceau janv→mars est avant le domaine (août) : seul nov 15 → déc reste.
      expect(ligne.segments).toEqual([
        { debut: diffJours('2026-11-15', '2026-08-01'), fin: ruban.jours },
      ])
      expect(ligne.enCours).toBe(false) // août est hors nov→mars
    } else {
      expect.unreachable('la fenêtre doit produire une ligne fenetre')
    }
  })
})

describe('lundiDe', () => {
  test('retourne le lundi local de la semaine', () => {
    expect(lundiDe('2026-08-26')).toBe('2026-08-24') // mercredi → lundi
    expect(lundiDe('2026-08-24')).toBe('2026-08-24')
    expect(lundiDe('2026-08-30')).toBe('2026-08-24') // dimanche appartient à la semaine entamée
  })
})
