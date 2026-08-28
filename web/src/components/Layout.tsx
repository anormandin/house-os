import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarDays, Check, Copy, LogOut, RefreshCw } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import Avatar from '@/components/Avatar'
import { api, ApiError, type Utilisateur } from '@/lib/api'
import { signalerErreur } from '@/lib/erreurs'
import { cn, copierDansPressePapiers } from '@/lib/utils'

const onglets = [
  { vers: '/', libelle: "Aujourd'hui" },
  { vers: '/pieces', libelle: 'Pièces' },
  { vers: '/taches', libelle: 'Tâches' },
  { vers: '/equipements', libelle: 'Équipements' },
  { vers: '/documents', libelle: 'Documents' },
  { vers: '/budget', libelle: 'Budget' },
]

export default function Layout({ moi }: { moi: Utilisateur }) {
  const queryClient = useQueryClient()
  const [calendrierOuvert, setCalendrierOuvert] = useState(false)
  const [copie, setCopie] = useState<string | null>(null)
  const [confirmerRotation, setConfirmerRotation] = useState(false)

  const { data: utilisateurs } = useQuery({
    queryKey: ['utilisateurs'],
    queryFn: api.utilisateurs,
  })
  const { data: monFlux } = useQuery({
    queryKey: ['mon-flux-ical'],
    queryFn: api.monFluxIcal,
    enabled: calendrierOuvert,
  })

  // L'interne (même origine) reste la plus fiable pour les appareils sur
  // Tailscale ; la publique (Funnel) est la seule que Google Agenda peut lire.
  const flux =
    monFlux === undefined
      ? []
      : [
          ...(monFlux.urlPublique === null
            ? []
            : [
                {
                  cle: 'publique',
                  libelle: 'Pour Google Agenda (publique)',
                  url: monFlux.urlPublique,
                },
              ]),
          {
            cle: 'interne',
            libelle: 'Sur Tailscale (interne)',
            url: new URL(monFlux.chemin, window.location.origin).href,
          },
        ]

  const rotation = useMutation({
    mutationFn: api.rotationFluxIcal,
    onSuccess: (nouveau) => {
      queryClient.setQueryData(['mon-flux-ical'], nouveau)
      setConfirmerRotation(false)
    },
  })

  async function copier(cle: string, url: string) {
    const reussi = await copierDansPressePapiers(url)
    if (reussi === false) {
      signalerErreur('Impossible de copier automatiquement — sélectionne l’URL et copie-la à la main.')
      return
    }
    setCopie(cle)
    setTimeout(() => setCopie(null), 2000)
  }

  function fermerCalendrier() {
    setCalendrierOuvert(false)
    setConfirmerRotation(false)
  }

  async function deconnecter() {
    try {
      await api.deconnexion()
    } catch (erreur) {
      // Une session déjà expirée (401) est une déconnexion réussie.
      const dejaExpiree = erreur instanceof ApiError && erreur.statut === 401
      if (dejaExpiree === false) {
        signalerErreur('La déconnexion a échoué — réessaie.')
        return
      }
    }
    // clear() seul ne notifie pas l'observateur actif de `moi` (v5) : l'app
    // garderait son instantané. Le reset le notifie (moi → undefined → écran de
    // connexion) ; le clear() qui suit purge les données de l'utilisateur.
    await queryClient.resetQueries({ queryKey: ['moi'], exact: true })
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
          onClick={fermerCalendrier}
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
            {flux.length === 0 && (
              <div className="rounded-xl bg-creux px-3 py-2.5 text-sm text-dore">Chargement…</div>
            )}
            {flux.map(({ cle, libelle, url }) => (
              <div key={cle} className="flex flex-col gap-1">
                <span className="text-xs font-bold text-sourdine">{libelle}</span>
                <div className="flex items-center gap-2 rounded-xl bg-creux px-3 py-2.5">
                  <span className="min-w-0 flex-1 truncate text-sm text-dore">{url}</span>
                  <button
                    type="button"
                    onClick={() => copier(cle, url)}
                    aria-label={`Copier l'URL (${libelle})`}
                    className="flex items-center gap-1.5 rounded-full bg-orange px-3 py-1.5 text-xs font-bold text-carte"
                  >
                    {copie === cle ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
                    {copie === cle ? 'Copiée!' : 'Copier'}
                  </button>
                </div>
              </div>
            ))}
            <p className="text-xs text-sourdine">
              iPhone : Réglages → Calendrier → Comptes → Ajouter un abonnement.
              Google Agenda : Autres agendas → S’abonner par URL (utiliser la publique).
            </p>
            <div className="flex items-center justify-between gap-3 border-t border-creux pt-3">
              <span className="text-xs text-sourdine">
                {confirmerRotation
                  ? 'Les abonnements existants casseront — il faudra se réabonner.'
                  : 'URL fuitée? Régénère le jeton pour révoquer l’ancienne.'}
              </span>
              <button
                type="button"
                onClick={() =>
                  confirmerRotation ? rotation.mutate() : setConfirmerRotation(true)
                }
                disabled={flux.length === 0 || rotation.isPending}
                className={cn(
                  'flex shrink-0 items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-bold disabled:opacity-40',
                  confirmerRotation ? 'bg-orange text-carte' : 'bg-creux text-texte',
                )}
              >
                <RefreshCw className={cn('size-3.5', rotation.isPending && 'animate-spin')} />
                {confirmerRotation ? 'Confirmer la rotation' : 'Régénérer'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
