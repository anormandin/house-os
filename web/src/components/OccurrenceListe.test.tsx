import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { expect, test } from 'vitest'
import OccurrenceListe from '@/components/OccurrenceListe'
import { dateLocaleIso, type Occurrence } from '@/lib/api'
import { dateCourte } from '@/lib/format'
import { rendre } from '@/test/rendre'
import { ALAIN, serveur } from '@/test/serveur-msw'

function occurrence(champs: Partial<Occurrence>): Occurrence {
  return {
    id: 'o-1',
    tacheId: 't-1',
    titre: 'Balayer',
    description: null,
    echeance: dateLocaleIso(),
    statut: 'EnAttente',
    assigneA: null,
    completeePar: null,
    completeeLe: null,
    notes: null,
    zoneId: null,
    equipementId: null,
    modeRecurrence: 'Ponctuelle',
    ...champs,
  }
}

function hier(): string {
  const date = new Date()
  date.setDate(date.getDate() - 1)
  return dateLocaleIso(date)
}

test('la description reste visible, multi-lignes, même en retard', () => {
  rendre(
    <OccurrenceListe
      occurrences={[occurrence({ echeance: hier(), description: 'Ligne A\nLigne B' })]}
      vide="rien"
    />,
  )

  expect(screen.getByText('depuis hier — on s’en occupe?')).toBeInTheDocument()
  const description = screen.getByText(/Ligne A/)
  expect(description).toHaveTextContent('Ligne B')
  expect(description).toHaveClass('whitespace-pre-line')
})

test('cocher une occurrence appelle la complétion', async () => {
  let complete = false
  serveur.use(
    http.post('/api/occurrences/o-1/completer', () => {
      complete = true
      return new HttpResponse(null, { status: 204 })
    }),
  )
  rendre(<OccurrenceListe occurrences={[occurrence({})]} vide="rien" />)

  await userEvent.click(screen.getByRole('button', { name: 'Compléter Balayer' }))

  await waitFor(() => expect(complete).toBe(true))
})

test('une rangée faite accepte une note post-hoc', async () => {
  let noteEnvoyee: unknown = null
  serveur.use(
    http.put('/api/occurrences/o-1/notes', async ({ request }) => {
      noteEnvoyee = await request.json()
      return new HttpResponse(null, { status: 204 })
    }),
  )
  rendre(
    <OccurrenceListe
      occurrences={[
        occurrence({
          statut: 'Completee',
          completeePar: ALAIN,
          completeeLe: new Date().toISOString(),
        }),
      ]}
      vide="rien"
    />,
  )

  await userEvent.click(screen.getByRole('button', { name: 'Ajouter une note à Balayer' }))
  await userEvent.type(screen.getByPlaceholderText(/Une note/), 'coût : 12 $[Enter]')

  await waitFor(() => expect(noteEnvoyee).toEqual({ notes: 'coût : 12 $' }))
})

test("enregistrer une note n'envoie qu'une seule requête (Enter puis blur)", async () => {
  let requetes = 0
  serveur.use(
    http.put('/api/occurrences/o-1/notes', async () => {
      requetes++
      await new Promise((resoudre) => setTimeout(resoudre, 30))
      return new HttpResponse(null, { status: 204 })
    }),
  )
  rendre(
    <OccurrenceListe
      occurrences={[
        occurrence({
          statut: 'Completee',
          completeePar: ALAIN,
          completeeLe: new Date().toISOString(),
        }),
      ]}
      vide="rien"
    />,
  )

  await userEvent.click(screen.getByRole('button', { name: 'Ajouter une note à Balayer' }))
  // Enter (submit) déclenche la mutation ; le blur qui suit repasse par sauverNote.
  await userEvent.type(screen.getByPlaceholderText(/Une note/), 'coût : 12 $[Enter]')
  await userEvent.tab()

  await waitFor(() => expect(requetes).toBe(1))
})

test("une complétion d'hier affiche la date, pas l'heure", () => {
  const hierIso = hier()
  const instant = new Date(`${hierIso}T23:50:00`)
  rendre(
    <OccurrenceListe
      occurrences={[
        occurrence({
          statut: 'Completee',
          completeePar: ALAIN,
          completeeLe: instant.toISOString(),
        }),
      ]}
      vide="rien"
    />,
  )

  // À 00 h 10, « bravo Alain ✓ 23 h 50 » mentirait sur la journée.
  expect(screen.getByText(new RegExp(`✓ ${dateCourte(hierIso)}`))).toBeInTheDocument()
})

test('cliquer le titre ouvre la modification quand onModifier est fourni', async () => {
  let tacheModifiee: string | null = null
  rendre(
    <OccurrenceListe
      occurrences={[occurrence({})]}
      vide="rien"
      onModifier={(tacheId) => {
        tacheModifiee = tacheId
      }}
    />,
  )

  await userEvent.click(screen.getByText('Balayer'))

  expect(tacheModifiee).toBe('t-1')
})
