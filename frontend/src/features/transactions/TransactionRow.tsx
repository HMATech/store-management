import { ShoppingCart, Truck } from 'lucide-react'
import { CardRow } from '../../components/Card'
import { IconChip } from '../../components/IconChip'
import { formatRupiah, formatTime } from '../../lib/format'
import type { Transaction } from './api'

/** A sale or purchase in the history. Mirrors the Transaction row component in Loom, with its Kind. */
export function TransactionRow({ transaction }: { transaction: Transaction }) {
  const isSale = transaction.kind === 'Sale'
  return (
    <CardRow>
      <IconChip icon={isSale ? ShoppingCart : Truck} tone={isSale ? 'primary' : 'info'} />
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="font-semibold">{isSale ? 'Penjualan' : 'Belanja'}</span>
        <span className="text-xs text-text-muted">
          {formatTime(new Date(transaction.occurrenceTime))} · {transaction.quantity} barang
        </span>
      </div>
      {isSale ? (
        <span className="shrink-0 font-bold text-primary-default">+ {formatRupiah(transaction.total)}</span>
      ) : (
        <span className="shrink-0 font-bold">− {formatRupiah(transaction.total)}</span>
      )}
    </CardRow>
  )
}
