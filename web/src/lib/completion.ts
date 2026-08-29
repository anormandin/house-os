import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { journaliser } from '@/lib/journal'
import { afficherToast, DUREE_TOAST_MS, SLOT_GESTE_LOCAL } from '@/lib/toast'

/** Invalidations croisées d'un geste sur une occurrence : la console des tâches,
 * les occurrences, le journal (bilan hebdo) et le budget, qui dérive des
 * occurrences (case virement, échéance effective). */
export function invaliderAutourOccurrences(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: ['taches'] })
  queryClient.invalidateQueries({ queryKey: ['occurrences'] })
  queryClient.invalidateQueries({ queryKey: ['journal'] })
  queryClient.invalidateQueries({ queryKey: ['budget'] })
}

/** Le geste porte le titre de sa tâche : le toast l'affiche en sous-ligne (« Annuler »
 * doit dire sur quoi il porte quand il est empilé sous des annonces distantes) et
 * l'action inverse le repasse au toast suivant. */
export type GesteOccurrence = { id: string; titre: string }

/** Compléter ⇄ annuler la complétion, chaque sens confirmé par un toast portant
 * l'action inverse (issue #60) — le filet du mauvais clic, partagé entre la
 * console Tâches et les listes d'occurrences (Aujourd'hui).
 *
 * Retourne aussi l'occurrence fraîchement complétée : la rangée qu'on vient de cocher
 * porte un lavis vert qui redescend vers son fond de repos sur les mêmes 6 s que le
 * toast, pour que le geste et sa confirmation se lisent au même endroit
 * (vault/Decisions/D-2026-08-28 Toast Carte Posée). */
export function useCompletionAvecUndo() {
  const queryClient = useQueryClient()
  const [fraichementCompletee, setFraichementCompletee] = useState<string | null>(null)
  const minuterieLavis = useRef<ReturnType<typeof setTimeout> | null>(null)

  const armerLavis = (occurrenceId: string | null) => {
    if (minuterieLavis.current !== null) {
      clearTimeout(minuterieLavis.current)
      minuterieLavis.current = null
    }
    setFraichementCompletee(occurrenceId)
    if (occurrenceId !== null) {
      minuterieLavis.current = setTimeout(() => setFraichementCompletee(null), DUREE_TOAST_MS)
    }
  }

  useEffect(() => () => {
    if (minuterieLavis.current !== null) {
      clearTimeout(minuterieLavis.current)
    }
  }, [])

  const completer = useMutation({
    // Le geste est journalisé au départ, pas seulement à l'arrivée : c'est
    // exactement le cas « j'ai coché et rien ne s'est passé » qu'on cherche à
    // reconstituer — un clic sans réponse doit laisser sa moitié de trace.
    mutationFn: ({ id, titre }: GesteOccurrence) => {
      journaliser('info', 'Geste', 'Complétion demandée', { occurrenceId: id, titre })
      return api.completer(id)
    },
    onSuccess: (_, geste) => {
      journaliser('info', 'Geste', 'Complétion confirmée', {
        occurrenceId: geste.id,
        titre: geste.titre,
      })
      invaliderAutourOccurrences(queryClient)
      armerLavis(geste.id)
      afficherToast({
        message: 'Tâche complétée',
        sousTitre: geste.titre,
        ton: 'succes',
        actionLibelle: 'Annuler',
        onAction: () => annuler.mutate(geste),
        slot: SLOT_GESTE_LOCAL,
      })
    },
  })

  const annuler = useMutation({
    mutationFn: ({ id, titre }: GesteOccurrence) => {
      journaliser('info', 'Geste', 'Annulation demandée', { occurrenceId: id, titre })
      return api.annulerCompletion(id)
    },
    onSuccess: (_, geste) => {
      journaliser('info', 'Geste', 'Annulation confirmée', {
        occurrenceId: geste.id,
        titre: geste.titre,
      })
      invaliderAutourOccurrences(queryClient)
      // La rangée n'est plus complétée : son lavis n'a plus d'objet.
      armerLavis(null)
      afficherToast({
        message: 'Complétion annulée',
        sousTitre: geste.titre,
        ton: 'retour',
        actionLibelle: 'Refaire',
        onAction: () => completer.mutate(geste),
        slot: SLOT_GESTE_LOCAL,
      })
    },
  })

  return { completer, annuler, fraichementCompletee }
}
