import { Button } from '../components/Button'
import { clearSession } from '../lib/session'

/** Placeholder for the Beranda screen, so signing in and out can be tried end to end. */
export function HomePage() {
  return (
    <main className="mx-auto flex min-h-dvh w-full max-w-md flex-col justify-center gap-8 px-4 py-8">
      <header className="flex flex-col gap-2">
        <h1 className="text-2xl font-bold">Beranda</h1>
        <p className="text-text-muted">Kamu sudah masuk. Halaman ini masih dibuat.</p>
      </header>
      <Button kind="secondary" onClick={clearSession}>
        Keluar
      </Button>
    </main>
  )
}
