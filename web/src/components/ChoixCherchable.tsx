import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { ChevronDown, Search } from 'lucide-react'
import { correspond } from '@/lib/recherche'
import { cn } from '@/lib/utils'

export type OptionCherchable = {
  valeur: string
  libelle: string
  /** En-tête de section (options consécutives du même groupe). */
  groupe?: string
  /** Texte secondaire affiché en sourdine, cherché lui aussi (dossier, pièce…). */
  detail?: string | null
}

/** Un `<select>` qu'on peut filtrer au clavier, pour les listes qui ont dépassé
 * ce qu'on parcourt à l'œil (≈ 40 documents, 100 tâches). Le bouton porte
 * l'aria-label ; ouvert, un champ filtre les options (sans accents, mots dans
 * n'importe quel ordre) ; ↑/↓ + Entrée choisissent, Échap referme sans fermer le
 * modal parent. */
export default function ChoixCherchable({
  options,
  valeur,
  onChoisir,
  libelle,
  texteVide,
  optionVide = false,
  className,
}: {
  options: OptionCherchable[]
  valeur: string
  onChoisir: (valeur: string) => void
  /** aria-label du bouton et du champ de filtre. */
  libelle: string
  /** Texte du bouton quand rien n'est choisi (`valeur` vide). */
  texteVide: string
  /** Propose `texteVide` comme première option, pour revenir à « aucun ». */
  optionVide?: boolean
  className?: string
}) {
  const [ouvert, setOuvert] = useState(false)
  const [terme, setTerme] = useState('')
  const [actif, setActif] = useState(0)
  const racine = useRef<HTMLDivElement>(null)
  const liste = useRef<HTMLUListElement>(null)
  const idListe = useId()

  const choisie = options.find((o) => o.valeur === valeur)
  const filtrees = [
    ...(optionVide && terme.trim() === '' ? [{ valeur: '', libelle: texteVide }] : []),
    ...options.filter((o) => correspond(terme, [o.libelle, o.detail, o.groupe])),
  ]
  const actifBorne = Math.min(actif, Math.max(filtrees.length - 1, 0))

  // Un clic hors du composant referme la liste.
  useEffect(() => {
    if (ouvert === false) {
      return
    }
    const surClic = (e: MouseEvent) => {
      if (racine.current?.contains(e.target as Node) === false) {
        setOuvert(false)
      }
    }
    document.addEventListener('mousedown', surClic)
    return () => document.removeEventListener('mousedown', surClic)
  }, [ouvert])

  useEffect(() => {
    liste.current
      ?.querySelector(`[data-index="${actifBorne}"]`)
      ?.scrollIntoView?.({ block: 'nearest' })
  }, [actifBorne])

  function ouvrir() {
    setTerme('')
    setActif(0)
    setOuvert(true)
  }

  function choisir(option: OptionCherchable) {
    onChoisir(option.valeur)
    setOuvert(false)
  }

  function surTouche(e: KeyboardEvent) {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActif(Math.min(actifBorne + 1, filtrees.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActif(Math.max(actifBorne - 1, 0))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (filtrees[actifBorne] !== undefined) {
        choisir(filtrees[actifBorne])
      }
    } else if (e.key === 'Escape') {
      // Sans ceci, Échap fermerait aussi le modal qui contient le choix.
      e.stopPropagation()
      setOuvert(false)
    } else if (e.key === 'Tab') {
      setOuvert(false)
    }
  }

  return (
    <div ref={racine} className={cn('relative', className)}>
      <button
        type="button"
        aria-label={libelle}
        aria-haspopup="listbox"
        aria-expanded={ouvert}
        onClick={() => (ouvert ? setOuvert(false) : ouvrir())}
        className={cn(
          'flex w-full items-center gap-2 rounded-xl bg-creux px-3 py-2 text-left text-sm focus:outline-2 focus:outline-orange/60',
          choisie === undefined ? 'text-sourdine' : 'text-texte',
        )}
      >
        <span className="min-w-0 flex-1 truncate">{choisie?.libelle ?? texteVide}</span>
        <ChevronDown className="size-4 shrink-0 text-sourdine" />
      </button>

      {ouvert && (
        <div className="absolute left-0 right-0 z-20 mt-1 min-w-64 overflow-hidden rounded-2xl bg-carte shadow-carte-lg">
          <label className="flex items-center gap-2 border-b border-dashed border-tiret px-3 py-2">
            <Search className="size-4 shrink-0 text-dore" />
            <input
              autoFocus
              role="combobox"
              aria-label={libelle}
              aria-expanded
              aria-controls={idListe}
              aria-activedescendant={filtrees.length > 0 ? `${idListe}-${actifBorne}` : undefined}
              value={terme}
              onChange={(e) => {
                setTerme(e.target.value)
                setActif(0)
              }}
              onKeyDown={surTouche}
              placeholder="Filtrer…"
              className="min-w-0 flex-1 bg-transparent text-sm text-texte focus:outline-none"
            />
          </label>
          <ul ref={liste} id={idListe} role="listbox" className="max-h-72 overflow-y-auto py-1">
            {filtrees.length === 0 && (
              <li className="px-3 py-2 text-sm text-sourdine">Rien ne correspond.</li>
            )}
            {filtrees.map((option, i) => (
              <li key={option.valeur || 'vide'} role="presentation">
                {option.groupe !== undefined && option.groupe !== filtrees[i - 1]?.groupe && (
                  <div className="px-3 pb-0.5 pt-2 text-[11px] font-extrabold uppercase tracking-[0.08em] text-sourdine">
                    {option.groupe}
                  </div>
                )}
                <div
                  id={`${idListe}-${i}`}
                  data-index={i}
                  role="option"
                  aria-selected={option.valeur === valeur}
                  onMouseEnter={() => setActif(i)}
                  onMouseDown={(e) => e.preventDefault()}
                  onClick={() => choisir(option)}
                  className={cn(
                    'flex cursor-pointer items-baseline gap-2 px-3 py-1.5 text-sm',
                    i === actifBorne && 'bg-creux',
                    option.valeur === valeur && 'font-bold text-orange',
                  )}
                >
                  <span className="min-w-0 truncate" title={option.libelle}>{option.libelle}</span>
                  {option.detail && (
                    <span className="ml-auto shrink-0 text-xs text-sourdine">{option.detail}</span>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
