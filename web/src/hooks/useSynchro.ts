import { useEffect } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import {
  cleFusion,
  creerInvalidateurCoalesce,
  doitAnnoncer,
  messagePour,
  type EvenementSynchro,
} from '@/lib/synchro'
import { afficherToast } from '@/lib/toast'

const CHEMIN_HUB = '/hubs/synchro'

/**
 * Branche l'onglet sur le hub de synchro : les écritures des autres (l'autre membre
 * du foyer, l'agent MCP, les services d'arrière-plan) rafraîchissent les données
 * affichées sans attendre un refocus, et les gestes qui ne viennent pas de moi
 * s'annoncent par un toast.
 *
 * Appelé seulement depuis la branche authentifiée d'App : la connexion naît à la
 * connexion et tombe à la déconnexion.
 */
export function useSynchro(moiId: string) {
  const queryClient = useQueryClient()

  useEffect(() => {
    const invalidateur = creerInvalidateurCoalesce(queryClient)
    const connexion = new HubConnectionBuilder()
      .withUrl(CHEMIN_HUB)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connexion.on('Evenement', (evenement: EvenementSynchro) => {
      invalidateur.pousser(evenement.module)
      if (doitAnnoncer(evenement, moiId) === false) {
        return
      }
      afficherToast({
        message: messagePour(evenement),
        cleFusion: cleFusion(evenement),
        nombre: evenement.nombre ?? 1,
        recomposer: (nombre) => messagePour({ ...evenement, nombre }),
      })
    })

    // Pendant une coupure, des écritures ont pu passer : on ne sait pas lesquelles,
    // donc tout est marqué périmé d'un coup.
    connexion.onreconnected(() => {
      queryClient.invalidateQueries()
    })

    // La synchro est un confort : si le hub refuse la connexion, l'app continue de
    // fonctionner sur staleTime + refocus comme avant.
    connexion.start().catch(() => {})

    return () => {
      invalidateur.arreter()
      if (connexion.state !== HubConnectionState.Disconnected) {
        connexion.stop().catch(() => {})
      }
    }
  }, [queryClient, moiId])
}
