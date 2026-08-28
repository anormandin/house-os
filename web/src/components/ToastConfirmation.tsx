import { useEffect } from 'react'
import { X } from 'lucide-react'
import { effacerToast, useToasts, type ToastConfirmation as Toast } from '@/lib/toast'

/** Pile de toasts en bas d'écran : les confirmations de mes gestes (avec leur action
 * inverse — le filet du mauvais clic, issue #60) et les annonces poussées par la
 * synchro temps réel (informatives, sans action). Trois au plus, 6 s chacun. */
export default function ToastConfirmation() {
  const toasts = useToasts()

  if (toasts.length === 0) {
    return null
  }

  return (
    <div className="fixed inset-x-0 bottom-5 z-40 mx-auto flex w-fit max-w-[calc(100%-2.5rem)] flex-col items-center gap-2">
      {toasts.map((toast) => (
        <Ligne key={toast.id} toast={toast} />
      ))}
    </div>
  )
}

function Ligne({ toast }: { toast: Toast }) {
  // arriveeMs dans les dépendances : une fusion réécrit le toast en place et doit
  // lui redonner ses 6 s complètes.
  useEffect(() => {
    const minuterie = setTimeout(() => effacerToast(toast.id), 6000)
    return () => clearTimeout(minuterie)
  }, [toast.id, toast.arriveeMs])

  return (
    <div
      role="status"
      className="flex items-center gap-3 rounded-[16px] bg-vert px-5 py-3 text-sm font-bold text-carte shadow-carte"
    >
      <span>{toast.message}</span>
      {typeof toast.actionLibelle === 'string' && (
        <button
          type="button"
          onClick={() => {
            effacerToast(toast.id)
            toast.onAction?.()
          }}
          className="shrink-0 rounded-full bg-carte/20 px-3 py-1 transition-colors hover:bg-carte/30"
        >
          {toast.actionLibelle}
        </button>
      )}
      <button
        type="button"
        aria-label="Fermer la confirmation"
        onClick={() => effacerToast(toast.id)}
        className="shrink-0 opacity-80 transition-opacity hover:opacity-100"
      >
        <X className="size-4" />
      </button>
    </div>
  )
}
