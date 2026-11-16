import { AlertDialog } from '@base-ui/react/alert-dialog'
import { Check, Home, Plus } from 'lucide-react'
import { formatRupiah } from '../lib/format'
import { Button, ButtonLink } from './Button'

/** What was just saved, for the confirmation. */
export type SavedSummary = { totalQuantity: number; total: number }

type SavedDialogProps = {
  /** What it confirms, such as that the sale was saved. */
  title: string
  /** What was saved, or null while there is nothing to confirm. */
  saved: SavedSummary | null
  /** The action that starts the same job again, such as recording another sale. */
  againLabel: string
  onAgain: () => void
}

/**
 * Confirms that a sale or purchase was saved and asks what to do next. It stays until one of its two actions
 * is chosen, so the confirmation can't be missed. Mirrors the 03a overlay in Loom.
 */
export function SavedDialog({ title, saved, againLabel, onAgain }: SavedDialogProps) {
  return (
    <AlertDialog.Root open={saved !== null}>
      <AlertDialog.Portal>
        <AlertDialog.Backdrop className="fixed inset-0 bg-bg-inverse/50" />
        <AlertDialog.Popup
          className={
            'fixed top-1/2 left-1/2 flex w-[calc(100%-48px)] max-w-[400px] -translate-x-1/2 -translate-y-1/2'
            + ' flex-col items-center gap-20 rounded-xl bg-bg-surface p-20 pt-28'
            + ' shadow-[0_12px_32px_color-mix(in_srgb,var(--color-shadow)_18%,transparent)]'
          }
        >
          <span className="flex size-72 items-center justify-center rounded-full bg-primary-default">
            <Check aria-hidden className="size-40 text-primary-on" />
          </span>
          <div className="flex flex-col items-center gap-4 text-center">
            <AlertDialog.Title className="text-3xl font-bold">{title}</AlertDialog.Title>
            <AlertDialog.Description className="text-lg text-text-muted">
              {saved !== null && `${saved.totalQuantity} barang · ${formatRupiah(saved.total)}`}
            </AlertDialog.Description>
          </div>
          <div className="flex w-full flex-col gap-10">
            <Button onClick={onAgain}>
              <Plus aria-hidden className="size-22" />
              {againLabel}
            </Button>
            <ButtonLink kind="secondary" to="/">
              <Home aria-hidden className="size-24" />
              Kembali ke Beranda
            </ButtonLink>
          </div>
        </AlertDialog.Popup>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  )
}
