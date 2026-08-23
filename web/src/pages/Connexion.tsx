import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { api } from '@/lib/api'

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
    } catch {
      setErreur('Identifiants invalides.')
    } finally {
      setEnCours(false)
    }
  }

  return (
    <main className="flex min-h-dvh items-center justify-center p-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-center text-2xl">Maison</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={soumettre} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="nom">Qui es-tu ?</Label>
              <Input
                id="nom"
                autoComplete="username"
                autoCapitalize="none"
                value={nomUtilisateur}
                onChange={(e) => setNomUtilisateur(e.target.value)}
                placeholder="alain ou ariane"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="mdp">Mot de passe</Label>
              <Input
                id="mdp"
                type="password"
                autoComplete="current-password"
                value={motDePasse}
                onChange={(e) => setMotDePasse(e.target.value)}
              />
            </div>
            {erreur && <p className="text-sm text-destructive">{erreur}</p>}
            <Button type="submit" disabled={enCours || !nomUtilisateur || !motDePasse}>
              {enCours ? 'Connexion…' : 'Se connecter'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </main>
  )
}
