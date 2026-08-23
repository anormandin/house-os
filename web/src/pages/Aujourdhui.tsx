import { useQuery } from '@tanstack/react-query'
import OccurrenceListe from '@/components/OccurrenceListe'
import QuickAdd from '@/components/QuickAdd'
import { api } from '@/lib/api'

export default function Aujourdhui() {
  const { data: occurrences, isLoading } = useQuery({
    queryKey: ['occurrences', 'aujourdhui'],
    queryFn: () => api.occurrences('aujourdhui'),
  })

  const dateLongue = new Date().toLocaleDateString('fr-CA', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  })

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm capitalize text-muted-foreground">{dateLongue}</p>
      <QuickAdd />
      {isLoading ? (
        <p className="py-8 text-center text-sm text-muted-foreground">Chargement…</p>
      ) : (
        <OccurrenceListe
          occurrences={occurrences ?? []}
          vide="Rien pour aujourd'hui. 🎉"
        />
      )}
    </div>
  )
}
