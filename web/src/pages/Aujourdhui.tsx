import { useQuery } from '@tanstack/react-query'
import Avatar, { paletteAvatar } from '@/components/Avatar'
import { Camion, MaisonSoleil, Sapin } from '@/components/Illustrations'
import OccurrenceListe from '@/components/OccurrenceListe'
import QuickAdd from '@/components/QuickAdd'
import { api, dateLocaleIso, type Occurrence } from '@/lib/api'
import { bornesJourneeLocale, dateLongue, dodosAvant, jourCourt } from '@/lib/format'
import { phraseDuJour } from '@/lib/humeur'

const DATE_DEMENAGEMENT = '2026-10-06'

function prochainNoel(): string {
  const maintenant = new Date()
  const annee =
    maintenant.getMonth() === 11 && maintenant.getDate() > 25
      ? maintenant.getFullYear() + 1
      : maintenant.getFullYear()
  return `${annee}-12-25`
}

export default function Aujourdhui() {
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

  const comptesARebours = [
    {
      nom: 'Déménagement',
      date: DATE_DEMENAGEMENT,
      icone: <Camion />,
      couleur: 'var(--orange)',
    },
    { nom: 'Noël', date: prochainNoel(), icone: <Sapin />, couleur: 'var(--vert)' },
  ].filter((c) => dodosAvant(c.date) > 0)

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
            />
          )}
          <QuickAdd />
        </div>

        {/* Colonne latérale */}
        <div className="flex flex-col gap-[18px]">
          <CetteSemaine enAttente={enAttente ?? []} aujourdhui={aujourdhui} />
          <Equipe enAttente={enAttente ?? []} utilisateurs={utilisateurs ?? []} />
          <ComptesARebours comptes={comptesARebours} />
        </div>
      </div>
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
}: {
  comptes: { nom: string; date: string; icone: React.ReactNode; couleur: string }[]
}) {
  if (comptes.length === 0) {
    return null
  }
  return (
    <section className="flex flex-col gap-3 rounded-3xl bg-carte px-6 py-5 shadow-carte">
      <h2 className="text-lg font-bold">Comptes à rebours</h2>
      {comptes.map((c) => {
        const dodos = dodosAvant(c.date)
        const [, mois, jour] = c.date.split('-').map(Number)
        const libelleDate = new Date(2000, mois - 1, jour).toLocaleDateString('fr-CA', {
          day: 'numeric',
          month: 'long',
        })
        return (
          <div key={c.nom} className="flex items-center gap-3.5 rounded-2xl bg-creux px-4 py-3">
            {c.icone}
            <div className="flex-1">
              <div className="text-[15px] font-bold">{c.nom}</div>
              <div className="text-xs text-sourdine">{libelleDate}</div>
            </div>
            <div className="font-titre text-[22px] font-bold" style={{ color: c.couleur }}>
              {dodos} <span className="text-[13px] text-dore">{dodos === 1 ? 'dodo' : 'dodos'}</span>
            </div>
          </div>
        )
      })}
    </section>
  )
}
