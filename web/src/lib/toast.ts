import { useSyncExternalStore } from 'react'

// Mini-store de confirmations : le dernier geste confirmé, affiché par
// ToastConfirmation avec son action inverse (undo). Un seul toast à la fois —
// le plus récent remplace (même patron que lib/erreurs.ts).

export type ToastConfirmation = {
  id: number
  message: string
  /** Libellé du bouton d'action inverse (« Annuler », « Refaire »…). */
  actionLibelle: string
  onAction: () => void
}

let toastCourant: ToastConfirmation | null = null
let prochainId = 1
const abonnes = new Set<() => void>()

export function afficherToast(toast: Omit<ToastConfirmation, 'id'>) {
  toastCourant = { id: prochainId++, ...toast }
  abonnes.forEach((notifier) => notifier())
}

export function effacerToast() {
  toastCourant = null
  abonnes.forEach((notifier) => notifier())
}

function sAbonner(notifier: () => void) {
  abonnes.add(notifier)
  return () => abonnes.delete(notifier)
}

export function useToastCourant(): ToastConfirmation | null {
  return useSyncExternalStore(sAbonner, () => toastCourant)
}
