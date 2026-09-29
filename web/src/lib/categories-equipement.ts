import type { CategorieEquipement } from '@/lib/api'

// La liste fermée du serveur (vault : D-2026-09-28 Catégorie D'équipement En Liste
// Fermée), dans l'ordre où on la propose : les systèmes de la maison d'abord, le
// reste ensuite. Les libellés sont en dur, comme partout dans l'UI.
export const CATEGORIES_EQUIPEMENT: CategorieEquipement[] = [
  'Chauffage',
  'EauChaude',
  'Plomberie',
  'Electricite',
  'Toiture',
  'Exterieur',
  'PetitsMoteurs',
  'Electromenager',
  'Vehicule',
  'Autre',
]

export const LIBELLES_CATEGORIE_EQUIPEMENT: Record<CategorieEquipement, string> = {
  Chauffage: 'Chauffage & climatisation',
  EauChaude: 'Eau chaude',
  Plomberie: 'Plomberie',
  Electricite: 'Électricité',
  Toiture: 'Toiture',
  Exterieur: 'Extérieur & terrain',
  PetitsMoteurs: 'Petits moteurs',
  Electromenager: 'Électroménager',
  Vehicule: 'Véhicule',
  Autre: 'Autre',
}

/** Le libellé d'une catégorie, ou « Sans catégorie » pour un équipement pas encore classé. */
export function libelleCategorieEquipement(categorie: CategorieEquipement | null | undefined): string {
  return categorie ? LIBELLES_CATEGORIE_EQUIPEMENT[categorie] : 'Sans catégorie'
}
