import { useQuery } from '@tanstack/react-query'
import { Navigate, Route, Routes } from 'react-router-dom'
import { api } from '@/lib/api'
import Layout from '@/components/Layout'
import Connexion from '@/pages/Connexion'
import Aujourdhui from '@/pages/Aujourdhui'
import Taches from '@/pages/Taches'

function App() {
  const { data: moi, isLoading } = useQuery({
    queryKey: ['moi'],
    queryFn: api.moi,
  })

  if (isLoading) {
    return (
      <main className="flex min-h-dvh items-center justify-center bg-fond text-sourdine">
        Chargement…
      </main>
    )
  }

  if (moi === undefined) {
    return <Connexion />
  }

  return (
    <Routes>
      <Route element={<Layout moi={moi} />}>
        <Route path="/" element={<Aujourdhui />} />
        <Route path="/taches" element={<Taches />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

export default App
