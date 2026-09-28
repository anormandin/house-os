import { describe, expect, it } from 'vitest'
import { correspond, normaliser } from './recherche'

describe('normaliser', () => {
  it('retire accents et majuscules', () => {
    expect(normaliser('Corvée DÉNEIGEMENT')).toBe('corvee deneigement')
  })
})

describe('correspond', () => {
  it('un terme vide correspond à tout', () => {
    expect(correspond('   ', ['quoi que ce soit'])).toBe(true)
  })

  it('ignore les accents dans les deux sens', () => {
    expect(correspond('deneigement', ['Facture déneigement 2026'])).toBe(true)
    expect(correspond('déneigement', ['Facture deneigement 2026'])).toBe(true)
  })

  it('exige chaque mot, dans n’importe quel ordre, sur n’importe quel champ', () => {
    expect(correspond('jura manuel', ['Manuel — machine à café', 'Jura'])).toBe(true)
    expect(correspond('jura facture', ['Manuel — machine à café', 'Jura'])).toBe(false)
  })

  it('ignore les champs absents', () => {
    expect(correspond('fournaise', [null, undefined, 'Fournaise centrale'])).toBe(true)
  })
})
