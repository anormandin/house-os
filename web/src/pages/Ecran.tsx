import { useEffect, useRef, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { Check } from 'lucide-react'
import { api, type DonneesEcran, type FaitEcran, type LigneEcran } from '@/lib/api'
import {
  capaciteListe,
  capaciteWidgets,
  dimensionsEcran,
  etatDuJour,
  faitAvecTexteLong,
  faitsDeLaFamille,
  faitsEnWidgets,
  formeDuCiel,
  placesDuFonds,
  grilleDuJour,
  MAX_RANGEES_CIEL,
  rangeesDuCiel,
  initiales,
  manchetteDuJour,
  libelleDodos,
  pileAnnoncee,
  plancher,
  quandCeJour,
  rangeeSerree,
  repartitionColonnes,
  resteAAnnoncer,
  surtitreEdition,
  type EtatDuJour,
  type Grille,
  type Plancher,
} from '@/lib/ecran-vues'
import { dateJournal, dodosAvant, heureQuebec } from '@/lib/format'
import { DATE_DEMENAGEMENT, phraseDuJour } from '@/lib/humeur'
import { meteoEnMots, pastillesMeteo } from '@/lib/meteo-vues'
import { cn } from '@/lib/utils'

/*
 * Le journal de la maison — la vue e-ink (vault : Features/Journal De La Maison,
 * grammaire dans Inspiration UI/Affichage Mural Et E-ink). Noir plein sur blanc,
 * aucune nuance : tout gris disparaîtrait au seuillage 1-bit. Une seule mise en
 * page, six remplissages : le rang est une fonction (lib/ecran-vues), jamais un
 * second gabarit. Conçue pour 1872×1404 (reTerminal E1003 en paysage) ; une autre
 * taille passe par un zoom uniforme, jamais par un layout desktop agrandi.
 *
 * La manchette est encore le titre d'humeur : l'éditorialiste arrive à l'étape 7
 * du Plan 2026-09-20 Journal Éditorial.
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
  const cadre = useRef<HTMLDivElement>(null)
  const zoom = largeur / LARGEUR_CONCUE

  // Le signal « prêt » que le navigateur de rendu attend : polices chargées et
  // données rendues (ou l'échec, marqué — le serveur ne doit pas servir une page
  // à moitié vide comme si de rien n'était). On en profite pour mesurer le
  // débordement : c'est le mode de panne historique de cette vue, et il est
  // invisible autrement (le bas est coupé, rien ne le dit).
  useEffect(() => {
    if (!pret) return
    let actif = true
    document.fonts.ready.then(() => {
      if (!actif) return
      const boite = cadre.current
      if (boite !== null) {
        document.documentElement.dataset.debordement = String(debordementPx(boite, zoom))
      }
      document.documentElement.dataset.pret = '1'
      if (isError) document.documentElement.dataset.erreur = '1'
    })
    return () => {
      actif = false
    }
  }, [pret, isError, zoom])

  return (
    <div
      ref={cadre}
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

  const aPlancher = plancher(donnees.lignes, donnees.prochainCompte, donnees.date)
  const grille = grilleDuJour(donnees.ouvertes, aPlancher !== null)
  const aUnCompte = donnees.prochainCompte !== null
  const candidats = widgetsDuJour(donnees, grille).slice(0, grille.widgets)
  const colonnes = repartitionColonnes(
    donnees.lignes.length === 0 ? 0 : grille.colonnesListe,
    candidats.length + (aUnCompte ? 1 : 0),
  )
  // Le budget du rang dit ce que le journal veut ; la capacité dit ce que le papier
  // tient. Ce qui déborde disparaît de lui-même — le fonds est déjà classé, donc ce
  // qui tombe est ce qui compte le moins (vault : Fonds De Tiroir).
  const widgets = candidats.slice(0, capaciteWidgets(colonnes, aUnCompte))
  // Rien du tout à mettre dans le corps : aucune tâche, aucun widget, aucun compte à
  // rebours. C'est l'installation neuve (ou un serveur qui n'a pas encore de
  // coordonnées) ; la manchette prend alors la page au lieu d'une grille blanche.
  const corpsVide = colonnes.liste === 0 && colonnes.aparte === 0
  const serree = rangeeSerree(donnees.lignes.length, colonnes.liste)
  // Ce que le papier ne peut pas montrer est annoncé, jamais coupé en silence.
  const visibles = donnees.lignes.slice(0, capaciteListe(colonnes.liste, serree, grille.chapeau))
  const enPlus = resteAAnnoncer(donnees.lignes, visibles, donnees.lignesEnPlus)
  // Au sommaire, l'aparté n'existe plus : le compte à rebours descend avec les
  // widgets dans la bande de pied. Il ne disparaît jamais.
  const bandeDePied = colonnes.bandeDePied
    ? [
        ...(donnees.prochainCompte === null
          ? []
          : [
              <Widget
                key="compte"
                widget={{
                  cle: 'compte',
                  etiquette: donnees.prochainCompte.titre,
                  valeur: libelleDodos(donnees.prochainCompte.dateCible),
                }}
                compact
              />,
            ]),
        ...widgets.map((w) => <Widget key={w.cle} widget={w} compact />),
      ]
    : []

  return (
    <div className="flex h-full flex-col">
      <BlocTitre
        donnees={donnees}
        date={date}
        surtitre={surtitreEdition(donnees.renduLe)}
        etat={etatDuJour(aPlancher, donnees.ouvertes, donnees.faites)}
      />

      {grille.rang === 'sommaire' ? (
        // La seule bande inversée du journal, et elle ne sort qu'au sommaire :
        // passé six tâches, la manchette n'a plus de sens (maquettes, 2026-09-20).
        <div className="flex items-baseline justify-between bg-black px-16 py-5 text-white">
          <span className="font-titre text-[76px] font-bold leading-none">Journée chargée</span>
          <span className="text-[40px] font-extrabold uppercase tracking-[0.1em]">
            {donnees.ouvertes} choses au programme
          </span>
        </div>
      ) : (
        <Manchette grille={grille} aPlancher={aPlancher} phrase={phrase} pleinePage={corpsVide} />
      )}

      {!corpsVide && (
        <main
          className="grid min-h-0 flex-1 border-t-[3px] border-black"
          style={{ gridTemplateColumns: `repeat(${colonnes.liste + colonnes.aparte}, minmax(0, 1fr))` }}
        >
          {colonnes.liste > 0 && (
            <section className="flex min-h-0 flex-col overflow-hidden px-14 pb-6 pt-7" style={{ gridColumn: `span ${colonnes.liste}` }}>
              <div className="flex items-baseline justify-between">
                <Etiquette>Aujourd&rsquo;hui</Etiquette>
                <span className="text-[30px] font-extrabold">
                  {donnees.faites > 0 && `${donnees.faites} faite${donnees.faites > 1 ? 's' : ''} · `}
                  {donnees.ouvertes} à faire
                </span>
              </div>
              <ul
                className="mt-3 min-h-0 flex-1 overflow-hidden"
                style={colonnes.liste > 1 ? { columnCount: colonnes.liste, columnGap: '56px' } : undefined}
              >
                {visibles.map((ligne, i) => (
                  <Rangee key={i} ligne={ligne} serree={serree} />
                ))}
              </ul>
              {enPlus > 0 && (
                <div className="shrink-0 pt-3 text-[30px] font-bold">
                  + {enPlus} autre{enPlus > 1 ? 's' : ''}
                </div>
              )}
            </section>
          )}

          {colonnes.aparte > 0 &&
            colonnesAparte(colonnes.aparte, donnees, widgets).map((contenu, i) => (
              <aside
                key={i}
                className={cn(
                  'flex min-h-0 flex-col divide-y-[3px] divide-black overflow-hidden px-12',
                  (colonnes.liste > 0 || i > 0) && 'border-l-[3px] border-black',
                )}
              >
                {contenu}
              </aside>
            ))}
        </main>
      )}

      {bandeDePied.length > 0 && (
        <div
          className="grid shrink-0 divide-x-[3px] divide-black border-t-[3px] border-black"
          style={{ gridTemplateColumns: `repeat(${bandeDePied.length}, minmax(0, 1fr))` }}
        >
          {bandeDePied.map((contenu, i) => (
            <div key={i} className="px-12 py-4">
              {contenu}
            </div>
          ))}
        </div>
      )}

      <footer className="flex shrink-0 items-center justify-between border-t-[6px] border-black px-16 py-4 text-[28px] font-bold">
        <span>Imprimé à {heureQuebec(donnees.renduLe)}</span>
        <span className="uppercase tracking-[0.2em]">House OS</span>
        <span>{pile === null ? '' : `Pile ${pile} %`}</span>
      </footer>
    </div>
  )
}

/*
 * Le bloc-titre du journal, dans l'ordre d'un quotidien (maquettes,
 * `design/maquettes/une-editorialiste.html`) : les oreilles (l'édition, le lieu de
 * publication, le numéro), le nom en capitales entre deux filets, puis la dateline
 * — date, état du jour, temps qu'il fait. Les nombres sont ceux des maquettes, qui
 * sont dans le même espace que cette page (1872 × 1404).
 *
 * Le lieu et le numéro peuvent manquer (`MAISON_LIEU` vide, journal vierge) : les
 * oreilles se composent alors avec ce qui reste, sans trou.
 */
function BlocTitre({
  donnees,
  date,
  surtitre,
  etat,
}: {
  donnees: DonneesEcran
  date: Date
  surtitre: string
  etat: EtatDuJour
}) {
  const oreilles: ReactNode[] = [surtitre]
  if (donnees.lieu !== null) oreilles.push(donnees.lieu)
  if (donnees.numeroEdition !== null) {
    oreilles.push(
      <>
        N<sup className="text-[0.62em]">o</sup> {donnees.numeroEdition}
      </>,
    )
  }

  return (
    <header className="shrink-0 border-b-[6px] border-black px-16 pt-8">
      <div className="flex items-end justify-between gap-10 pb-2 text-[24px] font-extrabold uppercase tracking-[0.18em]">
        {oreilles.map((oreille, i) => (
          <span key={i} className="truncate">
            {oreille}
          </span>
        ))}
      </div>
      <div className="border-y-[3px] border-black pb-3 pt-1.5 text-center">
        <span className="block font-titre text-[126px] font-black uppercase leading-[0.95] tracking-[-0.02em]">
          La maison
        </span>
      </div>
      <div className="flex items-center justify-between gap-10 pb-[11px] pt-[9px] text-[28px] font-bold">
        <span className="shrink-0">{dateJournal(date)}</span>
        <span
          className={cn(
            'truncate',
            etat.urgent
              ? 'bg-black px-5 py-1 font-extrabold uppercase tracking-[0.12em] text-white'
              : 'tracking-[0.05em]',
          )}
        >
          {etat.texte}
        </span>
        <span className="shrink-0">{donnees.meteo === null ? '' : meteoEnMots(donnees.meteo)}</span>
      </div>
    </header>
  )
}

/* La manchette : un surtitre qui dit de quoi il retourne, le titre, le chapeau.
   Au rang « événement », le plancher prend le titre quoi qu'il arrive. */
function Manchette({
  grille,
  aPlancher,
  phrase,
  pleinePage,
}: {
  grille: Grille
  aPlancher: Plancher | null
  phrase: { titre: string; sousTitre: string }
  pleinePage: boolean
}) {
  const titre = aPlancher === null ? phrase.titre : aPlancher.titre
  const surtitre = surtitreManchette(aPlancher)
  const { taille, lettrine } = manchetteDuJour(titre, grille)

  return (
    <section
      className={cn(
        'flex flex-col justify-center overflow-hidden px-16 pb-6 pt-6',
        pleinePage && 'min-h-0 flex-1',
      )}
    >
      {surtitre !== null && (
        <div className="text-[30px] font-extrabold uppercase tracking-[0.14em]">{surtitre}</div>
      )}
      <h1 className="mt-2 font-titre font-bold leading-[1.02]" style={{ fontSize: taille }}>
        {lettrine ? (
          <>
            {/* La lettrine : deux lignes de haut, comme au plomb. */}
            <span
              className="float-left mr-5 mt-2 font-titre font-bold leading-[0.74]"
              style={{ fontSize: taille * 1.5 }}
            >
              {titre.slice(0, 1)}
            </span>
            {titre.slice(1)}
          </>
        ) : (
          titre
        )}
      </h1>
      {grille.chapeau && (
        <p className="clear-both mt-4 line-clamp-2 text-[42px] font-bold leading-tight">{phrase.sousTitre}</p>
      )}
    </section>
  )
}

/**
 * Le surtitre de la manchette. Dans les maquettes c'est une ligne éditoriale
 * (« Le condo est vendu depuis le 1er septembre »), pas le compte du jour — celui-ci
 * vit dans la dateline du bloc-titre (`etatDuJour`). Tant que l'éditorialiste n'écrit
 * pas (étape 7), la seule chose vraie qu'on ait à mettre là est la raison du plancher ;
 * sinon la place reste vide plutôt que de répéter la dateline mot pour mot.
 */
function surtitreManchette(aPlancher: Plancher | null): string | null {
  if (aPlancher === null) {
    return null
  }
  return aPlancher.raison === 'compte'
    ? 'Le compte à rebours est à zéro'
    : 'En retard depuis plus de trois jours'
}

type WidgetEcran = { cle: string; etiquette: string; valeur: ReactNode; detail?: ReactNode }

/**
 * Ce que le journal a à montrer à côté de la liste, dans l'ordre. D'abord ce qui
 * engage la journée — un événement au calendrier, la collecte, le verdict du dehors —
 * puis le fonds de tiroir, qui remplit ce qu'il reste du budget du rang.
 *
 * Le fonds de tiroir rend des faits déjà classés et sans mise en forme : le choix de
 * la densité est fait ici, et nulle part ailleurs
 * (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
 */
function widgetsDuJour(donnees: DonneesEcran, grille: Grille): WidgetEcran[] {
  const widgets: WidgetEcran[] = []

  for (const [i, evenement] of donnees.evenementsDuJour.entries()) {
    widgets.push({
      cle: `evenement-${i}`,
      etiquette: "Aujourd'hui, au calendrier",
      valeur: evenement.titre,
    })
  }

  if (donnees.prochaineCollecte !== null) {
    widgets.push({
      cle: 'collecte',
      etiquette: 'Dans la ville',
      valeur: donnees.prochaineCollecte.titre,
      detail: quandCeJour(donnees.prochaineCollecte.date, donnees.date),
    })
  }

  const pastille = donnees.meteo === null ? undefined : pastillesMeteo(donnees.meteo.verdicts)[0]
  if (pastille !== undefined) {
    widgets.push({ cle: 'verdict', etiquette: 'Dehors', valeur: pastille.texte, detail: pastille.raison })
  }

  widgets.push(...widgetsDuFonds(donnees.faits, grille))
  return widgets
}

/**
 * Le fonds de tiroir, dans l'ordre où il a classé ses faits — c'est tout l'intérêt du
 * score : le journal ne réordonne rien, il coupe à la fin. Chaque fait devient un
 * widget empilé à sa place, sauf ceux que le bloc du ciel absorbe : le bloc prend
 * **une seule place**, celle du meilleur fait qu'il contient.
 *
 * Le budget du rang est tenu par l'appelant, qui tronque cette liste — ce qui déborde
 * du budget disparaît de lui-même, sans avoir à être compté deux fois ici.
 */
function widgetsDuFonds(faits: FaitEcran[], grille: Grille): WidgetEcran[] {
  const utiles = faitsEnWidgets(faits)
  const bloc = blocDuCiel(faitsDeLaFamille(utiles, 'Ciel'), grille)
  const avecTexte = faitAvecTexteLong(grille.widgets)

  return placesDuFonds(utiles, bloc?.prises ?? VIDE).flatMap((place) => {
    if (place.type === 'bloc-ciel') {
      return bloc === null ? [] : [bloc.widget]
    }
    return [
      {
        cle: place.fait.cle,
        etiquette: place.fait.etiquette,
        valeur: place.fait.valeur,
        detail: avecTexte ? place.fait.texte : undefined,
      },
    ]
  })
}

const VIDE: ReadonlySet<string> = new Set()

/**
 * Le ciel, aux trois densités : « les widgets rétrécissent avant de disparaître »
 * (vault : D-2026-09-20 Une Seule Mise En Page À Rangs). Ce que le ciel perd quand
 * la place manque, dans l'ordre : ses rangées de tableau, puis son texte long, puis
 * tout sauf le premier fait.
 *
 * Il rend **un** widget et la liste des clés qu'il a absorbées. Les faits du ciel qui
 * restent dehors ne suivent pas le bloc : ils retournent au classement et sortent à
 * leur propre score, comme n'importe quel autre fait. Sans ça, un jour d'équinoxe, la
 * durée du jour — le fait le plus banal du fonds — passait devant une garantie qui
 * expire, soixante fois mieux classée (trouvé en revue de code, 2026-09-20).
 */
function blocDuCiel(
  ciel: FaitEcran[],
  grille: Grille,
): { widget: WidgetEcran; prises: Set<string> } | null {
  if (ciel.length === 0) {
    return null
  }

  const court = (f: FaitEcran): WidgetEcran => ({ cle: f.cle, etiquette: f.etiquette, valeur: f.valeur })
  const rangees = rangeesDuCiel(ciel).slice(0, MAX_RANGEES_CIEL)
  const forme = formeDuCiel(rangees.length, grille.widgets)

  if (forme === 'demi-phrase') {
    return { widget: court(ciel[0]), prises: new Set([ciel[0].cle]) }
  }
  if (forme === 'phrase') {
    return {
      widget: { ...court(ciel[0]), detail: ciel[0].texte },
      prises: new Set([ciel[0].cle]),
    }
  }

  // Ce que le tableau n'a pas pris garde sa place de widget empilé, où il a deux
  // lignes pour se dire — un fait trop long ne se fait pas couper, il change de forme.
  return {
    widget: { cle: 'ciel', etiquette: 'Le ciel', valeur: <TableauDuCiel faits={rangees} /> },
    prises: new Set(rangees.map((f) => f.cle)),
  }
}

/* Le tableau du ciel : une rangée par fait, l'étiquette à gauche et la valeur à
   droite, aux dimensions des maquettes (28 px, filets de 3 px entre les rangées).
   La valeur ne rétrécit jamais — c'est elle qui porte l'information ; si quelque
   chose doit céder, c'est l'étiquette. En pratique `rangeesDuCiel` a déjà écarté
   les faits trop longs, et rien ne cède. */
function TableauDuCiel({ faits }: { faits: FaitEcran[] }) {
  return (
    <dl className="flex flex-col">
      {faits.map((fait) => (
        <div
          key={fait.cle}
          className="flex items-baseline justify-between gap-6 border-b-[3px] border-black py-[7px] last:border-b-0"
        >
          <dt className="truncate text-[28px] font-bold">{fait.etiquette}</dt>
          <dd className="shrink-0 whitespace-nowrap text-[28px] font-extrabold">{fait.valeur}</dd>
        </div>
      ))}
    </dl>
  )
}

/** Les widgets répartis en colonnes, l'encadré du compte à rebours en tête. */
function colonnesAparte(nombre: number, donnees: DonneesEcran, widgets: WidgetEcran[]): ReactNode[] {
  const colonnes: ReactNode[][] = Array.from({ length: nombre }, () => [])

  if (donnees.prochainCompte !== null) {
    colonnes[0].push(
      <Encadre key="compte" titre={donnees.prochainCompte.titre}>
        {libelleDodos(donnees.prochainCompte.dateCible)}
      </Encadre>,
    )
  }
  for (const [i, widget] of widgets.entries()) {
    colonnes[(i + (donnees.prochainCompte === null ? 0 : 1)) % nombre].push(
      <div key={widget.cle} className="py-6">
        <Widget widget={widget} />
      </div>,
    )
  }
  return colonnes
}

function Widget({ widget, compact = false }: { widget: WidgetEcran; compact?: boolean }) {
  // Le clamp ne vaut que pour du texte : appliqué à un bloc (le tableau du ciel), il
  // dépend d'un détail de `-webkit-box` pour ne rien couper, et ça n'est pas une chose
  // sur laquelle on veut parier au mur.
  const texte = typeof widget.valeur === 'string'
  return (
    <div className="flex flex-col gap-2">
      <Etiquette>{widget.etiquette}</Etiquette>
      <div
        className={cn(
          'font-bold leading-tight',
          texte && 'line-clamp-2',
          compact ? 'text-[38px]' : 'text-[52px]',
        )}
      >
        {widget.valeur}
      </div>
      {widget.detail !== undefined && (
        <div className={cn('line-clamp-2 font-bold', compact ? 'text-[30px]' : 'text-[36px]')}>
          {widget.detail}
        </div>
      )}
    </div>
  )
}

/* L'encadré du compte à rebours : le seul bloc cerné du journal. */
function Encadre({ titre, children }: { titre: string; children: ReactNode }) {
  return (
    <div className="my-6 border-[6px] border-black px-6 py-5 text-center">
      <div className="line-clamp-1 text-[30px] font-extrabold uppercase tracking-[0.12em]">{titre}</div>
      <div className="mt-2 font-titre text-[92px] font-bold leading-none">{children}</div>
    </div>
  )
}

function Rangee({ ligne, serree }: { ligne: LigneEcran; serree: boolean }) {
  const marque = initiales(ligne.assigne)
  return (
    <li
      className={cn(
        'flex items-center gap-5 border-b-[3px] border-black',
        // break-inside : en multi-colonnes, une rangée coupée en deux est illisible.
        serree ? 'break-inside-avoid py-[10px]' : 'py-[13px]',
      )}
    >
      {/* Pas de cercle vide : une tâche que personne ne porte n'a pas de pastille —
          elle gagnait une soixantaine de pixels de titre à ne rien dire. */}
      {(ligne.faite || marque !== '') && (
        <span
          className={cn(
            'flex shrink-0 items-center justify-center rounded-full border-[4px] border-black font-extrabold leading-none tracking-tight',
            serree ? 'size-[48px] text-[20px]' : 'size-[58px] text-[24px]',
            ligne.faite && 'bg-black text-white',
          )}
        >
          {ligne.faite ? <Check className={serree ? 'size-[30px]' : 'size-[36px]'} strokeWidth={4} /> : marque}
        </span>
      )}
      <span
        className={cn(
          // Serrée, la rangée tient sur une ligne : c'est ce qui fait entrer neuf
          // items par colonne (mesuré aux maquettes). Élaguer, pas rapetisser.
          'font-bold leading-[1.2]',
          serree ? 'line-clamp-1 text-[34px]' : 'line-clamp-2 text-[44px]',
          ligne.faite && 'line-through decoration-[4px]',
        )}
      >
        {ligne.titre}
      </span>
      {ligne.joursDeRetard > 0 && (
        <span className="ml-auto shrink-0 bg-black px-3 py-1 text-[22px] font-extrabold uppercase tracking-[0.08em] text-white">
          {ligne.joursDeRetard} j
        </span>
      )}
    </li>
  )
}

function Etiquette({ children }: { children: ReactNode }) {
  return (
    <div className="line-clamp-1 text-[28px] font-extrabold uppercase tracking-[0.12em]">{children}</div>
  )
}

/**
 * De combien le contenu dépasse le cadre, en pixels de l'appareil. Un `scrollHeight`
 * ne suffit pas : dans une grille en `overflow-hidden`, un bloc trop haut déborde
 * sans jamais agrandir la boîte — c'est exactement comme ça que le bas de l'écran
 * s'est fait couper deux fois en un mois. On mesure donc le plus bas des éléments.
 */
function debordementPx(cadre: HTMLElement, zoom: number): number {
  const bas = cadre.getBoundingClientRect().top + cadre.clientHeight
  let plusBas = bas
  for (const element of cadre.querySelectorAll('*')) {
    const rect = element.getBoundingClientRect()
    if (rect.height > 0 && rect.bottom > plusBas) {
      plusBas = rect.bottom
    }
  }
  return Math.round((plusBas - bas) * zoom)
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
