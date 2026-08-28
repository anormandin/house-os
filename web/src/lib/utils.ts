import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/**
 * Copie un texte dans le presse-papiers ; `true` si la copie a réussi.
 * `navigator.clipboard` n'existe qu'en contexte sécurisé (HTTPS) — sur le LAN en
 * HTTP, repli sur la sélection d'un champ hors écran + `execCommand('copy')`.
 */
export async function copierDansPressePapiers(texte: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(texte)
    return true
  } catch {
    const zone = document.createElement('textarea')
    zone.value = texte
    zone.setAttribute('readonly', '')
    zone.style.position = 'fixed'
    zone.style.opacity = '0'
    document.body.appendChild(zone)
    zone.select()
    try {
      return document.execCommand('copy')
    } catch {
      return false
    } finally {
      zone.remove()
    }
  }
}
