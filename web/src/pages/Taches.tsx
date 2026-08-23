import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import OccurrenceListe from '@/components/OccurrenceListe'
import QuickAdd from '@/components/QuickAdd'
import TacheEditeur from '@/components/TacheEditeur'
import { api } from '@/lib/api'
import { cn } from '@/lib/utils'

const filtres = [
  { cle: 'en-attente', libelle: 'À faire' },
  { cle: 'completees', libelle: 'Complétées' },
] as const

export default function Taches() {
  const [filtre, setFiltre] = useState<(typeof filtres)[number]['cle']>('en-attente')
  const [editeur, setEditeur] = useState<{ tacheId: string | null } | null>(null)

  const { data: occurrences, isLoading } = useQuery({
    queryKey: ['occurrences', filtre],
    queryFn: () => api.occurrences(filtre),
  })

  return (
    <div className="mx-auto flex max-w-[900px] flex-col gap-5">
      <div className="flex items-end justify-between gap-3">
        <h1 className="text-4xl font-bold">Toutes les tâches</h1>
        <div className="flex items-center gap-2">
          {filtres.map((f) => (
            <button
              key={f.cle}
              type="button"
              onClick={() => setFiltre(f.cle)}
              className={cn(
                'rounded-full px-4 py-1.5 text-sm font-bold transition-colors',
                filtre === f.cle
                  ? 'bg-orange text-carte'
                  : 'bg-carte text-sourdine shadow-carte hover:text-dore',
              )}
            >
              {f.libelle}
            </button>
          ))}
          <button
            type="button"
            onClick={() => setEditeur({ tacheId: null })}
            className="flex items-center gap-1.5 rounded-full bg-orange px-4 py-1.5 text-sm font-bold text-carte"
          >
            <Plus className="size-4" /> Nouvelle
          </button>
        </div>
      </div>
      <QuickAdd />
      {isLoading ? (
        <p className="py-8 text-center text-sm text-sourdine">Chargement…</p>
      ) : (
        <OccurrenceListe
          occurrences={occurrences ?? []}
          vide={filtre === 'en-attente' ? 'Aucune tâche à faire.' : 'Rien de complété encore.'}
          onModifier={(tacheId) => setEditeur({ tacheId })}
        />
      )}
      {editeur !== null && (
        <TacheEditeur tacheId={editeur.tacheId} onFermer={() => setEditeur(null)} />
      )}
    </div>
  )
}
