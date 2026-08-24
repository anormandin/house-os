import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import Avatar, { paletteAvatar } from '@/components/Avatar'
import ComptesAReboursGestion from '@/components/ComptesAReboursGestion'
import { ICONES_COMPTE, MaisonSoleil } from '@/components/Illustrations'
import MeteoCarte from '@/components/MeteoCarte'
import OccurrenceListe from '@/components/OccurrenceListe'
import QuickAdd from '@/components/QuickAdd'
import TacheEditeur from '@/components/TacheEditeur'
import { api, dateLocaleIso, type CompteARebours, type Occurrence } from '@/lib/api'
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
  const { data: utilisateurs } = useQuery({
    queryKey: ['utilisateurs'],
    queryFn: api.utilisateurs,
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
  const [gestionComptesOuverte, setGestionComptesOuverte] = useState(false)

  const chargement = chargementOuvertes || chargementFaites
  const aujourdhui = dateLocaleIso()
  const dodosDemenagement = dodosAvant(DATE_DEMENAGEMENT)

  const phrase = phraseDuJour({
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
          <CetteSemaine enAttente={enAttente ?? []} aujourdhui={aujourdhui} />
          <Equipe enAttente={enAttente ?? []} utilisateurs={utilisateurs ?? []} />
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
    </div>
  )
}

function CetteSemaine({
  enAttente,
  aujourdhui,
}: {
  enAttente: Occurrence[]
  aujourdhui: string
}) {
  const [annee, mois, jour] = aujourdhui.split('-').map(Number)
  const dansSeptJours = dateLocaleIso(new Date(annee, mois - 1, jour + 7))
  const semaine = enAttente
    .filter((o) => o.echeance !== null && o.echeance > aujourdhui && o.echeance <= dansSeptJours)
    .slice(0, 5)

  return (
    <section className="rounded-3xl bg-carte px-6 py-5 shadow-carte">
      <h2 className="mb-3 text-lg font-bold">Cette semaine</h2>
      {semaine.length === 0 ? (
        <p className="text-sm text-sourdine">Rien de prévu — belle semaine en vue.</p>
      ) : (
        <ul className="flex flex-col gap-[11px] text-sm">
          {semaine.map((o) => (
            <li key={o.id} className="flex items-center gap-2.5">
              <span className="rounded-full bg-creux px-2.5 py-0.5 text-xs font-bold text-dore">
                {jourCourt(o.echeance!)}
              </span>
              <span className="min-w-0 truncate">{o.titre}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function Equipe({
  enAttente,
  utilisateurs,
}: {
  enAttente: Occurrence[]
  utilisateurs: { id: string; nomUtilisateur: string; nomAffichage: string }[]
}) {
  const comptes = utilisateurs.map((u) => ({
    utilisateur: u,
    nombre: enAttente.filter((o) => o.assigneA?.id === u.id).length,
  }))
  const maximum = Math.max(1, ...comptes.map((c) => c.nombre))

  return (
    <section className="rounded-3xl bg-carte px-6 py-5 shadow-carte">
      <h2 className="mb-1 text-lg font-bold">L’équipe</h2>
      <p className="mb-3 text-xs text-sourdine">tâches à faire chacun</p>
      <div className="flex flex-col gap-2.5">
        {comptes.map(({ utilisateur, nombre }) => {
          const palette = paletteAvatar(utilisateur)
          return (
            <div key={utilisateur.id} className="flex items-center gap-3">
              <Avatar utilisateur={utilisateur} taille={28} />
              <div className="h-2.5 flex-1 rounded-full bg-creux">
                <div
                  className="h-2.5 rounded-full"
                  style={{ width: `${(nombre / maximum) * 100}%`, background: palette.barre }}
                />
              </div>
              <span className="w-4 text-right text-[15px] font-bold" style={{ color: palette.texte }}>
                {nombre}
              </span>
            </div>
          )
        })}
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
