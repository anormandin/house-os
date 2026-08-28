import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CalendarRange, ChevronDown, ChevronUp, List, Paperclip, Plus } from 'lucide-react'
import Avatar from '@/components/Avatar'
import TacheEditeur, { editeurDejaOuvert } from '@/components/TacheEditeur'
import { api, dateLocaleIso, type CompteARebours, type TacheResume } from '@/lib/api'
import { useCompletionAvecUndo } from '@/lib/completion'
import { dateCourte, jourCourt } from '@/lib/format'
import {
  chipsRecurrence,
  construireRuban,
  diffJours,
  grouperParRythme,
  tonEcheance,
  type ChipRecurrence,
  type GrappeJour,
  type PointRuban,
} from '@/lib/taches-vues'
import { cn } from '@/lib/utils'

// Console de gestion des définitions de tâches : vue Rythmes (défaut) et vue Année
// derrière un commutateur binaire, dernier mode mémorisé — voir
// vault/Decisions/D-2026-08-26 Page Tâches Rythmes Et Année.md (itération A) et
// D-2026-08-26 Vue Année Défilante.md (ruban, option D de la ronde 3).

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

/** Échelle du ruban (px par jour) et largeur de la colonne d'étiquettes sticky. */
const PX_JOUR = 11
const ETIQUETTE_PX = 224

const MINI_MOIS = ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D']
const MOIS_COURTS_NOTE = [
  'janv',
  'févr',
  'mars',
  'avr',
  'mai',
  'juin',
  'juil',
  'août',
  'sept',
  'oct',
  'nov',
  'déc',
]

/** Compléter une occurrence depuis la console — on peut prendre de l'avance :
 * le moteur matérialise la suivante à partir de max(complétion, échéance).
 * Toast avec undo et invalidations croisées : lib/completion.ts (issue #60). */
function useCompleter() {
  return useCompletionAvecUndo().completer
}

function CaseCompleter({
  tache,
  aujourdhui,
  taille = 20,
  completer,
}: {
  tache: TacheResume
  aujourdhui: string
  taille?: number
  completer: ReturnType<typeof useCompleter>
}) {
  if (tache.occurrenceId === null) {
    return null
  }
  const enRetard = tache.echeance !== null && tache.echeance < aujourdhui
  return (
    <button
      type="button"
      aria-label={`Compléter ${tache.titre}`}
      disabled={completer.isPending}
      onClick={() => completer.mutate(tache.occurrenceId!)}
      className={cn(
        'shrink-0 rounded-[7px] border-[2.5px] transition-colors hover:border-vert hover:bg-vert-fond',
        enRetard ? 'border-rouge' : 'border-coche',
      )}
      style={{ width: taille, height: taille }}
    />
  )
}

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
        <VueRuban
          taches={taches!}
          aujourdhui={aujourdhui}
          comptes={comptes ?? []}
          lieu={lieu}
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
  const completer = useCompleter()

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
              <div
                key={tache.id}
                className="flex w-full items-center gap-2.5 border-t border-dashed border-tiret py-2 text-sm first:border-t-0 hover:bg-creux/50"
              >
                <CaseCompleter tache={tache} aujourdhui={aujourdhui} completer={completer} />
                <div
                  role="button"
                  tabIndex={0}
                  onClick={() => onModifier(tache.id)}
                  onKeyDown={(e) => e.key === 'Enter' && onModifier(tache.id)}
                  className="flex min-w-0 flex-1 cursor-pointer items-center gap-2.5"
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
                </div>
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

// ————— Vue Année : ruban défilant (option D, ronde 3 des maquettes) —————

function Pastille({
  point,
  mensuelle,
  onClick,
}: {
  point: PointRuban
  mensuelle?: boolean
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'absolute top-2.5 flex -translate-x-1/2 flex-col items-center gap-1',
        point.passe && 'opacity-35',
      )}
      style={{ left: point.offset * PX_JOUR + PX_JOUR / 2 }}
    >
      <span
        className={cn('rounded-full', mensuelle ? 'size-2.5 bg-[#b9a8d1]' : 'size-3 bg-dore')}
      />
      <span
        className={cn(
          'text-[10px] font-extrabold whitespace-nowrap',
          mensuelle ? 'text-sourdine' : 'text-dore',
          point.libelle.startsWith('retard') && 'text-rouge',
        )}
      >
        {point.libelle}
      </span>
    </button>
  )
}

function EtiquetteRuban({
  tache,
  lieu,
  onModifier,
}: {
  tache: TacheResume
  lieu: (tache: TacheResume) => string | null
  onModifier: (tacheId: string) => void
}) {
  return (
    <button
      type="button"
      onClick={() => onModifier(tache.id)}
      className="sticky left-0 z-10 shrink-0 bg-carte px-4 py-2.5 text-left shadow-[10px_0_14px_-10px_rgba(84,84,100,0.22)]"
      style={{ width: ETIQUETTE_PX }}
    >
      <span className="block text-[13.5px] leading-tight font-bold">{tache.titre}</span>
      <span className="mt-1 flex flex-wrap items-center gap-1.5 text-[11.5px] text-sourdine">
        {chipsRecurrence(tache.recurrence).map((chip) => (
          <Chip key={chip.libelle} chip={chip} />
        ))}
        {lieu(tache)}
        {tache.assigneA !== null && <Avatar utilisateur={tache.assigneA} taille={18} />}
      </span>
    </button>
  )
}

function VueRuban({
  taches,
  aujourdhui,
  comptes,
  lieu,
  onModifier,
}: {
  taches: TacheResume[]
  aujourdhui: string
  comptes: CompteARebours[]
  lieu: (tache: TacheResume) => string | null
  onModifier: (tacheId: string) => void
}) {
  const ruban = construireRuban(taches, aujourdhui)
  const completer = useCompleter()
  const scrollRef = useRef<HTMLDivElement>(null)
  const bandeRef = useRef<HTMLDivElement>(null)
  const [fenetreMini, setFenetreMini] = useState<{ gauche: number; largeur: number } | null>(null)
  const [bandeDepliee, setBandeDepliee] = useState(true)

  const largeurPiste = ruban.jours * PX_JOUR
  const moisDebut = Number(ruban.debutDomaine.slice(5, 7))
  // Part de l'année couverte par le domaine, pour projeter le ruban sur la mini-carte.
  const anneeJours = diffJours(`${aujourdhui.slice(0, 4)}-12-31`, `${aujourdhui.slice(0, 4)}-01-01`) + 1
  const domainePctDebut = ((anneeJours - ruban.jours) / anneeJours) * 100

  const jalons = comptes
    .filter((c) => c.dateCible >= ruban.debutDomaine && c.dateCible.startsWith(aujourdhui.slice(0, 4)))
    .map((c) => ({ ...c, offset: diffJours(c.dateCible, ruban.debutDomaine) }))
    .sort((a, b) => a.offset - b.offset)
  // Deux voies d'étiquettes : une voisine à moins de ~16 jours descend sur la seconde.
  const finVoie = [-Infinity, -Infinity]
  const jalonsAvecVoie = jalons.map((jalon) => {
    const voie = jalon.offset * PX_JOUR - finVoie[0] > 180 ? 0 : 1
    finVoie[voie] = jalon.offset * PX_JOUR
    return { ...jalon, voie }
  })

  // Au montage : aujourd'hui ancré à ~20 % du bord gauche de la fenêtre visible.
  useEffect(() => {
    const conteneur = scrollRef.current
    if (conteneur === null || conteneur.clientWidth === 0) {
      return
    }
    conteneur.scrollLeft = Math.max(
      0,
      ruban.aujourdhuiOffset * PX_JOUR - (conteneur.clientWidth - ETIQUETTE_PX) * 0.2,
    )
    surDefilement()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function surDefilement() {
    const conteneur = scrollRef.current
    if (conteneur === null || conteneur.clientWidth === 0) {
      return
    }
    const visible = conteneur.clientWidth - ETIQUETTE_PX
    const fraction = (100 - domainePctDebut) / largeurPiste
    setFenetreMini({
      gauche: domainePctDebut + conteneur.scrollLeft * fraction,
      largeur: visible * fraction,
    })
  }

  function teleporter(e: React.MouseEvent<HTMLDivElement>) {
    const conteneur = scrollRef.current
    if (conteneur === null) {
      return
    }
    const rect = e.currentTarget.getBoundingClientRect()
    const pct = ((e.clientX - rect.left) / rect.width) * 100
    const cible = ((pct - domainePctDebut) / (100 - domainePctDebut)) * largeurPiste
    conteneur.scrollLeft = Math.max(0, cible - (conteneur.clientWidth - ETIQUETTE_PX) * 0.2)
  }

  // Au clavier, pas de position de souris : Entrée/Espace ramène à aujourd'hui.
  function teleporterClavier(e: React.KeyboardEvent<HTMLDivElement>) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      const conteneur = scrollRef.current
      if (conteneur === null) {
        return
      }
      conteneur.scrollLeft = Math.max(
        0,
        ruban.aujourdhuiOffset * PX_JOUR - (conteneur.clientWidth - ETIQUETTE_PX) * 0.2,
      )
    }
  }

  function surGrappe(grappe: GrappeJour) {
    if (grappe.taches.length === 1) {
      onModifier(grappe.taches[0].id)
      return
    }
    // Une grappe multiple renvoie à la bande semaine-par-semaine.
    setBandeDepliee(true)
    if (typeof bandeRef.current?.scrollIntoView === 'function') {
      bandeRef.current.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }

  return (
    <div className="flex flex-col gap-3">
      {/* Mini-carte de l'année : situe la fenêtre visible, cliquer téléporte. */}
      <div
        role="button"
        tabIndex={0}
        aria-label="Mini-carte de l’année — cliquer téléporte, Entrée ramène à aujourd’hui"
        className="relative h-9 cursor-pointer overflow-hidden rounded-xl bg-carte shadow-carte"
        onClick={teleporter}
        onKeyDown={teleporterClavier}
        title="Cliquer pour se téléporter"
      >
        <div className="absolute inset-0 grid grid-cols-12">
          {MINI_MOIS.map((m, i) => (
            <span
              key={`${m}-${i}`}
              className="grid place-items-center border-l border-[#eee8c8] text-[9.5px] font-extrabold text-[#c9c1a2] uppercase first:border-l-0"
            >
              {m}
            </span>
          ))}
        </div>
        {moisDebut > 1 && (
          <span className="absolute top-1/2 left-2 -translate-y-1/2 text-[9.5px] italic text-tiret-texte">
            {moisDebut === 2
              ? 'janv : rien à afficher'
              : `janv → ${MOIS_COURTS_NOTE[moisDebut - 2]} : rien à afficher`}
          </span>
        )}
        {jalons.map((jalon) => (
          <span
            key={jalon.id}
            className="absolute bottom-0.5 size-[5px] rounded-full bg-rouge"
            style={{ left: `${domainePctDebut + ((jalon.offset + 0.5) / ruban.jours) * (100 - domainePctDebut)}%` }}
          />
        ))}
        {fenetreMini !== null && (
          <div
            className="absolute top-0.5 bottom-0.5 rounded-lg border-2 border-orange bg-orange/10"
            style={{ left: `${fenetreMini.gauche}%`, width: `${fenetreMini.largeur}%` }}
          />
        )}
      </div>

      {/* Le ruban : ~3-4 mois visibles, défilement horizontal, étiquettes sticky. */}
      <div className="relative">
        <div
          ref={scrollRef}
          onScroll={surDefilement}
          className="overflow-x-auto rounded-[20px] bg-carte py-3 shadow-carte"
        >
          <div className="relative w-max">
            {/* Couche de fond : traits de mois et lignes de jalons pleine hauteur. */}
            <div
              className="pointer-events-none absolute top-16 bottom-0 z-0"
              style={{ left: ETIQUETTE_PX, width: largeurPiste }}
            >
              {ruban.moisDomaine.slice(1).map((mois) => (
                <span
                  key={mois.libelle}
                  className="absolute top-0 bottom-0 w-px bg-[#e9e2ba]"
                  style={{ left: mois.offset * PX_JOUR }}
                />
              ))}
              <span
                className="absolute top-0 bottom-0 w-0.5 bg-orange"
                style={{ left: ruban.aujourdhuiOffset * PX_JOUR + PX_JOUR / 2 }}
              />
              {jalonsAvecVoie.map((jalon) => (
                <span
                  key={jalon.id}
                  className="absolute top-0 bottom-0 w-0.5"
                  style={{
                    left: jalon.offset * PX_JOUR + PX_JOUR / 2,
                    background:
                      'repeating-linear-gradient(to bottom, var(--rouge) 0 4px, transparent 4px 8px)',
                  }}
                />
              ))}
            </div>

            {/* Rangée des mois. */}
            <div className="flex">
              <div className="sticky left-0 z-10 shrink-0 bg-carte" style={{ width: ETIQUETTE_PX }} />
              <div className="relative h-6" style={{ width: largeurPiste }}>
                {ruban.moisDomaine.map((mois) => (
                  <b
                    key={mois.libelle}
                    className="absolute top-1 text-[10.5px] font-extrabold tracking-wider text-tiret-texte uppercase"
                    style={{ left: mois.offset * PX_JOUR + 6 }}
                  >
                    {mois.libelle}
                  </b>
                ))}
              </div>
            </div>

            {/* Étage des jalons : deux voies, jamais de chevauchement. */}
            <div className="flex">
              <div
                className="sticky left-0 z-10 shrink-0 bg-carte px-4 pt-1 text-[10px] font-extrabold tracking-wider text-[#c9c1a2] uppercase"
                style={{ width: ETIQUETTE_PX }}
              >
                Jalons
              </div>
              <div className="relative h-11" style={{ width: largeurPiste }}>
                <span
                  className="absolute top-0.5 -translate-x-1/2 rounded-full bg-orange px-2.5 py-px text-[10px] font-extrabold whitespace-nowrap text-carte"
                  style={{ left: ruban.aujourdhuiOffset * PX_JOUR + PX_JOUR / 2 }}
                >
                  Aujourd’hui
                </span>
                {jalonsAvecVoie.map((jalon) => (
                  <span
                    key={jalon.id}
                    className={cn(
                      'absolute rounded-full border border-rouge bg-carte px-2.5 py-px text-[10px] font-extrabold whitespace-nowrap text-rouge',
                      // Près du bord droit, l'étiquette s'aligne à gauche de sa ligne
                      // au lieu d'être rognée par le conteneur défilant.
                      jalon.offset * PX_JOUR > largeurPiste - 100
                        ? '-translate-x-full'
                        : '-translate-x-1/2',
                    )}
                    style={{
                      left: jalon.offset * PX_JOUR + PX_JOUR / 2,
                      top: jalon.voie === 0 ? 2 : 22,
                    }}
                  >
                    {jalon.titre} · {dateCourte(jalon.dateCible)}
                  </span>
                ))}
              </div>
            </div>

            {/* Une rangée riche par tâche. */}
            {ruban.lignes.map((ligne) => (
              <div key={ligne.tache.id} className="flex items-stretch border-t border-dashed border-tiret">
                <EtiquetteRuban tache={ligne.tache} lieu={lieu} onModifier={onModifier} />
                <div className="relative min-h-[62px]" style={{ width: largeurPiste }}>
                  {ligne.type === 'mensuelle' &&
                    ligne.points.map((point) => (
                      <Pastille
                        key={point.offset}
                        point={point}
                        mensuelle
                        onClick={() => onModifier(ligne.tache.id)}
                      />
                    ))}
                  {ligne.type === 'fenetre' && (
                    <>
                      {ligne.segments.map((segment) => (
                        <button
                          key={segment.debut}
                          type="button"
                          onClick={() => onModifier(ligne.tache.id)}
                          className={cn(
                            'absolute top-4 flex h-5 items-center overflow-hidden rounded-full border px-3 text-[10.5px] font-extrabold whitespace-nowrap',
                            ligne.enCours
                              ? 'border-vert/45 bg-vert-fond text-vert'
                              : 'border-jaune bg-[#f6efd6] text-dore',
                          )}
                          style={{
                            left: segment.debut * PX_JOUR,
                            width: (segment.fin - segment.debut) * PX_JOUR,
                          }}
                        >
                          {ligne.libelle}
                        </button>
                      ))}
                      {ligne.vise !== null && (
                        <button
                          type="button"
                          onClick={() => onModifier(ligne.tache.id)}
                          className="absolute top-10 -translate-x-1/2 text-[10px] font-extrabold whitespace-nowrap text-dore"
                          style={{ left: ligne.vise.offset * PX_JOUR + PX_JOUR / 2 }}
                        >
                          {ligne.vise.libelle}
                        </button>
                      )}
                    </>
                  )}
                  {(ligne.type === 'annuelle' || ligne.type === 'intervalle') && (
                    <>
                      {ligne.point !== null && (
                        <Pastille point={ligne.point} onClick={() => onModifier(ligne.tache.id)} />
                      )}
                      {ligne.note !== null && (
                        <span
                          className="absolute top-5 text-[10.5px] font-bold whitespace-nowrap italic text-tiret-texte"
                          style={{
                            left:
                              ligne.point !== null
                                ? Math.min(ligne.point.offset * PX_JOUR + 90, largeurPiste - 180)
                                : largeurPiste - 220,
                          }}
                        >
                          {ligne.note}
                        </span>
                      )}
                    </>
                  )}
                </div>
              </div>
            ))}

            {/* Rangée des ponctuelles : grappes datées, détail dans la bande. */}
            {(ruban.grappes.length > 0 || ruban.noteAnProchain !== null) && (
              <div className="flex items-stretch border-t border-dashed border-tiret">
                <div
                  className="sticky left-0 z-10 shrink-0 bg-carte px-4 py-2.5 shadow-[10px_0_14px_-10px_rgba(84,84,100,0.22)]"
                  style={{ width: ETIQUETTE_PX }}
                >
                  <span className="block text-[13.5px] font-bold">Ponctuelles</span>
                  <span className="mt-1 flex items-center gap-1.5 text-[11.5px] text-sourdine">
                    <Chip
                      chip={{ libelle: `${ruban.totalPonctuelles} à faire`, classe: 'saison' }}
                    />
                    détail ci-dessous ↓
                  </span>
                </div>
                <div className="relative min-h-[68px]" style={{ width: largeurPiste }}>
                  {ruban.grappes.map((grappe) => (
                    <button
                      key={grappe.date}
                      type="button"
                      title={grappe.taches.map((t) => t.titre).join('\n')}
                      onClick={() => surGrappe(grappe)}
                      className="absolute flex -translate-x-1/2 flex-col items-center gap-1"
                      style={{
                        left: grappe.offset * PX_JOUR + PX_JOUR / 2,
                        top: grappe.voie === 0 ? 8 : 34,
                      }}
                    >
                      <span
                        className={cn(
                          'grid place-items-center rounded-full bg-orange font-extrabold text-carte',
                          grappe.taches.length >= 10
                            ? 'size-6 text-[10.5px]'
                            : grappe.taches.length >= 3
                              ? 'size-[18px] text-[9.5px]'
                              : 'size-[15px] text-[8.5px]',
                        )}
                      >
                        {grappe.taches.length}
                      </span>
                      {grappe.voie === 0 && (
                        <span className="text-[10px] font-extrabold whitespace-nowrap text-orange">
                          {grappe.libelle}
                        </span>
                      )}
                    </button>
                  ))}
                  {ruban.noteAnProchain !== null && (
                    <span
                      className="absolute top-5 text-[10.5px] font-bold whitespace-nowrap italic text-tiret-texte"
                      style={{ left: largeurPiste - 220 }}
                    >
                      {ruban.noteAnProchain}
                    </span>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
        {/* Voile de droite : indique que le ruban continue. */}
        <span className="pointer-events-none absolute top-0 right-0 bottom-3.5 w-12 rounded-r-[20px] bg-gradient-to-l from-carte to-transparent" />
      </div>

      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 px-1.5 text-[11.5px] text-sourdine">
        <span className="flex items-center gap-1.5 font-bold">
          <span className="h-3 w-6 rounded-full border border-vert/45 bg-vert-fond" /> fenêtre en
          cours
        </span>
        <span className="flex items-center gap-1.5 font-bold">
          <span className="h-3 w-6 rounded-full border border-jaune bg-[#f6efd6]" /> fenêtre à venir
        </span>
        <span className="flex items-center gap-1.5 font-bold">
          <span className="size-2.5 rounded-full bg-dore" /> annuelle / long intervalle
        </span>
        <span className="flex items-center gap-1.5 font-bold">
          <span className="size-2.5 rounded-full bg-[#b9a8d1]" /> mensuelle
        </span>
        <span className="flex items-center gap-1.5 font-bold">
          <span className="size-2.5 rounded-full bg-orange" /> ponctuelles
        </span>
        <span className="ml-auto font-bold text-tiret-texte italic">
          Cliquer une pastille ouvre l’éditeur · une grappe ouvre la liste
        </span>
      </div>

      {/* Bande accordéon : les ponctuelles semaine par semaine. */}
      {ruban.totalPonctuelles > 0 && (
        <div ref={bandeRef} className="rounded-[20px] bg-carte px-5 py-4 shadow-carte">
          <div className="flex items-baseline gap-3">
            <h3 className="text-lg font-bold">Les ponctuelles — semaine par semaine</h3>
            <span className="rounded-full bg-creux px-2 text-xs font-bold text-dore">
              {ruban.totalPonctuelles}
            </span>
            <button
              type="button"
              onClick={() => setBandeDepliee(bandeDepliee === false)}
              className="ml-auto flex items-center gap-1 text-xs font-bold text-dore"
            >
              {bandeDepliee ? (
                <>
                  Replier <ChevronUp className="size-3.5" />
                </>
              ) : (
                <>
                  Déplier <ChevronDown className="size-3.5" />
                </>
              )}
            </button>
          </div>
          {bandeDepliee && (
            <div className="mt-3 grid grid-cols-1 gap-x-8 gap-y-4 md:grid-cols-2 lg:grid-cols-3">
              {ruban.enRetard.length > 0 && (
                <SemaineBloc
                  titre="En retard"
                  taches={ruban.enRetard}
                  retard
                  aujourdhui={aujourdhui}
                  completer={completer}
                  onModifier={onModifier}
                />
              )}
              {ruban.semaines.map((semaine) => (
                <SemaineBloc
                  key={semaine.lundi}
                  titre={semaine.libelle}
                  taches={semaine.taches}
                  aujourdhui={aujourdhui}
                  completer={completer}
                  onModifier={onModifier}
                />
              ))}
            </div>
          )}
        </div>
      )}

      {ruban.tempoCourt.length > 0 && (
        <div className="flex flex-wrap items-center gap-2 rounded-2xl bg-carte px-4 py-3 shadow-carte">
          <span className="mr-1 text-[11px] font-extrabold tracking-wider text-dore uppercase">
            Le tempo court
          </span>
          {ruban.tempoCourt.map((tache) => (
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
            Cadences de 15 jours et moins — les longs intervalles vivent sur la chronologie.
          </span>
        </div>
      )}
    </div>
  )
}

function SemaineBloc({
  titre,
  taches,
  retard = false,
  aujourdhui,
  completer,
  onModifier,
}: {
  titre: string
  taches: TacheResume[]
  retard?: boolean
  aujourdhui: string
  completer: ReturnType<typeof useCompleter>
  onModifier: (tacheId: string) => void
}) {
  return (
    <div>
      <h4
        className={cn(
          'mb-1 text-[13px] font-bold',
          retard ? 'text-rouge' : 'text-encre',
        )}
      >
        {titre} <span className="font-normal text-sourdine">· {taches.length}</span>
      </h4>
      {taches.map((tache) => (
        <div
          key={tache.id}
          className="flex w-full items-center gap-2 border-t border-dashed border-tiret py-1.5 text-[12.5px] first:border-t-0 hover:bg-creux/50"
        >
          <CaseCompleter tache={tache} aujourdhui={aujourdhui} taille={16} completer={completer} />
          <div
            role="button"
            tabIndex={0}
            onClick={() => onModifier(tache.id)}
            onKeyDown={(e) => e.key === 'Enter' && onModifier(tache.id)}
            className="flex min-w-0 flex-1 cursor-pointer items-center gap-2"
          >
            <span className="min-w-0 flex-1 truncate">{tache.titre}</span>
            <span
              className={cn(
                'shrink-0 text-[11px] font-bold tabular-nums',
                retard ? 'text-rouge' : 'text-sourdine',
              )}
            >
              {tache.echeance !== null &&
                `${jourCourt(tache.echeance)} ${Number(tache.echeance.slice(8, 10))}`}
            </span>
            {tache.assigneA !== null && <Avatar utilisateur={tache.assigneA} taille={18} />}
          </div>
        </div>
      ))}
    </div>
  )
}
