import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  CalendarDays,
  Check,
  Link2,
  Monitor,
  Plus,
  X,
} from 'lucide-react'
import {
  api,
  dateLocaleIso,
  type EnveloppeBudget,
  type TypeEnveloppe,
} from '@/lib/api'
import { pctJauge } from '@/lib/budget-vues'
import { dollars } from '@/lib/format'
import { cn } from '@/lib/utils'

// Budget au téléphone : le bureau pose une bande en
// `grid-cols-[230px_34px_1fr_34px_300px]` et une grille d'enveloppes à trois
// colonnes — ici tout s'empile. La vue « Flux tracé » (E2) reste au bureau : ses
// colonnes sont en pixels fixes sur 1148 px de large, elle ne se replie pas.

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

const CLASSE_CHAMP =
  'min-h-[48px] w-full rounded-xl bg-creux px-3.5 text-[16px] text-texte focus:outline-2 focus:outline-orange/60'

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
        'inline-flex shrink-0 items-center rounded-full border border-tiret bg-creux px-2.5 py-px text-[12px] font-bold whitespace-nowrap',
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

/** Une des trois mesures du haut — empilées, jamais en colonnes fixes. */
function Stat({
  etiquette,
  valeur,
  classe,
}: {
  etiquette: string
  valeur: string
  classe?: string
}) {
  return (
    <div className="flex items-baseline justify-between gap-3 py-1.5">
      <span className="text-[13px] font-extrabold tracking-[0.06em] text-sourdine uppercase">
        {etiquette}
      </span>
      <span className={cn('font-titre text-[22px] font-bold text-encre tabular-nums', classe)}>
        {valeur}
      </span>
    </div>
  )
}

export default function BudgetTelephone() {
  const { data: resume, isLoading } = useQuery({ queryKey: ['budget'], queryFn: api.budget })

  if (isLoading || resume === undefined) {
    return <p className="py-8 text-center text-[15px] text-sourdine">Chargement…</p>
  }

  if (resume.compte === null) {
    return <Ancrage />
  }

  const actives = resume.enveloppes.filter((e) => e.statut === 'Active')

  return (
    <div className="flex flex-col gap-3 pt-1">
      <h1 className="font-titre text-[27px] leading-tight font-bold text-encre">Budget</h1>

      {resume.nonAffecte < 0 && (
        <div
          role="alert"
          className="flex items-start gap-2.5 rounded-2xl bg-rouge px-4 py-3 text-[14px] font-bold text-carte shadow-carte"
        >
          <AlertTriangle className="mt-0.5 size-5 shrink-0" />
          <span>
            Sur-allocation : les enveloppes totalisent{' '}
            <b className="tabular-nums">{dollars(resume.totalEnveloppes)}</b>, soit{' '}
            <b className="tabular-nums">{dollars(-resume.nonAffecte)}</b> de plus que le compte.
            Réduis une enveloppe ou attends le prochain virement.
          </span>
        </div>
      )}

      <section className="flex flex-col divide-y divide-creux rounded-[20px] bg-carte px-4 py-2 shadow-carte">
        <Stat etiquette="Solde du compte" valeur={dollars(resume.soldeCourant)} />
        <Stat etiquette="Σ enveloppes" valeur={dollars(resume.totalEnveloppes)} />
        <Stat
          etiquette="Non affecté"
          valeur={dollars(resume.nonAffecte)}
          classe={resume.nonAffecte < 0 ? 'text-rouge' : undefined}
        />
        <div className="py-2">
          {resume.nonAffecte >= 0 ? (
            <span className="flex items-center gap-1.5 text-[14px] font-bold text-vert">
              <Check className="size-4 shrink-0" /> solde = enveloppes + non affecté
            </span>
          ) : (
            <span className="flex items-center gap-1.5 text-[14px] font-extrabold text-rouge">
              <X className="size-4 shrink-0" /> enveloppes &gt; solde : {dollars(resume.nonAffecte)}{' '}
              non affecté
            </span>
          )}
        </div>
      </section>

      <section className="rounded-[20px] bg-creux px-4 py-3.5">
        <div className="text-[13px] font-extrabold tracking-[0.08em] text-dore uppercase">
          Entre chaque 1ᵉʳ du mois
        </div>
        <div className="font-titre text-[30px] font-bold text-orange tabular-nums">
          {dollars(resume.virementSuggere)}
          <span className="font-sans text-[15px] font-bold text-dore"> / mois</span>
        </div>
        <div className="mt-0.5 text-[13px] text-sourdine">
          virement mensuel suggéré — Σ des provisions
        </div>
      </section>

      {actives.length === 0 ? (
        <p className="rounded-[20px] border border-dashed border-tiret px-5 py-8 text-center text-[15px] font-bold text-tiret-texte">
          Aucune enveloppe encore — crée la première : taxes, gros entretien, projet ou réserve.
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {actives.map((enveloppe) => (
            <li key={enveloppe.id}>
              <CarteEnveloppe enveloppe={enveloppe} />
            </li>
          ))}
        </ul>
      )}

      {/* L'éditeur d'enveloppe (cible, échéancier, versements lissés, liens tâche
       * et équipement) est un formulaire dense : il reste au bureau plutôt que
       * d'exister ici en demi-version. */}
      <div className="flex flex-col gap-1 rounded-2xl border border-dashed border-tiret px-4 py-3.5">
        <span className="flex items-center gap-2 text-[16px] font-extrabold text-dore">
          <Plus className="size-5 shrink-0" />
          Nouvelle enveloppe
        </span>
        <span className="text-[13px] text-sourdine">
          Créer ou modifier une enveloppe se fait au bureau — cible, échéancier et liens y
          tiennent au complet.
        </span>
      </div>

      <p className="flex items-start gap-2 px-1 text-[13px] text-sourdine">
        <Monitor className="mt-0.5 size-4 shrink-0 text-tiret-texte" />
        <span>
          {resume.nbTransactionsNouvelles > 0
            ? `${resume.nbTransactionsNouvelles} transaction${resume.nbTransactionsNouvelles > 1 ? 's' : ''} à rapprocher — l’import du relevé et le rapprochement se font au bureau.`
            : 'L’import d’un relevé et le rapprochement des transactions se font au bureau.'}
        </span>
      </p>
    </div>
  )
}

/** Une enveloppe, tout empilé : au bureau la même carte vit dans une grille à
 * trois colonnes, ici elle prend la largeur et les chiffres passent en dessous. */
function CarteEnveloppe({ enveloppe }: { enveloppe: EnveloppeBudget }) {
  const pct = pctJauge(enveloppe.solde, enveloppe.montantCible)
  const nbVersements = enveloppe.echeancier?.length ?? 0

  return (
    <article className="flex flex-col gap-2 rounded-[20px] bg-carte px-4 py-3.5 shadow-carte">
      <div className="flex items-center gap-2">
        <h2 className="min-w-0 flex-1 truncate text-[16px] font-extrabold text-encre">
          {enveloppe.nom}
        </h2>
        <ChipType type={enveloppe.type} />
      </div>

      <div className="flex items-center gap-1.5 truncate text-[13px] text-sourdine">
        {enveloppe.titreTache !== null && (
          <>
            <Link2 className="size-3.5 shrink-0 text-tiret-texte" />
            <span className="truncate">Tâche « {enveloppe.titreTache} »</span>
          </>
        )}
        {enveloppe.nomEquipement !== null && (
          <>
            <Link2 className="size-3.5 shrink-0 text-tiret-texte" />
            <span className="truncate">Équipement « {enveloppe.nomEquipement} »</span>
          </>
        )}
        {nbVersements > 0 && (
          <>
            <CalendarDays className="size-3.5 shrink-0 text-tiret-texte" />
            <span className="truncate">
              {nbVersements} versement{nbVersements > 1 ? 's' : ''}
            </span>
          </>
        )}
        {enveloppe.titreTache === null && enveloppe.nomEquipement === null && nbVersements === 0 && (
          <span className="italic">
            {enveloppe.type === 'Reserve'
              ? 'pas de cible ni de provision — le coussin'
              : 'cible libre'}
          </span>
        )}
      </div>

      {pct !== null && (
        <span className="h-2 overflow-hidden rounded-full bg-barre-piste">
          <i className="block h-full rounded-full bg-vert" style={{ width: `${pct}%` }} />
        </span>
      )}

      <div className="flex items-baseline justify-between gap-3 text-[13px] text-sourdine tabular-nums">
        <span>
          <b className={cn('text-[16px]', enveloppe.solde < 0 ? 'text-rouge' : 'text-encre')}>
            {dollars(enveloppe.solde, enveloppe.solde % 1 !== 0)}
          </b>
          {enveloppe.montantCible !== null && <> / {dollars(enveloppe.montantCible)}</>}
        </span>
        {pct !== null && <span className="font-bold">{Math.round(pct)}&nbsp;%</span>}
      </div>

      <div className="flex items-baseline justify-between gap-3 border-t border-dashed border-tiret pt-2 text-[13px] text-sourdine tabular-nums">
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
    </article>
  )
}

/** Premier écran : tant qu'aucun compte n'est ancré, il n'y a rien à partitionner. */
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

  return (
    <div className="mt-4 flex flex-col gap-3.5 rounded-[24px] bg-carte px-5 py-5 shadow-carte-lg">
      <h1 className="font-titre text-[24px] leading-tight font-bold text-encre">
        Ancrer le fonds de prévoyance
      </h1>
      <p className="text-[15px] leading-snug text-texte">
        Un seul compte bancaire réel, partitionné en enveloppes virtuelles. Donne son solde
        d’aujourd’hui : ensuite, seules les transactions importées le feront bouger.
      </p>
      <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
        Nom du compte
        <input
          value={nom}
          onChange={(e) => setNom(e.target.value)}
          aria-label="Nom du compte"
          placeholder="Nom du compte"
          className={cn(CLASSE_CHAMP, 'font-bold')}
        />
      </label>
      <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
        Institution
        <input
          value={institution}
          onChange={(e) => setInstitution(e.target.value)}
          aria-label="Institution"
          placeholder="Institution (ex. Desjardins)"
          className={CLASSE_CHAMP}
        />
      </label>
      <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
        Solde initial
        <input
          type="number"
          inputMode="decimal"
          step="0.01"
          value={solde}
          onChange={(e) => setSolde(e.target.value)}
          aria-label="Solde initial"
          placeholder="0,00"
          className={CLASSE_CHAMP}
        />
      </label>
      <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
        Ancré le
        <input
          type="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          aria-label="Date d'ancrage"
          className={CLASSE_CHAMP}
        />
      </label>
      <button
        type="button"
        disabled={nom.trim().length === 0 || solde === '' || ancrer.isPending}
        onClick={() => ancrer.mutate()}
        className="flex min-h-[56px] items-center justify-center rounded-2xl bg-orange text-[17px] font-extrabold text-carte disabled:opacity-40"
      >
        Ancrer le compte
      </button>
    </div>
  )
}
