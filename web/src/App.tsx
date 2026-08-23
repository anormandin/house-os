import { useQuery } from '@tanstack/react-query'

type Sante = { statut: string }

function App() {
  const { data } = useQuery({
    queryKey: ['sante'],
    queryFn: async (): Promise<Sante> => {
      const reponse = await fetch('/api/sante')
      if (reponse.ok) {
        return reponse.json()
      }
      throw new Error('Serveur injoignable')
    },
    retry: false,
  })

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-2">
      <h1 className="text-3xl font-semibold">Maison</h1>
      <p className="text-sm opacity-70">
        {data?.statut === 'ok' ? 'Serveur en ligne' : 'Serveur hors ligne'}
      </p>
    </main>
  )
}

export default App
