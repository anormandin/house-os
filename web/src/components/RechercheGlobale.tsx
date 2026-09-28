import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import { correspond } from '@/lib/recherche'
import { cn } from '@/lib/utils'

/** Au-delà, la section dit « et N autres » : on affine plutôt que de défiler. */
const MAX_PAR_GROUPE = 6

type Resultat = {
  cle: string
  groupe: 'Tâches' | 'Équipements' | 'Documents' | 'Pièces'
  libelle: string
  detail: string | null
  ouvrir: () => void
}

/** La palette de recherche du bureau (touche `/` ou la loupe de l'en-tête) :
 * tâches, équipements, documents et pièces d'un coup. Une tâche s'ouvre dans son
 * éditeur sur place ; le reste mène à sa page, fiche ouverte (`?id=` / `?zone=`).
 * Montée seulement quand elle est ouverte — les requêtes partent à l'ouverture et
 * réutilisent le cache des pages. */
export default function RechercheGlobale({
  onFermer,
  onOuvrirTache,
}: {
  onFermer: () => void
  onOuvrirTache: (tacheId: string) => void
}) {
  const naviguer = useNavigate()
  const [terme, setTerme] = useState('')
  const [actif, setActif] = useState(0)
  const liste = useRef<HTMLUListElement>(null)
  const idListe = useId()

  const { data: taches } = useQuery({ queryKey: ['taches'], queryFn: api.taches })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })
  const { data: documents } = useQuery({ queryKey: ['documents'], queryFn: () => api.documents() })
  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })

  const nomZone = (zoneId: string | null) => zones?.find((z) => z.id === zoneId)?.nom ?? null
  const aller = (chemin: string) => () => {
    onFermer()
    naviguer(chemin)
  }

  const candidats: Resultat[] =
    terme.trim() === ''
      ? []
      : [
          ...(taches ?? [])
            .filter((t) => correspond(terme, [t.titre, t.description, nomZone(t.zoneId)]))
            // Les ponctuelles déjà faites passent après ce qui reste à faire.
            .sort((a, b) => Number(a.completee) - Number(b.completee))
            .map((t) => ({
              cle: `t:${t.id}`,
              groupe: 'Tâches' as const,
              libelle: t.titre,
              detail: t.completee ? 'faite' : nomZone(t.zoneId),
              ouvrir: () => {
                onFermer()
                onOuvrirTache(t.id)
              },
            })),
          ...(equipements ?? [])
            .filter((e) => correspond(terme, [e.nom, e.marque, e.modele, nomZone(e.zoneId)]))
            .map((e) => ({
              cle: `e:${e.id}`,
              groupe: 'Équipements' as const,
              libelle: e.nom,
              detail: [e.marque, nomZone(e.zoneId)].filter(Boolean).join(' · ') || null,
              ouvrir: aller(`/equipements?id=${e.id}`),
            })),
          ...(documents ?? [])
            .filter((d) => correspond(terme, [d.titre, d.notes, d.nomFichier, d.dossier]))
            .map((d) => ({
              cle: `d:${d.id}`,
              groupe: 'Documents' as const,
              libelle: d.titre,
              detail: d.dossier,
              ouvrir: aller(`/documents?id=${d.id}`),
            })),
          ...(zones ?? [])
            .filter((z) => correspond(terme, [z.nom]))
            .map((z) => ({
              cle: `z:${z.id}`,
              groupe: 'Pièces' as const,
              libelle: z.nom,
              detail: null,
              ouvrir: aller(`/pieces?zone=${z.id}`),
            })),
        ]

  // On plafonne chaque groupe, en retenant combien on en a tu.
  const comptes = new Map<string, number>()
  const resultats = candidats.filter((r) => {
    const rang = (comptes.get(r.groupe) ?? 0) + 1
    comptes.set(r.groupe, rang)
    return rang <= MAX_PAR_GROUPE
  })
  const actifBorne = Math.min(actif, Math.max(resultats.length - 1, 0))

  useEffect(() => {
    liste.current
      ?.querySelector(`[data-index="${actifBorne}"]`)
      ?.scrollIntoView?.({ block: 'nearest' })
  }, [actifBorne])

  function surTouche(e: KeyboardEvent) {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActif(Math.min(actifBorne + 1, resultats.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActif(Math.max(actifBorne - 1, 0))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      resultats[actifBorne]?.ouvrir()
    } else if (e.key === 'Escape') {
      e.stopPropagation()
      onFermer()
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-encre/30 p-4 pt-[12vh]"
      onClick={onFermer}
    >
      <div
        role="dialog"
        aria-label="Recherche"
        className="flex max-h-[70dvh] w-full max-w-xl flex-col overflow-hidden rounded-[28px] bg-carte shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <label className="flex items-center gap-3 border-b border-dashed border-tiret px-6 py-4">
          <Search className="size-5 shrink-0 text-dore" />
          <input
            autoFocus
            role="combobox"
            aria-label="Chercher dans la maison"
            aria-expanded={resultats.length > 0}
            aria-controls={idListe}
            aria-activedescendant={resultats.length > 0 ? `${idListe}-${actifBorne}` : undefined}
            value={terme}
            onChange={(e) => {
              setTerme(e.target.value)
              setActif(0)
            }}
            onKeyDown={surTouche}
            placeholder="Tâches, équipements, documents, pièces…"
            className="min-w-0 flex-1 bg-transparent text-lg text-texte focus:outline-none"
          />
          <kbd className="rounded-md bg-creux px-1.5 text-xs font-bold text-sourdine">Échap</kbd>
        </label>

        {terme.trim() === '' ? (
          <p className="px-6 py-5 text-sm text-sourdine">
            Tape quelques lettres — les accents et l’ordre des mots ne comptent pas.
          </p>
        ) : resultats.length === 0 ? (
          <p className="px-6 py-5 text-sm text-sourdine">Rien ne correspond à « {terme.trim()} ».</p>
        ) : (
          <ul ref={liste} id={idListe} role="listbox" className="overflow-y-auto py-2">
            {resultats.map((r, i) => {
              const premier = r.groupe !== resultats[i - 1]?.groupe
              const dernier = r.groupe !== resultats[i + 1]?.groupe
              const tus = (comptes.get(r.groupe) ?? 0) - MAX_PAR_GROUPE
              return (
                <li key={r.cle} role="presentation">
                  {premier && (
                    <div className="px-6 pb-1 pt-3 text-[11px] font-extrabold uppercase tracking-[0.08em] text-sourdine">
                      {r.groupe}
                    </div>
                  )}
                  <div
                    id={`${idListe}-${i}`}
                    data-index={i}
                    role="option"
                    aria-selected={i === actifBorne}
                    onMouseEnter={() => setActif(i)}
                    onClick={r.ouvrir}
                    className={cn(
                      'mx-2 flex cursor-pointer items-baseline gap-3 rounded-xl px-4 py-2 text-sm',
                      i === actifBorne && 'bg-creux',
                    )}
                  >
                    <span className="min-w-0 truncate font-bold">{r.libelle}</span>
                    {r.detail && (
                      <span className="ml-auto shrink-0 text-xs text-sourdine">{r.detail}</span>
                    )}
                  </div>
                  {dernier && tus > 0 && (
                    <div className="px-6 pt-0.5 text-xs text-sourdine">
                      et {tus} autre{tus > 1 ? 's' : ''} — précise la recherche
                    </div>
                  )}
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </div>
  )
}
