import clsx from 'clsx'
import { Trash2 } from 'lucide-react'
import type { ReactNode } from 'react'
import { QuantityStepper } from '../../components/QuantityStepper'
import { formatRupiah } from '../../lib/format'
import type { RecordedLine } from './useRecordedLines'

type RecordedLineCardProps = {
  line: RecordedLine
  /** The price each was sold for, shown under the name on a sale's line. */
  price?: number
  /** The most it can be changed to. */
  max: number
  /**
   * Puts the quantity on its own row, named by this label ("Jumlah dibeli"), for cards that also hold a field.
   * Without it the quantity sits beside the name.
   */
  quantityLabel?: string
  /** Lets the quantity be typed, for lines where it is often large. */
  onQuantityChange?: (quantity: number) => void
  onIncrease: () => void
  onDecrease: () => void
  onRemove: () => void
  onRestore: () => void
  /** More to change on the line, such as its cost. */
  children?: ReactNode
}

const restoreButton =
  'flex min-h-touch-min shrink-0 items-center rounded-md border-medium border-primary-default bg-bg-surface'
  + ' px-14 py-10 font-semibold text-primary-default'
  + ' transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none'
  + ' focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default'

/**
 * A line already recorded in the sale or purchase being changed: its quantity can be corrected, or it can be
 * removed and brought back. Mirrors the line cards on the "13 · Ubah Barang Penjualan" board in Loom.
 */
export function RecordedLineCard({
  line,
  price,
  max,
  quantityLabel,
  onQuantityChange,
  onIncrease,
  onDecrease,
  onRemove,
  onRestore,
  children,
}: RecordedLineCardProps) {
  const name = line.entry.productName

  if (line.isRemoved) {
    return (
      <li
        className={clsx(
          'flex items-center gap-12 rounded-lg border-thin border-border-default bg-bg-sunken',
          'py-12 pr-12 pl-16',
        )}
      >
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <span className="text-lg font-semibold break-words text-text-muted line-through">{name}</span>
          <span className="font-bold text-danger-default">Akan dihapus</span>
          <span className="text-xs text-text-muted">
            {line.entry.quantity} × {formatRupiah(line.entry.unitPrice)}
          </span>
        </div>
        <button type="button" aria-label={`Batal hapus ${name}`} onClick={onRestore} className={restoreButton}>
          Batal
        </button>
      </li>
    )
  }

  const stepper = (
    <QuantityStepper
      itemName={name}
      quantity={line.quantity}
      min={1}
      max={max}
      onDecrease={onDecrease}
      onIncrease={onIncrease}
      onChange={onQuantityChange}
    />
  )

  return (
    <li
      className={clsx(
        'flex flex-col gap-12 rounded-lg border-thick border-primary-default bg-primary-soft',
        'py-12 pr-12 pl-16',
      )}
    >
      <div className="flex items-center gap-12">
        <div className="flex min-w-0 flex-1 flex-col items-start gap-2">
          <span className="text-lg font-semibold break-words">{name}</span>
          {price !== undefined && <span className="font-bold text-primary-default">{formatRupiah(price)}</span>}
          <button
            type="button"
            aria-label={`Hapus ${name}`}
            onClick={onRemove}
            className={clsx(
              'flex min-h-touch-min items-center gap-6 rounded-sm pr-8 text-sm font-semibold text-danger-default',
              'focus-visible:outline-2 focus-visible:outline-danger-default',
            )}
          >
            <Trash2 aria-hidden className="size-20" />
            Hapus
          </button>
        </div>
        {quantityLabel === undefined && stepper}
      </div>
      {quantityLabel !== undefined && (
        <div className="flex items-center justify-between gap-12">
          <span className="text-sm font-semibold">{quantityLabel}</span>
          {stepper}
        </div>
      )}
      {children}
    </li>
  )
}
