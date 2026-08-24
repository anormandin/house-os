import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'

/**
 * Suppression en deux temps : le déclencheur (icône poubelle par défaut, ou
 * `children` pour une variante texte) devient une pastille rouge « Vraiment? »
 * qui confirme au second clic. Revient à l'état initial après quelques secondes
 * sans confirmation.
 */
export default function ConfirmerSuppression({
  onConfirmer,
  ariaLabel,
  children,
  className,
}: {
  onConfirmer: () => void
  ariaLabel: string
  children?: ReactNode
  className?: string
}) {
  const [confirmer, setConfirmer] = useState(false)
  const minuterie = useRef<ReturnType<typeof setTimeout>>(undefined)

  useEffect(() => {
    if (confirmer) {
      minuterie.current = setTimeout(() => setConfirmer(false), 4000)
    }
    return () => clearTimeout(minuterie.current)
  }, [confirmer])

  if (confirmer) {
    return (
      <button
        type="button"
        aria-label={ariaLabel}
        onClick={() => {
          setConfirmer(false)
          onConfirmer()
        }}
        className="shrink-0 rounded-full bg-rouge px-3 py-1 text-xs font-bold text-carte"
      >
        Vraiment?
      </button>
    )
  }

  return (
    <button
      type="button"
      aria-label={ariaLabel}
      onClick={() => setConfirmer(true)}
      className={cn('text-sourdine transition-colors hover:text-rouge', className)}
    >
      {children ?? <Trash2 className="size-4" />}
    </button>
  )
}
