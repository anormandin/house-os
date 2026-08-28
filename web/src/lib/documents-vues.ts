import { dateLocaleIso, type CategorieDocument, type Document } from '@/lib/api'

export const LIBELLES_CATEGORIE: Record<CategorieDocument, string> = {
  Manuel: 'Manuel',
  Photo: 'Photo',
  Assurance: 'Assurance',
  Facture: 'Facture',
  Garantie: 'Garantie',
  Contrat: 'Contrat',
  PlanPermis: 'Plan & permis',
  ImpotsTaxes: 'Impôts & taxes',
  Autre: 'Autre',
}

/** Facette « documents sans dossier » — sentinelle distincte de tout vrai dossier
 * (le préfixe ␀ n'est pas saisissable dans le champ Dossier). */
export const SANS_DOSSIER = '␀sans-dossier'

/** Limite serveur d'un téléversement — vérifiée côté client pour épargner l'attente. */
export const TAILLE_MAX_FICHIER = 50 * 1024 * 1024

/** Message d'erreur si le fichier dépasse la limite, sinon null (fichier acceptable). */
export function problemeTailleFichier(fichier: { name: string; size: number }): string | null {
  if (fichier.size <= TAILLE_MAX_FICHIER) {
    return null
  }
  const taille = (fichier.size / 1024 / 1024).toFixed(0)
  return `« ${fichier.name} » fait ${taille} Mo — la limite est de 50 Mo.`
}

/** Jours entre aujourd'hui (local) et une date ISO — négatif si passée. */
export function joursAvant(dateIso: string): number {
  const [a, m, j] = dateIso.split('-').map(Number)
  const [aa, am, aj] = dateLocaleIso().split('-').map(Number)
  return Math.round(
    (Date.UTC(a, m - 1, j) - Date.UTC(aa, am - 1, aj)) / (24 * 60 * 60 * 1000),
  )
}

export type ColonneTri = 'titre' | 'categorie' | 'dateDocument' | 'echeance'
export type Tri = { colonne: ColonneTri; desc: boolean }

/** Compare deux documents selon la colonne triée — dates nulles toujours en fin. */
export function comparer(a: Document, b: Document, tri: Tri): number {
  const sens = tri.desc ? -1 : 1
  if (tri.colonne === 'titre') {
    return sens * a.titre.localeCompare(b.titre, 'fr')
  }
  if (tri.colonne === 'categorie') {
    return sens * LIBELLES_CATEGORIE[a.categorie].localeCompare(LIBELLES_CATEGORIE[b.categorie], 'fr')
  }
  const da = tri.colonne === 'dateDocument' ? a.dateDocument : a.echeance
  const db = tri.colonne === 'dateDocument' ? b.dateDocument : b.echeance
  if (da === null && db === null) {
    return b.creeLe.localeCompare(a.creeLe)
  }
  if (da === null) {
    return 1
  }
  if (db === null) {
    return -1
  }
  return sens * da.localeCompare(db)
}
