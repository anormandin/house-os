import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MutationCache, QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import { ApiError } from '@/lib/api'
import { signalerErreur } from '@/lib/erreurs'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false, staleTime: 30_000 } },
  // Toutes les erreurs de mutation remontent dans la bannière globale ;
  // les 401 sont exclus (la requête `moi` ramène à l'écran de connexion).
  mutationCache: new MutationCache({
    onError: (erreur) => {
      if (erreur instanceof ApiError && erreur.statut === 401) {
        return
      }
      signalerErreur(erreur instanceof ApiError ? erreur.message : 'Une erreur est survenue.')
    },
  }),
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
