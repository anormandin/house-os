import { Search, X } from 'lucide-react'
import { cn } from '@/lib/utils'

/** Le champ de recherche des pages de bureau : loupe, ✕ pour effacer, Échap vide
 * le champ. Le filtrage lui-même reste dans la page (voir `lib/recherche`). */
export default function ChampRecherche({
  valeur,
  onChange,
  libelle,
  className,
}: {
  valeur: string
  onChange: (valeur: string) => void
  /** Sert de placeholder et d'aria-label. */
  libelle: string
  className?: string
}) {
  return (
    <label
      className={cn(
        'flex items-center gap-2 rounded-full bg-carte py-1.5 pl-3.5 pr-2 shadow-carte focus-within:outline-2 focus-within:outline-orange/60',
        className,
      )}
    >
      <Search className="size-4 shrink-0 text-dore" />
      <input
        type="search"
        value={valeur}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Escape' && valeur.length > 0) {
            e.stopPropagation()
            onChange('')
          }
        }}
        placeholder={libelle}
        aria-label={libelle}
        className="min-w-0 flex-1 bg-transparent text-sm text-texte focus:outline-none [&::-webkit-search-cancel-button]:hidden"
      />
      {valeur.length > 0 && (
        <button
          type="button"
          aria-label="Effacer la recherche"
          onClick={() => onChange('')}
          className="grid size-5 shrink-0 place-items-center rounded-full text-sourdine hover:text-orange"
        >
          <X className="size-3.5" />
        </button>
      )}
    </label>
  )
}
