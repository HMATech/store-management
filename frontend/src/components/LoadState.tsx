import { Card } from './Card'

/** Shown in place of content that is still being fetched. */
export function Loading() {
  return (
    <Card className="p-16">
      <p role="status" className="text-text-muted">
        Memuat…
      </p>
    </Card>
  )
}

/** Shown in place of content that could not be fetched, with a way to try again. */
export function LoadFailed({ onRetry }: { onRetry: () => void }) {
  return (
    <Card className="flex flex-col items-start gap-8 p-16">
      <p role="alert" className="text-text-muted">
        Tidak bisa memuat. Periksa internet, lalu coba lagi.
      </p>
      <button
        type="button"
        onClick={onRetry}
        className={
          'min-h-touch-min rounded-sm text-sm font-semibold text-primary-default'
          + ' focus-visible:outline-2 focus-visible:outline-primary-default'
        }
      >
        Coba lagi
      </button>
    </Card>
  )
}

/** Shown in place of a list with nothing in it. */
export function Empty({ children }: { children: string }) {
  return (
    <Card className="p-16">
      <p className="text-text-muted">{children}</p>
    </Card>
  )
}
