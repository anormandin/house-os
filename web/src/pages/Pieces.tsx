import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus } from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import OccurrenceListe from '@/components/OccurrenceListe'
import Ruban from '@/components/Ruban'
import TacheEditeur from '@/components/TacheEditeur'
import { api, dateLocaleIso, type Occurrence, type Zone } from '@/lib/api'
import { bornesJourneeLocale, dateLongue } from '@/lib/format'
import { cn } from '@/lib/utils'

type Sante = {
  retard: number
  aujourdhui: number
  aVenir: number
  pct: number
  couleur: string
  libelle: string
}

function santeZone(occurrences: Occurrence[], aujourdhui: string): Sante {
  const retard = occurrences.filter((o) => o.echeance !== null && o.echeance < aujourdhui).length
  const ceJour = occurrences.filter((o) => o.echeance === aujourdhui).length
  const aVenir = occurrences.length - retard - ceJour
  const pct = Math.max(0.15, 1 - retard * 0.35 - ceJour * 0.15)
  if (retard > 0) {
    return {
      retard, aujourdhui: ceJour, aVenir, pct,
      couleur: '#d98d6e',
      libelle: 'ça déborde un peu',
    }
  }
  if (ceJour > 0) {
    return {
      retard, aujourdhui: ceJour, aVenir, pct,
      couleur: '#d9bd6e',
      libelle: ceJour === 1 ? '1 chose à faire aujourd’hui' : `${ceJour} choses à faire aujourd’hui`,
    }
  }
  return { retard, aujourdhui: ceJour, aVenir, pct: Math.max(pct, 0.85), couleur: '#9db07e', libelle: 'tout est frais' }
}

export default function Pieces() {
  const queryClient = useQueryClient()
  const aujourdhui = dateLocaleIso()
  const bornes = bornesJourneeLocale()
  const [zoneChoisieId, setZoneChoisieId] = useState<string | null>(null)
  const [ajoutOuvert, setAjoutOuvert] = useState(false)
  const [nomAjout, setNomAjout] = useState('')
  const [typeAjout, setTypeAjout] = useState<'Interieur' | 'Exterieur'>('Interieur')
  const [renommage, setRenommage] = useState<string | null>(null)
  const [editeur, setEditeur] = useState<{ tacheId: string | null } | null>(null)

  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: enAttente } = useQuery({
    queryKey: ['occurrences', 'en-attente'],
    queryFn: () => api.occurrences('en-attente'),
  })
  const { data: faites } = useQuery({
    queryKey: ['occurrences', 'faites', bornes.de],
    queryFn: () => api.occurrencesFaites(bornes.de, bornes.a),
  })
  const { data: evenementsExternes } = useQuery({
    queryKey: ['evenements-externes'],
    queryFn: () => api.evenementsExternes(7),
  })

  const invaliderZones = () => queryClient.invalidateQueries({ queryKey: ['zones'] })
  const creerZone = useMutation({
    mutationFn: () => api.creerZone({ nom: nomAjout, type: typeAjout }),
    onSuccess: (zone) => {
      setNomAjout('')
      setAjoutOuvert(false)
      setZoneChoisieId(zone.id)
      invaliderZones()
    },
  })
  const renommerZone = useMutation({
    mutationFn: (zone: Zone) => api.modifierZone(zone.id, { nom: renommage ?? zone.nom, type: zone.type }),
    onSuccess: () => {
      setRenommage(null)
      invaliderZones()
    },
  })
  const supprimerZone = useMutation({
    mutationFn: (id: string) => api.supprimerZone(id),
    onSuccess: () => {
      setZoneChoisieId(null)
      invaliderZones()
      queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    },
  })

  const ouvertes = enAttente ?? []
  const faitesJour = faites ?? []
  const zoneChoisie = zones?.find((z) => z.id === zoneChoisieId) ?? null

  const dansZone = (occurrences: Occurrence[], zoneId: string) =>
    occurrences.filter((o) => o.zoneId === zoneId)
  const duJourHorsZone = ouvertes.filter(
    (o) =>
      o.echeance !== null &&
      o.echeance <= aujourdhui &&
      (zoneChoisie === null || o.zoneId !== zoneChoisie.id),
  )

  const listeZone = zoneChoisie
    ? [
        ...dansZone(ouvertes, zoneChoisie.id).filter(
          (o) => o.echeance !== null && o.echeance <= aujourdhui,
        ),
        ...dansZone(ouvertes, zoneChoisie.id).filter(
          (o) => o.echeance === null || o.echeance > aujourdhui,
        ),
        ...dansZone(faitesJour, zoneChoisie.id),
      ]
    : []

  return (
    <div className="flex flex-col gap-5">
      <div>
        <div className="text-sm font-bold uppercase tracking-[0.06em] text-orange">
          {dateLongue()} · la barre = fraîcheur de la pièce, jamais une punition
        </div>
        <h1 className="mt-1 text-4xl font-bold">La maison, pièce par pièce</h1>
      </div>

      <Ruban
        occurrences={ouvertes}
        evenements={evenementsExternes ?? []}
        zones={zones ?? []}
        aujourdhui={aujourdhui}
        focusZone={zoneChoisie}
        onOuvrirTache={(tacheId) => setEditeur({ tacheId })}
      />

      <div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]">
        {/* Grille des pièces */}
        <div className="grid content-start gap-4 sm:grid-cols-2">
          {zones?.map((zone) => {
            const sante = santeZone(dansZone(ouvertes, zone.id), aujourdhui)
            const badge = sante.retard + sante.aujourdhui
            return (
              <button
                key={zone.id}
                type="button"
                onClick={() => {
                  setZoneChoisieId(zone.id)
                  setRenommage(null)
                }}
                className={cn(
                  'rounded-[20px] bg-carte px-5 py-5 text-left shadow-carte transition-shadow hover:shadow-carte-lg',
                  sante.retard > 0 && 'border-l-[6px] border-orange',
                  zoneChoisie?.id === zone.id && 'outline-2 outline-orange/50',
                )}
              >
                <div className="flex items-center justify-between">
                  <span className="text-[15px] font-bold">{zone.nom}</span>
                  {badge > 0 && (
                    <span className="rounded-full bg-orange px-2.5 py-0.5 text-xs font-bold text-carte">
                      {badge}
                    </span>
                  )}
                </div>
                <div className="mt-3 h-2 rounded-full bg-barre-piste">
                  <div
                    className="h-2 rounded-full"
                    style={{ width: `${sante.pct * 100}%`, background: sante.couleur }}
                  />
                </div>
                <div
                  className={cn(
                    'mt-2 text-xs',
                    sante.retard > 0 ? 'font-bold text-orange' : 'text-sourdine',
                  )}
                >
                  {sante.libelle}
                </div>
              </button>
            )
          })}

          {ajoutOuvert ? (
            <form
              className="flex flex-col gap-2 rounded-[20px] bg-carte px-5 py-4 shadow-carte"
              onSubmit={(e) => {
                e.preventDefault()
                if (nomAjout.trim().length > 0) {
                  creerZone.mutate()
                }
              }}
            >
              <input
                autoFocus
                value={nomAjout}
                onChange={(e) => setNomAjout(e.target.value)}
                placeholder="Nom de la pièce"
                className="rounded-xl bg-creux px-3 py-2 text-sm font-bold placeholder:font-normal focus:outline-2 focus:outline-orange/60"
              />
              <div className="flex gap-1.5">
                {(['Interieur', 'Exterieur'] as const).map((type) => (
                  <button
                    key={type}
                    type="button"
                    onClick={() => setTypeAjout(type)}
                    className={cn(
                      'rounded-full px-3 py-1 text-xs font-bold',
                      typeAjout === type ? 'bg-orange text-carte' : 'bg-creux text-sourdine',
                    )}
                  >
                    {type === 'Interieur' ? 'Intérieur' : 'Extérieur'}
                  </button>
                ))}
                <button
                  type="submit"
                  disabled={nomAjout.trim().length === 0}
                  className="ml-auto rounded-full bg-orange px-3 py-1 text-xs font-bold text-carte disabled:opacity-40"
                >
                  Ajouter
                </button>
              </div>
            </form>
          ) : (
            <button
              type="button"
              onClick={() => setAjoutOuvert(true)}
              className="flex min-h-24 items-center justify-center gap-2 rounded-[20px] border-2 border-dashed border-tiret text-sm font-bold text-tiret-texte transition-colors hover:border-orange/50 hover:text-dore"
            >
              <Plus className="size-4" /> Ajouter une pièce
            </button>
          )}
        </div>

        {/* Détail de la pièce choisie */}
        <div className="flex min-h-[420px] flex-col gap-3.5 rounded-3xl bg-carte px-7 py-6 shadow-carte-lg">
          {zoneChoisie === null ? (
            <p className="m-auto max-w-60 text-center text-sm text-sourdine">
              Choisis une pièce à gauche pour voir ce qui s’y passe.
            </p>
          ) : (
            <>
              <div className="flex items-center gap-2">
                {renommage === null ? (
                  <h2 className="text-2xl font-bold">{zoneChoisie.nom}</h2>
                ) : (
                  <form
                    onSubmit={(e) => {
                      e.preventDefault()
                      renommerZone.mutate(zoneChoisie)
                    }}
                  >
                    <input
                      autoFocus
                      value={renommage}
                      onChange={(e) => setRenommage(e.target.value)}
                      onBlur={() => renommerZone.mutate(zoneChoisie)}
                      className="rounded-xl bg-creux px-3 py-1.5 font-titre text-2xl font-bold text-encre focus:outline-2 focus:outline-orange/60"
                    />
                  </form>
                )}
                <button
                  type="button"
                  aria-label="Renommer la pièce"
                  onClick={() => setRenommage(zoneChoisie.nom)}
                  className="text-sourdine hover:text-dore"
                >
                  <Pencil className="size-4" />
                </button>
                <span className="ml-auto text-[13px] text-sourdine">
                  {dansZone(ouvertes, zoneChoisie.id).length} à faire ·{' '}
                  {dansZone(faitesJour, zoneChoisie.id).length} faite
                  {dansZone(faitesJour, zoneChoisie.id).length > 1 ? 's' : ''} aujourd’hui
                </span>
                <ConfirmerSuppression
                  key={zoneChoisie.id}
                  ariaLabel="Supprimer la pièce"
                  onConfirmer={() => supprimerZone.mutate(zoneChoisie.id)}
                />
              </div>

              <OccurrenceListe
                occurrences={listeZone}
                vide="Rien à faire ici. La pièce respire."
                onModifier={(tacheId) => setEditeur({ tacheId })}
              />

              <button
                type="button"
                onClick={() => setEditeur({ tacheId: null })}
                className="flex items-center justify-center gap-2 rounded-[20px] border-2 border-dashed border-tiret px-5 py-3 text-sm font-bold text-tiret-texte transition-colors hover:border-orange/50 hover:text-dore"
              >
                <Plus className="size-4" /> Ajouter une tâche dans {zoneChoisie.nom}
              </button>

              {duJourHorsZone.length > 0 && (
                <div className="mt-auto border-t border-barre-piste pt-4">
                  <div className="mb-3 text-[13px] font-bold uppercase tracking-[0.05em] text-sourdine">
                    Ailleurs aujourd’hui
                  </div>
                  <div className="flex flex-col gap-2.5 text-sm">
                    {duJourHorsZone.slice(0, 5).map((o) => (
                      <div key={o.id} className="flex items-center gap-2.5">
                        <span
                          className={cn(
                            'size-[7px] rounded-full',
                            o.echeance !== null && o.echeance < aujourdhui ? 'bg-rouge' : 'bg-coche',
                          )}
                        />
                        <span className="min-w-0 flex-1 truncate">{o.titre}</span>
                        {o.assigneA && (
                          <span className="text-[13px] text-sourdine">{o.assigneA.nomAffichage}</span>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {editeur !== null && (
        <TacheEditeur
          tacheId={editeur.tacheId}
          zoneInitialeId={zoneChoisie?.id}
          onFermer={() => setEditeur(null)}
        />
      )}
    </div>
  )
}
