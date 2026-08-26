import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CalendarRange, List, Paperclip, Plus } from 'lucide-react'
import Avatar from '@/components/Avatar'
import TacheEditeur, { editeurDejaOuvert } from '@/components/TacheEditeur'
import { api, dateLocaleIso, type TacheResume } from '@/lib/api'
import { dateCourte, jourCourt } from '@/lib/format'
import {
  chipsRecurrence,
  construireAnnee,
  grouperParRythme,
  pctDansAnnee,
  tonEcheance,
  type ChipRecurrence,
} from '@/lib/taches-vues'
import { cn } from '@/lib/utils'

// Console de gestion des définitions de tâches : vue Rythmes (défaut) et vue Année
// derrière un commutateur binaire, dernier mode mémorisé — voir
// vault/Decisions/D-2026-08-26 Page Tâches Rythmes Et Année.md (itération A).

type Vue = 'liste' | 'annee'

const CLE_VUE = 'taches-vue'

// localStorage peut être absent ou lever (navigation privée) : la page doit
// rendre normalement sans mémoire de mode dans ce cas.
function lireVueMemorisee(): Vue {
  try {
    return localStorage.getItem(CLE_VUE) === 'annee' ? 'annee' : 'liste'
  } catch {
    return 'liste'
  }
}

function memoriserVue(vue: Vue) {
  try {
    localStorage.setItem(CLE_VUE, vue)
  } catch {
    // Stockage indisponible : tant pis pour la mémoire du mode.
  }
}

const STRATEGIES: Record<TacheResume['strategie'], string> = {
  Fixe: 'Fixe',
  Alternance: 'Alternance',
  MoinsLAFait: 'Moins-l’a-fait',
}

const COULEURS_CHIPS: Record<ChipRecurrence['classe'], string> = {
  hebdo: 'text-[#4d699b]',
  intervalle: 'text-[#597b75]',
  mois: 'text-[#624c83]',
  annuelle: 'text-dore',
  saison: 'text-orange',
}

const MOIS_AXE = [
  'Janv',
  'Févr',
  'Mars',
  'Avr',
  'Mai',
  'Juin',
  'Juil',
  'Août',
  'Sept',
  'Oct',
  'Nov',
  'Déc',
]

/** Marge gauche de la piste (px) : la colonne des étiquettes de la vue Année. */
const ETIQUETTE_PX = 200

function Chip({ chip }: { chip: ChipRecurrence }) {
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center rounded-full border border-tiret bg-creux px-2.5 py-px text-[11.5px] leading-relaxed font-bold whitespace-nowrap',
        COULEURS_CHIPS[chip.classe],
      )}
    >
      {chip.libelle}
    </span>
  )
}

function LibelleEcheance({ echeance, aujourdhui }: { echeance: string | null; aujourdhui: string }) {
  const ton = tonEcheance(echeance, aujourdhui)
  if (ton === 'aucune') {
    return <span className="text-sourdine">—</span>
  }
  const texte =
    echeance === aujourdhui ? 'Aujourd’hui' : `${jourCourt(echeance!)} ${dateCourte(echeance!)}`
  return (
    <span
      className={cn(
        'tabular-nums',
        ton === 'retard' && 'font-extrabold text-rouge',
        ton === 'proche' && 'font-extrabold text-dore',
        ton === 'normal' && 'text-sourdine',
      )}
    >
      {ton === 'retard' ? `retard · ${texte}` : texte}
    </span>
  )
}

export default function Taches() {
  const [vue, setVue] = useState<Vue>(lireVueMemorisee)
  const [editeur, setEditeur] = useState<{ tacheId: string | null } | null>(null)
  const aujourdhui = dateLocaleIso()

  const { data: taches, isLoading } = useQuery({ queryKey: ['taches'], queryFn: api.taches })
  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })
  const { data: comptes } = useQuery({
    queryKey: ['comptes-a-rebours'],
    queryFn: api.comptesARebours,
  })

  useEffect(() => {
    memoriserVue(vue)
  }, [vue])

  // ⌘K ouvre l'éditeur de création (l'ancienne barre quick-add a disparu avec la refonte).
  useEffect(() => {
    function surTouche(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        if (editeurDejaOuvert()) {
          return
        }
        setEditeur({ tacheId: null })
      }
    }
    window.addEventListener('keydown', surTouche)
    return () => window.removeEventListener('keydown', surTouche)
  }, [])

  function lieu(tache: TacheResume): string | null {
    const morceaux = [
      zones?.find((z) => z.id === tache.zoneId)?.nom,
      equipements?.find((e) => e.id === tache.equipementId)?.nom,
    ].filter((m): m is string => m !== undefined)
    return morceaux.length > 0 ? morceaux.join(' · ') : null
  }

  const jalons = (comptes ?? [])
    .filter((c) => c.dateCible.startsWith(aujourdhui.slice(0, 4)))
    .map((c) => ({ ...c, position: pctDansAnnee(c.dateCible) }))

  return (
    <div className="mx-auto flex max-w-[1100px] flex-col gap-5">
      <div className="flex items-end justify-between gap-3">
        <h1 className="text-4xl font-bold">Toutes les tâches</h1>
        <div className="flex items-center gap-2">
          <div className="flex rounded-full bg-carte p-1 shadow-carte">
            {(
              [
                { cle: 'liste', libelle: 'Liste', Icone: List },
                { cle: 'annee', libelle: 'Année', Icone: CalendarRange },
              ] as const
            ).map(({ cle, libelle, Icone }) => (
              <button
                key={cle}
                type="button"
                onClick={() => setVue(cle)}
                className={cn(
                  'flex items-center gap-1.5 rounded-full px-4 py-1 text-sm font-bold transition-colors',
                  vue === cle ? 'bg-orange text-carte' : 'text-sourdine hover:text-dore',
                )}
              >
                <Icone className="size-3.5" /> {libelle}
              </button>
            ))}
          </div>
          <button
            type="button"
            onClick={() => setEditeur({ tacheId: null })}
            className="flex items-center gap-1.5 rounded-full bg-orange px-4 py-1.5 text-sm font-bold text-carte"
          >
            <Plus className="size-4" /> Nouvelle
          </button>
        </div>
      </div>

      {isLoading ? (
        <p className="py-8 text-center text-sm text-sourdine">Chargement…</p>
      ) : (taches ?? []).length === 0 ? (
        <p className="py-8 text-center text-sm text-sourdine">
          Aucune tâche encore — ⌘K pour créer la première.
        </p>
      ) : vue === 'liste' ? (
        <VueListe
          taches={taches!}
          aujourdhui={aujourdhui}
          lieu={lieu}
          onModifier={(tacheId) => setEditeur({ tacheId })}
        />
      ) : (
        <VueAnnee
          taches={taches!}
          aujourdhui={aujourdhui}
          jalons={jalons}
          onModifier={(tacheId) => setEditeur({ tacheId })}
        />
      )}

      {editeur !== null && (
        <TacheEditeur tacheId={editeur.tacheId} onFermer={() => setEditeur(null)} />
      )}
    </div>
  )
}

function VueListe({
  taches,
  aujourdhui,
  lieu,
  onModifier,
}: {
  taches: TacheResume[]
  aujourdhui: string
  lieu: (tache: TacheResume) => string | null
  onModifier: (tacheId: string) => void
}) {
  const { groupes, progression } = grouperParRythme(taches, aujourdhui)

  return (
    <div className="flex flex-col gap-4">
      {groupes.map((groupe) => (
        <section key={groupe.cle} className="rounded-[20px] bg-carte px-5 py-4 shadow-carte">
          <header className="flex items-baseline gap-3 pb-1">
            <h3 className="text-lg font-bold">{groupe.titre}</h3>
            <span className="rounded-full bg-creux px-2 text-xs font-bold text-dore">
              {groupe.taches.length}
            </span>
            {groupe.enRetard > 0 && (
              <span className="ml-auto text-xs font-bold text-rouge">
                {groupe.enRetard} en retard
              </span>
            )}
          </header>
          {groupe.cle === 'ponctuelles' && progression.total > 0 && (
            <div className="flex items-center gap-2.5 pb-2 text-xs font-bold text-dore">
              <span className="h-2 w-44 overflow-hidden rounded-full bg-barre-piste">
                <span
                  className="block h-full rounded-full bg-vert"
                  style={{ width: `${(progression.faites / progression.total) * 100}%` }}
                />
              </span>
              {progression.faites} faites sur {progression.total}
            </div>
          )}
          <div>
            {groupe.taches.map((tache) => (
              <button
                key={tache.id}
                type="button"
                onClick={() => onModifier(tache.id)}
                className="flex w-full items-center gap-2.5 border-t border-dashed border-tiret py-2 text-left text-sm first:border-t-0 hover:bg-creux/50"
              >
                <span className="min-w-0 truncate font-bold">{tache.titre}</span>
                {chipsRecurrence(tache.recurrence).map((chip) => (
                  <Chip key={chip.libelle} chip={chip} />
                ))}
                {lieu(tache) !== null && (
                  <span className="shrink-0 text-xs text-sourdine">{lieu(tache)}</span>
                )}
                {tache.nbDocuments > 0 && (
                  <span className="flex shrink-0 items-center gap-0.5 text-xs text-sourdine">
                    <Paperclip className="size-3" />
                    {tache.nbDocuments}
                  </span>
                )}
                <span className="ml-auto w-36 shrink-0 text-right text-xs">
                  <LibelleEcheance echeance={tache.echeance} aujourdhui={aujourdhui} />
                </span>
                <span className="flex w-32 shrink-0 items-center justify-end gap-2 text-xs text-sourdine">
                  {tache.assigneA !== null && <Avatar utilisateur={tache.assigneA} taille={22} />}
                  {tache.recurrence.mode === 'Ponctuelle' ? '' : STRATEGIES[tache.strategie]}
                </span>
              </button>
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

function VueAnnee({
  taches,
  aujourdhui,
  jalons,
  onModifier,
}: {
  taches: TacheResume[]
  aujourdhui: string
  jalons: { id: string; titre: string; dateCible: string; position: number }[]
  onModifier: (tacheId: string) => void
}) {
  const annee = construireAnnee(taches, aujourdhui)
  const gauche = (pct: number) =>
    `calc(${ETIQUETTE_PX}px + (100% - ${ETIQUETTE_PX}px) * ${pct / 100})`

  return (
    <div className="rounded-[20px] bg-carte px-6 pt-11 pb-4 shadow-carte">
      <div className="relative">
        {/* Ligne d'aujourd'hui et jalons des comptes à rebours, par-dessus toute la chronologie. */}
        <div
          className="absolute -top-6 bottom-0 z-10 w-0.5 bg-orange"
          style={{ left: gauche(annee.aujourdhuiPct) }}
        >
          <span className="absolute -top-0.5 -left-8 rounded-full bg-orange px-2 py-px text-[10px] font-extrabold whitespace-nowrap text-carte">
            Aujourd’hui
          </span>
        </div>
        {jalons.map((jalon) => (
          <div
            key={jalon.id}
            className="absolute -top-6 bottom-0 z-10 w-0.5"
            style={{
              left: gauche(jalon.position),
              background: 'repeating-linear-gradient(to bottom, var(--rouge) 0 4px, transparent 4px 8px)',
            }}
          >
            <span className="absolute -top-0.5 left-1.5 text-[10px] font-extrabold whitespace-nowrap text-rouge">
              {jalon.titre} · {dateCourte(jalon.dateCible)}
            </span>
          </div>
        ))}

        <div
          className="grid grid-cols-12 pb-1.5 text-[10.5px] font-extrabold tracking-wider text-tiret-texte uppercase"
          style={{ marginLeft: ETIQUETTE_PX }}
        >
          {MOIS_AXE.map((mois) => (
            <span key={mois}>{mois}</span>
          ))}
        </div>

        {annee.lignes.map((ligne) => (
          <button
            key={ligne.tache.id}
            type="button"
            onClick={() => onModifier(ligne.tache.id)}
            className="grid w-full items-center gap-3.5 border-t border-dashed border-tiret py-2 text-left hover:bg-creux/50"
            style={{ gridTemplateColumns: `${ETIQUETTE_PX}px 1fr` }}
          >
            <span className="min-w-0">
              <span className="block truncate text-[13px] font-bold">{ligne.tache.titre}</span>
              <span className="mt-0.5 flex gap-1">
                {chipsRecurrence(ligne.tache.recurrence).map((chip) => (
                  <Chip key={chip.libelle} chip={chip} />
                ))}
              </span>
            </span>
            <span
              className="relative h-6 rounded-md"
              style={{
                backgroundImage: 'linear-gradient(to right, #e9e2ba 1px, transparent 1px)',
                backgroundSize: 'calc(100%/12) 100%',
              }}
            >
              {ligne.type === 'mensuelle' &&
                ligne.points.map((point) => (
                  <span
                    key={point}
                    className="absolute top-2 size-2 rounded-full bg-[#b9a8d1]"
                    style={{ left: `calc(${point}% - 4px)` }}
                  />
                ))}
              {ligne.type === 'fenetre' &&
                ligne.segments.map((segment) => (
                  <span
                    key={segment.debut}
                    className={cn(
                      'absolute top-[3px] flex h-[18px] items-center overflow-hidden rounded-full border px-2.5 text-[10.5px] font-extrabold whitespace-nowrap',
                      ligne.enCours
                        ? 'border-vert/45 bg-vert-fond text-vert'
                        : 'border-jaune bg-[#f6efd6] text-dore',
                    )}
                    style={{
                      left: `${segment.debut}%`,
                      width: `${segment.fin - segment.debut}%`,
                    }}
                  >
                    {ligne.libelle}
                  </span>
                ))}
              {ligne.type === 'annuelle' && (
                <>
                  <span
                    className="absolute top-1.5 size-3 rounded-full bg-dore"
                    style={{ left: `calc(${ligne.position}% - 6px)` }}
                  />
                  <span
                    className="absolute top-[5px] text-[10.5px] font-extrabold whitespace-nowrap text-dore"
                    style={{ left: `calc(${ligne.position}% + 10px)` }}
                  >
                    {ligne.libelle}
                  </span>
                </>
              )}
            </span>
          </button>
        ))}

        {annee.ponctuelles.length > 0 && (
          <div
            className="grid w-full items-center gap-3.5 border-t border-dashed border-tiret py-2"
            style={{ gridTemplateColumns: `${ETIQUETTE_PX}px 1fr` }}
          >
            <span>
              <span className="block text-[13px] font-bold">Ponctuelles</span>
              <span className="text-[11px] text-sourdine">
                le chiffre = combien ce jour-là
              </span>
            </span>
            <span className="relative h-6">
              {annee.ponctuelles.map((grappe) => (
                <button
                  key={grappe.date}
                  type="button"
                  title={grappe.taches.map((t) => t.titre).join('\n')}
                  onClick={
                    grappe.taches.length === 1 ? () => onModifier(grappe.taches[0].id) : undefined
                  }
                  className={cn(
                    'absolute top-1 grid size-4 place-items-center rounded-full bg-orange text-[9.5px] font-extrabold text-carte',
                    grappe.taches.length === 1 && 'cursor-pointer',
                  )}
                  style={{ left: `calc(${grappe.position}% - 8px)` }}
                >
                  {grappe.taches.length}
                </button>
              ))}
            </span>
          </div>
        )}
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 border-t border-dashed border-tiret pt-3 text-[11px] text-sourdine">
        <span className="flex items-center gap-1.5">
          <span className="h-3 w-6 rounded-full border border-vert/45 bg-vert-fond" /> fenêtre en cours
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-3 w-6 rounded-full border border-jaune bg-[#f6efd6]" /> fenêtre à venir
        </span>
        <span className="flex items-center gap-1.5">
          <span className="size-2.5 rounded-full bg-dore" /> annuelle
        </span>
        <span className="flex items-center gap-1.5">
          <span className="size-2.5 rounded-full bg-[#b9a8d1]" /> mensuelle
        </span>
        <span className="flex items-center gap-1.5">
          <span className="size-2.5 rounded-full bg-orange" /> ponctuelles
        </span>
        <span className="ml-auto italic">Cliquer une rangée ouvre l’éditeur</span>
      </div>

      {annee.tempoCourt.length > 0 && (
        <div className="mt-3 flex flex-wrap items-center gap-2 rounded-2xl bg-creux px-4 py-3">
          <span className="mr-1 text-[11px] font-extrabold tracking-wider text-tiret-texte uppercase">
            Le tempo court
          </span>
          {annee.tempoCourt.map((tache) => (
            <button key={tache.id} type="button" onClick={() => onModifier(tache.id)}>
              <Chip
                chip={{
                  libelle: `${tache.titre} · ${chipsRecurrence(tache.recurrence)[0]?.libelle ?? ''}`,
                  classe: chipsRecurrence(tache.recurrence)[0]?.classe ?? 'hebdo',
                }}
              />
            </button>
          ))}
          <span className="w-full text-[11px] text-sourdine">
            Trop fréquentes pour l’échelle de l’année — cliquer une pilule ouvre l’éditeur.
          </span>
        </div>
      )}
    </div>
  )
}
