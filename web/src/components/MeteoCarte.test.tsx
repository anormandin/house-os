import { render, screen } from '@testing-library/react'
import { expect, test } from 'vitest'
import MeteoCarte from '@/components/MeteoCarte'
import type { JourMeteo, Meteo } from '@/lib/api'

function jour(champs: Partial<JourMeteo> = {}): JourMeteo {
  return {
    date: '2026-08-25',
    tempMin: 12,
    tempMax: 24,
    precipitationMm: 0,
    probabilitePrecipitation: 10,
    codeMeteo: 1,
    ...champs,
  }
}

function meteo(champs: Partial<Meteo> = {}): Meteo {
  return {
    misAJourLe: '2026-08-25T12:00:00Z',
    maintenant: { temperatureC: 21.4, codeMeteo: 2 },
    jours: [jour()],
    verdicts: [],
    ...champs,
  }
}

test('sans données, la carte ne rend rien', () => {
  const { container } = render(<MeteoCarte meteo={undefined} />)
  expect(container).toBeEmptyDOMElement()

  const { container: vide } = render(<MeteoCarte meteo={meteo({ jours: [] })} />)
  expect(vide).toBeEmptyDOMElement()
})

test('sans ligne horaire, la carte retombe sur la température du jour', () => {
  render(<MeteoCarte meteo={meteo({ maintenant: null, jours: [jour({ tempMax: 19.6 })] })} />)

  expect(screen.getByText('20°')).toBeInTheDocument()
})

test('un verdict défavorable « Être dehors » affiche la pastille rester en dedans', () => {
  render(<MeteoCarte meteo={meteo({
    verdicts: [
      { regle: 'Être dehors', etat: 'Defavorable', raison: 'De la bruine toute la journée.' },
      { regle: 'Tondre', etat: 'Passable', raison: 'Gazon humide.' },
    ],
  })} />)

  expect(screen.getByText('Une journée pour rester en dedans')).toBeInTheDocument()
  // Passable reste muet : aucune pastille pour la tonte.
  expect(screen.queryByText(/tondre/i)).not.toBeInTheDocument()
})

test("un verdict Bon d'une règle inconnue est ignoré sans planter", () => {
  render(<MeteoCarte meteo={meteo({
    verdicts: [{ regle: 'Astiquer le patio', etat: 'Bon', raison: 'Sec.' }],
  })} />)

  expect(screen.getByText('Dehors')).toBeInTheDocument()
})

test('la pastille de pluie apparaît à 30 %, pas à 29 %', () => {
  const { unmount } = render(<MeteoCarte meteo={meteo({
    jours: [jour({ probabilitePrecipitation: 30 })],
  })} />)
  expect(screen.getByText('30 % de pluie')).toBeInTheDocument()
  unmount()

  render(<MeteoCarte meteo={meteo({ jours: [jour({ probabilitePrecipitation: 29 })] })} />)
  expect(screen.queryByText(/% de pluie/)).not.toBeInTheDocument()
})
