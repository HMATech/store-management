import clsx from 'clsx'
import { Plus } from 'lucide-react'
import type { ReactNode } from 'react'
import { QuantityStepper } from '../../components/QuantityStepper'
import { formatRupiah } from '../../lib/format'
import type { ProductSummary } from './api'

type ProductCardProps = {
  product: ProductSummary
  /** How many of it are picked; 0 when it hasn't been added. */
  quantity: number
  /** The most of it that can be picked. */
  max: number
  /** Shows the price as a small labelled note, such as the selling price, where another price is the main one. */
  priceLabel?: string
  onAdd: () => void
  onIncrease: () => void
  onDecrease: () => void
  /** Lets the quantity be typed, for screens where it is often large. */
  onQuantityChange?: (quantity: number) => void
  /**
   * Puts the quantity on its own row under the product, named by this label, such as how many were bought. For cards that
   * hold more than the quantity, where the row beside the name is too narrow on a phone.
   */
  quantityLabel?: string
  /** More about the picked product, such as a field to fill in. Shown under it once it is added. */
  children?: ReactNode
}

/** A product that can be picked, with how many. Mirrors the Sale item component in Loom (Selected or not). */
export function ProductCard({
  product,
  quantity,
  max,
  priceLabel,
  onAdd,
  onIncrease,
  onDecrease,
  onQuantityChange,
  quantityLabel,
  children,
}: ProductCardProps) {
  const isSelected = quantity > 0
  const stepper = (
    <QuantityStepper
      itemName={product.name}
      quantity={quantity}
      max={max}
      onDecrease={onDecrease}
      onIncrease={onIncrease}
      onChange={onQuantityChange}
    />
  )

  return (
    <li
      className={clsx(
        'flex flex-col gap-12 rounded-lg py-12 pr-12 pl-16',
        isSelected
          ? 'border-thick border-primary-default bg-primary-soft'
          : 'border-thin border-border-default bg-bg-surface',
      )}
    >
      <div className="flex items-center gap-12">
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <span className="text-lg font-semibold break-words">{product.name}</span>
          {priceLabel === undefined ? (
            <span className="font-bold text-primary-default">{formatRupiah(product.price)}</span>
          ) : (
            <span className="text-xs text-text-muted">
              {priceLabel} {formatRupiah(product.price)}
            </span>
          )}
          <StockNote stock={product.stock} />
        </div>
        {isSelected ? (
          quantityLabel === undefined && stepper
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
      </div>
      {isSelected && quantityLabel !== undefined && (
        <div className="flex items-center justify-between gap-12">
          <span className="text-sm font-semibold">{quantityLabel}</span>
          {stepper}
        </div>
      )}
      {isSelected && children}
    </li>
  )
}

function StockNote({ stock }: { stock: ProductSummary['stock'] }) {
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
