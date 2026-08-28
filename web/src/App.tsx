import { useQuery } from '@tanstack/react-query'
import { Navigate, Route, Routes } from 'react-router-dom'
import { api, type Utilisateur } from '@/lib/api'
import { useSynchro } from '@/hooks/useSynchro'
import BanniereErreur from '@/components/BanniereErreur'
import Layout from '@/components/Layout'
import ToastConfirmation from '@/components/ToastConfirmation'
import Connexion from '@/pages/Connexion'
import Aujourdhui from '@/pages/Aujourdhui'
import Budget from '@/pages/Budget'
import Documents from '@/pages/Documents'
import Equipements from '@/pages/Equipements'
import Pieces from '@/pages/Pieces'
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
    return (
      <>
        <Connexion />
        <BanniereErreur />
      </>
    )
  }

  return <AppConnectee moi={moi} />
}

/** La partie connectée, extraite pour que useSynchro n'existe qu'une fois la session
 * établie (les hooks ne peuvent pas être conditionnels) : la connexion au hub naît
 * avec la session et tombe à la déconnexion. */
function AppConnectee({ moi }: { moi: Utilisateur }) {
  useSynchro(moi.id)

  return (
    <>
      <Routes>
        <Route element={<Layout moi={moi} />}>
          <Route path="/" element={<Aujourdhui />} />
          <Route path="/pieces" element={<Pieces />} />
          <Route path="/taches" element={<Taches />} />
          <Route path="/equipements" element={<Equipements />} />
          <Route path="/documents" element={<Documents />} />
          <Route path="/budget" element={<Budget />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
      <ToastConfirmation />
      <BanniereErreur />
    </>
  )
}

export default App
