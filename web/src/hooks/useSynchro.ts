import { useEffect } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import type { ILogger } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { journaliser } from '@/lib/journal'
import {
  auteurPour,
  cleFusion,
  creerInvalidateurCoalesce,
  doitAnnoncer,
  messagePour,
  type EvenementSynchro,
} from '@/lib/synchro'
import { afficherToast } from '@/lib/toast'

const CHEMIN_HUB = '/hubs/synchro'

/**
 * Déverse les logs de SignalR dans le journal de session. Sans ça, le
 * « The connection was stopped during negotiation » observé pendant l'enquête sur
 * le 503 n'existait que dans la console d'un navigateur ouvert au bon moment.
 */
const journalSignalR: ILogger = {
  log(niveau, message) {
    if (niveau >= LogLevel.Error) {
      journaliser('error', 'Hub', message)
      return
    }
    if (niveau >= LogLevel.Warning) {
      journaliser('warn', 'Hub', message)
    }
  },
}

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
      .configureLogging(journalSignalR)
      .build()

    connexion.on('Evenement', (evenement: EvenementSynchro) => {
      invalidateur.pousser(evenement.module)
      if (doitAnnoncer(evenement, moiId) === false) {
        return
      }
      afficherToast({
        message: messagePour(evenement),
        ton: 'distant',
        auteur: auteurPour(evenement),
        cleFusion: cleFusion(evenement),
        nombre: evenement.nombre ?? 1,
        recomposer: (nombre) => messagePour({ ...evenement, nombre }),
      })
    })

    // Pendant une coupure, des écritures ont pu passer : on ne sait pas lesquelles,
    // donc tout est marqué périmé d'un coup.
    connexion.onreconnected((connexionId) => {
      journaliser('info', 'Hub', 'Reconnecté au hub', { connexionId })
      queryClient.invalidateQueries()
    })
    connexion.onreconnecting((erreur) => {
      journaliser('warn', 'Hub', 'Reconnexion au hub en cours', { detail: erreur?.message })
    })
    // Une connexion tombée mais pas encore détectée côté serveur est le suspect n°1
    // du 503 de complétion : la diffusion attend tous les clients.
    connexion.onclose((erreur) => {
      journaliser(erreur === undefined ? 'info' : 'warn', 'Hub', 'Connexion au hub fermée', {
        detail: erreur?.message,
      })
    })

    // La synchro est un confort : si le hub refuse la connexion, l'app continue de
    // fonctionner sur staleTime + refocus comme avant. Mais l'échec se journalise
    // désormais au lieu d'être avalé.
    connexion
      .start()
      .then(() => journaliser('info', 'Hub', 'Connecté au hub', { connexionId: connexion.connectionId }))
      .catch((erreur: unknown) => {
        journaliser('error', 'Hub', 'Connexion au hub refusée', {
          detail: erreur instanceof Error ? erreur.message : String(erreur),
        })
      })

    return () => {
      invalidateur.arreter()
      if (connexion.state !== HubConnectionState.Disconnected) {
        connexion.stop().catch((erreur: unknown) => {
          journaliser('warn', 'Hub', 'Fermeture du hub en erreur', {
            detail: erreur instanceof Error ? erreur.message : String(erreur),
          })
        })
      }
    }
  }, [queryClient, moiId])
}
