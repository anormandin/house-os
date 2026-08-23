import { useQueryClient } from '@tanstack/react-query'
import { CalendarDays, ListTodo, LogOut } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { api, type Utilisateur } from '@/lib/api'
import { cn } from '@/lib/utils'

const onglets = [
  { vers: '/', libelle: "Aujourd'hui", icone: CalendarDays },
  { vers: '/taches', libelle: 'Tâches', icone: ListTodo },
]

export default function Layout({ moi }: { moi: Utilisateur }) {
  const queryClient = useQueryClient()

  async function deconnecter() {
    await api.deconnexion()
    queryClient.setQueryData(['moi'], undefined)
    queryClient.clear()
  }

  return (
    <div className="mx-auto flex min-h-dvh max-w-lg flex-col">
      <header className="flex items-center justify-between px-4 pb-2 pt-4">
        <h1 className="text-lg font-semibold">Maison</h1>
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          {moi.nomAffichage}
          <Button variant="ghost" size="icon" aria-label="Se déconnecter" onClick={deconnecter}>
            <LogOut className="size-4" />
          </Button>
        </div>
      </header>

      <main className="flex-1 px-4 pb-24">
        <Outlet />
      </main>

      <nav className="fixed inset-x-0 bottom-0 border-t bg-background">
        <div className="mx-auto flex max-w-lg">
          {onglets.map(({ vers, libelle, icone: Icone }) => (
            <NavLink
              key={vers}
              to={vers}
              end={vers === '/'}
              className={({ isActive }) =>
                cn(
                  'flex flex-1 flex-col items-center gap-1 py-3 text-xs',
                  isActive ? 'text-primary' : 'text-muted-foreground',
                )
              }
            >
              <Icone className="size-5" />
              {libelle}
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  )
}
