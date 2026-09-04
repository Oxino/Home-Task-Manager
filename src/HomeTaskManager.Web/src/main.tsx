// Vite/React entry point: mounts the single-page <App/> (see App.tsx) into #root, wrapped in StrictMode for dev-time checks.
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
