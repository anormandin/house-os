import { Component, type ErrorInfo, type ReactNode } from 'react'
import { journaliser, vider } from '@/lib/journal'

type Props = { children: ReactNode }
type Etat = { plante: boolean }

/**
 * Filet de rendu. Jusqu'ici l'app n'en avait aucun : une exception dans un composant
 * démontait tout l'arbre React et laissait un écran blanc — sans message, sans trace,
 * sans même une ligne de console (le code n'en écrit nulle part).
 *
 * Classe et non hook : React ne propose toujours pas d'équivalent fonctionnel à
 * componentDidCatch.
 */
export default class GardeErreur extends Component<Props, Etat> {
  state: Etat = { plante: false }

  static getDerivedStateFromError(): Etat {
    return { plante: true }
  }

  componentDidCatch(erreur: Error, infos: ErrorInfo) {
    journaliser('error', 'Rendu', erreur.message, {
      pile: erreur.stack,
      composants: infos.componentStack ?? undefined,
    })
    // L'écran est mort : envoyer tout de suite, sans attendre le prochain cycle de
    // vidange, parce qu'Alain va recharger la page dans la seconde.
    void vider()
  }

  render() {
    if (this.state.plante === false) {
      return this.props.children
    }
    return (
      <main className="flex min-h-dvh flex-col items-center justify-center gap-4 bg-fond px-6 text-center">
        <p className="text-lg font-bold text-encre">L'écran a planté.</p>
        <p className="max-w-sm text-sm text-sourdine">
          L'incident a été enregistré. Recharger la page devrait suffire.
        </p>
        <button
          type="button"
          onClick={() => location.reload()}
          className="rounded-[12px] bg-encre px-5 py-2 text-sm font-bold text-carte"
        >
          Recharger
        </button>
      </main>
    )
  }
}
