// Helpers purs de la vue e-ink (pages/Ecran.tsx). La page met en forme ; ce qui se
// calcule se calcule ici, testé.

import { dateLocaleIso } from '@/lib/api'
import { dodosAvant, jourCourt } from '@/lib/format'

/**
 * « AL » pour Alain, « AR » pour Ariane — la couleur par personne n'existe pas en
 * 1-bit, et une seule lettre ne distingue pas deux prénoms qui commencent pareil.
 */
export function initiales(nomAffichage: string | null): string {
  if (nomAffichage === null || nomAffichage.trim() === '') {
    return ''
  }
  return [...nomAffichage.trim()].slice(0, 2).join('').toLocaleUpperCase('fr-CA')
}

/**
 * Quand une date arrive, en mots de tous les jours : « aujourd'hui », « demain »,
 * le jour de la semaine cette semaine, sinon « dans N jours ».
 */
export function quandCeJour(dateIso: string, aujourdhui = dateLocaleIso()): string {
  const jours = joursEntre(aujourdhui, dateIso)
  if (jours <= 0) return "aujourd'hui"
  if (jours === 1) return 'demain'
  if (jours < 7) return jourCourt(dateIso)
  return `dans ${jours} jours`
}

/** « 33 dodos », « 1 dodo », « aujourd'hui » — le vocabulaire des comptes à rebours. */
export function libelleDodos(dateIso: string): string {
  const dodos = dodosAvant(dateIso)
  if (dodos <= 0) return "c'est aujourd'hui"
  return dodos === 1 ? '1 dodo' : `${dodos} dodos`
}

/** Taille du cadre : les dimensions de l'appareil, ou celles de l'E1003 en paysage. */
export function dimensionsEcran(recherche: URLSearchParams): { largeur: number; hauteur: number } {
  return {
    largeur: entierBorne(recherche.get('largeur'), 1872),
    hauteur: entierBorne(recherche.get('hauteur'), 1404),
  }
}

/** Le niveau de pile que l'appareil a annoncé, si le serveur nous l'a transmis. */
export function pileAnnoncee(recherche: URLSearchParams): number | null {
  const valeur = Number(recherche.get('pile'))
  if (recherche.get('pile') === null || Number.isNaN(valeur)) {
    return null
  }
  return Math.min(100, Math.max(0, Math.round(valeur)))
}

function entierBorne(valeur: string | null, defaut: number): number {
  const n = Number(valeur)
  if (valeur === null || !Number.isInteger(n) || n < 200 || n > 4000) {
    return defaut
  }
  return n
}

function joursEntre(deIso: string, aIso: string): number {
  return Math.round(
    (new Date(`${aIso}T00:00:00`).getTime() - new Date(`${deIso}T00:00:00`).getTime()) / 86_400_000,
  )
}
