import { useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  ArrowRight,
  CalendarDays,
  Check,
  LayoutGrid,
  Link2,
  Pencil,
  Plus,
  Share2,
  Upload,
  X,
} from 'lucide-react'
import {
  api,
  dateLocaleIso,
  type EnveloppeBudget,
  type EnveloppeBudgetDonnees,
  type ResumeBudget,
  type TransactionBudget,
  type TypeEnveloppe,
  type Versement,
} from '@/lib/api'
import {
  construireFlux,
  construirePartition,
  couleursEnveloppes,
  pctJauge,
  FLUX_LARGEUR,
} from '@/lib/budget-vues'
import { dollars, dateLisible } from '@/lib/format'
import { cn } from '@/lib/utils'

// Page Budget : fonds de prévoyance en enveloppes virtuelles. Vue « Aperçu »
// (flux raffiné, E1) par défaut et vue « Flux » (diagramme tracé, E2) derrière un
// commutateur binaire, dernier mode mémorisé — voir
// vault/Decisions/D-2026-08-26 Page Budget Flux Raffiné Et Bascule Flux Tracé.md.

type Vue = 'apercu' | 'flux'

const CLE_VUE = 'budget-vue'

function lireVueMemorisee(): Vue {
  try {
    return localStorage.getItem(CLE_VUE) === 'flux' ? 'flux' : 'apercu'
  } catch {
    return 'apercu'
  }
}

function memoriserVue(vue: Vue) {
  try {
    localStorage.setItem(CLE_VUE, vue)
  } catch {
    // Stockage indisponible : tant pis pour la mémoire du mode.
  }
}

const LIBELLES_TYPE: Record<TypeEnveloppe, string> = {
  Equipement: 'Équipement',
  Taxes: 'Taxes',
  Projet: 'Projet',
  Reserve: 'Réserve',
}

const COULEURS_CHIP: Record<TypeEnveloppe, string> = {
  Equipement: 'text-[#597b75]',
  Taxes: 'text-[#624c83]',
  Projet: 'text-[#4d699b]',
  Reserve: 'text-dore',
}

const MOTIF_NON_AFFECTE =
  'repeating-linear-gradient(45deg, #f0ead6 0 6px, #e6dfc2 6px 12px)'
const MOTIF_DEPASSEMENT =
  'repeating-linear-gradient(45deg, rgba(200,64,83,.28) 0 5px, rgba(200,64,83,.1) 5px 10px)'

/** « ~juin 2033 » pour les échéances lointaines dérivées. */
function moisAnnee(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString('fr-CA', {
    month: 'long',
    year: 'numeric',
  })
}

function ChipType({ type }: { type: TypeEnveloppe }) {
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center rounded-full border border-tiret bg-creux px-2.5 py-px text-[11.5px] leading-relaxed font-bold whitespace-nowrap',
        COULEURS_CHIP[type],
      )}
    >
      {LIBELLES_TYPE[type]}
    </span>
  )
}

function useInvaliderBudget() {
  const queryClient = useQueryClient()
  return () => {
    queryClient.invalidateQueries({ queryKey: ['budget'] })
    queryClient.invalidateQueries({ queryKey: ['budget-transactions'] })
    queryClient.invalidateQueries({ queryKey: ['budget-enveloppe'] })
  }
}

export default function Budget() {
  const [vue, setVue] = useState<Vue>(lireVueMemorisee)
  const [editeur, setEditeur] = useState<{ enveloppeId: string | null } | null>(null)
  const [compteOuvert, setCompteOuvert] = useState(false)
  const [rapport, setRapport] = useState<string | null>(null)
  const champFichier = useRef<HTMLInputElement>(null)
  const invalider = useInvaliderBudget()

  const { data: resume, isLoading } = useQuery({ queryKey: ['budget'], queryFn: api.budget })
  const { data: transactions } = useQuery({
    queryKey: ['budget-transactions'],
    queryFn: () => api.transactionsBudget(),
  })

  const importer = useMutation({
    mutationFn: (fichier: File) => api.importerReleve(fichier),
    onSuccess: (r) => {
      const morceaux = [`${r.importees} importée${r.importees > 1 ? 's' : ''}`]
      if (r.doublons > 0) morceaux.push(`${r.doublons} déjà connue${r.doublons > 1 ? 's' : ''}`)
      if (r.anterieures > 0) morceaux.push(`${r.anterieures} antérieure${r.anterieures > 1 ? 's' : ''} à l’ancrage`)
      setRapport(morceaux.join(' · '))
      invalider()
    },
  })

  function changerVue(prochaine: Vue) {
    setVue(prochaine)
    memoriserVue(prochaine)
  }

  if (isLoading || resume === undefined) {
    return <p className="py-8 text-center text-sm text-sourdine">Chargement…</p>
  }

  if (resume.compte === null) {
    return <Ancrage />
  }

  return (
    <div className="mx-auto flex max-w-[1240px] flex-col gap-4">
      <div className="flex items-end justify-between gap-3">
        <h1 className="text-4xl font-bold">Budget</h1>
        <div className="flex items-center gap-2">
          <div className="flex rounded-full bg-carte p-1 shadow-carte">
            {(
              [
                { cle: 'apercu', libelle: 'Aperçu', Icone: LayoutGrid },
                { cle: 'flux', libelle: 'Flux', Icone: Share2 },
              ] as const
            ).map(({ cle, libelle, Icone }) => (
              <button
                key={cle}
                type="button"
                onClick={() => changerVue(cle)}
                className={cn(
                  'flex items-center gap-1.5 rounded-full px-4 py-1 text-sm font-bold transition-colors',
                  vue === cle ? 'bg-orange text-carte' : 'text-sourdine hover:text-dore',
                )}
              >
                <Icone className="size-3.5" /> {libelle}
              </button>
            ))}
          </div>
          <input
            ref={champFichier}
            type="file"
            accept=".csv,.ofx,text/csv"
            className="hidden"
            onChange={(e) => {
              const fichier = e.target.files?.[0]
              if (fichier !== undefined) {
                importer.mutate(fichier)
                e.target.value = ''
              }
            }}
          />
          <button
            type="button"
            disabled={importer.isPending}
            onClick={() => champFichier.current?.click()}
            className="flex items-center gap-1.5 rounded-full bg-creux px-3.5 py-1.5 text-xs font-bold text-dore hover:text-orange disabled:opacity-40"
          >
            <Upload className="size-3.5" />
            {importer.isPending ? 'Import…' : 'Importer un fichier'}
          </button>
          <button
            type="button"
            onClick={() => setEditeur({ enveloppeId: null })}
            className="flex items-center gap-1.5 rounded-full bg-orange px-4 py-1.5 text-sm font-bold text-carte"
          >
            <Plus className="size-4" /> Nouvelle enveloppe
          </button>
        </div>
      </div>

      {rapport !== null && (
        <div className="flex items-center gap-2 rounded-2xl bg-vert-fond px-4 py-2 text-sm font-bold text-vert">
          <Check className="size-4 shrink-0" /> Import terminé : {rapport}.
          <button
            type="button"
            aria-label="Fermer le rapport d'import"
            onClick={() => setRapport(null)}
            className="ml-auto text-sourdine hover:text-texte"
          >
            <X className="size-4" />
          </button>
        </div>
      )}

      {resume.nonAffecte < 0 && (
        <div role="alert" className="flex items-center gap-3 rounded-2xl bg-rouge px-4.5 py-2.5 text-[13.5px] font-bold text-carte shadow-carte">
          <AlertTriangle className="size-4.5 shrink-0" />
          <span>
            Sur-allocation : les enveloppes totalisent{' '}
            <b className="tabular-nums">{dollars(resume.totalEnveloppes)}</b>, soit{' '}
            <b className="tabular-nums">{dollars(-resume.nonAffecte)}</b> de plus que le compte.
            Réduis une enveloppe ou attends le prochain virement.
          </span>
        </div>
      )}

      {vue === 'apercu' ? (
        <VueApercu resume={resume} onModifierCompte={() => setCompteOuvert(true)} />
      ) : (
        <VueFlux resume={resume} onModifierCompte={() => setCompteOuvert(true)} />
      )}

      {vue === 'apercu' && (
        <GrilleEnveloppes
          resume={resume}
          onOuvrir={(enveloppeId) => setEditeur({ enveloppeId })}
        />
      )}

      <Inbox resume={resume} transactions={transactions ?? []} />

      {editeur !== null && (
        <EnveloppeEditeur enveloppeId={editeur.enveloppeId} onFermer={() => setEditeur(null)} />
      )}
      {compteOuvert && <CompteEditeur resume={resume} onFermer={() => setCompteOuvert(false)} />}
    </div>
  )
}

// ——— Ancrage (premier écran, aucun compte) ———

function Ancrage() {
  const invalider = useInvaliderBudget()
  const [nom, setNom] = useState('Fonds de prévoyance')
  const [institution, setInstitution] = useState('')
  const [solde, setSolde] = useState('')
  const [date, setDate] = useState(dateLocaleIso())

  const ancrer = useMutation({
    mutationFn: () =>
      api.ancrerCompteBudget({
        nom,
        institution: institution || null,
        soldeInitial: Number(solde),
        dateAncrage: date,
      }),
    onSuccess: invalider,
  })

  const classeChamp =
    'rounded-xl bg-creux px-3 py-2 text-sm text-texte focus:outline-2 focus:outline-orange/60'

  return (
    <div className="mx-auto mt-10 flex w-full max-w-md flex-col gap-4 rounded-3xl bg-carte px-7 py-6 shadow-carte-lg">
      <h1 className="text-2xl font-bold">Ancrer le fonds de prévoyance</h1>
      <p className="text-sm text-texte">
        Un seul compte bancaire réel, partitionné en enveloppes virtuelles. Donne son
        solde d’aujourd’hui : ensuite, seules les transactions importées le feront bouger.
      </p>
      <input value={nom} onChange={(e) => setNom(e.target.value)} aria-label="Nom du compte"
        placeholder="Nom du compte" className={cn(classeChamp, 'font-bold')} />
      <input value={institution} onChange={(e) => setInstitution(e.target.value)}
        aria-label="Institution" placeholder="Institution (ex. Desjardins)" className={classeChamp} />
      <label className="flex items-center gap-2 text-xs text-sourdine">
        Solde initial
        <input type="number" step="0.01" value={solde} onChange={(e) => setSolde(e.target.value)}
          aria-label="Solde initial" placeholder="0,00" className={cn(classeChamp, 'flex-1')} />
      </label>
      <label className="flex items-center gap-2 text-xs text-sourdine">
        Ancré le
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)}
          aria-label="Date d'ancrage" className={cn(classeChamp, 'flex-1')} />
      </label>
      <button
        type="button"
        disabled={nom.trim().length === 0 || solde === '' || ancrer.isPending}
        onClick={() => ancrer.mutate()}
        className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
      >
        Ancrer le compte
      </button>
    </div>
  )
}

// ——— Vue Aperçu (E1) : chemin de l'argent + rythme mensuel ———

function VueApercu({
  resume,
  onModifierCompte,
}: {
  resume: ResumeBudget
  onModifierCompte: () => void
}) {
  const partition = construirePartition(resume)
  const sortiesAffichees = resume.sorties.slice(0, 4)

  return (
    <>
      <section className="grid grid-cols-[230px_34px_1fr_34px_300px] items-center rounded-[20px] bg-carte px-7 py-5 shadow-carte">
        {/* Entrée */}
        <div className="rounded-2xl bg-creux px-4.5 py-4">
          <div className="text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-dore">
            Entre chaque 1ᵉʳ du mois
          </div>
          <div className="font-titre text-3xl font-bold text-orange tabular-nums">
            {dollars(resume.virementSuggere)}
            <span className="font-sans text-sm font-bold text-dore"> / mois</span>
          </div>
          <div className="mt-0.5 text-xs text-sourdine">
            virement mensuel suggéré — Σ des provisions
          </div>
        </div>

        <div className="flex justify-center text-tiret-texte"><ArrowRight className="size-5" /></div>

        {/* Le compte partitionné */}
        <div>
          <div className="flex items-center gap-2 text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-sourdine">
            Le compte, partitionné
            <button
              type="button"
              aria-label="Modifier le compte"
              onClick={onModifierCompte}
              className="text-sourdine hover:text-orange"
            >
              <Pencil className="size-3" />
            </button>
          </div>
          <div className="flex items-baseline gap-3">
            <span className="font-titre text-[32px] font-bold text-encre tabular-nums">
              {dollars(resume.soldeCourant)}
            </span>
            {resume.nonAffecte >= 0 ? (
              <span className="flex items-center gap-1 text-xs font-bold text-vert">
                <Check className="size-3" /> solde = enveloppes + non affecté
              </span>
            ) : (
              <span className="flex items-center gap-1 text-xs font-extrabold text-rouge">
                <X className="size-3" /> enveloppes &gt; solde : {dollars(resume.nonAffecte)} non affecté
              </span>
            )}
          </div>
          {partition.segments.length > 0 && (
            <>
              <div className={cn('relative mt-2.5 flex h-[30px] rounded-xl border border-tiret',
                partition.surAllocation ? 'overflow-visible' : 'overflow-hidden')}>
                {partition.segments.map((segment, i) => (
                  <span
                    key={segment.id ?? 'non-affecte'}
                    title={`${segment.nom} — ${dollars(segment.montant)}`}
                    className={cn('block h-full', i > 0 && 'border-l border-carte')}
                    style={{
                      width: `${segment.pct}%`,
                      background: segment.couleur ?? MOTIF_NON_AFFECTE,
                    }}
                  />
                ))}
                {partition.surAllocation && partition.marqueurSoldePct !== null && (
                  <>
                    <span
                      className="absolute -top-1.5 -bottom-1.5 w-0.5 bg-encre"
                      style={{ left: `${partition.marqueurSoldePct}%` }}
                    />
                    <span
                      className="absolute -inset-y-px right-0 rounded-r-xl border-2 border-rouge"
                      title={`Dépassement — ${dollars(-resume.nonAffecte)}`}
                      style={{
                        left: `${partition.marqueurSoldePct}%`,
                        background: MOTIF_DEPASSEMENT,
                      }}
                    />
                  </>
                )}
              </div>
              <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-sourdine">
                {partition.segments.map((segment) => (
                  <span key={segment.id ?? 'non-affecte'} className="inline-flex items-center gap-1.5 font-bold">
                    <i
                      className="size-[9px] rounded-[3px] border border-encre/10"
                      style={{ background: segment.couleur ?? MOTIF_NON_AFFECTE }}
                    />
                    {segment.nom} <b className="text-texte tabular-nums">{dollars(segment.montant)}</b>
                  </span>
                ))}
                {partition.surAllocation && (
                  <span className="inline-flex items-center gap-1.5 font-bold text-rouge">
                    <i className="size-[9px] rounded-[3px] border border-rouge"
                      style={{ background: MOTIF_DEPASSEMENT }} />
                    Non affecté <b className="tabular-nums">{dollars(resume.nonAffecte)}</b>
                  </span>
                )}
              </div>
            </>
          )}
        </div>

        <div className="flex justify-center text-tiret-texte"><ArrowRight className="size-5" /></div>

        {/* Sorties prévues */}
        <div className="rounded-2xl bg-creux px-4.5 py-3.5">
          <div className="mb-1 text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-dore">
            Sort — prévu
          </div>
          {sortiesAffichees.length === 0 ? (
            <p className="text-xs text-sourdine italic">Aucune sortie datée encore.</p>
          ) : (
            sortiesAffichees.map((sortie, i) => (
              <div
                key={`${sortie.enveloppeId}-${sortie.date}`}
                className={cn(
                  'flex items-baseline gap-2 py-1 text-[12.5px] tabular-nums',
                  i > 0 && 'border-t border-dashed border-tiret',
                )}
              >
                <span className={cn('w-[104px] shrink-0 font-extrabold',
                  i === 0 ? 'text-orange' : 'text-dore')}>
                  {dateLisible(sortie.date)}
                </span>
                <span className="min-w-0 flex-1 truncate">{sortie.nom}</span>
                <span className="font-extrabold text-rouge">−&nbsp;{dollars(sortie.montant)}</span>
              </div>
            ))
          )}
        </div>
      </section>

      <RythmeMensuel resume={resume} />
    </>
  )
}

function RythmeMensuel({ resume }: { resume: ResumeBudget }) {
  const couleurs = couleursEnveloppes(resume.enveloppes)
  const lignes = resume.enveloppes
    .filter((e) => e.statut === 'Active' && e.provision > 0)
    .sort((a, b) => b.provision - a.provision)
  if (lignes.length === 0) {
    return null
  }
  const max = lignes[0].provision

  return (
    <section className="rounded-[20px] bg-carte px-6 py-4 shadow-carte">
      <header className="flex items-baseline gap-3 pb-2">
        <h3 className="text-lg font-bold">Le rythme mensuel</h3>
        <span className="rounded-full bg-creux px-2 text-xs font-bold text-dore">
          {lignes.length} provision{lignes.length > 1 ? 's' : ''}
        </span>
        <span className="ml-auto font-titre text-[22px] font-bold text-orange tabular-nums">
          {dollars(resume.virementSuggere)}
          <span className="font-sans text-[13px] font-bold text-dore"> / mois au total</span>
        </span>
      </header>
      {lignes.map((enveloppe) => (
        <div key={enveloppe.id} className="flex items-center gap-3 py-1 text-[13px]">
          <span className="w-[230px] shrink-0 truncate font-bold">{enveloppe.nom}</span>
          <span className="h-3 flex-1 overflow-hidden rounded-full bg-barre-piste">
            <i
              className="block h-full rounded-full"
              style={{
                width: `${(enveloppe.provision / max) * 100}%`,
                background: couleurs.get(enveloppe.id),
              }}
            />
          </span>
          <span className="w-24 shrink-0 text-right font-extrabold text-dore tabular-nums">
            +&nbsp;{dollars(enveloppe.provision)}
          </span>
          <span className="w-[190px] shrink-0 text-right text-[11.5px] text-sourdine">
            {complementRythme(enveloppe)}
          </span>
        </div>
      ))}
      <p className="mt-1.5 border-t border-dashed border-tiret pt-2 text-[11.5px] text-tiret-texte italic">
        La réserve ne reçoit pas de provision — elle se renfloue à la main quand le non
        affecté le permet.
      </p>
    </section>
  )
}

function complementRythme(enveloppe: EnveloppeBudget): string {
  if (enveloppe.echeancier !== null && enveloppe.echeancier.length > 0) {
    return `${enveloppe.echeancier.length} versement${enveloppe.echeancier.length > 1 ? 's' : ''}, lissés`
  }
  if (enveloppe.montantCible !== null && enveloppe.dateEffective !== null) {
    return `${dollars(enveloppe.montantCible)} pour ~${moisAnnee(enveloppe.dateEffective)}`
  }
  return ''
}

// ——— Grille des enveloppes (E1) ———

function GrilleEnveloppes({
  resume,
  onOuvrir,
}: {
  resume: ResumeBudget
  onOuvrir: (enveloppeId: string) => void
}) {
  const actives = resume.enveloppes.filter((e) => e.statut === 'Active')
  if (actives.length === 0) {
    return (
      <p className="rounded-[20px] border-2 border-dashed border-tiret px-5 py-8 text-center text-sm font-bold text-tiret-texte">
        Aucune enveloppe encore — crée la première : taxes, gros entretien, projet ou réserve.
      </p>
    )
  }

  return (
    <div className="grid grid-cols-3 gap-3">
      {actives.map((enveloppe) => {
        const pct = pctJauge(enveloppe.solde, enveloppe.montantCible)
        return (
          <button
            key={enveloppe.id}
            type="button"
            onClick={() => onOuvrir(enveloppe.id)}
            className="flex flex-col gap-1.5 rounded-[20px] bg-carte px-4.5 py-3.5 text-left shadow-carte transition-shadow hover:shadow-carte-lg"
          >
            <div className="flex items-center gap-2">
              <span className="min-w-0 flex-1 truncate text-sm font-extrabold">{enveloppe.nom}</span>
              <ChipType type={enveloppe.type} />
            </div>
            <div className="flex items-center gap-1.5 truncate text-[11.5px] text-sourdine">
              {enveloppe.titreTache !== null && (
                <><Link2 className="size-3 shrink-0 text-tiret-texte" />
                  <span className="truncate">Tâche « {enveloppe.titreTache} »</span></>
              )}
              {enveloppe.nomEquipement !== null && (
                <><Link2 className="size-3 shrink-0 text-tiret-texte" />
                  <span className="truncate">Équipement « {enveloppe.nomEquipement} »</span></>
              )}
              {enveloppe.echeancier !== null && enveloppe.echeancier.length > 0 && (
                <><CalendarDays className="size-3 shrink-0 text-tiret-texte" />
                  <span className="truncate">
                    {enveloppe.echeancier.length} versement{enveloppe.echeancier.length > 1 ? 's' : ''}
                  </span></>
              )}
              {enveloppe.titreTache === null && enveloppe.nomEquipement === null &&
                (enveloppe.echeancier === null || enveloppe.echeancier.length === 0) && (
                  <span className="italic">
                    {enveloppe.type === 'Reserve'
                      ? 'pas de cible ni de provision — le coussin'
                      : 'cible libre'}
                  </span>
                )}
            </div>
            {pct !== null && (
              <span className="h-[7px] overflow-hidden rounded-full bg-barre-piste">
                <i className="block h-full rounded-full bg-vert" style={{ width: `${pct}%` }} />
              </span>
            )}
            <div className="flex items-baseline justify-between text-xs text-sourdine tabular-nums">
              <span>
                <b className={cn('text-[13px]', enveloppe.solde < 0 ? 'text-rouge' : 'text-encre')}>
                  {dollars(enveloppe.solde, enveloppe.solde % 1 !== 0)}
                </b>
                {enveloppe.montantCible !== null && <> / {dollars(enveloppe.montantCible)}</>}
              </span>
              {pct !== null && <span>{Math.round(pct)}&nbsp;%</span>}
            </div>
            <div className="flex items-baseline justify-between border-t border-dashed border-tiret pt-1.5 text-[11.5px] text-sourdine tabular-nums">
              {enveloppe.provision > 0 ? (
                <span className="font-extrabold text-dore">+&nbsp;{dollars(enveloppe.provision)}/mois</span>
              ) : (
                <span className="text-tiret-texte">—</span>
              )}
              {enveloppe.enRetard ? (
                <span className="font-extrabold text-rouge">en retard</span>
              ) : enveloppe.echeancierARenouveler ? (
                <span className="font-extrabold text-dore">échéancier à renouveler</span>
              ) : enveloppe.dateEffective !== null ? (
                <span>échéance ~{moisAnnee(enveloppe.dateEffective)}</span>
              ) : (
                <span />
              )}
            </div>
          </button>
        )
      })}
    </div>
  )
}

// ——— Vue Flux (E2) : diagramme de flux tracé ———

function VueFlux({
  resume,
  onModifierCompte,
}: {
  resume: ResumeBudget
  onModifierCompte: () => void
}) {
  const flux = useMemo(() => construireFlux(resume, dateLocaleIso()), [resume])

  return (
    <>
      <section className="flex items-center gap-6 rounded-[20px] bg-carte px-5.5 py-3 text-[13px] shadow-carte">
        <Stat etiquette="Solde du compte" valeur={dollars(resume.soldeCourant)} />
        <span className="w-px self-stretch border-l border-dashed border-tiret" />
        <Stat etiquette="Σ enveloppes" valeur={dollars(resume.totalEnveloppes)} />
        <span className="w-px self-stretch border-l border-dashed border-tiret" />
        <Stat
          etiquette="Non affecté"
          valeur={dollars(resume.nonAffecte)}
          classe={resume.nonAffecte < 0 ? 'text-rouge' : undefined}
        />
        <span className="w-px self-stretch border-l border-dashed border-tiret" />
        <div className="flex flex-col">
          <span className="text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-sourdine">
            Équilibre
          </span>
          {resume.nonAffecte >= 0 ? (
            <span className="flex items-center gap-1 text-[11.5px] font-bold text-vert">
              <Check className="size-3" /> solde = enveloppes + non affecté
            </span>
          ) : (
            <span className="flex items-center gap-1 text-[11.5px] font-extrabold text-rouge">
              <X className="size-3" /> sur-allocation
            </span>
          )}
        </div>
        <div className="ml-auto flex items-center gap-2">
          <Stat
            etiquette="Virement mensuel suggéré"
            valeur={`${dollars(resume.virementSuggere)} / mois`}
            classe="text-orange"
            droite
          />
          <button
            type="button"
            aria-label="Modifier le compte"
            onClick={onModifierCompte}
            className="text-sourdine hover:text-orange"
          >
            <Pencil className="size-3.5" />
          </button>
        </div>
      </section>

      <section className="rounded-[20px] bg-carte px-6 py-4 shadow-carte">
        <header className="flex items-baseline gap-3 pb-2">
          <h3 className="text-lg font-bold">Le chemin de l’argent</h3>
          <span className="ml-auto text-[11.5px] text-sourdine">
            épaisseur à gauche = provision mensuelle · à droite = sorties sur 12 mois
          </span>
        </header>

        <div className="relative mx-auto" style={{ width: FLUX_LARGEUR, height: flux.hauteur }}>
          <svg
            className="absolute inset-0"
            width={FLUX_LARGEUR}
            height={flux.hauteur}
            aria-hidden="true"
          >
            {flux.rubansEntree.map((r, i) => (
              <path key={`e-${i}`} d={r.path} fill={r.couleur} />
            ))}
            {flux.rubansSortie.map((r, i) => (
              <path key={`s-${i}`} d={r.path} fill={r.couleur} />
            ))}
            {flux.lointains.map((l, i) => (
              <path key={`l-${i}`} d={l.path} fill="none" stroke="#a89e77"
                strokeWidth="2" strokeDasharray="4 5" />
            ))}
          </svg>

          <span className="absolute top-0 left-0 text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-tiret-texte">Entre</span>
          <span className="absolute top-0 left-[430px] text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-tiret-texte">Les enveloppes</span>
          <span className="absolute top-0 left-[880px] text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-tiret-texte">Sort — prévu</span>

          {/* Source */}
          <div
            className="absolute left-0 flex w-[190px] flex-col justify-center rounded-2xl bg-creux px-4 py-3"
            style={{ top: flux.source.y, height: flux.source.h }}
          >
            <span className="text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-dore">
              Chaque 1ᵉʳ du mois
            </span>
            <span className="font-titre text-[28px] font-bold text-orange tabular-nums">
              {dollars(resume.virementSuggere)}
              <span className="font-sans text-[13px] font-bold text-dore"> / mois</span>
            </span>
            <span className="text-[11.5px] text-sourdine">virement mensuel suggéré</span>
          </div>

          {flux.etiquettes.map((etiquette, i) => (
            <span
              key={i}
              className="absolute text-[10.5px] font-extrabold text-dore tabular-nums"
              style={{ left: etiquette.x, top: etiquette.y - 7 }}
            >
              {etiquette.texte}
            </span>
          ))}

          {/* Nœuds */}
          {flux.noeuds.map((noeud) => (
            <div
              key={noeud.enveloppe.id}
              className={cn(
                'absolute left-[430px] flex w-[210px] flex-col justify-center rounded-xl border border-tiret bg-carte px-3 py-1 shadow-carte',
                noeud.sansFlux && 'border-dashed shadow-none',
              )}
              style={{ top: noeud.y, height: noeud.h }}
            >
              <span className="truncate text-[13px] font-extrabold">{noeud.enveloppe.nom}</span>
              <span className="text-[11.5px] text-sourdine tabular-nums">
                <b className={cn(noeud.enveloppe.solde < 0 ? 'text-rouge' : 'text-encre')}>
                  {dollars(noeud.enveloppe.solde)}
                </b>
                {noeud.enveloppe.montantCible !== null && <> / {dollars(noeud.enveloppe.montantCible)}</>}
                {noeud.enveloppe.provision > 0 && (
                  <span className="font-extrabold text-dore"> · +&nbsp;{dollars(noeud.enveloppe.provision)}/mois</span>
                )}
                {noeud.sansFlux && <i> · sans provision</i>}
              </span>
            </div>
          ))}

          {/* Sorties */}
          {flux.sorties.map((sortie, i) => (
            <div
              key={i}
              className="absolute left-[880px] flex w-[268px] flex-col justify-center rounded-xl bg-creux px-3.5 py-1.5 tabular-nums"
              style={{ top: sortie.y, height: sortie.h }}
            >
              <span className="text-xs font-extrabold text-orange">
                prochain : {dateLisible(sortie.date)}
              </span>
              <span className="text-sm font-extrabold text-rouge">−&nbsp;{dollars(sortie.montant)}</span>
              <span className="truncate text-[11.5px] text-sourdine">
                {sortie.nom}
                {sortie.detail !== null && <> — {sortie.detail}</>}
              </span>
            </div>
          ))}

          {flux.lointains.map((lointain, i) => (
            <span
              key={i}
              className="absolute left-[880px] text-[11px] text-tiret-texte italic tabular-nums"
              style={{ top: lointain.y - 8 }}
            >
              ensuite ≈ {moisAnnee(lointain.sortie.date)} · {lointain.sortie.nom} ·
              −&nbsp;{dollars(lointain.sortie.montant)}
            </span>
          ))}
        </div>

        <p className="border-t border-dashed border-tiret pt-2 text-[11.5px] text-tiret-texte italic">
          Le non affecté ({dollars(resume.nonAffecte)}) ne reçoit rien du virement : les{' '}
          {dollars(resume.virementSuggere)} se répartissent entre les provisions.
        </p>
      </section>
    </>
  )
}

function Stat({
  etiquette,
  valeur,
  classe,
  droite,
}: {
  etiquette: string
  valeur: string
  classe?: string
  droite?: boolean
}) {
  return (
    <div className={cn('flex flex-col', droite && 'text-right')}>
      <span className="text-[10.5px] font-extrabold tracking-[0.08em] uppercase text-sourdine">
        {etiquette}
      </span>
      <span className={cn('font-titre text-xl font-bold text-encre tabular-nums', classe)}>
        {valeur}
      </span>
    </div>
  )
}

// ——— Inbox de rapprochement ———

function Inbox({
  resume,
  transactions,
}: {
  resume: ResumeBudget
  transactions: TransactionBudget[]
}) {
  const invalider = useInvaliderBudget()
  const [aLier, setALier] = useState<TransactionBudget | null>(null)

  const ignorer = useMutation({
    mutationFn: (id: string) => api.ignorerTransaction(id),
    onSuccess: invalider,
  })

  return (
    <section className="rounded-[20px] bg-carte px-6 py-4 shadow-carte">
      <header className="flex items-center gap-3 pb-1">
        <h3 className="text-lg font-bold">Rapprochement</h3>
        <span className="rounded-full bg-creux px-2 text-xs font-bold text-dore">
          {transactions.length} nouvelle{transactions.length > 1 ? 's' : ''}
        </span>
        <span className="ml-auto text-[11.5px] text-tiret-texte italic">
          suggestions à confirmer — jamais automatiques
        </span>
      </header>

      {transactions.length === 0 ? (
        <p className="py-3 text-sm text-sourdine">
          Rien à rapprocher — importe un relevé CSV ou OFX de la banque.
        </p>
      ) : (
        transactions.map((transaction, i) => (
          <div
            key={transaction.id}
            className={cn('flex items-center gap-2.5 py-2 text-[13px]',
              i > 0 && 'border-t border-dashed border-tiret')}
          >
            <span className="size-[7px] shrink-0 rounded-full bg-orange" />
            <span className="w-[230px] shrink-0 truncate text-[12.5px] font-extrabold tracking-[0.02em]">
              {transaction.description}
            </span>
            <span className="w-[110px] shrink-0 text-xs text-sourdine tabular-nums">
              {dateLisible(transaction.date)}
            </span>
            <span
              className={cn('w-[110px] shrink-0 text-right font-extrabold tabular-nums',
                transaction.montant < 0 ? 'text-rouge' : 'text-vert')}
            >
              {transaction.montant > 0 && '+'}
              {dollars(transaction.montant, true)}
            </span>
            <span className="min-w-0 flex-1 truncate text-xs text-sourdine">
              {transaction.suggererVentilation ? (
                <>suggestion&nbsp;: <b className="text-dore">répartir selon les provisions</b></>
              ) : transaction.suggestionNom !== null ? (
                <>suggestion&nbsp;: <b className="text-dore">{transaction.suggestionNom}</b></>
              ) : (
                <i className="text-tiret-texte">aucune suggestion — à classer</i>
              )}
            </span>
            <button
              type="button"
              onClick={() => setALier(transaction)}
              className="shrink-0 rounded-full bg-orange px-3 py-0.5 text-[11.5px] font-extrabold text-carte"
            >
              Lier
            </button>
            <button
              type="button"
              disabled={ignorer.isPending}
              onClick={() => ignorer.mutate(transaction.id)}
              className="shrink-0 text-[11.5px] font-bold text-sourdine hover:text-rouge"
            >
              Ignorer
            </button>
          </div>
        ))
      )}

      {aLier !== null && (
        <LierModal resume={resume} transaction={aLier} onFermer={() => setALier(null)} />
      )}
    </section>
  )
}

/** Liaison : retrait = une enveloppe ; dépôt = ventilation multi-enveloppes
 * pré-remplie par les provisions suggérées (D-2026-08-26 Ventilation Du Dépôt). */
function LierModal({
  resume,
  transaction,
  onFermer,
}: {
  resume: ResumeBudget
  transaction: TransactionBudget
  onFermer: () => void
}) {
  const invalider = useInvaliderBudget()
  const actives = resume.enveloppes.filter((e) => e.statut === 'Active')
  const estDepot = transaction.montant > 0

  // Pré-remplissage : les provisions suggérées, écrêtées au montant du dépôt.
  const [parts, setParts] = useState<Record<string, string>>(() => {
    if (estDepot === false) {
      return {}
    }
    let reste = transaction.montant
    const remplies: Record<string, string> = {}
    for (const enveloppe of [...actives].sort((a, b) => b.provision - a.provision)) {
      const part = Math.min(enveloppe.provision, reste)
      remplies[enveloppe.id] = part > 0 ? String(part) : ''
      reste -= part
    }
    return remplies
  })
  const [enveloppeRetrait, setEnveloppeRetrait] = useState(
    transaction.suggestionEnveloppeId ?? actives[0]?.id ?? '',
  )
  const [completerVirement, setCompleterVirement] = useState(
    transaction.suggererVentilation && resume.occurrenceVirementId !== null,
  )

  const totalVentile = actives.reduce(
    (somme, e) => somme + (Number(parts[e.id]) || 0), 0)
  const reste = transaction.montant - totalVentile

  const lier = useMutation({
    mutationFn: async () => {
      const ventilation = estDepot
        ? actives
            .map((e) => ({ enveloppeId: e.id, montant: Number(parts[e.id]) || 0 }))
            .filter((v) => v.montant > 0)
        : [{ enveloppeId: enveloppeRetrait, montant: -transaction.montant }]
      await api.lierTransaction(transaction.id, ventilation)
      if (estDepot && completerVirement && resume.occurrenceVirementId !== null) {
        await api.completer(resume.occurrenceVirementId)
      }
    },
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })

  const invalide = estDepot
    ? totalVentile <= 0 || reste < 0
    : enveloppeRetrait === ''

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4"
      onClick={onFermer}
    >
      <div
        className="flex w-full max-w-lg flex-col gap-3 rounded-[28px] bg-carte p-7 shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-2xl font-bold">{estDepot ? 'Ventiler le dépôt' : 'Lier le retrait'}</h2>
        <p className="text-sm text-sourdine tabular-nums">
          {transaction.description} · {dateLisible(transaction.date)} ·{' '}
          <b className={transaction.montant < 0 ? 'text-rouge' : 'text-vert'}>
            {dollars(transaction.montant, true)}
          </b>
        </p>

        {estDepot ? (
          <>
            <div className="flex max-h-72 flex-col gap-1.5 overflow-y-auto">
              {actives.map((enveloppe) => (
                <label key={enveloppe.id} className="flex items-center gap-2 text-sm">
                  <span className="min-w-0 flex-1 truncate font-bold">{enveloppe.nom}</span>
                  {enveloppe.provision > 0 && (
                    <span className="text-[11px] text-sourdine tabular-nums">
                      suggéré {dollars(enveloppe.provision)}
                    </span>
                  )}
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={parts[enveloppe.id] ?? ''}
                    onChange={(e) =>
                      setParts((anciennes) => ({ ...anciennes, [enveloppe.id]: e.target.value }))
                    }
                    aria-label={`Part pour ${enveloppe.nom}`}
                    className="w-28 rounded-xl bg-creux px-3 py-1.5 text-right text-sm tabular-nums focus:outline-2 focus:outline-orange/60"
                  />
                </label>
              ))}
            </div>
            <p className={cn('text-xs font-bold tabular-nums',
              reste < 0 ? 'text-rouge' : 'text-sourdine')}>
              {reste < 0
                ? `La ventilation dépasse le dépôt de ${dollars(-reste, true)}.`
                : `Reste en non affecté : ${dollars(reste, true)}.`}
            </p>
            {transaction.suggererVentilation && resume.occurrenceVirementId !== null && (
              <label className="flex items-center gap-2 text-sm font-bold text-texte">
                <input
                  type="checkbox"
                  checked={completerVirement}
                  onChange={(e) => setCompleterVirement(e.target.checked)}
                  className="size-4 accent-orange"
                />
                Compléter la tâche de virement du même coup
              </label>
            )}
          </>
        ) : (
          <label className="flex items-center gap-2 text-sm">
            <span className="shrink-0 text-sourdine">Enveloppe</span>
            <select
              value={enveloppeRetrait}
              onChange={(e) => setEnveloppeRetrait(e.target.value)}
              aria-label="Enveloppe du retrait"
              className="flex-1 rounded-xl bg-creux px-3 py-2 text-sm focus:outline-2 focus:outline-orange/60"
            >
              {actives.map((enveloppe) => (
                <option key={enveloppe.id} value={enveloppe.id}>
                  {enveloppe.nom}
                  {enveloppe.id === transaction.suggestionEnveloppeId ? ' (suggérée)' : ''}
                </option>
              ))}
            </select>
          </label>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <button type="button" onClick={onFermer}
            className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte">
            Annuler
          </button>
          <button
            type="button"
            disabled={invalide || lier.isPending}
            onClick={() => lier.mutate()}
            className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
          >
            Lier
          </button>
        </div>
      </div>
    </div>
  )
}

// ——— Éditeur d'enveloppe (fiche + mouvements + actions) ———

function EnveloppeEditeur({
  enveloppeId,
  onFermer,
}: {
  enveloppeId: string | null
  onFermer: () => void
}) {
  const invalider = useInvaliderBudget()
  const creation = enveloppeId === null

  const { data: detail } = useQuery({
    queryKey: ['budget-enveloppe', enveloppeId],
    queryFn: () => api.enveloppeDetail(enveloppeId!),
    enabled: creation === false,
  })
  const { data: taches } = useQuery({ queryKey: ['taches'], queryFn: api.taches })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })

  const [fiche, setFiche] = useState<{
    nom: string
    type: TypeEnveloppe
    montantCible: string
    dateCible: string
    lien: string // '' | `t:${id}` | `e:${id}`
    echeancier: { date: string; montant: string }[]
  } | null>(creation ? {
    nom: '', type: 'Equipement', montantCible: '', dateCible: '', lien: '', echeancier: [],
  } : null)

  // L'éditeur charge TOUS les champs avant d'exister : un PUT partiel effacerait
  // le reste de la fiche (même invariant que TacheEditeur).
  if (fiche === null && detail !== undefined) {
    const e = detail.enveloppe
    setFiche({
      nom: e.nom,
      type: e.type,
      montantCible: e.montantCible !== null ? String(e.montantCible) : '',
      dateCible: e.dateCible ?? '',
      lien: e.tacheId !== null ? `t:${e.tacheId}` : e.equipementId !== null ? `e:${e.equipementId}` : '',
      echeancier: (e.echeancier ?? []).map((v) => ({ date: v.date, montant: String(v.montant) })),
    })
  }

  const enregistrer = useMutation({
    mutationFn: () => {
      const f = fiche!
      const donnees: EnveloppeBudgetDonnees = {
        nom: f.nom,
        type: f.type,
        montantCible: f.montantCible === '' ? null : Number(f.montantCible),
        dateCible: f.dateCible === '' ? null : f.dateCible,
        tacheId: f.lien.startsWith('t:') ? f.lien.slice(2) : null,
        equipementId: f.lien.startsWith('e:') ? f.lien.slice(2) : null,
        echeancier:
          f.type === 'Taxes'
            ? f.echeancier
                .filter((v) => v.date !== '' && v.montant !== '')
                .map((v): Versement => ({ date: v.date, montant: Number(v.montant) }))
            : null,
      }
      return creation
        ? api.creerEnveloppe(donnees).then(() => undefined)
        : api.modifierEnveloppe(enveloppeId, donnees)
    },
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })

  const fermerEnveloppe = useMutation({
    mutationFn: () => api.fermerEnveloppe(enveloppeId!),
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })

  const [ajustement, setAjustement] = useState('')
  const ajuster = useMutation({
    mutationFn: () =>
      api.ajouterMouvement(enveloppeId!, { type: 'Ajustement', montant: Number(ajustement) }),
    onSuccess: () => {
      setAjustement('')
      invalider()
    },
  })

  const classeChamp =
    'rounded-xl bg-creux px-3 py-2 text-sm text-texte focus:outline-2 focus:outline-orange/60'

  if (fiche === null) {
    return null
  }
  const maj = (champ: Partial<typeof fiche & {}>) =>
    setFiche((ancienne) => ({ ...ancienne!, ...champ }))
  const tacheLiee = fiche.lien.startsWith('t:')

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4" onClick={onFermer}>
      <div
        className="flex max-h-[90dvh] w-full max-w-xl flex-col gap-3 overflow-y-auto rounded-[28px] bg-carte p-7 shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center gap-2">
          <h2 className="text-2xl font-bold">
            {creation ? 'Nouvelle enveloppe' : fiche.nom || '…'}
          </h2>
          {creation === false && detail !== undefined && (
            <span className="ml-auto text-sm text-sourdine tabular-nums">
              solde <b className={cn(detail.enveloppe.solde < 0 ? 'text-rouge' : 'text-encre')}>
                {dollars(detail.enveloppe.solde, true)}
              </b>
            </span>
          )}
        </div>

        <div className="grid grid-cols-2 gap-3">
          <input value={fiche.nom} onChange={(e) => maj({ nom: e.target.value })}
            placeholder="Nom (ex. Repeindre la toiture)" aria-label="Nom"
            className={cn(classeChamp, 'col-span-2 font-bold')} />
          <select value={fiche.type} aria-label="Type"
            onChange={(e) => maj({ type: e.target.value as TypeEnveloppe })}
            className={classeChamp}>
            {(Object.keys(LIBELLES_TYPE) as TypeEnveloppe[]).map((type) => (
              <option key={type} value={type}>{LIBELLES_TYPE[type]}</option>
            ))}
          </select>
          <input type="number" min="0" step="1" value={fiche.montantCible}
            onChange={(e) => maj({ montantCible: e.target.value })}
            placeholder="Cible ($)" aria-label="Montant cible" className={classeChamp} />
          <select
            value={fiche.lien}
            aria-label="Lien"
            onChange={(e) => maj({ lien: e.target.value })}
            className={cn(classeChamp, 'col-span-2')}
          >
            <option value="">Sans lien (cible libre)</option>
            <optgroup label="Tâches">
              {taches?.map((tache) => (
                <option key={tache.id} value={`t:${tache.id}`}>Tâche · {tache.titre}</option>
              ))}
            </optgroup>
            <optgroup label="Équipements">
              {equipements?.map((equipement) => (
                <option key={equipement.id} value={`e:${equipement.id}`}>
                  Équipement · {equipement.nom}
                </option>
              ))}
            </optgroup>
          </select>
          {tacheLiee ? (
            <p className="col-span-2 text-xs text-sourdine italic">
              L’échéance dérive de la prochaine occurrence de la tâche liée.
            </p>
          ) : (
            <label className="col-span-2 flex items-center gap-2 text-xs text-sourdine">
              Date cible
              <input type="date" value={fiche.dateCible}
                onChange={(e) => maj({ dateCible: e.target.value })}
                aria-label="Date cible" className={cn(classeChamp, 'flex-1')} />
            </label>
          )}
        </div>

        {fiche.type === 'Taxes' && (
          <div className="flex flex-col gap-2">
            <span className="text-sm font-bold text-dore">Échéancier des versements</span>
            {fiche.echeancier.map((versement, i) => (
              <div key={i} className="flex gap-2">
                <input type="date" value={versement.date} aria-label="Date du versement"
                  onChange={(e) => maj({
                    echeancier: fiche.echeancier.map((v, j) =>
                      j === i ? { ...v, date: e.target.value } : v),
                  })}
                  className={cn(classeChamp, 'flex-1')} />
                <input type="number" min="0" step="0.01" value={versement.montant}
                  aria-label="Montant du versement" placeholder="Montant"
                  onChange={(e) => maj({
                    echeancier: fiche.echeancier.map((v, j) =>
                      j === i ? { ...v, montant: e.target.value } : v),
                  })}
                  className={cn(classeChamp, 'w-32 text-right')} />
                <button type="button" aria-label="Retirer le versement"
                  onClick={() => maj({ echeancier: fiche.echeancier.filter((_, j) => j !== i) })}
                  className="text-sourdine hover:text-rouge">
                  <X className="size-4" />
                </button>
              </div>
            ))}
            <button type="button"
              onClick={() => maj({ echeancier: [...fiche.echeancier, { date: '', montant: '' }] })}
              className="flex w-fit items-center gap-1.5 text-sm font-bold text-tiret-texte hover:text-dore">
              <Plus className="size-3.5" /> Ajouter un versement
            </button>
          </div>
        )}

        <div className="flex items-center justify-end gap-2">
          {creation === false && (
            <button
              type="button"
              disabled={detail === undefined || detail.enveloppe.solde !== 0 || fermerEnveloppe.isPending}
              title={detail !== undefined && detail.enveloppe.solde !== 0
                ? 'Le solde doit être à zéro (transférer d’abord le reste).'
                : undefined}
              onClick={() => fermerEnveloppe.mutate()}
              className="mr-auto rounded-xl px-3 py-2 text-sm font-bold text-sourdine hover:text-rouge disabled:opacity-40"
            >
              Fermer l’enveloppe
            </button>
          )}
          <button type="button" onClick={onFermer}
            className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte">
            Annuler
          </button>
          <button
            type="button"
            disabled={fiche.nom.trim().length === 0 || enregistrer.isPending}
            onClick={() => enregistrer.mutate()}
            className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
          >
            Enregistrer
          </button>
        </div>

        {creation === false && detail !== undefined && (
          <>
            <div className="flex items-center gap-2 border-t border-barre-piste pt-3">
              <span className="text-sm font-bold text-dore">Ajustement rapide</span>
              <input type="number" step="0.01" value={ajustement}
                onChange={(e) => setAjustement(e.target.value)}
                placeholder="± montant" aria-label="Montant d'ajustement"
                className={cn(classeChamp, 'w-32 text-right')} />
              <button type="button"
                disabled={ajustement === '' || Number(ajustement) === 0 || ajuster.isPending}
                onClick={() => ajuster.mutate()}
                className="rounded-full bg-creux px-3 py-1.5 text-xs font-bold text-dore hover:text-orange disabled:opacity-40">
                Ajouter
              </button>
              <TransfertRapide enveloppeId={enveloppeId!} />
            </div>

            <div className="border-t border-barre-piste pt-3">
              <div className="mb-1 flex items-baseline justify-between">
                <span className="text-sm font-bold text-dore">Mouvements</span>
                <span className="text-[11.5px] text-sourdine">solde après chaque mouvement</span>
              </div>
              {detail.mouvements.length === 0 ? (
                <p className="text-xs text-sourdine">Aucun mouvement encore.</p>
              ) : (
                detail.mouvements.map((mouvement, i) => (
                  <div key={mouvement.id}
                    className={cn('flex items-center gap-2 py-1.5 text-[12.5px] tabular-nums',
                      i > 0 && 'border-t border-dashed border-tiret')}>
                    <span className={cn('size-2 shrink-0 rounded-full', {
                      'bg-vert': mouvement.type === 'Provision',
                      'bg-rouge': mouvement.type === 'Retrait',
                      'bg-jaune': mouvement.type === 'Ajustement',
                      'bg-[#b9a8d1]': mouvement.type === 'Transfert',
                    })} />
                    <span className="w-20 shrink-0 text-[11px] font-extrabold tracking-[0.04em] uppercase text-sourdine">
                      {mouvement.type}
                    </span>
                    <span className="w-24 shrink-0 text-xs text-sourdine">
                      {dateLisible(mouvement.date)}
                    </span>
                    <span className="min-w-0 flex-1 truncate">
                      {mouvement.note ?? mouvement.descriptionTransaction ?? ''}
                    </span>
                    <span className={cn('w-24 shrink-0 text-right font-extrabold',
                      mouvement.montant < 0 ? 'text-rouge' : 'text-vert')}>
                      {mouvement.montant > 0 && '+'}
                      {dollars(mouvement.montant, true)}
                    </span>
                    <span className="w-20 shrink-0 text-right text-xs text-sourdine">
                      {dollars(mouvement.soldeApres)}
                    </span>
                  </div>
                ))
              )}
            </div>
          </>
        )}
      </div>
    </div>
  )
}

function TransfertRapide({ enveloppeId }: { enveloppeId: string }) {
  const invalider = useInvaliderBudget()
  const { data: resume } = useQuery({ queryKey: ['budget'], queryFn: api.budget })
  const [vers, setVers] = useState('')
  const [montant, setMontant] = useState('')

  const transferer = useMutation({
    mutationFn: () =>
      api.transferer({ deEnveloppeId: enveloppeId, versEnveloppeId: vers, montant: Number(montant) }),
    onSuccess: () => {
      setMontant('')
      invalider()
    },
  })

  const autres = (resume?.enveloppes ?? []).filter(
    (e) => e.id !== enveloppeId && e.statut === 'Active')

  return (
    <span className="ml-auto flex items-center gap-2">
      <span className="text-sm font-bold text-dore">Transférer</span>
      <input type="number" min="0" step="0.01" value={montant}
        onChange={(e) => setMontant(e.target.value)} placeholder="Montant"
        aria-label="Montant du transfert"
        className="w-24 rounded-xl bg-creux px-3 py-2 text-right text-sm tabular-nums focus:outline-2 focus:outline-orange/60" />
      <span className="text-xs text-sourdine">vers</span>
      <select value={vers} onChange={(e) => setVers(e.target.value)} aria-label="Enveloppe destination"
        className="max-w-40 rounded-xl bg-creux px-2 py-2 text-sm focus:outline-2 focus:outline-orange/60">
        <option value="">…</option>
        {autres.map((e) => (
          <option key={e.id} value={e.id}>{e.nom}</option>
        ))}
      </select>
      <button type="button"
        disabled={vers === '' || montant === '' || Number(montant) <= 0 || transferer.isPending}
        onClick={() => transferer.mutate()}
        className="rounded-full bg-creux px-3 py-1.5 text-xs font-bold text-dore hover:text-orange disabled:opacity-40">
        Aller
      </button>
    </span>
  )
}

// ——— Éditeur du compte (ancrage + tâche de virement) ———

function CompteEditeur({ resume, onFermer }: { resume: ResumeBudget; onFermer: () => void }) {
  const invalider = useInvaliderBudget()
  const compte = resume.compte!
  const [nom, setNom] = useState(compte.nom)
  const [institution, setInstitution] = useState(compte.institution ?? '')
  const [solde, setSolde] = useState(String(compte.soldeInitial))
  const [date, setDate] = useState(compte.dateAncrage)
  const [tacheVirement, setTacheVirement] = useState(compte.tacheVirementId ?? '')

  const { data: taches } = useQuery({ queryKey: ['taches'], queryFn: api.taches })
  const recurrentes = (taches ?? []).filter((t) => t.recurrence.mode !== 'Ponctuelle')

  const enregistrer = useMutation({
    mutationFn: () =>
      api.modifierCompteBudget({
        nom,
        institution: institution || null,
        soldeInitial: Number(solde),
        dateAncrage: date,
        tacheVirementId: tacheVirement || null,
      }),
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })

  const classeChamp =
    'rounded-xl bg-creux px-3 py-2 text-sm text-texte focus:outline-2 focus:outline-orange/60'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4" onClick={onFermer}>
      <div
        className="flex w-full max-w-md flex-col gap-3 rounded-[28px] bg-carte p-7 shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-2xl font-bold">Le compte</h2>
        <input value={nom} onChange={(e) => setNom(e.target.value)} aria-label="Nom du compte"
          className={cn(classeChamp, 'font-bold')} />
        <input value={institution} onChange={(e) => setInstitution(e.target.value)}
          aria-label="Institution" placeholder="Institution" className={classeChamp} />
        <label className="flex items-center gap-2 text-xs text-sourdine">
          Solde initial
          <input type="number" step="0.01" value={solde} onChange={(e) => setSolde(e.target.value)}
            aria-label="Solde initial" className={cn(classeChamp, 'flex-1')} />
        </label>
        <label className="flex items-center gap-2 text-xs text-sourdine">
          Ancré le
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)}
            aria-label="Date d'ancrage" className={cn(classeChamp, 'flex-1')} />
        </label>
        <label className="flex flex-col gap-1 text-xs text-sourdine">
          Tâche de virement mensuel — sa complétion sera suggérée quand un dépôt proche
          du virement suggéré apparaîtra
          <select value={tacheVirement} onChange={(e) => setTacheVirement(e.target.value)}
            aria-label="Tâche de virement" className={classeChamp}>
            <option value="">Aucune</option>
            {recurrentes.map((tache) => (
              <option key={tache.id} value={tache.id}>{tache.titre}</option>
            ))}
          </select>
        </label>
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onFermer}
            className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte">
            Annuler
          </button>
          <button
            type="button"
            disabled={nom.trim().length === 0 || enregistrer.isPending}
            onClick={() => enregistrer.mutate()}
            className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
          >
            Enregistrer
          </button>
        </div>
      </div>
    </div>
  )
}
