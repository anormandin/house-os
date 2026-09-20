import { expect, test } from 'vitest'
import { libelleMeteo, meteoEnMots, pastillesMeteo } from '@/lib/meteo-vues'

test('les codes WMO se disent en mots, dans le même découpage que les icônes', () => {
  expect(libelleMeteo(0)).toBe('Dégagé')
  expect(libelleMeteo(1)).toBe('Éclaircies')
  expect(libelleMeteo(2)).toBe('Passages nuageux')
  expect(libelleMeteo(3)).toBe('Nuageux')
  expect(libelleMeteo(45)).toBe('Brouillard')
  expect(libelleMeteo(53)).toBe('Bruine')
  expect(libelleMeteo(61)).toBe('Pluie')
  expect(libelleMeteo(73)).toBe('Neige')
  expect(libelleMeteo(81)).toBe('Averses')
  expect(libelleMeteo(86)).toBe('Averses de neige')
  expect(libelleMeteo(95)).toBe('Orages')
})

test('la météo de la dateline ne dit un maximum que s\'il reste à venir', () => {
  expect(
    meteoEnMots({ codeMeteo: 3, temperatureC: -1.2, tempMax: 4.4, probabilitePrecipitation: 10 }),
  ).toBe('Nuageux · −1 °C · max 4')
  // Fin d'après-midi : le maximum est déjà passé, l'annoncer serait du bruit.
  expect(
    meteoEnMots({ codeMeteo: 0, temperatureC: 19, tempMax: 19, probabilitePrecipitation: 0 }),
  ).toBe('Dégagé · 19 °C')
  // Au-dessus de 30 %, la pluie se dit — le même seuil que partout ailleurs.
  expect(
    meteoEnMots({ codeMeteo: 61, temperatureC: 8, tempMax: 12, probabilitePrecipitation: 70 }),
  ).toBe('Pluie · 8 °C · max 12 · 70 % de pluie')
  expect(
    meteoEnMots({ codeMeteo: 61, temperatureC: 8, tempMax: 12, probabilitePrecipitation: 29 }),
  ).toBe('Pluie · 8 °C · max 12')
})

test('les verdicts qui ne disent rien ne font pas de pastille', () => {
  expect(pastillesMeteo([{ regle: 'Tondre', etat: 'Defavorable', raison: 'Trop humide' }])).toEqual([])
  expect(pastillesMeteo([{ regle: 'Tondre', etat: 'Bon', raison: 'Sec et doux' }])).toEqual([
    { regle: 'Tondre', texte: 'Bonne journée pour tondre', raison: 'Sec et doux', enDedans: false },
  ])
})
