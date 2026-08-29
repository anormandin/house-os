import { QueryClient } from '@tanstack/react-query'
import { expect, test, vi } from 'vitest'
import {
  auteurPour,
  cleFusion,
  creerInvalidateurCoalesce,
  doitAnnoncer,
  invaliderPourModule,
  messagePour,
  type EvenementSynchro,
} from '@/lib/synchro'

const MOI = '11111111-1111-1111-1111-111111111111'
const AUTRE = '22222222-2222-2222-2222-222222222222'

function clesInvalidees(module: string): string[] {
  const client = new QueryClient()
  const espion = vi.spyOn(client, 'invalidateQueries')
  invaliderPourModule(client, module)
  return espion.mock.calls.map((appel) => String(appel[0]?.queryKey?.[0]))
}

test('le module taches invalide aussi occurrences, journal et budget', () => {
  // Une complétion touche la console, les listes, le bilan hebdo et la case virement.
  expect(clesInvalidees('taches')).toEqual(
    expect.arrayContaining(['taches', 'occurrences', 'journal', 'budget', 'tache']),
  )
})

test('le module budget invalide ses trois racines', () => {
  expect(clesInvalidees('budget')).toEqual(
    expect.arrayContaining(['budget', 'budget-transactions', 'budget-enveloppe']),
  )
})

test('le module flux-externes invalide aussi les évènements', () => {
  expect(clesInvalidees('flux-externes')).toEqual(
    expect.arrayContaining(['flux-externes', 'evenements-externes']),
  )
})

test('un module inconnu n’invalide rien plutôt que de lever', () => {
  expect(clesInvalidees('module-du-futur')).toEqual([])
})

test('une rafale de modules ne déclenche qu’un tour d’invalidation', () => {
  vi.useFakeTimers()
  const client = new QueryClient()
  const espion = vi.spyOn(client, 'invalidateQueries')
  const invalidateur = creerInvalidateurCoalesce(client, 300)

  invalidateur.pousser('meteo')
  invalidateur.pousser('meteo')
  invalidateur.pousser('meteo')
  expect(espion).not.toHaveBeenCalled()

  vi.advanceTimersByTime(300)
  expect(espion.mock.calls.filter((a) => a[0]?.queryKey?.[0] === 'meteo')).toHaveLength(1)
  vi.useRealTimers()
})

test('arrêter l’invalidateur annule la rafale en attente', () => {
  vi.useFakeTimers()
  const client = new QueryClient()
  const espion = vi.spyOn(client, 'invalidateQueries')
  const invalidateur = creerInvalidateurCoalesce(client, 300)

  invalidateur.pousser('meteo')
  invalidateur.arreter()
  vi.advanceTimersByTime(300)

  expect(espion).not.toHaveBeenCalled()
  vi.useRealTimers()
})

test('un évènement grossier ne s’annonce jamais', () => {
  expect(doitAnnoncer({ module: 'meteo' }, MOI)).toBe(false)
})

test('mes propres gestes web ne s’annoncent pas', () => {
  const evenement: EvenementSynchro = {
    module: 'taches', genre: 'occurrence.completee', source: 'web', acteurId: MOI,
  }
  expect(doitAnnoncer(evenement, MOI)).toBe(false)
})

test('le geste de l’autre s’annonce', () => {
  const evenement: EvenementSynchro = {
    module: 'taches', genre: 'occurrence.completee', source: 'web', acteurId: AUTRE,
  }
  expect(doitAnnoncer(evenement, MOI)).toBe(true)
})

test('une écriture MCP s’annonce même quand elle agit en mon nom', () => {
  // AgirComme impersonne un vrai compte : sans la règle sur la source, une création
  // par Claude « au nom d'Alain » resterait invisible dans l'onglet d'Alain.
  const evenement: EvenementSynchro = {
    module: 'taches', genre: 'taches.creees', source: 'mcp', acteurId: MOI,
  }
  expect(doitAnnoncer(evenement, MOI)).toBe(true)
})

test('le message nomme la tâche au singulier et le compte au pluriel', () => {
  const base: EvenementSynchro = {
    module: 'taches', genre: 'occurrence.completee', source: 'web', acteurNom: 'Ariane',
  }
  expect(messagePour({ ...base, libelle: 'Litière' })).toBe('Ariane a complété « Litière »')
  expect(messagePour({ ...base, libelle: 'Litière', nombre: 3 })).toBe('Ariane a complété 3 tâches')
})

test('le message MCP nomme Claude et la personne au nom de qui il agit', () => {
  const evenement: EvenementSynchro = {
    module: 'taches', genre: 'taches.creees', source: 'mcp', acteurNom: 'Alain', nombre: 10,
  }
  expect(messagePour(evenement)).toBe("Claude (au nom d'Alain) a créé 10 tâches")
})

test('la clé de fusion sépare genre, acteur et source', () => {
  const a: EvenementSynchro = { module: 'taches', genre: 'occurrence.completee', source: 'web', acteurId: MOI }
  expect(cleFusion(a)).toBe(cleFusion({ ...a, libelle: 'autre titre' }))
  expect(cleFusion(a)).not.toBe(cleFusion({ ...a, acteurId: AUTRE }))
  expect(cleFusion(a)).not.toBe(cleFusion({ ...a, source: 'mcp' }))
})

test('l’auteur du toast distingue l’agent MCP d’un membre du foyer', () => {
  const base: EvenementSynchro = { module: 'taches', genre: 'occurrence.completee', acteurNom: 'Ariane' }
  expect(auteurPour({ ...base, source: 'web' })).toEqual({ nom: 'Ariane' })
  expect(auteurPour({ ...base, source: 'mcp' })).toEqual({ nom: 'Ariane', estClaude: true })
  expect(auteurPour({ ...base, source: 'web', acteurNom: null })).toEqual({ nom: null })
})
