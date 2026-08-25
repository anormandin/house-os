// Titre d'humeur — couche 2 (banque de gabarits) du design en trois couches décrit
// dans vault/Features/Titre D'humeur/Titre D'humeur.md. Les faits sont calculés,
// jamais inventés ; la phrase tourne selon la date. Le polissage LLM (couche 3)
// vit côté serveur — cette banque est le repli permanent quand il est absent.

// Date propre à la phrase d'humeur (les cartes compte à rebours viennent de l'API).
// Se retire naturellement après le 6 octobre 2026.
export const DATE_DEMENAGEMENT = '2026-10-06'

export type FaitsDuJour = {
  ouvertes: number
  enRetard: number
  faites: number
  dodosDemenagement: number | null
}

type Phrase = { titre: string; sousTitre: string }

function choisir<T>(variantes: T[], graine: number): T {
  return variantes[graine % variantes.length]
}

function pluriel(n: number, mot: string): string {
  return n === 1 ? `1 ${mot}` : `${n} ${mot}s`
}

export function phraseDuJour(faits: FaitsDuJour, date = new Date()): Phrase {
  // Math.round, pas floor : le jour du passage à l'heure avancée, la journée locale
  // fait 23 h et floor donnerait la même graine deux jours de suite.
  const graine =
    date.getFullYear() * 366 +
    Math.round((date.getTime() - new Date(date.getFullYear(), 0, 0).getTime()) / 86_400_000)
  const demenagementProche =
    faits.dodosDemenagement !== null && faits.dodosDemenagement > 0 && faits.dodosDemenagement <= 60

  const titresDemenagement = ['On y est presque.', 'Bientôt chez nous.', 'Le compte à rebours est parti.']
  const titresCalmes = ['La maison respire.', 'Tout doux aujourd’hui.', 'Belle journée pour flâner.']
  const titresActifs = ['On avance, tranquillement.', 'Une chose à la fois.', 'La maison s’occupe de nous.']

  if (faits.ouvertes === 0 && faits.faites > 0) {
    return {
      titre: choisir(demenagementProche ? titresDemenagement : titresCalmes, graine),
      sousTitre: choisir(
        [
          `Tout est fait — ${pluriel(faits.faites, 'chose')} de réglée${faits.faites > 1 ? 's' : ''} aujourd’hui. Bravo l’équipe.`,
          'Rien ne reste sur la liste. Profitez de la soirée.',
        ],
        graine,
      ),
    }
  }

  if (faits.ouvertes === 0) {
    return {
      titre: choisir(demenagementProche ? titresDemenagement : titresCalmes, graine),
      sousTitre: choisir(
        ['Rien au programme aujourd’hui.', 'Journée libre — la maison ne demande rien.'],
        graine,
      ),
    }
  }

  const titre = choisir(demenagementProche ? titresDemenagement : titresActifs, graine)

  if (faits.enRetard > 0) {
    const retard =
      faits.enRetard === 1 ? 'une attend depuis hier' : `${faits.enRetard} attendent depuis un moment`
    return {
      titre,
      sousTitre: `${pluriel(faits.ouvertes, 'petite chose')} aujourd’hui — ${retard}, le reste est sous contrôle.`,
    }
  }

  return {
    titre,
    sousTitre: choisir(
      [
        `${pluriel(faits.ouvertes, 'petite chose')} aujourd’hui — rien ne presse.`,
        `${pluriel(faits.ouvertes, 'chose')} au programme, à votre rythme.`,
      ],
      graine,
    ),
  }
}
