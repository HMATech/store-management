import '@fontsource-variable/inter'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { App } from './App'
import './index.css'
import { getToken, subscribe } from './lib/session'

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: 1 } } })

// What was fetched belongs to the user who was signed in, so it goes when the session ends.
subscribe(() => {
  if (getToken() === null) {
    queryClient.clear()
  }
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
