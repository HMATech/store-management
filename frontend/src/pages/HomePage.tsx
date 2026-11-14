import { Button } from '../components/Button'
import { clearSession } from '../lib/session'

/** Placeholder for the Beranda screen, so signing in and out can be tried end to end. */
export function HomePage() {
  return (
    <main className="mx-auto flex min-h-dvh w-full max-w-[420px] flex-col justify-center gap-32 px-24 py-40">
      <header className="flex flex-col gap-8">
        <h1 className="text-4xl font-bold">Beranda</h1>
        <p className="text-text-muted">Kamu sudah masuk. Halaman ini masih dibuat.</p>
      </header>
      <Button kind="secondary" onClick={clearSession}>
        Keluar
      </Button>
    </main>
  )
}
