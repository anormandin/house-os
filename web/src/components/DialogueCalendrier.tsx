import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Copy, RefreshCw } from 'lucide-react'
import { api, ApiError } from '@/lib/api'
import { signalerErreur } from '@/lib/erreurs'
import { cn, copierDansPressePapiers } from '@/lib/utils'

/** Le dialogue « Mon calendrier », partagé par la coquille de bureau et celle du
 * téléphone : même contenu, même rotation de jeton, un seul endroit à corriger.
 * Monté seulement quand il est ouvert — la requête part donc à l'ouverture. */
export default function DialogueCalendrier({ onFermer }: { onFermer: () => void }) {
  const queryClient = useQueryClient()
  const [copie, setCopie] = useState<string | null>(null)
  const [confirmerRotation, setConfirmerRotation] = useState(false)

  const { data: monFlux } = useQuery({
    queryKey: ['mon-flux-ical'],
    queryFn: api.monFluxIcal,
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

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4"
      onClick={onFermer}
    >
      <div
        className="flex max-h-[92dvh] w-full max-w-lg flex-col gap-3 overflow-y-auto rounded-[28px] bg-carte p-7 shadow-carte-lg"
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
            onClick={() => (confirmerRotation ? rotation.mutate() : setConfirmerRotation(true))}
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
  )
}

/** La déconnexion, partagée par les deux coquilles. */
export function useDeconnexion() {
  const queryClient = useQueryClient()
  return async function deconnecter() {
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
}
