/**
 * Piste de session du navigateur, expédiée au serveur et lisible dans Seq.
 *
 * Le web était totalement muet : aucun `console.*`, aucune error boundary, et
 * l'échec de connexion au hub avalé par un `catch(() => {})`. Quand Alain dit
 * « j'ai coché et rien ne s'est passé », il n'existait rien à relire. Ce module
 * enregistre la suite des gestes, des requêtes et des états de connexion, et les
 * envoie par lots à `/api/journal-client`, où ils rejoignent les logs serveur —
 * corrélés par le `TraceId` que l'API renvoie en en-tête.
 */

export type NiveauJournal = 'debug' | 'info' | 'warn' | 'error'

export type EvenementJournal = {
  horodatage: string
  niveau: NiveauJournal
  message: string
  categorie: string
  url: string
  traceId?: string
  proprietes?: Record<string, string>
}

const URL_JOURNAL = '/api/journal-client'
/** Le tampon est circulaire : un incident en rafale ne doit pas faire enfler la page. */
const TAILLE_TAMPON = 200
const SEUIL_VIDANGE = 20
const INTERVALLE_VIDANGE_MS = 5000
/** Doit rester sous la borne du serveur (50 par lot). */
const MAX_PAR_LOT = 50

let tampon: EvenementJournal[] = []
let minuterie: ReturnType<typeof setTimeout> | null = null
let sessionId = ''
let actif = false

/** Un identifiant par onglet : regroupe toute une session dans Seq. */
function idSession(): string {
  if (sessionId !== '') {
    return sessionId
  }
  try {
    const memorise = sessionStorage.getItem('houseos-session')
    if (memorise !== null) {
      sessionId = memorise
      return sessionId
    }
    sessionId = crypto.randomUUID()
    sessionStorage.setItem('houseos-session', sessionId)
  } catch {
    // Navigation privée, stockage bloqué : un id éphémère vaut mieux que rien.
    sessionId = crypto.randomUUID()
  }
  return sessionId
}

/**
 * Enregistre un évènement. Ne lève jamais : un journal qui casse la page qu'il
 * observe serait pire que pas de journal du tout.
 */
export function journaliser(
  niveau: NiveauJournal,
  categorie: string,
  message: string,
  proprietes?: Record<string, unknown>,
) {
  try {
    tampon.push({
      horodatage: new Date().toISOString(),
      niveau,
      categorie,
      message,
      url: typeof location === 'undefined' ? '' : location.pathname + location.search,
      traceId: typeof proprietes?.traceId === 'string' ? proprietes.traceId : undefined,
      proprietes: enChaines(proprietes),
    })
    if (tampon.length > TAILLE_TAMPON) {
      tampon = tampon.slice(-TAILLE_TAMPON)
    }
    if (tampon.length >= SEUIL_VIDANGE) {
      void vider()
      return
    }
    programmerVidange()
  } catch {
    // Rien : voir la note de `vider` — le journal ne se journalise jamais lui-même.
  }
}

/** Les propriétés partent en chaînes : le serveur les borne, inutile d'y envoyer des objets. */
function enChaines(proprietes?: Record<string, unknown>): Record<string, string> | undefined {
  if (proprietes === undefined) {
    return undefined
  }
  const sortie: Record<string, string> = {}
  for (const [cle, valeur] of Object.entries(proprietes)) {
    if (cle === 'traceId' || valeur === undefined || valeur === null) {
      continue
    }
    sortie[cle] = typeof valeur === 'string' ? valeur : JSON.stringify(valeur)
  }
  return Object.keys(sortie).length === 0 ? undefined : sortie
}

function programmerVidange() {
  if (minuterie !== null) {
    return
  }
  minuterie = setTimeout(() => {
    minuterie = null
    void vider()
  }, INTERVALLE_VIDANGE_MS)
}

/**
 * Envoie le tampon. `beacon` sert au départ de la page (les `fetch` y sont annulés) ;
 * `fetch` le reste du temps, pour connaître l'échec.
 *
 * Un échec d'envoi ne passe JAMAIS par `journaliser` : ce serait une boucle
 * (l'échec produit un évènement, qui déclenche une vidange, qui échoue…).
 */
export async function vider(beacon = false): Promise<void> {
  if (tampon.length === 0) {
    return
  }
  if (minuterie !== null) {
    clearTimeout(minuterie)
    minuterie = null
  }

  const lot = {
    sessionId: idSession(),
    // L'origine dit à quel hôte l'onglet parle : proxy Vite, Kestrel en direct ou
    // prod derrière NPM. Trois chemins réseau, un même symptôme possible.
    origine: typeof location === 'undefined' ? '' : location.origin,
    evenements: tampon.slice(0, MAX_PAR_LOT),
  }
  tampon = tampon.slice(MAX_PAR_LOT)
  const corps = JSON.stringify(lot)

  try {
    if (beacon && typeof navigator?.sendBeacon === 'function') {
      navigator.sendBeacon(URL_JOURNAL, new Blob([corps], { type: 'application/json' }))
      return
    }
    await fetch(URL_JOURNAL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: corps,
      keepalive: true,
    })
  } catch {
    // Silence volontaire — voir la note ci-dessus. Les évènements de ce lot sont
    // perdus plutôt que rejoués : un rejeu illimité ferait grossir le tampon
    // pendant une panne réseau, exactement quand la page a besoin de sa mémoire.
  }
}

/**
 * Branche les gardes globales : erreurs non attrapées, promesses rejetées, départ
 * de la page. Idempotent — StrictMode monte deux fois en développement.
 */
export function installerJournal() {
  if (actif || typeof window === 'undefined') {
    return
  }
  actif = true

  window.addEventListener('error', (evenement) => {
    journaliser('error', 'Fenetre', evenement.message, {
      fichier: evenement.filename,
      ligne: evenement.lineno,
      pile: evenement.error instanceof Error ? evenement.error.stack : undefined,
    })
  })

  window.addEventListener('unhandledrejection', (evenement) => {
    const raison = evenement.reason
    journaliser('error', 'Promesse', raison instanceof Error ? raison.message : String(raison), {
      pile: raison instanceof Error ? raison.stack : undefined,
    })
  })

  // `pagehide` couvre aussi le bfcache de Safari, que `beforeunload` rate.
  window.addEventListener('pagehide', () => void vider(true))
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      void vider(true)
    }
  })
}

/** Remet le module à zéro — réservé aux tests. */
export function reinitialiserJournal() {
  tampon = []
  if (minuterie !== null) {
    clearTimeout(minuterie)
    minuterie = null
  }
  sessionId = ''
  actif = false
}
