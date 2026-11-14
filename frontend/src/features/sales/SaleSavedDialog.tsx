import { AlertDialog } from '@base-ui/react/alert-dialog'
import { Check, Home, Plus } from 'lucide-react'
import { Button, ButtonLink } from '../../components/Button'
import { formatRupiah } from '../../lib/format'

/** What was just saved, for the confirmation. */
export type SavedSale = { totalQuantity: number; totalPrice: number }

type SaleSavedDialogProps = {
  /** The sale that was saved, or null while there is nothing to confirm. */
  sale: SavedSale | null
  onRecordAnother: () => void
}

/**
 * Confirms a saved sale and asks what to do next. It stays until one of its two actions is chosen, so the
 * confirmation can't be missed. Mirrors the "Penjualan tersimpan" overlay in Loom.
 */
export function SaleSavedDialog({ sale, onRecordAnother }: SaleSavedDialogProps) {
  return (
    <AlertDialog.Root open={sale !== null}>
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
            <AlertDialog.Title className="text-3xl font-bold">Penjualan tersimpan</AlertDialog.Title>
            <AlertDialog.Description className="text-lg text-text-muted">
              {sale !== null && `${sale.totalQuantity} barang · ${formatRupiah(sale.totalPrice)}`}
            </AlertDialog.Description>
          </div>
          <div className="flex w-full flex-col gap-10">
            <Button onClick={onRecordAnother}>
              <Plus aria-hidden className="size-22" />
              Catat Penjualan Lagi
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
