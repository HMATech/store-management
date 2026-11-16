import { Dialog } from '@base-ui/react/dialog'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, Info } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Button } from '../../components/Button'
import { InfoNote } from '../../components/InfoNote'
import { MoneyField } from '../../components/MoneyField'
import { ApiError } from '../../lib/api'
import { formatRupiah } from '../../lib/format'
import { changePrice } from './detail'

type ChangePriceDialogProps = {
  product: { id: string; name: string; price: number }
  isOpen: boolean
  onClose: () => void
}

/** Asks for a product's new selling price and saves it. Mirrors the 08a overlay in Loom. */
export function ChangePriceDialog({ product, isOpen, onClose }: ChangePriceDialogProps) {
  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Backdrop className="fixed inset-0 bg-bg-inverse/50" />
        <Dialog.Popup
          className={
            'fixed top-1/2 left-1/2 flex max-h-[calc(100dvh-32px)] w-[calc(100%-48px)] max-w-[400px]'
            + ' -translate-x-1/2 -translate-y-1/2 flex-col overflow-y-auto rounded-xl bg-bg-surface px-20 py-24'
            + ' shadow-[0_12px_32px_color-mix(in_srgb,var(--color-shadow)_18%,transparent)]'
          }
        >
          {/* The form lives only while the dialog is open, so each opening starts empty. */}
          <ChangePriceForm product={product} onClose={onClose} />
        </Dialog.Popup>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

function ChangePriceForm({ product, onClose }: Omit<ChangePriceDialogProps, 'isOpen'>) {
  const queryClient = useQueryClient()
  const [price, setPrice] = useState<number | null>(null)
  // A problem with the price is pointed out only once saving was tried.
  const [wasSaveTried, setWasSaveTried] = useState(false)

  const problem = findProblem(price, product.price)
  const save = useMutation({
    mutationFn: (value: number) => changePrice(product.id, value),
    onSuccess: () => {
      // The product, its price history and every list that shows the price changed.
      void queryClient.invalidateQueries()
      onClose()
    },
  })

  function trySave(event: FormEvent) {
    event.preventDefault()
    setWasSaveTried(true)
    if (price !== null && problem === undefined) {
      save.mutate(price)
    }
  }

  return (
    <form noValidate onSubmit={trySave} className="flex flex-col gap-16">
      <div className="flex flex-col gap-2">
        <Dialog.Title className="text-2xl font-bold">Ubah harga jual</Dialog.Title>
        <Dialog.Description className="text-sm break-words text-text-muted">{product.name}</Dialog.Description>
      </div>
      <p className="flex items-center justify-between gap-12 rounded-md bg-bg-canvas px-16 py-12">
        <span className="text-sm font-medium text-text-muted">Harga sekarang</span>
        <span className="text-xl font-bold">{formatRupiah(product.price)}</span>
      </p>
      <MoneyField
        label="Harga baru"
        placeholder="0"
        value={price}
        onChange={setPrice}
        error={wasSaveTried ? problem : undefined}
      />
      <InfoNote icon={Info}>
        Harga baru berlaku mulai sekarang. Penjualan yang sudah dicatat tetap memakai harga lama.
      </InfoNote>
      {save.isError && (
        <p role="alert" className="rounded-md bg-danger-soft p-12 text-sm font-medium text-danger-default">
          {describeSaveFailure(save.error)}
        </p>
      )}
      <div className="flex flex-col gap-10">
        <Button type="submit" disabled={save.isPending}>
          <Check aria-hidden className="size-24" />
          {save.isPending ? 'Menyimpan…' : 'Simpan Harga'}
        </Button>
        <Button kind="secondary" onClick={onClose}>
          Batal
        </Button>
      </div>
    </form>
  )
}

function findProblem(price: number | null, currentPrice: number): string | undefined {
  if (price === null) {
    return 'Harga baru wajib diisi, contoh: 18000'
  }
  if (price === currentPrice) {
    return 'Harga baru sama dengan harga sekarang.'
  }
  return undefined
}

function describeSaveFailure(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return 'Tidak bisa terhubung, harga belum berubah. Periksa internet, lalu coba lagi.'
    }
    if (error.status === 404) {
      return 'Barang ini sudah dihapus. Kembali ke menu Produk.'
    }
  }
  return 'Harga belum berubah. Coba lagi.'
}
