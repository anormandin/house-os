import { useQuery, useQueryClient } from '@tanstack/react-query'
import { LogOut } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import Avatar from '@/components/Avatar'
import { api, type Utilisateur } from '@/lib/api'
import { cn } from '@/lib/utils'

// Pièces et Équipements arrivent avec la V1 — on n'affiche pas d'onglets morts.
const onglets = [
  { vers: '/', libelle: "Aujourd'hui" },
  { vers: '/taches', libelle: 'Tâches' },
]

export default function Layout({ moi }: { moi: Utilisateur }) {
  const queryClient = useQueryClient()
  const { data: utilisateurs } = useQuery({
    queryKey: ['utilisateurs'],
    queryFn: api.utilisateurs,
  })

  async function deconnecter() {
    await api.deconnexion()
    queryClient.setQueryData(['moi'], undefined)
    queryClient.clear()
  }

  return (
    <div className="mx-auto flex min-h-dvh w-full max-w-[1500px] flex-col px-6 lg:px-14">
      <header className="flex items-center justify-between py-6">
        <span className="font-titre text-[26px] font-bold text-encre">Maison</span>
        <nav className="flex gap-7 text-[15px] font-bold">
          {onglets.map(({ vers, libelle }) => (
            <NavLink
              key={vers}
              to={vers}
              end={vers === '/'}
              className={({ isActive }) =>
                cn(
                  'pb-1 transition-colors',
                  isActive
                    ? 'border-b-[3px] border-orange text-orange'
                    : 'text-sourdine hover:text-dore',
                )
              }
            >
              {libelle}
            </NavLink>
          ))}
        </nav>
        <div className="flex items-center gap-3">
          <div className="flex">
            {(utilisateurs ?? [moi]).map((u, i) => (
              <Avatar
                key={u.id}
                utilisateur={u}
                taille={36}
                className={cn('border-2 border-fond', i > 0 && '-ml-3')}
              />
            ))}
          </div>
          <button
            type="button"
            aria-label="Se déconnecter"
            onClick={deconnecter}
            className="text-sourdine transition-colors hover:text-orange"
          >
            <LogOut className="size-4" />
          </button>
        </div>
      </header>

      <main className="flex-1 pb-10">
        <Outlet />
      </main>
    </div>
  )
}
