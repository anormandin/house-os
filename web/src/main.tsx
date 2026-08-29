import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import GardeErreur from '@/components/GardeErreur'
import { installerJournal } from '@/lib/journal'
import { creerQueryClient } from '@/lib/query-client'

// Avant le premier rendu : une erreur pendant le montage doit déjà être captée.
installerJournal()

const queryClient = creerQueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <GardeErreur>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </QueryClientProvider>
    </GardeErreur>
  </StrictMode>,
)
