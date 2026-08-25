import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CalendarPlus, Plus } from 'lucide-react'
import ComptesAReboursGestion from '@/components/ComptesAReboursGestion'
import FluxExternesGestion, { ICONES_FLUX } from '@/components/FluxExternesGestion'
import { ICONES_COMPTE, MaisonSoleil } from '@/components/Illustrations'
import MeteoCarte from '@/components/MeteoCarte'
import OccurrenceListe from '@/components/OccurrenceListe'
import QuickAdd from '@/components/QuickAdd'
import TacheEditeur from '@/components/TacheEditeur'
import {
  api,
  dateLocaleIso,
  type CompteARebours,
  type EvenementExterne,
  type Occurrence,
} from '@/lib/api'
import { bornesJourneeLocale, dateCourte, dateLongue, dodosAvant, jourCourt } from '@/lib/format'
import { DATE_DEMENAGEMENT, phraseDuJour } from '@/lib/humeur'

export default function Aujourdhui() {
  const [editeurTacheId, setEditeurTacheId] = useState<string | null>(null)
  const bornes = bornesJourneeLocale()
  const { data: ouvertes, isLoading: chargementOuvertes } = useQuery({
    queryKey: ['occurrences', 'aujourdhui'],
    queryFn: () => api.occurrences('aujourdhui'),
  })
  const { data: faites, isLoading: chargementFaites } = useQuery({
    queryKey: ['occurrences', 'faites', bornes.de],
    queryFn: () => api.occurrencesFaites(bornes.de, bornes.a),
  })
  const { data: enAttente } = useQuery({
    queryKey: ['occurrences', 'en-attente'],
    queryFn: () => api.occurrences('en-attente'),
  })
  const bornesBilan = bornesSemainesBilan()
  const { data: bilan } = useQuery({
    queryKey: ['journal', 'bilan', bornesBilan.de],
    queryFn: () => api.bilanJournal(bornesBilan.de, bornesBilan.a),
  })
  const { data: comptesARebours } = useQuery({
    queryKey: ['comptes-a-rebours'],
    queryFn: api.comptesARebours,
  })
  const { data: meteo } = useQuery({
    queryKey: ['meteo'],
    queryFn: api.meteo,
    refetchInterval: 15 * 60 * 1000,
  })
  const { data: phraseServeur } = useQuery({
    queryKey: ['phrase-du-jour'],
    queryFn: api.phraseDuJour,
    refetchInterval: 15 * 60 * 1000,
  })
  const { data: evenementsExternes } = useQuery({
    queryKey: ['evenements-externes'],
    queryFn: () => api.evenementsExternes(7),
  })
  const [gestionFluxOuverte, setGestionFluxOuverte] = useState(false)
  const [gestionComptesOuverte, setGestionComptesOuverte] = useState(false)

  const chargement = chargementOuvertes || chargementFaites
  const aujourdhui = dateLocaleIso()
  const dodosDemenagement = dodosAvant(DATE_DEMENAGEMENT)

  // Phrase serveur (banque météo-consciente + polissage LLM) quand elle existe ;
  // la banque client reste le repli ultime.
  const phrase =
    phraseServeur ??
    phraseDuJour({
      ouvertes: ouvertes?.length ?? 0,
      enRetard: ouvertes?.filter((o) => o.echeance !== null && o.echeance < aujourdhui).length ?? 0,
      faites: faites?.length ?? 0,
      dodosDemenagement: dodosDemenagement > 0 ? dodosDemenagement : null,
    })

  const listeDuJour: Occurrence[] = [...(ouvertes ?? []), ...(faites ?? [])]

  return (
    <div className="flex flex-col gap-6">
      {/* Héros illustré */}
      <section className="flex items-center justify-between gap-8 rounded-[28px] bg-carte px-8 py-8 shadow-carte-lg lg:px-12">
        <div className="max-w-[660px]">
          <div className="text-sm font-bold uppercase tracking-[0.06em] text-orange">
            {dateLongue()}
          </div>
          <h1 className="mt-2 text-4xl font-bold leading-[1.15] lg:text-[44px]">
            {phrase.titre}
          </h1>
          <p className="mt-3 text-[17px] text-dore">{phrase.sousTitre}</p>
        </div>
        <div className="hidden shrink-0 md:block">
          <MaisonSoleil />
        </div>
      </section>

      <div className="grid gap-6 lg:grid-cols-[1.6fr_1fr]">
        {/* Liste du jour */}
        <div className="flex flex-col gap-[11px]">
          <EvenementsDuJour
            evenements={(evenementsExternes ?? []).filter((e) => e.date === aujourdhui)}
          />
          {chargement ? (
            <p className="py-8 text-center text-sm text-sourdine">Chargement…</p>
          ) : (
            <OccurrenceListe
              occurrences={listeDuJour}
              vide="Rien pour aujourd'hui. La maison respire."
              onModifier={setEditeurTacheId}
            />
          )}
          <QuickAdd />
          {editeurTacheId !== null && (
            <TacheEditeur tacheId={editeurTacheId} onFermer={() => setEditeurTacheId(null)} />
          )}
        </div>

        {/* Colonne latérale */}
        <div className="flex flex-col gap-[18px]">
          <MeteoCarte meteo={meteo} />
          <CetteSemaine
            enAttente={enAttente ?? []}
            aujourdhui={aujourdhui}
            evenements={evenementsExternes ?? []}
            onGerer={() => setGestionFluxOuverte(true)}
          />
          <Bilan instants={bilan ?? []} lundis={bornesBilan.lundis} />
          <ComptesARebours
            comptes={comptesARebours ?? []}
            onGerer={() => setGestionComptesOuverte(true)}
          />
        </div>
      </div>

      {gestionComptesOuverte && (
        <ComptesAReboursGestion
          comptes={comptesARebours ?? []}
          onFermer={() => setGestionComptesOuverte(false)}
        />
      )}
      {gestionFluxOuverte && <FluxExternesGestion onFermer={() => setGestionFluxOuverte(false)} />}
    </div>
  )
}

/* Les événements du jour : des faits (collecte, école…), pas des tâches — un
   bandeau discret, rien à cocher. */
function EvenementsDuJour({ evenements }: { evenements: EvenementExterne[] }) {
  if (evenements.length === 0) {
    return null
  }
  return (
    <div className="flex flex-wrap gap-2">
      {evenements.map((e) => {
        const { Icone } = ICONES_FLUX[e.type]
        return (
          <span
            key={`${e.titre}-${e.date}`}
            className="flex items-center gap-2 rounded-full bg-carte px-4 py-2 text-sm font-bold shadow-carte"
          >
            <Icone className="size-4 text-orange" />
            {e.titre} aujourd&rsquo;hui
          </span>
        )
      })}
    </div>
  )
}

function CetteSemaine({
  enAttente,
  aujourdhui,
  evenements,
  onGerer,
}: {
  enAttente: Occurrence[]
  aujourdhui: string
  evenements: EvenementExterne[]
  onGerer: () => void
}) {
  const [annee, mois, jour] = aujourdhui.split('-').map(Number)
  const dansSeptJours = dateLocaleIso(new Date(annee, mois - 1, jour + 7))
  const taches = enAttente
    .filter((o) => o.echeance !== null && o.echeance > aujourdhui && o.echeance <= dansSeptJours)
    .map((o) => ({ cle: `t-${o.id}`, date: o.echeance!, titre: o.titre, type: null }))
  const externes = evenements
    .filter((e) => e.date > aujourdhui)
    .map((e, i) => ({ cle: `e-${i}`, date: e.date, titre: e.titre, type: e.type }))
  const semaine = [...taches, ...externes].sort((a, b) => a.date.localeCompare(b.date)).slice(0, 7)

  return (
    <section className="rounded-3xl bg-carte px-6 py-5 shadow-carte">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-lg font-bold">Cette semaine</h2>
        <button
          type="button"
          aria-label="Gérer les calendriers externes"
          onClick={onGerer}
          className="rounded-lg p-1.5 text-sourdine transition-colors hover:text-dore"
        >
          <CalendarPlus className="size-4" />
        </button>
      </div>
      {semaine.length === 0 ? (
        <p className="text-sm text-sourdine">Rien de prévu — belle semaine en vue.</p>
      ) : (
        <ul className="flex flex-col gap-[11px] text-sm">
          {semaine.map((item) => {
            const Icone = item.type === null ? null : ICONES_FLUX[item.type].Icone
            return (
              <li key={item.cle} className="flex items-center gap-2.5">
                <span className="rounded-full bg-creux px-2.5 py-0.5 text-xs font-bold text-dore">
                  {jourCourt(item.date)}
                </span>
                <span
                  className={
                    Icone === null ? 'min-w-0 truncate' : 'min-w-0 truncate italic text-dore'
                  }
                >
                  {item.titre}
                </span>
                {Icone !== null && <Icone className="size-3.5 shrink-0 text-sourdine" />}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}

/* Bilan du ménage : total complété par semaine, sans égard à qui a cliqué —
   l'attribution individuelle est indicative, le foyer compte ensemble. */
const NB_SEMAINES_BILAN = 8

function bornesSemainesBilan(date = new Date()): { de: string; a: string; lundis: string[] } {
  const jour = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const lundiCourant = new Date(
    jour.getFullYear(),
    jour.getMonth(),
    jour.getDate() - ((jour.getDay() + 6) % 7),
  )
  const lundis: string[] = []
  for (let i = NB_SEMAINES_BILAN - 1; i >= 0; i--) {
    lundis.push(
      dateLocaleIso(
        new Date(
          lundiCourant.getFullYear(),
          lundiCourant.getMonth(),
          lundiCourant.getDate() - 7 * i,
        ),
      ),
    )
  }
  const debut = new Date(`${lundis[0]}T00:00:00`)
  const fin = new Date(
    lundiCourant.getFullYear(),
    lundiCourant.getMonth(),
    lundiCourant.getDate() + 7,
  )
  return { de: debut.toISOString(), a: fin.toISOString(), lundis }
}

function Bilan({ instants, lundis }: { instants: string[]; lundis: string[] }) {
  const indexParLundi = new Map(lundis.map((lundi, i) => [lundi, i]))
  const comptes = lundis.map(() => 0)
  for (const instant of instants) {
    const local = new Date(instant)
    const lundi = dateLocaleIso(
      new Date(
        local.getFullYear(),
        local.getMonth(),
        local.getDate() - ((local.getDay() + 6) % 7),
      ),
    )
    const i = indexParLundi.get(lundi)
    if (i !== undefined) {
      comptes[i] += 1
    }
  }
  const maximum = Math.max(1, ...comptes)
  const cetteSemaine = comptes[comptes.length - 1]

  return (
    <section className="rounded-3xl bg-carte px-6 py-5 shadow-carte">
      <h2 className="mb-1 text-lg font-bold">Bilan</h2>
      <p className="mb-3 text-xs text-sourdine">tâches faites, semaine après semaine</p>
      <div className="flex items-baseline gap-2">
        <span className="text-3xl font-bold leading-none text-orange">{cetteSemaine}</span>
        <span className="text-sm text-dore">
          {cetteSemaine === 1 ? 'tâche faite cette semaine' : 'tâches faites cette semaine'}
        </span>
      </div>
      <div className="mt-3 flex h-14 items-end gap-1.5">
        {comptes.map((nombre, i) => (
          <div
            key={lundis[i]}
            title={`Semaine du ${dateCourte(lundis[i])} : ${nombre}`}
            className={`flex-1 rounded-t-md ${i === comptes.length - 1 ? 'bg-orange' : 'bg-orange/30'}`}
            style={{ height: `${Math.max((nombre / maximum) * 100, 6)}%` }}
          />
        ))}
      </div>
    </section>
  )
}

function ComptesARebours({
  comptes,
  onGerer,
}: {
  comptes: CompteARebours[]
  onGerer: () => void
}) {
  // Célébrer puis masquer : le jour J s'affiche, le lendemain la ligne disparaît
  // (elle reste supprimable dans la gestion).
  const aVenir = comptes.filter((c) => dodosAvant(c.dateCible) >= 0)

  return (
    <section className="flex flex-col gap-3 rounded-3xl bg-carte px-6 py-5 shadow-carte">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-bold">Comptes à rebours</h2>
        <button
          type="button"
          aria-label="Gérer les comptes à rebours"
          onClick={onGerer}
          className="rounded-lg p-1.5 text-sourdine transition-colors hover:text-dore"
        >
          <Plus className="size-4" />
        </button>
      </div>
      {aVenir.length === 0 && (
        <p className="text-sm text-sourdine">On n'attend rien pour l'instant.</p>
      )}
      {aVenir.map((c) => {
        const dodos = dodosAvant(c.dateCible)
        const { Icone, couleur } = ICONES_COMPTE[c.icone]
        return (
          <div key={c.id} className="flex items-center gap-3.5 rounded-2xl bg-creux px-4 py-3">
            <Icone />
            <div className="flex-1">
              <div className="text-[15px] font-bold">{c.titre}</div>
              <div className="text-xs text-sourdine">{dateCourte(c.dateCible)}</div>
            </div>
            {dodos === 0 ? (
              <div className="font-titre text-[17px] font-bold" style={{ color: couleur }}>
                C'est aujourd'hui !
              </div>
            ) : (
              <div className="font-titre text-[22px] font-bold" style={{ color: couleur }}>
                {dodos}{' '}
                <span className="text-[13px] text-dore">{dodos === 1 ? 'dodo' : 'dodos'}</span>
              </div>
            )}
          </div>
        )
      })}
    </section>
  )
}
