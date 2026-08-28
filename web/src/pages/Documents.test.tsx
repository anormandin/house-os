import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { expect, test } from 'vitest'
import Documents from '@/pages/Documents'
import type { Document } from '@/lib/api'
import { rendre } from '@/test/rendre'
import { serveur } from '@/test/serveur-msw'

function document(partiel: Partial<Document> & { id: string; titre: string }): Document {
  return {
    categorie: 'Autre',
    equipementId: null,
    nomEquipement: null,
    zoneId: null,
    nomZone: null,
    dossier: null,
    notes: null,
    dateDocument: null,
    echeance: null,
    nomFichier: `${partiel.id}.pdf`,
    typeMime: 'application/pdf',
    taille: 1024,
    creeLe: '2026-08-01T12:00:00Z',
    ...partiel,
  }
}

/** Chaque champ non nul — le test « rien ne se perd » ne pardonne aucun oubli. */
const DOCUMENT_COMPLET = document({
  id: 'd-complet',
  titre: 'Promesse d’achat',
  categorie: 'Contrat',
  zoneId: 'z-bureau',
  nomZone: 'Bureau',
  dossier: '17 rue de la Colline',
  notes: 'Signée chez le notaire',
  dateDocument: '2026-03-01',
  echeance: '2030-01-01',
})

function servirDocuments(liste: Document[]) {
  serveur.use(http.get('/api/documents', () => HttpResponse.json(liste)))
}

test('une erreur de chargement s’affiche comme telle — jamais comme un classeur vide', async () => {
  serveur.use(
    http.get('/api/documents', () => new HttpResponse(null, { status: 500 }), { once: true }),
  )
  rendre(<Documents />)

  expect(await screen.findByText(/Impossible de charger les documents/)).toBeInTheDocument()
  expect(screen.queryByText(/Aucun document encore/)).not.toBeInTheDocument()

  // Réessayer repart la requête — le handler par défaut répond cette fois.
  await userEvent.click(screen.getByRole('button', { name: 'Réessayer' }))
  expect(await screen.findByText('Rapport d’inspection')).toBeInTheDocument()
})

test('le tiroir charge tous les champs et n’en perd aucun à l’enregistrement', async () => {
  servirDocuments([DOCUMENT_COMPLET])
  let corpsEnvoye: unknown = null
  serveur.use(
    http.put('/api/documents/:id', async ({ request }) => {
      corpsEnvoye = await request.json()
      return new HttpResponse(null, { status: 204 })
    }),
  )
  rendre(<Documents />)

  await userEvent.click(await screen.findByText('Promesse d’achat'))
  await waitFor(() =>
    expect(screen.getByLabelText('Titre')).toHaveValue('Promesse d’achat'))
  expect(screen.getByLabelText('Catégorie')).toHaveValue('Contrat')
  expect(screen.getByLabelText('Dossier')).toHaveValue('17 rue de la Colline')
  expect(screen.getByLabelText('Pièce liée')).toHaveValue('z-bureau')
  expect(screen.getByLabelText('Notes')).toHaveValue('Signée chez le notaire')

  await userEvent.click(screen.getByRole('button', { name: 'Enregistrer' }))

  await waitFor(() => expect(corpsEnvoye).not.toBeNull())
  expect(corpsEnvoye).toEqual({
    titre: 'Promesse d’achat',
    categorie: 'Contrat',
    equipementId: null,
    zoneId: 'z-bureau',
    dossier: '17 rue de la Colline',
    notes: 'Signée chez le notaire',
    dateDocument: '2026-03-01',
    echeance: '2030-01-01',
  })
})

test('les facettes se combinent en ET et se retirent par jeton', async () => {
  servirDocuments([
    document({ id: 'd-1', titre: 'Promesse d’achat', categorie: 'Contrat', dossier: '17 rue de la Colline' }),
    document({ id: 'd-2', titre: 'Offre acceptée', categorie: 'Contrat', dossier: '428 rue Fraser' }),
    document({ id: 'd-3', titre: 'Facture toiture', categorie: 'Facture', dossier: '17 rue de la Colline' }),
  ])
  rendre(<Documents />)
  await screen.findByText('Facture toiture')

  // Facette catégorie : seuls les contrats restent.
  await userEvent.click(screen.getByRole('button', { name: 'Filtrer : Contrat' }))
  expect(screen.queryByText('Facture toiture')).not.toBeInTheDocument()
  expect(screen.getByText('Offre acceptée')).toBeInTheDocument()

  // + facette dossier : intersection.
  await userEvent.click(screen.getByRole('button', { name: 'Filtrer : 17 rue de la Colline' }))
  expect(screen.queryByText('Offre acceptée')).not.toBeInTheDocument()
  expect(screen.getByText('Promesse d’achat')).toBeInTheDocument()
  expect(screen.getByText('1 document')).toBeInTheDocument()

  // Retirer le jeton Contrat : le dossier reste seul actif.
  await userEvent.click(screen.getByRole('button', { name: 'Retirer le filtre Contrat' }))
  expect(screen.getByText('Facture toiture')).toBeInTheDocument()
  expect(screen.queryByText('Offre acceptée')).not.toBeInTheDocument()
})

test('le tri par titre s’inverse au deuxième clic', async () => {
  servirDocuments([
    document({ id: 'd-a', titre: 'Attestation', dateDocument: '2026-01-01' }),
    document({ id: 'd-z', titre: 'Zonage', dateDocument: '2026-06-01' }),
  ])
  rendre(<Documents />)
  await screen.findByText('Zonage')

  const titres = () =>
    screen.getAllByRole('row').slice(1).map((rangee) => within(rangee).getAllByRole('cell')[0].textContent)

  // Défaut : Daté du ↓ — Zonage (juin) d'abord.
  expect(titres()[0]).toContain('Zonage')

  await userEvent.click(screen.getByRole('button', { name: /Titre/ }))
  expect(titres()[0]).toContain('Attestation')

  await userEvent.click(screen.getByRole('button', { name: /Titre/ }))
  expect(titres()[0]).toContain('Zonage')
})

test('la pagination coupe à 25 et navigue', async () => {
  servirDocuments(Array.from({ length: 30 }, (_, i) =>
    document({
      id: `d-${i + 1}`,
      titre: `Doc ${String(i + 1).padStart(2, '0')}`,
      dateDocument: `2026-01-${String(i + 1).padStart(2, '0')}`,
    })))
  rendre(<Documents />)
  await screen.findByText('Doc 30')

  // 25 rangées + l'en-tête ; le plus récent (Doc 30) en premier.
  expect(screen.getAllByRole('row')).toHaveLength(26)
  expect(screen.getByText('1–25 sur 30 documents')).toBeInTheDocument()
  expect(screen.queryByText('Doc 05')).not.toBeInTheDocument()

  await userEvent.click(screen.getByRole('button', { name: 'Page suivante' }))
  expect(screen.getAllByRole('row')).toHaveLength(6)
  expect(screen.getByText('26–30 sur 30 documents')).toBeInTheDocument()
  expect(screen.getByText('Doc 05')).toBeInTheDocument()
})
