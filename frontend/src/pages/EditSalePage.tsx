import { SaleProductList } from '../features/sales/SaleProductList'
import { fetchSale, saleDetailPath, updateSaleEntries, type SaleEntryChange } from '../features/sales/detail'
import { MaxQuantity, useCart } from '../features/sales/useCart'
import { EditEntriesLayout, EditEntriesScreen, type EditKindText } from '../features/transactions/EditEntries'
import { RecordedLineCard } from '../features/transactions/RecordedLineCard'
import type { RecordedTransaction } from '../features/transactions/TransactionDetail'
import { describeSaveFailure, useSaveEntries } from '../features/transactions/saveEntries'
import { useRecordedLines } from '../features/transactions/useRecordedLines'
import { formatRupiah } from '../lib/format'

const sale: EditKindText = { noun: 'Penjualan', queryKey: 'sales', fetch: fetchSale, detailPath: saleDetailPath }

/** A sale's line can change only its quantity: it keeps the price it was sold at. */
const nothingElse = () => ({})

/** Ubah Barang for a sale: correct how many of each were sold, remove a line, or add what was missed. */
export function EditSalePage() {
  return <EditEntriesScreen kind={sale}>{(recorded) => <SaleEditor recorded={recorded} />}</EditEntriesScreen>
}

function SaleEditor({ recorded }: { recorded: RecordedTransaction }) {
  const lines = useRecordedLines(recorded.entries, nothingElse, MaxQuantity)
  const cart = useCart()
  const save = useSaveEntries(sale, recorded, (entries: SaleEntryChange[]) => updateSaleEntries(recorded.id, entries))

  const kept = lines.keptLines
  const entries: SaleEntryChange[] = [
    ...kept.map((line) => ({ id: line.entry.id, productId: line.entry.productId, quantity: line.quantity })),
    ...cart.lines.map(({ product, quantity }) => ({ id: null, productId: product.id, quantity })),
  ]

  return (
    <EditEntriesLayout
      kind={sale}
      recorded={recorded}
      recordedLines={lines.lines.map((line) => (
        <RecordedLineCard
          key={line.entry.id}
          line={line}
          price={line.entry.unitPrice}
          max={lines.maxOf(line)}
          onIncrease={() => lines.increase(line.entry.id)}
          onDecrease={() => lines.decrease(line.entry.id)}
          onRemove={() => lines.remove(line.entry.id)}
          onRestore={() => lines.restore(line.entry.id)}
        />
      ))}
      isEmpty={entries.length === 0}
      note="Harga barang yang sudah tercatat tidak berubah. Barang tambahan memakai harga sekarang."
      products={(searchText) => <SaleProductList searchText={searchText} cart={cart} />}
      checkoutLines={[
        ...kept.map((line) => ({
          id: line.entry.id,
          name: line.entry.productName,
          detail: `${line.quantity} × ${formatRupiah(line.entry.unitPrice)}`,
          amount: formatRupiah(line.quantity * line.entry.unitPrice),
        })),
        ...cart.lines.map(({ product, quantity }) => ({
          id: `new-${product.id}`,
          name: product.name,
          detail: `${quantity} × ${formatRupiah(product.price)}`,
          amount: formatRupiah(quantity * product.price),
        })),
      ]}
      totalQuantity={kept.reduce((sum, line) => sum + line.quantity, 0) + cart.totalQuantity}
      total={kept.reduce((sum, line) => sum + line.quantity * line.entry.unitPrice, 0) + cart.totalPrice}
      error={save.isError ? describeSaveFailure(save.error, sale.noun) : undefined}
      isSaving={save.isPending}
      onSave={() => save.mutate(entries)}
    />
  )
}
