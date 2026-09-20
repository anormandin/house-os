import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, ChevronLeft, MoreHorizontal, Pencil, Plus, Trash2 } from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import ErreurChargement from '@/components/ErreurChargement'
import TacheEditeur from '@/components/TacheEditeur'
import FeuilleActions from '@/components/telephone/FeuilleActions'
import { api, dateLocaleIso, type Occurrence, type Zone } from '@/lib/api'
import { useCompletionAvecUndo } from '@/lib/completion'
import { bornesJourneeLocale } from '@/lib/format'
import { santeZone } from '@/lib/pieces-vues'
import { cn } from '@/lib/utils'

/** Les pièces au téléphone : liste ↔ détail dans le même écran. Le bureau montre
 * la grille et le panneau côte à côte ; sur 390 px il faut choisir, donc l'état
 * `zoneChoisie` remplace la grille par le détail — sans nouvelle route, pour que
 * le routeur reste commun aux deux vues. */
export default function PiecesTelephone() {
  const queryClient = useQueryClient()
  const aujourdhui = dateLocaleIso()
  const bornes = bornesJourneeLocale()
  const [zoneChoisie, setZoneChoisie] = useState<string | null>(null)
  const [ajoutOuvert, setAjoutOuvert] = useState(false)
  const [nomAjout, setNomAjout] = useState('')
  const [typeAjout, setTypeAjout] = useState<'Interieur' | 'Exterieur'>('Interieur')
  const [renommage, setRenommage] = useState<string | null>(null)
  const [feuillePour, setFeuillePour] = useState<Occurrence | null>(null)
  const [editeur, setEditeur] = useState<{ tacheId: string | null } | null>(null)

  const {
    data: zones,
    isError: zonesEnErreur,
    refetch: rechargerZones,
  } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: enAttente } = useQuery({
    queryKey: ['occurrences', 'en-attente'],
    queryFn: () => api.occurrences('en-attente'),
  })
  const { data: faites } = useQuery({
    queryKey: ['occurrences', 'faites', bornes.de],
    queryFn: () => api.occurrencesFaites(bornes.de, bornes.a),
  })

  const invaliderZones = () => queryClient.invalidateQueries({ queryKey: ['zones'] })
  const creerZone = useMutation({
    mutationFn: () => api.creerZone({ nom: nomAjout, type: typeAjout }),
    onSuccess: (zone) => {
      setNomAjout('')
      setAjoutOuvert(false)
      // On entre directement dans la pièce créée : c'est là qu'on veut ajouter
      // ses premières tâches.
      setZoneChoisie(zone.id)
      invaliderZones()
    },
  })
  const renommerZone = useMutation({
    mutationFn: ({ zone, nom }: { zone: Zone; nom: string }) =>
      api.modifierZone(zone.id, { nom, type: zone.type }),
    onSuccess: () => {
      setRenommage(null)
      invaliderZones()
    },
  })
  // Entrée soumet puis le blur suit : le garde isPending évite un second PUT ;
  // un nom vide ou inchangé annule le renommage au lieu de partir vers un 400.
  const soumettreRenommage = (zone: Zone) => {
    if (renommage === null || renommerZone.isPending) {
      return
    }
    const nom = renommage.trim()
    if (nom.length === 0 || nom === zone.nom) {
      setRenommage(null)
      return
    }
    renommerZone.mutate({ zone, nom })
  }
  const supprimerZone = useMutation({
    mutationFn: (id: string) => api.supprimerZone(id),
    onSuccess: () => {
      setZoneChoisie(null)
      invaliderZones()
      queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    },
  })

  const { completer } = useCompletionAvecUndo()

  const ouvertes = enAttente ?? []
  const faitesJour = faites ?? []
  const zone = zones?.find((z) => z.id === zoneChoisie) ?? null
  const dansZone = (occurrences: Occurrence[], zoneId: string) =>
    occurrences.filter((o) => o.zoneId === zoneId)

  // Ordre de lecture du détail : ce qui est dû (retard et aujourd'hui) en haut,
  // le reste ensuite, les faites du jour au fond.
  const listeZone = zone
    ? [
        ...dansZone(ouvertes, zone.id).filter(
          (o) => o.echeance !== null && o.echeance <= aujourdhui,
        ),
        ...dansZone(ouvertes, zone.id).filter(
          (o) => o.echeance === null || o.echeance > aujourdhui,
        ),
        ...dansZone(faitesJour, zone.id),
      ]
    : []

  if (zone !== null) {
    const aFaire = dansZone(ouvertes, zone.id).length
    const faitesIci = dansZone(faitesJour, zone.id).length
    return (
      <div className="flex flex-col gap-3 pt-1">
        <div className="flex items-center gap-1">
          <button
            type="button"
            aria-label="Retour aux pièces"
            onClick={() => {
              setZoneChoisie(null)
              setRenommage(null)
            }}
            className="-ml-2 flex size-11 shrink-0 items-center justify-center text-dore"
          >
            <ChevronLeft className="size-6" />
          </button>
          {renommage === null ? (
            <h1 className="min-w-0 flex-1 truncate font-titre text-[25px] leading-tight font-bold text-encre">
              {zone.nom}
            </h1>
          ) : (
            <form
              className="min-w-0 flex-1"
              onSubmit={(e) => {
                e.preventDefault()
                soumettreRenommage(zone)
              }}
            >
              <input
                autoFocus
                value={renommage}
                onChange={(e) => setRenommage(e.target.value)}
                onBlur={() => soumettreRenommage(zone)}
                onKeyDown={(e) => {
                  if (e.key === 'Escape') {
                    setRenommage(null)
                  }
                }}
                aria-label="Nouveau nom de la pièce"
                className="min-h-[48px] w-full rounded-xl bg-creux px-3 font-titre text-[20px] font-bold text-encre focus:outline-2 focus:outline-orange/60"
              />
            </form>
          )}
          <button
            type="button"
            aria-label="Renommer la pièce"
            onClick={() => setRenommage(zone.nom)}
            className="flex size-11 shrink-0 items-center justify-center text-dore"
          >
            <Pencil className="size-5" />
          </button>
        </div>

        <p className="text-[13px] font-bold text-dore">
          {aFaire} à faire · {faitesIci} faite{faitesIci > 1 ? 's' : ''} aujourd’hui
        </p>

        {listeZone.length === 0 ? (
          <div className="rounded-[20px] border border-dashed border-tiret px-5 py-10 text-center text-[15px] text-dore">
            Rien à faire ici. La pièce respire.
          </div>
        ) : (
          <div className="flex flex-col overflow-hidden rounded-[20px] bg-carte shadow-carte">
            {listeZone.map((o) => {
              const faite = o.statut === 'Completee'
              return (
                <div
                  key={o.id}
                  className={cn(
                    'flex min-h-[56px] items-center border-b border-creux last:border-b-0',
                    faite && 'bg-vert-fond',
                  )}
                >
                  <button
                    type="button"
                    aria-label={faite ? `${o.titre} est complétée` : `Compléter ${o.titre}`}
                    disabled={faite}
                    onClick={() => completer.mutate({ id: o.id, titre: o.titre })}
                    className="flex size-11 shrink-0 items-center justify-center"
                  >
                    <span
                      className={cn(
                        'flex size-[26px] items-center justify-center rounded-[9px] border-[2.5px]',
                        faite
                          ? 'border-vert bg-vert'
                          : o.echeance !== null && o.echeance < aujourdhui
                            ? 'border-rouge'
                            : 'border-coche',
                      )}
                    >
                      {faite && <Check className="size-4 text-carte" />}
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={() => setFeuillePour(o)}
                    className="min-h-[56px] min-w-0 flex-1 py-2 text-left"
                  >
                    <span
                      className={cn(
                        'block truncate text-[16px] font-bold',
                        faite ? 'text-sourdine line-through' : 'text-encre',
                      )}
                    >
                      {o.titre}
                    </span>
                    {o.assigneA !== null && (
                      <span className="block text-[13px] text-dore">
                        {o.assigneA.nomAffichage}
                      </span>
                    )}
                  </button>
                  <button
                    type="button"
                    aria-label={`Actions pour ${o.titre}`}
                    onClick={() => setFeuillePour(o)}
                    className="flex size-11 shrink-0 items-center justify-center text-dore"
                  >
                    <MoreHorizontal className="size-5" />
                  </button>
                </div>
              )
            })}
          </div>
        )}

        <button
          type="button"
          onClick={() => setEditeur({ tacheId: null })}
          className="mt-1 flex min-h-[52px] items-center justify-center gap-2 rounded-2xl border border-dashed border-tiret px-4 text-center text-[16px] font-extrabold text-dore"
        >
          <Plus className="size-5 shrink-0" />
          Ajouter une tâche dans {zone.nom}
        </button>

        {/* Supprimer est une commande à part entière : au doigt, il n'y a pas de
         * survol pour révéler une poubelle discrète comme au bureau. Le gabarit
         * `[&>button]` porte la cible tactile sur les deux états du composant
         * partagé — sa pastille « Vraiment? » est taillée pour la souris. */}
        <div className="mt-2 [&>button]:flex [&>button]:min-h-[52px] [&>button]:w-full [&>button]:items-center [&>button]:justify-center [&>button]:gap-2 [&>button]:rounded-2xl [&>button]:px-4 [&>button]:text-[16px] [&>button]:font-bold">
          <ConfirmerSuppression
            key={zone.id}
            ariaLabel="Supprimer la pièce"
            onConfirmer={() => supprimerZone.mutate(zone.id)}
            className="border border-rouge/40 text-rouge"
          >
            <Trash2 className="size-5 shrink-0" />
            Supprimer la pièce
          </ConfirmerSuppression>
        </div>

        {feuillePour !== null && (
          <FeuilleActions
            occurrence={feuillePour}
            onFermer={() => setFeuillePour(null)}
            onModifier={(tacheId) => setEditeur({ tacheId })}
          />
        )}
        {editeur !== null && (
          <TacheEditeur
            tacheId={editeur.tacheId}
            zoneInitialeId={zone.id}
            onFermer={() => setEditeur(null)}
          />
        )}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3 pt-1">
      <div className="flex flex-col gap-1">
        <h1 className="font-titre text-[27px] leading-tight font-bold text-encre">
          La maison, pièce par pièce
        </h1>
        <p className="text-[13px] text-dore">
          La barre = fraîcheur de la pièce, jamais une punition.
        </p>
      </div>

      {zonesEnErreur && (
        <ErreurChargement quoi="les pièces" onReessayer={() => void rechargerZones()} />
      )}

      <div className="flex flex-col gap-2.5">
        {zones?.map((z) => {
          const sante = santeZone(dansZone(ouvertes, z.id), aujourdhui)
          const enAttenteIci = sante.retard + sante.aujourdhui + sante.aVenir
          return (
            <button
              key={z.id}
              type="button"
              onClick={() => {
                setZoneChoisie(z.id)
                setRenommage(null)
              }}
              className={cn(
                'flex min-h-[80px] flex-col justify-center gap-2 rounded-[20px] bg-carte px-4 py-3 text-left shadow-carte',
                sante.retard > 0 && 'border-l-[6px] border-orange',
              )}
            >
              <div className="flex items-center gap-2">
                <span className="min-w-0 flex-1 truncate text-[17px] font-bold text-encre">
                  {z.nom}
                </span>
                {enAttenteIci > 0 && (
                  <span
                    className={cn(
                      'shrink-0 rounded-full px-2.5 py-0.5 text-[13px] font-extrabold',
                      sante.retard > 0 ? 'bg-orange text-carte' : 'bg-creux text-dore',
                    )}
                  >
                    {enAttenteIci} en attente
                  </span>
                )}
              </div>
              <div className="h-2 rounded-full bg-barre-piste">
                <div
                  className="h-2 rounded-full"
                  style={{ width: `${sante.pct * 100}%`, background: sante.couleur }}
                />
              </div>
              <span
                className={cn(
                  'text-[13px]',
                  sante.retard > 0 ? 'font-bold text-orange' : 'text-dore',
                )}
              >
                {sante.libelle}
              </span>
            </button>
          )
        })}
      </div>

      {ajoutOuvert ? (
        <form
          className="flex flex-col gap-2.5 rounded-[20px] bg-carte px-4 py-4 shadow-carte"
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
            aria-label="Nom de la pièce"
            className="min-h-[48px] rounded-xl bg-creux px-3 text-[16px] font-bold text-texte placeholder:font-normal focus:outline-2 focus:outline-orange/60"
          />
          <div className="flex flex-wrap items-center gap-2">
            {(['Interieur', 'Exterieur'] as const).map((type) => (
              <button
                key={type}
                type="button"
                onClick={() => setTypeAjout(type)}
                className={cn(
                  'min-h-[44px] rounded-full px-4 text-[15px] font-bold',
                  typeAjout === type ? 'bg-orange text-carte' : 'bg-creux text-dore',
                )}
              >
                {type === 'Interieur' ? 'Intérieur' : 'Extérieur'}
              </button>
            ))}
            <button
              type="submit"
              disabled={nomAjout.trim().length === 0}
              className="ml-auto min-h-[44px] rounded-full bg-orange px-5 text-[15px] font-extrabold text-carte disabled:opacity-40"
            >
              Ajouter
            </button>
          </div>
        </form>
      ) : (
        <button
          type="button"
          onClick={() => setAjoutOuvert(true)}
          className="mt-1 flex min-h-[52px] items-center justify-center gap-2 rounded-2xl border border-dashed border-tiret text-[16px] font-extrabold text-dore"
        >
          <Plus className="size-5" />
          Ajouter une pièce
        </button>
      )}
    </div>
  )
}
