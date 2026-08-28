import { describe, expect, test } from 'vitest'
import type { EnveloppeBudget, ResumeBudget } from '@/lib/api'
import {
  arrondirCents,
  bilanVentilation,
  construireFlux,
  construirePartition,
  couleursEnveloppes,
  dansDouzeMois,
  pctJauge,
  preRemplirVentilation,
  PALES,
} from '@/lib/budget-vues'

const AUJOURDHUI = '2026-08-27'

function enveloppe(
  partiel: Partial<EnveloppeBudget> & { id: string; nom: string; type: EnveloppeBudget['type'] },
): EnveloppeBudget {
  return {
    montantCible: null,
    dateCible: null,
    dateEffective: null,
    tacheId: null,
    titreTache: null,
    equipementId: null,
    nomEquipement: null,
    echeancier: null,
    statut: 'Active',
    solde: 0,
    provision: 0,
    enRetard: false,
    echeancierARenouveler: false,
    ...partiel,
  }
}

function resume(partiel: Partial<ResumeBudget>): ResumeBudget {
  return {
    compte: null,
    soldeCourant: 0,
    totalEnveloppes: 0,
    nonAffecte: 0,
    virementSuggere: 0,
    occurrenceVirementId: null,
    enveloppes: [],
    sorties: [],
    nbTransactionsNouvelles: 0,
    ...partiel,
  }
}

describe('couleursEnveloppes', () => {
  test('alternance des deux teintes du type, stable par enveloppe', () => {
    const couleurs = couleursEnveloppes([
      enveloppe({ id: 't1', nom: 'Taxes mun.', type: 'Taxes' }),
      enveloppe({ id: 't2', nom: 'Taxes scol.', type: 'Taxes' }),
      enveloppe({ id: 't3', nom: 'Taxes eau', type: 'Taxes' }),
      enveloppe({ id: 'e1', nom: 'Toiture', type: 'Equipement' }),
    ])

    expect(couleurs.get('t1')).toBe(PALES.Taxes[0])
    expect(couleurs.get('t2')).toBe(PALES.Taxes[1])
    // La troisième du même type reboucle sur la première teinte.
    expect(couleurs.get('t3')).toBe(PALES.Taxes[0])
    expect(couleurs.get('e1')).toBe(PALES.Equipement[0])
  })
})

describe('construirePartition', () => {
  test('cas normal : enveloppes positives + non affecté hachuré, pas de dépassement', () => {
    const partition = construirePartition(
      resume({
        soldeCourant: 1000,
        nonAffecte: 200,
        enveloppes: [
          enveloppe({ id: 'a', nom: 'Toiture', type: 'Equipement', solde: 600 }),
          enveloppe({ id: 'b', nom: 'Taxes', type: 'Taxes', solde: 200 }),
          enveloppe({ id: 'zero', nom: 'Vide', type: 'Projet', solde: 0 }),
        ],
      }),
    )

    // L'enveloppe à solde nul ne produit pas de segment.
    expect(partition.segments.map((s) => [s.id, s.pct])).toEqual([
      ['a', 60],
      ['b', 20],
      [null, 20],
    ])
    expect(partition.segments.at(-1)!.couleur).toBeNull() // non affecté = motif hachuré
    expect(partition.surAllocation).toBe(false)
    expect(partition.marqueurSoldePct).toBeNull()
    expect(partition.depassementPct).toBeNull()
  })

  test('sur-allocation avec enveloppe négative : le dépassement est le vrai trou', () => {
    // Positives 1000 $, Réserve à −200 $, solde 700 $ → non affecté −100 $.
    const partition = construirePartition(
      resume({
        soldeCourant: 700,
        totalEnveloppes: 800,
        nonAffecte: -100,
        enveloppes: [
          enveloppe({ id: 'a', nom: 'Toiture', type: 'Equipement', solde: 700 }),
          enveloppe({ id: 'b', nom: 'Taxes', type: 'Taxes', solde: 300 }),
          enveloppe({ id: 'r', nom: 'Réserve', type: 'Reserve', solde: -200 }),
        ],
      }),
    )

    expect(partition.surAllocation).toBe(true)
    expect(partition.segments.map((s) => [s.id, s.pct])).toEqual([
      ['a', 70],
      ['b', 30],
    ])
    expect(partition.marqueurSoldePct).toBe(70)
    // Le hachuré couvre 100 $ (le non affecté négatif), pas les 300 $ entre le
    // marqueur et le bout de la barre — c'est le bug de l'overlay surestimé.
    expect(partition.depassementPct).toBe(10)
  })

  test('aucun montant positif : barre vide', () => {
    const partition = construirePartition(
      resume({
        soldeCourant: -50,
        nonAffecte: -50,
        enveloppes: [enveloppe({ id: 'r', nom: 'Réserve', type: 'Reserve', solde: 0 })],
      }),
    )

    expect(partition.segments).toEqual([])
    expect(partition.marqueurSoldePct).toBeNull()
    expect(partition.depassementPct).toBeNull()
  })
})

describe('pctJauge', () => {
  test('bornée à [0, 100], null sans cible exploitable', () => {
    expect(pctJauge(50, 200)).toBe(25)
    expect(pctJauge(-80, 200)).toBe(0)
    expect(pctJauge(500, 200)).toBe(100)
    expect(pctJauge(50, null)).toBeNull()
    expect(pctJauge(50, 0)).toBeNull()
  })
})

describe('dansDouzeMois', () => {
  test('borne incluse, y compris depuis un 29 février', () => {
    expect(dansDouzeMois('2027-08-27', AUJOURDHUI)).toBe(true)
    expect(dansDouzeMois('2027-08-28', AUJOURDHUI)).toBe(false)
    // Année bissextile : la borne « 2029-02-29 » n'existe pas comme date, mais
    // la comparaison lexicale reste juste des deux côtés.
    expect(dansDouzeMois('2029-02-28', '2028-02-29')).toBe(true)
    expect(dansDouzeMois('2029-03-01', '2028-02-29')).toBe(false)
  })
})

describe('construireFlux', () => {
  const RESUME_FLUX = resume({
    virementSuggere: 303,
    enveloppes: [
      enveloppe({ id: 'mince', nom: 'Petit projet', type: 'Projet', provision: 3 }),
      enveloppe({ id: 'grosse', nom: 'Taxes', type: 'Taxes', provision: 300 }),
      enveloppe({ id: 'coussin', nom: 'Réserve', type: 'Reserve', provision: 0 }),
      enveloppe({ id: 'fermee', nom: 'Finie', type: 'Projet', provision: 50, statut: 'Fermee' }),
    ],
    sorties: [
      { date: '2026-09-01', nom: 'Taxes sept', montant: 1240, enveloppeId: 'grosse' },
      { date: '2027-03-01', nom: 'Taxes mars', montant: 1240, enveloppeId: 'grosse' },
      { date: '2033-01-01', nom: 'Taxes 2033', montant: 1300, enveloppeId: 'grosse' },
      { date: '2033-06-01', nom: 'Refaire le patio', montant: 8000, enveloppeId: 'coussin' },
    ],
  })

  /** Épaisseur d'un ruban à son arrivée (bas − haut de la tranche droite). */
  function epaisseurArrivee(path: string): number {
    const m = path.match(/,([\d.]+) L [\d.]+,([\d.]+)/)!
    return Number(m[2]) - Number(m[1])
  }

  test('nœuds empilés par provision décroissante, fermées exclues', () => {
    const flux = construireFlux(RESUME_FLUX, AUJOURDHUI)

    expect(flux.noeuds.map((n) => n.enveloppe.id)).toEqual(['grosse', 'mince', 'coussin'])
    // Grosse : épaisseur ∝ provision (300/303 × 150), nœud moulé dessus.
    const epaisseurGrosse = (300 / 303) * 150
    expect(flux.noeuds[0].h).toBeCloseTo(epaisseurGrosse + 10)
    // Mince et sans-provision : hauteur plancher de 46 px.
    expect(flux.noeuds[1].h).toBe(46)
    expect(flux.noeuds[2].h).toBe(46)
    // Empilement sans chevauchement : chaque nœud commence 14 px sous le précédent.
    expect(flux.noeuds[1].y).toBeCloseTo(flux.noeuds[0].y + flux.noeuds[0].h + 14)
    expect(flux.noeuds[2].y).toBeCloseTo(flux.noeuds[1].y + flux.noeuds[1].h + 14)
    expect(flux.noeuds.map((n) => n.sansFlux)).toEqual([false, false, true])
    expect(flux.hauteur).toBeGreaterThan(flux.noeuds[2].y + flux.noeuds[2].h)
  })

  test('rubans d’entrée : épaisseur plancher 6 px, empilés à la source', () => {
    const flux = construireFlux(RESUME_FLUX, AUJOURDHUI)

    // Deux rubans (le coussin sans provision n'en a pas).
    expect(flux.rubansEntree).toHaveLength(2)
    expect(flux.etiquettes.map((e) => e.texte)).toEqual(['+ 300 $', '+ 3 $'])
    // La provision de 3 $ (0,99 % du flux) reste visible : plancher 6 px.
    expect(epaisseurArrivee(flux.rubansEntree[0].path)).toBeCloseTo((300 / 303) * 150)
    expect(epaisseurArrivee(flux.rubansEntree[1].path)).toBeCloseTo(6)
    // La source englobe l'empilement des deux épaisseurs.
    expect(flux.source.h).toBeCloseTo((300 / 303) * 150 + 6 + 24)
  })

  test('sorties : 12 prochains mois agrégées par enveloppe, lointaines en pointillé', () => {
    const flux = construireFlux(RESUME_FLUX, AUJOURDHUI)

    // Taxes : deux versements < 12 mois → une boîte agrégée ; celui de 2033 est
    // absorbé (l'enveloppe a déjà des sorties proches, pas de pointillé en plus).
    expect(flux.sorties).toHaveLength(1)
    expect(flux.sorties[0]).toMatchObject({
      date: '2026-09-01',
      montant: 1240,
      annuel: 2480,
      detail: '2 versements sur 12 mois',
    })
    expect(flux.rubansSortie).toHaveLength(1)
    // Le patio 2033 (réserve sans sortie proche) devient la note lointaine.
    expect(flux.lointains).toHaveLength(1)
    expect(flux.lointains[0].sortie.nom).toBe('Refaire le patio')
  })
})

describe('ventilation au cent', () => {
  test('arrondirCents neutralise la dérive flottante et le −0', () => {
    expect(arrondirCents(0.1 + 0.2)).toBe(0.3)
    expect(arrondirCents(338.00000000000006 - 338)).toBe(0)
    expect(Object.is(arrondirCents(-1e-13), 0)).toBe(true) // jamais « −0,00 $ »
    expect(arrondirCents(-0.01)).toBe(-0.01)
  })

  test('preRemplirVentilation : provisions écrêtées au dépôt, 2 décimales max', () => {
    const enveloppes = [
      { id: 'petit', provision: 28 },
      { id: 'tiers', provision: 1000 / 3 }, // 333.333…
      { id: 'sans', provision: 0 },
    ]

    // Dépôt couvrant : chaque part est arrondie au cent, jamais 14 décimales.
    expect(preRemplirVentilation(400, enveloppes)).toEqual({
      tiers: '333.33',
      petit: '28',
      sans: '',
    })
    // Dépôt trop petit : servi par provision décroissante, le reste écrêté.
    expect(preRemplirVentilation(350, enveloppes)).toEqual({
      tiers: '333.33',
      petit: '16.67',
      sans: '',
    })
  })

  test('bilanVentilation : un dépôt exactement réparti donne reste = 0', () => {
    // 3 × 112.67 = 338.00999… en flottant si on ne borne pas au cent.
    expect(bilanVentilation(338, [110.1, 110.1, 117.8])).toEqual({ total: 338, reste: 0 })
    // Un vrai dépassement d'un cent reste détecté.
    expect(bilanVentilation(338, [110.1, 110.1, 117.81])).toEqual({
      total: 338.01,
      reste: -0.01,
    })
    expect(bilanVentilation(338, [300])).toEqual({ total: 300, reste: 38 })
  })
})
