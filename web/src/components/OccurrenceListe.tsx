import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { api, dateLocaleIso, type Occurrence } from '@/lib/api'
import { cn } from '@/lib/utils'

function libelleEcheance(echeance: string | null): string | null {
  if (echeance === null) {
    return null
  }
  const aujourdhui = dateLocaleIso()
  if (echeance === aujourdhui) {
    return "aujourd'hui"
  }
  const date = new Date(`${echeance}T00:00:00`)
  return date.toLocaleDateString('fr-CA', { weekday: 'short', day: 'numeric', month: 'short' })
}

export default function OccurrenceListe({
  occurrences,
  vide,
}: {
  occurrences: Occurrence[]
  vide: string
}) {
  const queryClient = useQueryClient()
  const invalider = () => queryClient.invalidateQueries({ queryKey: ['occurrences'] })

  const completer = useMutation({
    mutationFn: (id: string) => api.completer(id),
    onSuccess: invalider,
  })
  const supprimer = useMutation({
    mutationFn: (tacheId: string) => api.supprimerTache(tacheId),
    onSuccess: invalider,
  })

  if (occurrences.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">{vide}</p>
  }

  const aujourdhui = dateLocaleIso()

  return (
    <ul className="flex flex-col divide-y">
      {occurrences.map((o) => {
        const completee = o.statut === 'Completee'
        const enRetard = !completee && o.echeance !== null && o.echeance < aujourdhui
        return (
          <li key={o.id} className="flex items-start gap-3 py-3">
            <Checkbox
              className="mt-0.5"
              checked={completee}
              disabled={completee || completer.isPending}
              onCheckedChange={() => completer.mutate(o.id)}
              aria-label={`Compléter ${o.titre}`}
            />
            <div className="min-w-0 flex-1">
              <p className={cn('leading-snug', completee && 'text-muted-foreground line-through')}>
                {o.titre}
              </p>
              {o.description && (
                <p className="text-sm text-muted-foreground">{o.description}</p>
              )}
              <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                {o.echeance && !completee && (
                  <Badge variant={enRetard ? 'destructive' : 'secondary'}>
                    {enRetard ? 'en retard · ' : ''}
                    {libelleEcheance(o.echeance)}
                  </Badge>
                )}
                {o.assigneA && !completee && <span>→ {o.assigneA.nomAffichage}</span>}
                {completee && o.completeePar && (
                  <span>
                    fait par {o.completeePar.nomAffichage}
                    {o.completeeLe &&
                      ` · ${new Date(o.completeeLe).toLocaleDateString('fr-CA', { day: 'numeric', month: 'short' })}`}
                  </span>
                )}
              </div>
            </div>
            {!completee && (
              <Button
                variant="ghost"
                size="icon"
                className="text-muted-foreground"
                aria-label={`Supprimer ${o.titre}`}
                onClick={() => supprimer.mutate(o.tacheId)}
              >
                <Trash2 className="size-4" />
              </Button>
            )}
          </li>
        )
      })}
    </ul>
  )
}
