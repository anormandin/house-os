import type { QueryClient } from '@tanstack/react-query'
import { invaliderAutourOccurrences } from '@/lib/completion'

/** Message poussé par le hub (contrat partagé : server/…/Features/Synchro/EvenementSynchro.cs). */
export type EvenementSynchro = {
  module: string
  genre?: string | null
  source?: string | null
  acteurId?: string | null
  acteurNom?: string | null
  libelle?: string | null
  nombre?: number
}

export const GENRE_COMPLETEE = 'occurrence.completee'
export const GENRE_ANNULEE = 'occurrence.annulee'
export const GENRE_TACHES_CREEES = 'taches.creees'
export const SOURCE_MCP = 'mcp'

/**
 * Module diffusé → racines de clés à invalider. Miroir des closures `invalider()`
 * de chaque page : quand une page en ajoute une, la ligne correspondante bouge ici.
 * Le module `taches` délègue à invaliderAutourOccurrences (lib/completion.ts), qui
 * est déjà la référence des invalidations croisées d'un geste sur une occurrence.
 */
const CLES_PAR_MODULE: Record<string, string[][]> = {
  taches: [['tache']],
  zones: [['zones'], ['occurrences']],
  equipements: [['equipements'], ['equipement'], ['documents']],
  documents: [['documents'], ['equipements'], ['equipement']],
  'comptes-a-rebours': [['comptes-a-rebours']],
  meteo: [['meteo']],
  'phrase-du-jour': [['phrase-du-jour']],
  'flux-externes': [['flux-externes'], ['evenements-externes']],
  budget: [['budget'], ['budget-transactions'], ['budget-enveloppe']],
}

export function invaliderPourModule(queryClient: QueryClient, module: string) {
  if (module === 'taches') {
    invaliderAutourOccurrences(queryClient)
  }
  for (const queryKey of CLES_PAR_MODULE[module] ?? []) {
    queryClient.invalidateQueries({ queryKey })
  }
}

/** Fenêtre de regroupement des invalidations : une rafale d'écritures = un refetch. */
export const DELAI_COALESCENCE_MS = 300

/**
 * Regroupe les modules reçus coup sur coup avant d'invalider. Dix tâches créées en
 * lot n'émettent déjà qu'un événement côté serveur, mais trois clics rapides en
 * émettent trois : sans ceci, trois refetchs de la même liste.
 */
export function creerInvalidateurCoalesce(
  queryClient: QueryClient,
  delaiMs: number = DELAI_COALESCENCE_MS,
) {
  let enAttente = new Set<string>()
  let minuterie: ReturnType<typeof setTimeout> | null = null

  const vider = () => {
    minuterie = null
    const modules = enAttente
    enAttente = new Set()
    for (const module of modules) {
      invaliderPourModule(queryClient, module)
    }
  }

  return {
    pousser(module: string) {
      enAttente.add(module)
      if (minuterie === null) {
        minuterie = setTimeout(vider, delaiMs)
      }
    },
    arreter() {
      if (minuterie !== null) {
        clearTimeout(minuterie)
        minuterie = null
      }
      enAttente = new Set()
    },
  }
}

/**
 * Doit-on annoncer cet événement ? Jamais pour le tier grossier (sans genre), jamais
 * pour mes propres gestes — sauf quand l'écriture vient de l'agent MCP : celui-ci agit
 * « au nom de » quelqu'un, donc l'acteur peut être moi alors que je n'ai rien cliqué.
 */
export function doitAnnoncer(evenement: EvenementSynchro, moiId: string): boolean {
  if (typeof evenement.genre !== 'string') {
    return false
  }
  return evenement.source === SOURCE_MCP || evenement.acteurId !== moiId
}

/** Phrase affichée pour un événement fin. Au pluriel, le titre cède la place au décompte. */
export function messagePour(evenement: EvenementSynchro): string {
  const nombre = evenement.nombre ?? 1
  const auteur = evenement.source === SOURCE_MCP
    ? (typeof evenement.acteurNom === 'string'
      ? `Claude (au nom d'${evenement.acteurNom})`
      : 'Claude')
    : (evenement.acteurNom ?? 'Quelqu’un')

  const verbe = {
    [GENRE_COMPLETEE]: 'a complété',
    [GENRE_ANNULEE]: 'a annulé la complétion de',
    [GENRE_TACHES_CREEES]: 'a créé',
  }[evenement.genre ?? ''] ?? 'a modifié'

  if (nombre > 1) {
    return `${auteur} ${verbe} ${nombre} tâches`
  }
  const cible = typeof evenement.libelle === 'string' ? `« ${evenement.libelle} »` : 'une tâche'
  return `${auteur} ${verbe} ${cible}`
}

/** Clé de fusion des toasts : même geste, même acteur, même origine. */
export function cleFusion(evenement: EvenementSynchro): string {
  return `${evenement.genre}|${evenement.acteurId ?? ''}|${evenement.source ?? ''}`
}
