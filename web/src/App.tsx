import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { api, type Utilisateur } from '@/lib/api'
import { journaliser } from '@/lib/journal'
import { useSynchro } from '@/hooks/useSynchro'
import BanniereErreur from '@/components/BanniereErreur'
import Layout from '@/components/Layout'
import CoquilleTelephone from '@/components/telephone/CoquilleTelephone'
import ToastConfirmation from '@/components/ToastConfirmation'
import Connexion from '@/pages/Connexion'
import Aujourdhui from '@/pages/Aujourdhui'
import Budget from '@/pages/Budget'
import Documents from '@/pages/Documents'
import Ecran from '@/pages/Ecran'
import Equipements from '@/pages/Equipements'
import Pieces from '@/pages/Pieces'
import Taches from '@/pages/Taches'
import AujourdhuiTelephone from '@/pages/telephone/AujourdhuiTelephone'
import BudgetTelephone from '@/pages/telephone/BudgetTelephone'
import DocumentsTelephone from '@/pages/telephone/DocumentsTelephone'
import EquipementsTelephone from '@/pages/telephone/EquipementsTelephone'
import PiecesTelephone from '@/pages/telephone/PiecesTelephone'
import TachesTelephone from '@/pages/telephone/TachesTelephone'
import { useFormatPhone } from '@/hooks/useFormatPhone'

function App() {
  const location = useLocation()
  // La vue e-ink vit hors de la session : le navigateur de rendu du serveur n'a pas
  // de cookie (il porte un jeton), et un humain connecté la voit telle quelle.
  if (location.pathname === '/ecran') {
    return <Ecran />
  }
  return <AppSession />
}

function AppSession() {
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
  useJournalNavigation()
  // Téléphone et tablette en portrait rendent leur propre arbre de présentation ;
  // le bureau ne bouge pas (vault/Decisions/D-2026-09-19 Interface Téléphone Distincte).
  const phone = useFormatPhone()

  return (
    <>
      <Routes>
        <Route element={phone ? <CoquilleTelephone moi={moi} /> : <Layout moi={moi} />}>
          <Route path="/" element={phone ? <AujourdhuiTelephone /> : <Aujourdhui />} />
          <Route path="/pieces" element={phone ? <PiecesTelephone /> : <Pieces />} />
          <Route path="/taches" element={phone ? <TachesTelephone /> : <Taches />} />
          <Route path="/equipements" element={phone ? <EquipementsTelephone /> : <Equipements />} />
          <Route path="/documents" element={phone ? <DocumentsTelephone /> : <Documents />} />
          <Route path="/budget" element={phone ? <BudgetTelephone /> : <Budget />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
      <ToastConfirmation />
      <BanniereErreur />
    </>
  )
}

/** Trace les changements d'écran : sans eux, la piste de session n'a pas de décor —
 * on lit un geste raté sans savoir d'où il est parti. */
function useJournalNavigation() {
  const location = useLocation()
  useEffect(() => {
    journaliser('debug', 'Navigation', `Écran ${location.pathname}`, {
      recherche: location.search === '' ? undefined : location.search,
    })
  }, [location.pathname, location.search])
}

export default App
