import { useEffect } from 'react'
import { Check, Sparkles, Undo2, X } from 'lucide-react'
import { paletteParNom } from '@/components/Avatar'
import {
  DUREE_TOAST_MS,
  effacerToast,
  useToasts,
  type AuteurToast,
  type ToastConfirmation as Toast,
  type TonToast,
} from '@/lib/toast'

/** Pile de toasts en bas d'écran : les confirmations de mes gestes (avec leur action
 * inverse — le filet du mauvais clic, issue #60) et les annonces poussées par la
 * synchro temps réel (informatives, sans action). Trois au plus, 6 s chacun.
 *
 * Forme : « la carte posée » — le toast est une carte de l'app en plus petit (même
 * crème, même rayon, même liseré gauche coloré que les rangées en retard), un
 * médaillon qui porte l'auteur, deux lignes pour dire quoi et sur quoi. Le compte à
 * rebours vit dans le bouton d'action inverse quand il y en a un : on voit la fenêtre
 * de rattrapage se refermer là où il faudra cliquer.
 * Voir vault/Decisions/D-2026-08-28 Toast Carte Posée. */
export default function ToastConfirmation() {
  const toasts = useToasts()

  if (toasts.length === 0) {
    return null
  }

  return (
    <div className="fixed inset-x-0 bottom-5 z-40 mx-auto flex w-fit max-w-[calc(100%-2.5rem)] flex-col items-center gap-2.5">
      {toasts.map((toast) => (
        <Ligne key={toast.id} toast={toast} />
      ))}
    </div>
  )
}

/** Teintes d'un ton : liseré gauche, médaillon, bouton. Pour une annonce distante,
 * la couleur est celle de l'avatar de l'auteur — la hiérarchie « moi / les autres »
 * se joue en teinte, sans second système. */
function teintes(ton: TonToast, auteur: AuteurToast | undefined) {
  if (ton === 'succes') {
    return {
      lisere: 'var(--vert)',
      fond: 'var(--toast-medaillon-vert)',
      texte: 'var(--vert)',
      jauge: 'var(--toast-jauge-vert)',
      boutonFond: 'var(--vert-fond)',
    }
  }
  if (ton === 'retour') {
    return {
      lisere: 'var(--orange)',
      fond: 'var(--toast-medaillon-orange)',
      texte: 'var(--orange)',
      jauge: 'var(--toast-jauge-orange)',
      boutonFond: 'var(--toast-bouton-orange-fond)',
    }
  }
  if (auteur?.estClaude === true) {
    return {
      lisere: 'var(--avatar-claude-fond)',
      fond: 'var(--avatar-claude-fond)',
      texte: 'var(--avatar-claude-texte)',
      jauge: 'transparent',
      boutonFond: 'transparent',
    }
  }
  const palette = paletteParNom(auteur?.nom)
  return {
    lisere: palette.fond,
    fond: palette.fond,
    texte: palette.texte,
    jauge: 'transparent',
    boutonFond: 'transparent',
  }
}

function Medaillon({ ton, auteur }: { ton: TonToast; auteur: AuteurToast | undefined }) {
  const { fond, texte } = teintes(ton, auteur)
  const style = { background: fond, color: texte }

  if (ton === 'succes') {
    return (
      <span className="flex size-[34px] shrink-0 items-center justify-center rounded-full" style={style}>
        <Check className="size-[17px]" strokeWidth={2.6} />
      </span>
    )
  }
  if (ton === 'retour') {
    return (
      <span className="flex size-[34px] shrink-0 items-center justify-center rounded-full" style={style}>
        <Undo2 className="size-[17px]" strokeWidth={2.2} />
      </span>
    )
  }
  return (
    <span
      className="flex size-[34px] shrink-0 items-center justify-center rounded-full text-xs font-extrabold"
      style={style}
      // L'étincelle est celle de l'agent, pas de la personne au nom de qui il agit :
      // celle-là est déjà nommée dans le message.
      title={auteur?.estClaude === true ? 'Claude' : (auteur?.nom ?? undefined)}
    >
      {auteur?.estClaude === true ? (
        <Sparkles className="size-[15px]" />
      ) : (
        auteur?.nom?.slice(0, 2) ?? '?'
      )}
    </span>
  )
}

function Ligne({ toast }: { toast: Toast }) {
  const ton = toast.ton ?? 'succes'
  const couleurs = teintes(ton, toast.auteur)
  const avecAction = typeof toast.actionLibelle === 'string'

  // arriveeMs dans les dépendances : une fusion réécrit le toast en place et doit
  // lui redonner ses 6 s complètes.
  useEffect(() => {
    const minuterie = setTimeout(() => effacerToast(toast.id), DUREE_TOAST_MS)
    return () => clearTimeout(minuterie)
  }, [toast.id, toast.arriveeMs])

  return (
    <div
      role="status"
      className="toast-entree relative flex min-w-[330px] max-w-[520px] items-center gap-3.5 overflow-hidden rounded-[20px] border-l-[6px] bg-carte py-3 pl-[18px] pr-4 shadow-carte-lg"
      style={{ borderLeftColor: couleurs.lisere }}
    >
      <Medaillon ton={ton} auteur={toast.auteur} />

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2 text-[14.5px] font-extrabold text-texte">
          <span className="min-w-0">{toast.message}</span>
          {(toast.nombre ?? 1) > 1 && (
            // key sur le nombre : le compteur repop à chaque fusion — c'est ce qui
            // dit « le toast s'est réécrit » plutôt que « en voici un nouveau ».
            <span
              key={toast.nombre}
              className="toast-compteur flex h-[22px] min-w-[22px] shrink-0 items-center justify-center rounded-full px-1.5 text-xs font-extrabold"
              style={{ background: couleurs.fond, color: couleurs.texte }}
            >
              {toast.nombre}
            </span>
          )}
        </div>
        {typeof toast.sousTitre === 'string' && (
          <div className="mt-px truncate text-xs text-sourdine">{toast.sousTitre}</div>
        )}
      </div>

      {avecAction && (
        <button
          type="button"
          onClick={() => {
            effacerToast(toast.id)
            toast.onAction?.()
          }}
          className="relative shrink-0 overflow-hidden rounded-full border-2 px-4 py-1.5 text-[13px] font-extrabold transition-[filter] hover:brightness-95"
          style={{
            borderColor: couleurs.texte,
            color: couleurs.texte,
            background: couleurs.boutonFond,
          }}
        >
          {/* La jauge se vide dans le bouton : le compte à rebours appartient à
              l'objet dont il conditionne la validité. key sur arriveeMs pour qu'une
              fusion la fasse repartir de plein. */}
          <span
            key={toast.arriveeMs}
            aria-hidden
            className="toast-jauge absolute inset-y-0 left-0"
            style={{ background: couleurs.jauge }}
          />
          <span className="relative">{toast.actionLibelle}</span>
        </button>
      )}

      <button
        type="button"
        aria-label="Fermer la confirmation"
        onClick={() => effacerToast(toast.id)}
        className="flex size-[26px] shrink-0 items-center justify-center rounded-full text-dore transition-colors hover:bg-barre-piste hover:text-texte"
      >
        <X className="size-4" />
      </button>

      {/* Sans bouton d'action, le minuteur reprend sa place au pied de la carte. */}
      {avecAction === false && (
        <span
          key={toast.arriveeMs}
          aria-hidden
          className="absolute inset-x-[18px] bottom-0 h-[3px] rounded-full bg-barre-piste"
        >
          <span
            className="toast-minuteur block h-full rounded-full opacity-60"
            style={{ background: couleurs.texte }}
          />
        </span>
      )}
    </div>
  )
}
