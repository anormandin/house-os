import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { api } from '@/lib/api'

export default function QuickAdd() {
  const queryClient = useQueryClient()
  const [titre, setTitre] = useState('')
  const [echeance, setEcheance] = useState('')
  const [assigneAId, setAssigneAId] = useState('')

  const { data: utilisateurs } = useQuery({
    queryKey: ['utilisateurs'],
    queryFn: api.utilisateurs,
  })

  const creer = useMutation({
    mutationFn: () =>
      api.creerTache({
        titre,
        echeance: echeance ? echeance : undefined,
        assigneAId: assigneAId ? assigneAId : undefined,
      }),
    onSuccess: () => {
      setTitre('')
      setEcheance('')
      setAssigneAId('')
      queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    },
  })

  function soumettre(e: React.SyntheticEvent<HTMLFormElement, SubmitEvent>) {
    e.preventDefault()
    if (titre.trim().length > 0) {
      creer.mutate()
    }
  }

  return (
    <form onSubmit={soumettre} className="flex flex-col gap-2 rounded-lg border bg-card p-3">
      <Input
        value={titre}
        onChange={(e) => setTitre(e.target.value)}
        placeholder="Nouvelle tâche…"
        aria-label="Titre de la tâche"
      />
      <div className="flex gap-2">
        <Input
          type="date"
          value={echeance}
          onChange={(e) => setEcheance(e.target.value)}
          aria-label="Échéance"
          className="flex-1"
        />
        <select
          value={assigneAId}
          onChange={(e) => setAssigneAId(e.target.value)}
          aria-label="Assigner à"
          className="h-9 flex-1 rounded-md border border-input bg-transparent px-3 text-sm"
        >
          <option value="">Personne</option>
          {utilisateurs?.map((u) => (
            <option key={u.id} value={u.id}>
              {u.nomAffichage}
            </option>
          ))}
        </select>
        <Button type="submit" size="icon" disabled={titre.trim().length === 0 || creer.isPending} aria-label="Ajouter">
          <Plus className="size-4" />
        </Button>
      </div>
    </form>
  )
}
