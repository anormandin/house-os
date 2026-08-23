import type { Utilisateur } from '@/lib/api'
import { cn } from '@/lib/utils'

// Avatars pastel du langage chaleureuse : Ariane violet, Alain bleu.
// Repli neutre si un jour un autre compte apparaît.
const palettes: Record<string, { fond: string; texte: string; barre: string }> = {
  ariane: { fond: 'var(--avatar-ariane-fond)', texte: 'var(--avatar-ariane-texte)', barre: '#b9a5d8' },
  alain: { fond: 'var(--avatar-alain-fond)', texte: 'var(--avatar-alain-texte)', barre: '#9db8dd' },
}

export function paletteAvatar(utilisateur: Utilisateur) {
  return (
    palettes[utilisateur.nomUtilisateur.toLowerCase()] ?? {
      fond: 'var(--creux)',
      texte: 'var(--dore)',
      barre: 'var(--jaune)',
    }
  )
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
