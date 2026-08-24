import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { MaisonSoleil } from '@/components/Illustrations'
import { api, ApiError } from '@/lib/api'

export default function Connexion() {
  const queryClient = useQueryClient()
  const [nomUtilisateur, setNomUtilisateur] = useState('')
  const [motDePasse, setMotDePasse] = useState('')
  const [erreur, setErreur] = useState<string | null>(null)
  const [enCours, setEnCours] = useState(false)

  async function soumettre(e: React.SyntheticEvent<HTMLFormElement, SubmitEvent>) {
    e.preventDefault()
    setErreur(null)
    setEnCours(true)
    try {
      const moi = await api.connexion(nomUtilisateur, motDePasse)
      queryClient.setQueryData(['moi'], moi)
    } catch (e) {
      if (e instanceof ApiError) {
        setErreur(e.statut === 401 ? 'Identifiants invalides.' : e.message)
      } else {
        setErreur('Serveur injoignable — vérifie que l’API tourne.')
      }
    } finally {
      setEnCours(false)
    }
  }

  return (
    <main className="flex min-h-dvh items-center justify-center bg-fond p-4">
      <div className="flex w-full max-w-md flex-col items-center gap-2 rounded-[28px] bg-carte px-10 py-10 shadow-carte-lg">
        <MaisonSoleil largeur={220} />
        <h1 className="text-4xl font-bold">Maison</h1>
        <p className="mb-4 text-sm text-dore">Bienvenue chez nous.</p>
        <form onSubmit={soumettre} className="flex w-full flex-col gap-4">
          <label className="flex flex-col gap-1.5 text-sm font-bold text-dore">
            Qui es-tu?
            <input
              autoComplete="username"
              autoCapitalize="none"
              value={nomUtilisateur}
              onChange={(e) => setNomUtilisateur(e.target.value)}
              placeholder="alain ou ariane"
              className="rounded-xl bg-creux px-4 py-2.5 text-base font-bold text-texte placeholder:font-normal placeholder:text-sourdine focus:outline-2 focus:outline-orange/60"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-bold text-dore">
            Mot de passe
            <input
              type="password"
              autoComplete="current-password"
              value={motDePasse}
              onChange={(e) => setMotDePasse(e.target.value)}
              className="rounded-xl bg-creux px-4 py-2.5 text-base font-bold text-texte focus:outline-2 focus:outline-orange/60"
            />
          </label>
          {erreur && <p className="text-sm font-bold text-rouge">{erreur}</p>}
          <button
            type="submit"
            disabled={enCours || nomUtilisateur.length === 0 || motDePasse.length === 0}
            className="mt-1 rounded-xl bg-orange px-5 py-2.5 text-base font-bold text-carte transition-opacity disabled:opacity-40"
          >
            {enCours ? 'Connexion…' : 'Entrer'}
          </button>
        </form>
      </div>
    </main>
  )
}
