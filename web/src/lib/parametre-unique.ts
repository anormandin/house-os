import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'

/** Paramètre d'URL « à usage unique » (`?id=`, `?zone=`) posé par la recherche
 * globale : `surValeur` est appelé une fois par valeur reçue — pendant le rendu,
 * comme un ajustement d'état — puis le paramètre est retiré de l'URL pour qu'un
 * retour arrière ne rouvre pas la même fiche. Fonctionne aussi quand la page est
 * déjà affichée (pas de remontage). */
export function useParametreUnique(nom: string, surValeur: (valeur: string) => void) {
  const [parametres, setParametres] = useSearchParams()
  const valeur = parametres.get(nom)
  const [traitee, setTraitee] = useState<string | null>(null)
  if (valeur !== traitee) {
    setTraitee(valeur)
    if (valeur !== null) {
      surValeur(valeur)
    }
  }

  useEffect(() => {
    if (valeur === null) {
      return
    }
    setParametres(
      (actuels) => {
        actuels.delete(nom)
        return actuels
      },
      { replace: true },
    )
  }, [valeur, nom, setParametres])
}
