import { useEffect, useState } from 'react'
import { Plus } from 'lucide-react'
import TacheEditeur from '@/components/TacheEditeur'

export default function QuickAdd() {
  const [ouvert, setOuvert] = useState(false)

  useEffect(() => {
    function surTouche(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        setOuvert(true)
      }
    }
    window.addEventListener('keydown', surTouche)
    return () => window.removeEventListener('keydown', surTouche)
  }, [])

  return (
    <>
      <button
        type="button"
        onClick={() => setOuvert(true)}
        className="flex w-full items-center gap-3 rounded-[20px] border-2 border-dashed border-tiret px-5 py-3 text-sm font-bold text-tiret-texte transition-colors hover:border-orange/50 hover:text-dore"
      >
        <Plus className="size-4" />
        Ajouter une tâche… <span className="ml-auto text-xs">⌘K</span>
      </button>
      {ouvert && <TacheEditeur tacheId={null} onFermer={() => setOuvert(false)} />}
    </>
  )
}
