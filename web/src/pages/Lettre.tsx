import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Mail, PenLine } from 'lucide-react'
import ErreurChargement from '@/components/ErreurChargement'
import { api, type LettreDuMatin } from '@/lib/api'
import { heureQuebec } from '@/lib/format'

/**
 * La lettre du matin, rendue comme le courriel qui part (vault : Lettre Du Matin).
 * Pas d'archive : la boîte de réception l'est déjà. Deux gestes — la faire réécrire
 * par la maison, et se l'envoyer à soi pour voir ce que ça donne dans un vrai client.
 */
export default function Lettre() {
  const queryClient = useQueryClient()
  const { data, isError, refetch } = useQuery({ queryKey: ['lettre'], queryFn: () => api.lettre() })

  const reecrire = useMutation({
    mutationFn: () => api.regenererLettre(false),
    onSuccess: (lettre) => queryClient.setQueryData(['lettre'], lettre),
  })
  const essai = useMutation({ mutationFn: () => api.envoyerEssaiLettre() })

  if (isError) {
    return <ErreurChargement quoi="la lettre du matin" onReessayer={() => refetch()} />
  }
  if (!data) {
    return <p className="text-sourdine">La maison relit sa lettre…</p>
  }

  return (
    <div className="mx-auto max-w-[680px]">
      <div className="mb-5 flex items-start justify-between gap-6">
        <div>
          <h1 className="font-titre text-[28px] font-bold text-encre">La lettre du matin</h1>
          <p className="mt-1 text-[14px] text-sourdine">{etat(data)}</p>
        </div>
        <div className="flex shrink-0 gap-2">
          <button
            type="button"
            onClick={() => reecrire.mutate()}
            disabled={reecrire.isPending}
            className="flex items-center gap-2 rounded-full border border-orange px-4 py-2 text-[14px] font-bold text-orange transition-colors hover:bg-orange hover:text-white disabled:opacity-60"
          >
            <PenLine className="size-4" />
            {reecrire.isPending ? 'La maison écrit…' : 'Réécrire'}
          </button>
          <button
            type="button"
            onClick={() => essai.mutate()}
            disabled={essai.isPending || !data.envoiActif}
            title={data.envoiActif ? undefined : "L'envoi de courriel n'est pas configuré"}
            className="flex items-center gap-2 rounded-full border border-sourdine px-4 py-2 text-[14px] font-bold text-encre transition-colors hover:border-orange hover:text-orange disabled:opacity-60"
          >
            <Mail className="size-4" />
            {essai.isPending ? 'Envoi…' : "M'envoyer un essai"}
          </button>
        </div>
      </div>
      {essai.isSuccess && (
        <p className="mb-4 text-[14px] text-dore" role="status">
          Essai envoyé à {essai.data.envoyeA}.
        </p>
      )}
      {essai.isError && (
        <p className="mb-4 text-[14px] text-orange" role="alert">
          {essai.error instanceof Error ? essai.error.message : "L'essai n'est pas parti."}
        </p>
      )}
      <article
        aria-label="La lettre telle qu'elle part"
        className="rounded-[14px] bg-[#faf7ed] px-8 py-7 text-[16.5px] leading-[1.62] text-[#545464] shadow-sm"
      >
        <p className="mb-4 font-titre text-[15px] text-[#43436c]">{data.sujet}</p>
        {data.texte
          .trimEnd()
          .split('\n\n')
          .map((bloc, i) => (
            <p key={i} className="mb-4 whitespace-pre-line last:mb-0">
              {bloc}
            </p>
          ))}
      </article>
    </div>
  )
}

function etat(lettre: LettreDuMatin): string {
  if (!lettre.ecrite) {
    return 'Aperçu en gabarit : la maison ne l’a pas encore écrite aujourd’hui.'
  }
  if (lettre.envoyeeLe) {
    return `Envoyée à ${heureQuebec(lettre.envoyeeLe)} à ${lettre.destinataires.join(', ')}.`
  }
  if (!lettre.envoiActif) {
    return 'Composée, pas envoyée : aucun serveur de courriel configuré.'
  }
  if (lettre.destinatairesPrevus.length === 0) {
    return 'Composée, pas envoyée : aucun compte n’a d’adresse.'
  }
  return `Composée à ${heureQuebec(lettre.composeeLe)}, pas encore partie.`
}
