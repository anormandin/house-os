import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import OccurrenceListe from '@/components/OccurrenceListe'
import QuickAdd from '@/components/QuickAdd'
import { api } from '@/lib/api'
import { cn } from '@/lib/utils'

const filtres = [
  { cle: 'en-attente', libelle: 'À faire' },
  { cle: 'completees', libelle: 'Complétées' },
] as const

export default function Taches() {
  const [filtre, setFiltre] = useState<(typeof filtres)[number]['cle']>('en-attente')

  const { data: occurrences, isLoading } = useQuery({
    queryKey: ['occurrences', filtre],
    queryFn: () => api.occurrences(filtre),
  })

  return (
    <div className="flex flex-col gap-4">
      <QuickAdd />
      <div className="flex gap-1 rounded-lg bg-muted p-1">
        {filtres.map((f) => (
          <button
            key={f.cle}
            type="button"
            onClick={() => setFiltre(f.cle)}
            className={cn(
              'flex-1 rounded-md py-1.5 text-sm transition-colors',
              filtre === f.cle ? 'bg-background font-medium shadow-sm' : 'text-muted-foreground',
            )}
          >
            {f.libelle}
          </button>
        ))}
      </div>
      {isLoading ? (
        <p className="py-8 text-center text-sm text-muted-foreground">Chargement…</p>
      ) : (
        <OccurrenceListe
          occurrences={occurrences ?? []}
          vide={filtre === 'en-attente' ? 'Aucune tâche à faire.' : 'Rien de complété encore.'}
        />
      )}
    </div>
  )
}
