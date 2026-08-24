import { useSyncExternalStore } from 'react'

// Mini-store d'erreurs applicatives : la dernière erreur signalée, affichée par
// BanniereErreur. Alimenté globalement par le MutationCache (main.tsx).

export type ErreurSignalee = {
  id: number
  message: string
}

let erreurCourante: ErreurSignalee | null = null
let prochainId = 1
const abonnes = new Set<() => void>()

export function signalerErreur(message: string) {
  erreurCourante = { id: prochainId++, message }
  abonnes.forEach((notifier) => notifier())
}

export function effacerErreur() {
  erreurCourante = null
  abonnes.forEach((notifier) => notifier())
}

function sAbonner(notifier: () => void) {
  abonnes.add(notifier)
  return () => abonnes.delete(notifier)
}

export function useErreurCourante(): ErreurSignalee | null {
  return useSyncExternalStore(sAbonner, () => erreurCourante)
}
