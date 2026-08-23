import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { api } from '@/lib/api'

export default function QuickAdd() {
  const queryClient = useQueryClient()
  const [ouvert, setOuvert] = useState(false)
  const [titre, setTitre] = useState('')
  const [echeance, setEcheance] = useState('')
  const [assigneAId, setAssigneAId] = useState('')
  const champTitre = useRef<HTMLInputElement>(null)

  const { data: utilisateurs } = useQuery({
    queryKey: ['utilisateurs'],
    queryFn: api.utilisateurs,
  })

  useEffect(() => {
    function surTouche(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        setOuvert(true)
        champTitre.current?.focus()
      }
      if (e.key === 'Escape') {
        setOuvert(false)
      }
    }
    window.addEventListener('keydown', surTouche)
    return () => window.removeEventListener('keydown', surTouche)
  }, [])

  useEffect(() => {
    if (ouvert) {
      champTitre.current?.focus()
    }
  }, [ouvert])

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
      setOuvert(false)
      queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    },
  })

  function soumettre(e: React.SyntheticEvent<HTMLFormElement, SubmitEvent>) {
    e.preventDefault()
    if (titre.trim().length > 0) {
      creer.mutate()
    }
  }

  if (ouvert === false) {
    return (
      <button
        type="button"
        onClick={() => setOuvert(true)}
        className="flex w-full items-center gap-3 rounded-[20px] border-2 border-dashed border-tiret px-5 py-3 text-sm font-bold text-tiret-texte transition-colors hover:border-orange/50 hover:text-dore"
      >
        <Plus className="size-4" />
        Ajouter une tâche… <span className="ml-auto text-xs">⌘K</span>
      </button>
    )
  }

  return (
    <form
      onSubmit={soumettre}
      className="flex flex-col gap-3 rounded-[20px] bg-carte p-4 shadow-carte"
    >
      <input
        ref={champTitre}
        value={titre}
        onChange={(e) => setTitre(e.target.value)}
        placeholder="Quoi faire?"
        aria-label="Titre de la tâche"
        className="rounded-xl bg-creux px-4 py-2.5 text-base font-bold placeholder:font-normal placeholder:text-sourdine focus:outline-2 focus:outline-orange/60"
      />
      <div className="flex flex-wrap items-center gap-2">
        <input
          type="date"
          value={echeance}
          onChange={(e) => setEcheance(e.target.value)}
          aria-label="Échéance"
          className="rounded-xl bg-creux px-3 py-2 text-sm text-dore focus:outline-2 focus:outline-orange/60"
        />
        <select
          value={assigneAId}
          onChange={(e) => setAssigneAId(e.target.value)}
          aria-label="Assigner à"
          className="rounded-xl bg-creux px-3 py-2 text-sm text-dore focus:outline-2 focus:outline-orange/60"
        >
          <option value="">Personne</option>
          {utilisateurs?.map((u) => (
            <option key={u.id} value={u.id}>
              {u.nomAffichage}
            </option>
          ))}
        </select>
        <div className="ml-auto flex gap-2">
          <button
            type="button"
            onClick={() => setOuvert(false)}
            className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte"
          >
            Annuler
          </button>
          <button
            type="submit"
            disabled={titre.trim().length === 0 || creer.isPending}
            className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte transition-opacity disabled:opacity-40"
          >
            Ajouter
          </button>
        </div>
      </div>
    </form>
  )
}
