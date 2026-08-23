import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'

// Mode sombre automatique selon la préférence système (shadcn utilise la classe .dark)
const media = window.matchMedia('(prefers-color-scheme: dark)')
function appliquerTheme() {
  document.documentElement.classList.toggle('dark', media.matches)
}
appliquerTheme()
media.addEventListener('change', appliquerTheme)

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false, staleTime: 30_000 } },
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
