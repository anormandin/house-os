import type { Utilisateur } from '@/lib/api'
import { cn } from '@/lib/utils'

// Avatars pastel du langage chaleureuse : une teinte par membre du foyer, attribuée
// par son rang dans la liste des comptes (ordre de l'API, par nom d'affichage) —
// jamais par prénom, pour qu'une autre maison ait les mêmes couleurs sans rien
// configurer. Repli neutre tant que la liste n'est pas connue ou pour un inconnu.
const palettes = [
  { fond: 'var(--avatar-1-fond)', texte: 'var(--avatar-1-texte)', barre: '#9db8dd' }, // bleu
  { fond: 'var(--avatar-2-fond)', texte: 'var(--avatar-2-texte)', barre: '#b9a5d8' }, // violet
]

const paletteNeutre = { fond: 'var(--creux)', texte: 'var(--dore)', barre: 'var(--jaune)' }

// Rang de chaque membre, indexé par nom d'utilisateur ET nom d'affichage (minuscules) :
// la synchro ne transmet que le nom d'affichage de l'acteur, les occurrences portent
// l'utilisateur complet. Alimenté par Layout dès que la liste du foyer est chargée.
let rangs = new Map<string, number>()

export function enregistrerFoyer(utilisateurs: Utilisateur[]) {
  rangs = new Map()
  utilisateurs.forEach((u, i) => {
    rangs.set(u.nomUtilisateur.toLowerCase(), i)
    rangs.set(u.nomAffichage.toLowerCase(), i)
  })
}

/** Palette à partir d'un nom seul (nom d'utilisateur ou d'affichage). */
export function paletteParNom(nom: string | null | undefined) {
  const rang = rangs.get((nom ?? '').toLowerCase())
  return rang === undefined ? paletteNeutre : palettes[rang % palettes.length]
}

export function paletteAvatar(utilisateur: Utilisateur) {
  return paletteParNom(utilisateur.nomUtilisateur)
}

export function initiales(utilisateur: Utilisateur): string {
  return utilisateur.nomAffichage.slice(0, 2)
}

export default function Avatar({
  utilisateur,
  taille = 32,
  className,
}: {
  utilisateur: Utilisateur
  taille?: number
  className?: string
}) {
  const palette = paletteAvatar(utilisateur)
  return (
    <span
      title={utilisateur.nomAffichage}
      className={cn('flex shrink-0 items-center justify-center rounded-full font-bold', className)}
      style={{
        width: taille,
        height: taille,
        fontSize: Math.round(taille * 0.38),
        background: palette.fond,
        color: palette.texte,
      }}
    >
      {initiales(utilisateur)}
    </span>
  )
}
