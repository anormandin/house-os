import { useEffect } from 'react'
import { X } from 'lucide-react'
import { effacerToast, useToastCourant } from '@/lib/toast'

/** Toast fixe en bas d'écran confirmant le dernier geste (compléter/annuler),
 * avec son action inverse — le filet du mauvais clic (issue #60). Même famille
 * visuelle que BanniereErreur, ton succès, disparition après 6 s. */
export default function ToastConfirmation() {
  const toast = useToastCourant()

  useEffect(() => {
    if (toast === null) {
      return
    }
    const minuterie = setTimeout(effacerToast, 6000)
    return () => clearTimeout(minuterie)
  }, [toast])

  if (toast === null) {
    return null
  }

  return (
    <div
      role="status"
      className="fixed inset-x-0 bottom-5 z-40 mx-auto flex w-fit max-w-[calc(100%-2.5rem)] items-center gap-3 rounded-[16px] bg-vert px-5 py-3 text-sm font-bold text-carte shadow-carte"
    >
      <span>{toast.message}</span>
      <button
        type="button"
        onClick={() => {
          effacerToast()
          toast.onAction()
        }}
        className="shrink-0 rounded-full bg-carte/20 px-3 py-1 transition-colors hover:bg-carte/30"
      >
        {toast.actionLibelle}
      </button>
      <button
        type="button"
        aria-label="Fermer la confirmation"
        onClick={effacerToast}
        className="shrink-0 opacity-80 transition-opacity hover:opacity-100"
      >
        <X className="size-4" />
      </button>
    </div>
  )
}
