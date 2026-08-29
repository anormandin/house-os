import { useSyncExternalStore } from 'react'

// Mini-store de confirmations : les gestes récents, affichés par ToastConfirmation.
// Deux sources s'y croisent — mes propres gestes (avec action inverse « Annuler »)
// et ceux poussés par la synchro temps réel (sans action). D'où une petite file
// plutôt qu'un slot unique, et une fusion pour que dix tâches créées d'un coup, ou
// trois cases cochées coup sur coup, ne fassent pas dix ou trois toasts.

/**
 * Registre visuel du toast. `succes` et `retour` sont mes deux gestes (la complétion
 * et son retour arrière) ; `distant` est tout ce qui vient d'ailleurs. Un seul
 * composant, trois accents — le toast reste une carte de l'app dans les trois cas.
 */
export type TonToast = 'succes' | 'retour' | 'distant'

/** Auteur d'une annonce distante, tel que la synchro le connaît : un nom d'affichage,
 * ou l'agent MCP (qui n'a pas de compte et porte une étincelle). */
export type AuteurToast = {
  nom: string | null
  estClaude?: boolean
}

export type ToastConfirmation = {
  id: number
  message: string
  /** Seconde ligne : sur quoi porte le geste. Lève l'ambiguïté quand mon toast se
   * retrouve empilé sous deux annonces distantes (« Annuler », mais annuler quoi?). */
  sousTitre?: string
  ton?: TonToast
  auteur?: AuteurToast
  /** Libellé du bouton d'action inverse (« Annuler », « Refaire »…). Absent = toast informatif. */
  actionLibelle?: string
  onAction?: () => void
  /** Emplacement exclusif : un nouveau toast du même slot chasse le précédent.
   * Utilisé par mes propres gestes — une seule action inverse est valide à la fois,
   * un « Annuler » périmé rejouerait une mutation déjà défaite. */
  slot?: string
  /** Gestes de même clé arrivés dans la fenêtre de fusion cumulent leur nombre. */
  cleFusion?: string
  nombre?: number
  /** Recalcule le message quand le compte augmente. */
  recomposer?: (nombre: number) => string
  /** Horodatage d'arrivée — borne la fenêtre de fusion. */
  arriveeMs: number
}

/** Slot de mes propres gestes : une seule action inverse valide à la fois. */
export const SLOT_GESTE_LOCAL = 'geste-local'

/** Nombre de toasts affichés simultanément ; au-delà, le plus ancien sort. */
export const MAX_TOASTS = 3

/** Durée de vie d'un toast. Les animations de minuteur (jauge du bouton, barre au
 * pied, lavis de la rangée) durent autant — leur `6s` est écrit dans index.css. */
export const DUREE_TOAST_MS = 6000

/** Au-delà de ce délai, un geste identique ouvre un nouveau toast au lieu de fusionner. */
export const FENETRE_FUSION_MS = 2000

let toasts: ToastConfirmation[] = []
let prochainId = 1
const abonnes = new Set<() => void>()

function notifier() {
  abonnes.forEach((abonne) => abonne())
}

export function afficherToast(toast: Omit<ToastConfirmation, 'id' | 'arriveeMs'>) {
  const maintenant = Date.now()

  if (typeof toast.cleFusion === 'string') {
    const existant = toasts.find(
      (t) => t.cleFusion === toast.cleFusion && maintenant - t.arriveeMs < FENETRE_FUSION_MS,
    )
    if (existant !== undefined) {
      // Fusion en place : le toast déjà visible se réécrit et son minuteur repart,
      // au lieu d'empiler un doublon.
      // L'id reste stable (c'est le même toast qui se réécrit) ; arriveeMs bouge, ce
      // qui relance son minuteur d'effacement.
      const nombre = (existant.nombre ?? 1) + (toast.nombre ?? 1)
      const recompose = existant.recomposer?.(nombre) ?? toast.message
      toasts = toasts.map((t) =>
        t.id === existant.id ? { ...t, nombre, message: recompose, arriveeMs: maintenant } : t,
      )
      notifier()
      return
    }
  }

  const restants = typeof toast.slot === 'string'
    ? toasts.filter((t) => t.slot !== toast.slot)
    : toasts
  toasts = [...restants, { id: prochainId++, arriveeMs: maintenant, ...toast }].slice(-MAX_TOASTS)
  notifier()
}

export function effacerToast(id?: number) {
  toasts = id === undefined ? [] : toasts.filter((t) => t.id !== id)
  notifier()
}

function sAbonner(abonne: () => void) {
  abonnes.add(abonne)
  return () => abonnes.delete(abonne)
}

export function useToasts(): ToastConfirmation[] {
  return useSyncExternalStore(sAbonner, () => toasts)
}
