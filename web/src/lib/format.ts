// Typographie québécoise (OQLF) — voir vault/Inspiration UI/Typographie Québécoise.md :
// dates en minuscules (« samedi 23 août »), heures « 14 h 10 », pas de format punitif.

export function dateLongue(date = new Date()): string {
  return date.toLocaleDateString('fr-CA', { weekday: 'long', day: 'numeric', month: 'long' })
}

export function jourCourt(dateIso: string): string {
  const date = new Date(`${dateIso}T00:00:00`)
  return date.toLocaleDateString('fr-CA', { weekday: 'short' }).replace('.', '')
}

export function dateCourte(dateIso: string): string {
  const date = new Date(`${dateIso}T00:00:00`)
  return date.toLocaleDateString('fr-CA', { day: 'numeric', month: 'long' })
}

/** « 22 septembre » cette année, « 22 septembre 2055 » sinon. */
export function dateLisible(dateIso: string): string {
  const date = new Date(`${dateIso}T00:00:00`)
  const options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' }
  if (date.getFullYear() !== new Date().getFullYear()) {
    options.year = 'numeric'
  }
  return date.toLocaleDateString('fr-CA', options)
}

/** « 14 h 10 » — jamais « 14:10 ». */
export function heureQuebec(instant: string): string {
  const date = new Date(instant)
  const heures = date.getHours()
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${heures} h ${minutes}`
}

/** « 12 480 $ » (entier par défaut) ou « −84,12 $ » avec cents — format québécois. */
export function dollars(montant: number, cents = false): string {
  return new Intl.NumberFormat('fr-CA', {
    style: 'currency',
    currency: 'CAD',
    currencyDisplay: 'narrowSymbol',
    minimumFractionDigits: cents ? 2 : 0,
    maximumFractionDigits: cents ? 2 : 0,
  }).format(montant)
}

/** Nombre de dodos (nuits) entre aujourd'hui et une date locale. */
export function dodosAvant(dateIso: string): number {
  const [annee, mois, jour] = dateIso.split('-').map(Number)
  const cible = new Date(annee, mois - 1, jour)
  const maintenant = new Date()
  const aujourdhui = new Date(maintenant.getFullYear(), maintenant.getMonth(), maintenant.getDate())
  return Math.round((cible.getTime() - aujourdhui.getTime()) / 86_400_000)
}

/** Bornes d'instants de la journée locale [minuit, minuit+1j) pour le filtre « faites ». */
export function bornesJourneeLocale(date = new Date()): { de: string; a: string } {
  const debut = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const fin = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 1)
  return { de: debut.toISOString(), a: fin.toISOString() }
}
