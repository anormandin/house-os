/** Recherche insensible aux accents et à la casse : « corvee » trouve « corvée »,
 * « deneigement » trouve « Facture déneigement ». */
export function normaliser(texte: string): string {
  return texte
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
}

/** Vrai si chaque mot du terme se trouve dans au moins un des champs : l'ordre
 * des mots ne compte pas (« jura manuel » trouve « Manuel — machine Jura »). Un
 * terme vide correspond à tout. */
export function correspond(terme: string, champs: (string | null | undefined)[]): boolean {
  const mots = normaliser(terme).split(/\s+/).filter((mot) => mot.length > 0)
  if (mots.length === 0) {
    return true
  }
  const botte = champs
    .filter((champ): champ is string => typeof champ === 'string')
    .map(normaliser)
    .join('\n')
  return mots.every((mot) => botte.includes(mot))
}
