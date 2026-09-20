import { useEffect, useState } from 'react'

/** Sous ce seuil, l'app rend sa vue téléphone (voir vault/Decisions/
 * D-2026-09-19 Interface Téléphone Distincte). 900 px attrape le téléphone et la
 * tablette en portrait (iPad : 820 pt) ; l'iPad en paysage (1180 pt) garde le
 * bureau. Une fenêtre de bureau rétrécie bascule aussi — c'est voulu : ça rend la
 * vue testable au navigateur et dans Playwright. */
export const SEUIL_TELEPHONE_PX = 900

const REQUETE = `(max-width: ${SEUIL_TELEPHONE_PX - 1}px)`

/** Vrai quand l'app doit rendre la présentation téléphone. Suit les changements de
 * largeur et de rotation, pas seulement l'état au montage. */
export function useFormatPhone(): boolean {
  const [phone, setPhone] = useState(
    () => typeof window !== 'undefined' && window.matchMedia(REQUETE).matches,
  )

  useEffect(() => {
    const mql = window.matchMedia(REQUETE)
    const surChangement = (e: MediaQueryListEvent) => setPhone(e.matches)
    setPhone(mql.matches)
    mql.addEventListener('change', surChangement)
    return () => mql.removeEventListener('change', surChangement)
  }, [])

  return phone
}
