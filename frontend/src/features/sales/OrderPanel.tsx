import { Check } from 'lucide-react'
import { Button } from '../../components/Button'
import { formatRupiah } from '../../lib/format'
import type { Cart } from './useCart'

type OrderPanelProps = {
  cart: Cart
  isSaving: boolean
  /** Why the last save failed, in words for the user. */
  error?: string
  onSave: () => void
}

/**
 * The sale's total and its save button: a bar pinned to the bottom on phones, and a panel that also lists the
 * picked products on wide screens. Mirrors the Checkout bar component and the desktop Order panel in Loom.
 */
export function OrderPanel({ cart, isSaving, error, onSave }: OrderPanelProps) {
  const isEmpty = cart.lines.length === 0

  return (
    <section
      aria-label="Pesanan"
      className={
        'sticky bottom-0 flex flex-col bg-bg-surface'
        + ' shadow-[0_-4px_16px_color-mix(in_srgb,var(--color-shadow)_8%,transparent)]'
        + ' lg:top-32 lg:bottom-auto lg:w-380 lg:shrink-0 lg:rounded-lg lg:border-thin'
        + ' lg:border-border-default lg:shadow-none'
      }
    >
      <div className="hidden flex-col lg:flex">
        <div className="flex flex-col gap-4 p-20">
          <h2 className="text-2xl font-bold">Pesanan</h2>
          <p className="text-xs text-text-muted">Barang yang dibeli pembeli ini</p>
        </div>
        {isEmpty ? (
          <p className="px-20 pb-16 text-sm text-text-muted">Belum ada barang dipilih.</p>
        ) : (
          <ul className="flex max-h-[40dvh] flex-col gap-12 overflow-y-auto overscroll-contain px-20 pt-4 pb-20">
            {cart.lines.map(({ product, quantity }) => (
              <li key={product.id} className="flex items-center gap-12">
                <div className="flex min-w-0 flex-1 flex-col gap-2">
                  <span className="font-semibold break-words">{product.name}</span>
                  <span className="text-xs text-text-muted">
                    {quantity} × {formatRupiah(product.price)}
                  </span>
                </div>
                <span className="shrink-0 font-bold">{formatRupiah(quantity * product.price)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="flex flex-col gap-12 px-16 pt-14 pb-[calc(20px+env(safe-area-inset-bottom,0px))] lg:pb-20">
        <div className="flex items-end justify-between gap-12">
          <div className="flex flex-col gap-2 text-sm">
            <span className="text-text-muted">Total belanja pembeli</span>
            <span className="font-semibold">{cart.totalQuantity} barang</span>
          </div>
          <output aria-label="Total" className="text-5xl font-extrabold">
            {formatRupiah(cart.totalPrice)}
          </output>
        </div>
        {error !== undefined && (
          <p role="alert" className="rounded-md bg-danger-soft p-12 text-sm font-medium text-danger-default">
            {error}
          </p>
        )}
        <Button onClick={onSave} disabled={isEmpty || isSaving}>
          <Check aria-hidden className="size-24" />
          {isSaving ? 'Menyimpan…' : 'Simpan Penjualan'}
        </Button>
      </div>
    </section>
  )
}
