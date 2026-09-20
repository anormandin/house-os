import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  CalendarDays,
  FileText,
  House,
  ListChecks,
  LogOut,
  Menu,
  Sun,
  Wallet,
  Wrench,
  X,
} from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import Avatar, { enregistrerFoyer } from '@/components/Avatar'
import DialogueCalendrier, { useDeconnexion } from '@/components/DialogueCalendrier'
import { api, dateLocaleIso, type Utilisateur } from '@/lib/api'
import { cn } from '@/lib/utils'

const destinations = [
  { vers: '/', libelle: "Aujourd'hui", Icone: Sun },
  { vers: '/taches', libelle: 'Tâches', Icone: ListChecks },
  { vers: '/pieces', libelle: 'Pièces', Icone: House },
  { vers: '/equipements', libelle: 'Équipements', Icone: Wrench },
  { vers: '/documents', libelle: 'Documents', Icone: FileText },
  { vers: '/budget', libelle: 'Budget', Icone: Wallet },
]

/** La coquille de la vue téléphone : barre du haut + menu déroulant, pas de barre
 * d'onglets du bas (direction « La pile », retenue 2026-09-19). Le menu porte la
 * pastille « en retard », ce qu'une barre d'onglets ne peut pas faire. */
export default function CoquilleTelephone({ moi }: { moi: Utilisateur }) {
  const [menuOuvert, setMenuOuvert] = useState(false)
  const [calendrierOuvert, setCalendrierOuvert] = useState(false)
  const deconnecter = useDeconnexion()

  const { data: utilisateurs } = useQuery({
    queryKey: ['utilisateurs'],
    queryFn: api.utilisateurs,
  })
  useEffect(() => {
    if (utilisateurs !== undefined) {
      enregistrerFoyer(utilisateurs)
    }
  }, [utilisateurs])

  const { data: enAttente } = useQuery({
    queryKey: ['occurrences', 'en-attente'],
    queryFn: () => api.occurrences('en-attente'),
  })
  const aujourdhui = dateLocaleIso()
  const enRetard =
    enAttente?.filter((o) => o.echeance !== null && o.echeance < aujourdhui).length ?? 0

  return (
    <div className="flex min-h-dvh flex-col bg-fond">
      <header className="sticky top-0 z-30 flex items-center gap-2 bg-fond/95 px-3 py-2 backdrop-blur">
        <button
          type="button"
          aria-label={menuOuvert ? 'Fermer le menu des sections' : 'Ouvrir le menu des sections'}
          aria-expanded={menuOuvert}
          onClick={() => setMenuOuvert((v) => !v)}
          className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-carte text-encre shadow-carte"
        >
          {menuOuvert ? <X className="size-5" /> : <Menu className="size-5" />}
        </button>
        <span className="font-titre text-[22px] font-bold text-encre">Maison</span>
        <div className="ml-auto flex items-center">
          {(utilisateurs ?? [moi]).map((u, i) => (
            <Avatar
              key={u.id}
              utilisateur={u}
              taille={30}
              className={cn('border-2 border-fond', i > 0 && '-ml-2.5')}
            />
          ))}
        </div>
      </header>

      {menuOuvert && (
        <>
          <button
            type="button"
            aria-label="Fermer le menu des sections"
            onClick={() => setMenuOuvert(false)}
            className="fixed inset-0 z-30 cursor-default bg-encre/35"
          />
          <nav
            aria-label="Sections"
            className="fixed top-14 left-3 z-40 flex w-[266px] flex-col rounded-2xl bg-carte py-1.5 shadow-carte-lg"
          >
            {destinations.map(({ vers, libelle, Icone }) => (
              <NavLink
                key={vers}
                to={vers}
                end={vers === '/'}
                // Le menu se referme sur le choix lui-même : un effet sur la route
                // relancerait un rendu pour un état qu'on connaît déjà ici.
                onClick={() => setMenuOuvert(false)}
                className={({ isActive }) =>
                  cn(
                    'flex min-h-[52px] items-center gap-3 px-4 text-[17px] font-bold',
                    isActive ? 'text-encre' : 'text-texte',
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    <Icone className={cn('size-5 shrink-0', isActive ? 'text-orange' : 'text-dore')} />
                    <span className="flex-1">{libelle}</span>
                    {vers === '/taches' && enRetard > 0 && (
                      <span className="rounded-full bg-rouge px-2 py-0.5 text-[13px] font-extrabold text-carte">
                        {enRetard}
                      </span>
                    )}
                  </>
                )}
              </NavLink>
            ))}
            <div className="mt-1 flex items-center gap-1 border-t border-creux px-2 pt-1">
              <button
                type="button"
                onClick={() => {
                  setMenuOuvert(false)
                  setCalendrierOuvert(true)
                }}
                className="flex min-h-[48px] flex-1 items-center gap-3 rounded-xl px-2 text-[15px] font-bold text-texte"
              >
                <CalendarDays className="size-5 shrink-0 text-dore" />
                Mon calendrier
              </button>
              <button
                type="button"
                aria-label="Se déconnecter"
                onClick={deconnecter}
                className="flex size-11 items-center justify-center rounded-xl text-dore"
              >
                <LogOut className="size-5" />
              </button>
            </div>
          </nav>
        </>
      )}

      <main className="flex-1 px-4 pb-[calc(env(safe-area-inset-bottom)+20px)]">
        <Outlet />
      </main>

      {calendrierOuvert && <DialogueCalendrier onFermer={() => setCalendrierOuvert(false)} />}
    </div>
  )
}
