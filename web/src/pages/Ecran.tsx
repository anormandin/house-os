import { createElement, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { Check } from 'lucide-react'
import { api, type DonneesEcran, type LigneEcran } from '@/lib/api'
import { dimensionsEcran, initiales, libelleDodos, pileAnnoncee, quandCeJour } from '@/lib/ecran-vues'
import { dateLongue, dodosAvant, heureQuebec } from '@/lib/format'
import { DATE_DEMENAGEMENT, phraseDuJour } from '@/lib/humeur'
import { iconeMeteo, pastillesMeteo } from '@/lib/meteo-vues'
import { cn } from '@/lib/utils'

/*
 * La vue e-ink — une page imprimée qui se réimprime (vault : Features/Affichage
 * E-ink, grammaire dans Inspiration UI/Affichage Mural Et E-ink). Noir plein sur
 * blanc, aucune nuance : tout gris disparaîtrait au seuillage 1-bit. Conçue pour
 * 1872×1404 (reTerminal E1003 en paysage) ; une autre taille passe par un zoom
 * uniforme, jamais par un layout desktop agrandi.
 */

const LARGEUR_CONCUE = 1872

export default function Ecran() {
  const [recherche] = useSearchParams()
  const { largeur, hauteur } = dimensionsEcran(recherche)
  const pile = pileAnnoncee(recherche)
  // ?accueil=1A2B3C : l'écran qu'un appareil reçoit juste après son enrôlement —
  // aucune donnée à charger, seulement dire qu'il est reconnu.
  const accueil = recherche.get('accueil')
  const { data, isSuccess, isError } = useQuery({
    queryKey: ['ecran'],
    queryFn: api.donneesEcran,
    retry: 1,
    enabled: accueil === null,
  })
  const pret = accueil !== null || isSuccess || isError

  // Le signal « prêt » que le navigateur de rendu attend : polices chargées et
  // données rendues (ou l'échec, marqué — le serveur ne doit pas servir une page
  // à moitié vide comme si de rien n'était).
  useEffect(() => {
    if (!pret) return
    let actif = true
    document.fonts.ready.then(() => {
      if (!actif) return
      document.documentElement.dataset.pret = '1'
      if (isError) document.documentElement.dataset.erreur = '1'
    })
    return () => {
      actif = false
    }
  }, [pret, isError])

  const zoom = largeur / LARGEUR_CONCUE

  return (
    <div
      className="overflow-hidden bg-white font-sans text-black"
      style={{ zoom, width: LARGEUR_CONCUE, height: hauteur / zoom }}
    >
      {accueil !== null ? (
        <Accueil identifiant={accueil} />
      ) : data ? (
        <Page donnees={data} pile={pile} />
      ) : isError ? (
        <Panne />
      ) : null}
    </div>
  )
}

function Page({ donnees, pile }: { donnees: DonneesEcran; pile: number | null }) {
  const phrase =
    donnees.phrase ??
    phraseDuJour({
      ouvertes: donnees.ouvertes,
      enRetard: donnees.enRetard,
      faites: donnees.faites,
      dodosDemenagement: dodosAvant(DATE_DEMENAGEMENT) > 0 ? dodosAvant(DATE_DEMENAGEMENT) : null,
    })
  const date = new Date(`${donnees.date}T12:00:00`)
  const aCote = donnees.meteo !== null || donnees.prochaineCollecte !== null || donnees.prochainCompte !== null

  return (
    <div className="flex h-full flex-col">
      {/* La seule bande inversée de l'écran : la date, ancre du tableau mural. */}
      <header className="flex items-end justify-between gap-16 bg-black px-16 pb-10 pt-11 text-white">
        <h1 className="font-titre text-[104px] font-bold leading-none text-white">{dateLongue(date)}</h1>
        <div className="max-w-[860px] text-right">
          <div className="text-[52px] font-extrabold leading-[1.1]">{phrase.titre}</div>
          <div className="mt-2 text-[36px] leading-tight">{phrase.sousTitre}</div>
        </div>
      </header>

      <main className={cn('grid min-h-0 flex-1', aCote ? 'grid-cols-[1fr_4px_640px]' : 'grid-cols-1')}>
        <section className="flex min-h-0 flex-col px-16 pb-8 pt-9">
          <div className="flex items-baseline justify-between">
            <Etiquette>Aujourd&rsquo;hui</Etiquette>
            {donnees.ouvertes > 0 && (
              <span className="text-[34px] font-extrabold">
                {donnees.faites > 0 ? `${donnees.faites} faite${donnees.faites > 1 ? 's' : ''} · ` : ''}
                {donnees.ouvertes} à faire
              </span>
            )}
          </div>

          {donnees.evenementsDuJour.length > 0 && (
            <div className="mt-5 flex flex-wrap gap-4">
              {donnees.evenementsDuJour.map((e, i) => (
                <span
                  key={`${e.titre}-${i}`}
                  className="rounded-full border-[5px] border-black px-7 py-1 text-[36px] font-extrabold"
                >
                  {e.titre} aujourd&rsquo;hui
                </span>
              ))}
            </div>
          )}

          {donnees.lignes.length === 0 ? (
            <div className="flex flex-1 items-center justify-center pb-16">
              <span className="font-titre text-[150px] font-bold leading-none text-black">
                Tout est beau ✓
              </span>
            </div>
          ) : (
            <>
              <ul className="mt-3 min-h-0 flex-1 overflow-hidden">
                {donnees.lignes.map((ligne, i) => (
                  // Peu de lignes : on laisse les titres respirer sur deux lignes ;
                  // une liste pleine élague à une seule (jamais de débordement).
                  <Rangee key={i} ligne={ligne} deuxLignes={donnees.lignes.length <= 5} />
                ))}
              </ul>
              {donnees.lignesEnPlus > 0 && (
                <div className="shrink-0 pt-4 text-[32px] font-bold">
                  + {donnees.lignesEnPlus} autre{donnees.lignesEnPlus > 1 ? 's' : ''}
                </div>
              )}
            </>
          )}
        </section>

        {aCote && (
          <>
            <div className="bg-black" />
            <aside className="flex flex-col divide-y-4 divide-black px-14">
              {donnees.meteo && <ZoneMeteo meteo={donnees.meteo} />}
              {donnees.prochaineCollecte && (
                <Zone etiquette="Collecte">
                  <div className="line-clamp-1 text-[72px] font-extrabold leading-tight">
                    {donnees.prochaineCollecte.titre}
                  </div>
                  <div className="text-[48px] font-bold">
                    {quandCeJour(donnees.prochaineCollecte.date, donnees.date)}
                  </div>
                </Zone>
              )}
              {donnees.prochainCompte && (
                <Zone etiquette={donnees.prochainCompte.titre}>
                  <div className="font-titre text-[120px] font-bold leading-none text-black">
                    {libelleDodos(donnees.prochainCompte.dateCible)}
                  </div>
                </Zone>
              )}
            </aside>
          </>
        )}
      </main>

      <footer className="flex items-center justify-between border-t-4 border-black px-16 py-4 text-[30px] font-bold">
        <span>Rendu à {heureQuebec(donnees.renduLe)}</span>
        {pile !== null && <span>Pile {pile} %</span>}
      </footer>
    </div>
  )
}

function Rangee({ ligne, deuxLignes }: { ligne: LigneEcran; deuxLignes: boolean }) {
  return (
    <li className="flex items-center gap-6 border-b-[3px] border-black py-[14px]">
      <span
        className={cn(
          'flex size-[60px] shrink-0 items-center justify-center rounded-full border-[5px] border-black text-[24px] font-extrabold leading-none tracking-tight',
          ligne.faite && 'bg-black text-white',
        )}
      >
        {ligne.faite ? <Check className="size-[38px]" strokeWidth={4} /> : initiales(ligne.assigne)}
      </span>
      <span
        className={cn(
          'text-[46px] font-bold leading-[1.2]',
          deuxLignes ? 'line-clamp-2' : 'line-clamp-1',
          ligne.faite && 'line-through decoration-[4px]',
        )}
      >
        {ligne.titre}
      </span>
      {ligne.enRetard && (
        <span className="ml-auto shrink-0 bg-black px-4 py-1 text-[24px] font-extrabold uppercase tracking-[0.1em] text-white">
          en retard
        </span>
      )}
    </li>
  )
}

function ZoneMeteo({ meteo }: { meteo: NonNullable<DonneesEcran['meteo']> }) {
  const pastille = pastillesMeteo(meteo.verdicts)[0]
  return (
    <Zone etiquette="Dehors">
      <div className="flex items-center gap-6">
        {createElement(iconeMeteo(meteo.codeMeteo), { className: 'size-[190px] shrink-0', strokeWidth: 2 })}
        <span className="font-titre text-[190px] font-bold leading-none text-black">
          {Math.round(meteo.temperatureC)}°
        </span>
      </div>
      <div className="text-[44px] font-bold">
        {Math.round(meteo.tempMin)}° à {Math.round(meteo.tempMax)}°
        {meteo.probabilitePrecipitation >= 30 && ` · ${meteo.probabilitePrecipitation} % de pluie`}
      </div>
      {pastille && <div className="mt-2 text-[38px] font-bold leading-tight">{pastille.texte}</div>}
    </Zone>
  )
}

function Zone({ etiquette, children }: { etiquette: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-3 py-8">
      <Etiquette>{etiquette}</Etiquette>
      {children}
    </div>
  )
}

function Etiquette({ children }: { children: React.ReactNode }) {
  return (
    <div className="line-clamp-1 text-[34px] font-extrabold uppercase tracking-[0.12em]">{children}</div>
  )
}

/* Après l'enrôlement : l'appareil montre qu'il est reconnu, et son identifiant —
   c'est ce qu'on lit dans la liste des appareils pour le nommer. */
function Accueil({ identifiant }: { identifiant: string }) {
  return (
    <div className="flex h-full flex-col items-center justify-center gap-10 text-center">
      <span className="font-titre text-[200px] font-bold leading-none text-black">House OS</span>
      <span className="text-[56px] font-bold">Écran enrôlé</span>
      <span className="border-[6px] border-black px-12 py-4 font-titre text-[120px] font-bold leading-none tracking-[0.12em] text-black">
        {identifiant}
      </span>
      <span className="text-[40px]">La maison s&rsquo;affichera au prochain réveil.</span>
    </div>
  )
}

/* La page ne cache pas une panne : l'écran mural montrerait un vide trompeur. */
function Panne() {
  return (
    <div className="flex h-full items-center justify-center">
      <span className="font-titre text-[96px] font-bold text-black">House OS ne répond pas</span>
    </div>
  )
}
