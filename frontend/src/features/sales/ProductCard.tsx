import clsx from 'clsx'
import { Plus } from 'lucide-react'
import { QuantityStepper } from '../../components/QuantityStepper'
import { formatRupiah } from '../../lib/format'
import type { SaleProduct } from './api'
import { MaxQuantity } from './useCart'

type ProductCardProps = {
  product: SaleProduct
  /** How many of it are in the sale; 0 when it hasn't been added. */
  quantity: number
  onAdd: () => void
  onIncrease: () => void
  onDecrease: () => void
}

/** A product that can be added to the sale. Mirrors the Sale item component in Loom (Selected or not). */
export function ProductCard({ product, quantity, onAdd, onIncrease, onDecrease }: ProductCardProps) {
  const isSelected = quantity > 0

  return (
    <li
      className={clsx(
        'flex items-center gap-12 rounded-lg py-12 pr-12 pl-16',
        isSelected
          ? 'border-thick border-primary-default bg-primary-soft'
          : 'border-thin border-border-default bg-bg-surface',
      )}
    >
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="text-lg font-semibold break-words">{product.name}</span>
        <span className="font-bold text-primary-default">{formatRupiah(product.price)}</span>
        <StockNote stock={product.stock} />
      </div>
      {isSelected ? (
        <QuantityStepper
          itemName={product.name}
          quantity={quantity}
          max={MaxQuantity}
          onDecrease={onDecrease}
          onIncrease={onIncrease}
        />
      ) : (
        <button
          type="button"
          aria-label={`Tambah ${product.name}`}
          onClick={onAdd}
          className={
            'flex min-h-touch-min shrink-0 items-center gap-8 rounded-md border-medium border-primary-default'
            + ' bg-bg-surface py-10 pr-14 pl-12 font-semibold text-primary-default'
            + ' transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none'
            + ' focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default'
          }
        >
          <Plus aria-hidden className="size-22" />
          Tambah
        </button>
      )}
    </li>
  )
}

function StockNote({ stock }: { stock: SaleProduct['stock'] }) {
  if (stock.status === 'Negative') {
    return <span className="text-xs text-danger-default">Stok minus {Math.abs(stock.quantity)}</span>
  }
  if (stock.quantity === 0) {
    return <span className="text-xs text-text-muted">Stok habis</span>
  }
  return (
    <span className="text-xs text-text-muted">
      {stock.status === 'Low' ? 'Sisa' : 'Stok'} {stock.quantity}
    </span>
  )
}
