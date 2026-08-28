import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { afficherToast } from '@/lib/toast'

/** Invalidations croisées d'un geste sur une occurrence : la console des tâches,
 * les occurrences, le journal (bilan hebdo) et le budget, qui dérive des
 * occurrences (case virement, échéance effective). */
export function invaliderAutourOccurrences(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: ['taches'] })
  queryClient.invalidateQueries({ queryKey: ['occurrences'] })
  queryClient.invalidateQueries({ queryKey: ['journal'] })
  queryClient.invalidateQueries({ queryKey: ['budget'] })
}

/** Compléter ⇄ annuler la complétion, chaque sens confirmé par un toast portant
 * l'action inverse (issue #60) — le filet du mauvais clic, partagé entre la
 * console Tâches et les listes d'occurrences (Aujourd'hui). */
export function useCompletionAvecUndo() {
  const queryClient = useQueryClient()

  const completer = useMutation({
    mutationFn: (occurrenceId: string) => api.completer(occurrenceId),
    onSuccess: (_, occurrenceId) => {
      invaliderAutourOccurrences(queryClient)
      afficherToast({
        message: 'Tâche complétée',
        actionLibelle: 'Annuler',
        onAction: () => annuler.mutate(occurrenceId),
      })
    },
  })

  const annuler = useMutation({
    mutationFn: (occurrenceId: string) => api.annulerCompletion(occurrenceId),
    onSuccess: (_, occurrenceId) => {
      invaliderAutourOccurrences(queryClient)
      afficherToast({
        message: 'Complétion annulée',
        actionLibelle: 'Refaire',
        onAction: () => completer.mutate(occurrenceId),
      })
    },
  })

  return { completer, annuler }
}
