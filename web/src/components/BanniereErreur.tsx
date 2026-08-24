import { useEffect } from 'react'
import { X } from 'lucide-react'
import { effacerErreur, useErreurCourante } from '@/lib/erreurs'

/** Bannière fixe en bas d'écran affichant la dernière erreur API signalée. */
export default function BanniereErreur() {
  const erreur = useErreurCourante()

  useEffect(() => {
    if (erreur === null) {
      return
    }
    const minuterie = setTimeout(effacerErreur, 6000)
    return () => clearTimeout(minuterie)
  }, [erreur])

  if (erreur === null) {
    return null
  }

  return (
    <div
      role="alert"
      className="fixed inset-x-0 bottom-5 z-50 mx-auto flex w-fit max-w-[calc(100%-2.5rem)] items-center gap-3 rounded-[16px] bg-rouge px-5 py-3 text-sm font-bold text-carte shadow-carte"
    >
      <span>{erreur.message}</span>
      <button
        type="button"
        aria-label="Fermer le message d'erreur"
        onClick={effacerErreur}
        className="shrink-0 opacity-80 transition-opacity hover:opacity-100"
      >
        <X className="size-4" />
      </button>
    </div>
  )
}
