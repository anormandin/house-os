import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarDays, Check, Copy, LogOut } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import Avatar from '@/components/Avatar'
import { api, type Utilisateur } from '@/lib/api'
import { cn } from '@/lib/utils'

const onglets = [
  { vers: '/', libelle: "Aujourd'hui" },
  { vers: '/pieces', libelle: 'Pièces' },
  { vers: '/taches', libelle: 'Tâches' },
  { vers: '/equipements', libelle: 'Équipements' },
]

export default function Layout({ moi }: { moi: Utilisateur }) {
  const queryClient = useQueryClient()
  const [calendrierOuvert, setCalendrierOuvert] = useState(false)
  const [copie, setCopie] = useState(false)

  const { data: utilisateurs } = useQuery({
    queryKey: ['utilisateurs'],
    queryFn: api.utilisateurs,
  })
  const { data: monFlux } = useQuery({
    queryKey: ['mon-flux-ical'],
    queryFn: api.monFluxIcal,
    enabled: calendrierOuvert,
  })

  const urlFlux = monFlux === undefined ? null : new URL(monFlux.chemin, window.location.origin).href

  async function copier() {
    if (urlFlux === null) {
      return
    }
    await navigator.clipboard.writeText(urlFlux)
    setCopie(true)
    setTimeout(() => setCopie(false), 2000)
  }

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
          <button
            type="button"
            aria-label="Mon calendrier iCal"
            onClick={() => setCalendrierOuvert(true)}
            className="text-sourdine transition-colors hover:text-orange"
          >
            <CalendarDays className="size-4" />
          </button>
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

      {calendrierOuvert && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4"
          onClick={() => setCalendrierOuvert(false)}
        >
          <div
            className="flex w-full max-w-lg flex-col gap-3 rounded-[28px] bg-carte p-7 shadow-carte-lg"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="text-2xl font-bold">Mon calendrier</h2>
            <p className="text-sm text-texte">
              Abonne ton téléphone à ce calendrier (lecture seule) : tes tâches à
              échéance — et celles que personne n’a prises — apparaîtront dans ton
              app de calendrier. L’URL est secrète, garde-la pour toi.
            </p>
            <div className="flex items-center gap-2 rounded-xl bg-creux px-3 py-2.5">
              <span className="min-w-0 flex-1 truncate text-sm text-dore">
                {urlFlux ?? 'Chargement…'}
              </span>
              <button
                type="button"
                onClick={copier}
                disabled={urlFlux === null}
                aria-label="Copier l'URL"
                className="flex items-center gap-1.5 rounded-full bg-orange px-3 py-1.5 text-xs font-bold text-carte disabled:opacity-40"
              >
                {copie ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
                {copie ? 'Copiée!' : 'Copier'}
              </button>
            </div>
            <p className="text-xs text-sourdine">
              iPhone : Réglages → Calendrier → Comptes → Ajouter un abonnement.
              Google Agenda : Autres agendas → Importer par URL.
            </p>
          </div>
        </div>
      )}
    </div>
  )
}
