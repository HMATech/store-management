import { useState } from 'react'
import { MoneyField } from '../components/MoneyField'
import { PurchaseProductList } from '../features/purchases/PurchaseProductList'
import {
  fetchPurchase,
  purchaseDetailPath,
  updatePurchaseEntries,
  type PurchaseEntryChange,
} from '../features/purchases/detail'
import { MaxQuantity, usePurchaseCart } from '../features/purchases/usePurchaseCart'
import { EditEntriesLayout, EditEntriesScreen, type EditKindText } from '../features/transactions/EditEntries'
import { RecordedLineCard } from '../features/transactions/RecordedLineCard'
import type { RecordedEntry, RecordedTransaction } from '../features/transactions/TransactionDetail'
import { describeSaveFailure, useSaveEntries } from '../features/transactions/saveEntries'
import { useRecordedLines } from '../features/transactions/useRecordedLines'
import { formatRupiah } from '../lib/format'

const purchase: EditKindText = {
  noun: 'Belanja',
  queryKey: 'purchases',
  fetch: fetchPurchase,
  detailPath: purchaseDetailPath,
}

/** A purchase's line can also correct its cost, which is empty while it is being retyped. */
const costOf = (entry: RecordedEntry): { unitCost: number | null } => ({ unitCost: entry.unitPrice })

/** Ubah Barang for a purchase: correct how many were bought and what each cost, remove a line, or add one. */
export function EditPurchasePage() {
  return <EditEntriesScreen kind={purchase}>{(recorded) => <PurchaseEditor recorded={recorded} />}</EditEntriesScreen>
}

function PurchaseEditor({ recorded }: { recorded: RecordedTransaction }) {
  const lines = useRecordedLines(recorded.entries, costOf, MaxQuantity)
  const cart = usePurchaseCart()
  // Missing costs are pointed out only once saving was tried, not while they are being retyped.
  const [wasSaveTried, setWasSaveTried] = useState(false)
  const save = useSaveEntries(purchase, recorded, (entries: PurchaseEntryChange[]) =>
    updatePurchaseEntries(recorded.id, entries),
  )

  const kept = lines.keptLines
  const isCostMissing = kept.some((line) => line.unitCost === null) || cart.costedLines === null

  function trySave() {
    setWasSaveTried(true)
    if (isCostMissing || cart.costedLines === null) {
      return
    }
    save.mutate([
      ...kept.map((line) => ({
        id: line.entry.id,
        productId: line.entry.productId,
        quantity: line.quantity,
        unitCost: line.unitCost ?? 0,
      })),
      ...cart.costedLines.map(({ product, quantity, unitCost }) => ({
        id: null,
        productId: product.id,
        quantity,
        unitCost,
      })),
    ])
  }

  return (
    <EditEntriesLayout
      kind={purchase}
      recorded={recorded}
      recordedLines={lines.lines.map((line) => (
        <RecordedLineCard
          key={line.entry.id}
          line={line}
          max={lines.maxOf(line)}
          quantityLabel="Jumlah dibeli"
          onQuantityChange={(quantity) => lines.setQuantity(line.entry.id, quantity)}
          onIncrease={() => lines.increase(line.entry.id)}
          onDecrease={() => lines.decrease(line.entry.id)}
          onRemove={() => lines.remove(line.entry.id)}
          onRestore={() => lines.restore(line.entry.id)}
        >
          <MoneyField
            label="Harga beli per barang"
            aria-label={`Harga beli ${line.entry.productName}, per barang`}
            placeholder="0"
            value={line.unitCost}
            onChange={(unitCost) => lines.update(line.entry.id, { unitCost })}
            error={wasSaveTried && line.unitCost === null ? 'Isi harga belinya dulu.' : undefined}
          />
        </RecordedLineCard>
      ))}
      isEmpty={kept.length === 0 && cart.lines.length === 0}
      note="Jumlah dan harga beli yang salah bisa dibetulkan di sini. Stok ikut menyesuaikan."
      products={(searchText) => (
        <PurchaseProductList searchText={searchText} cart={cart} showMissingCosts={wasSaveTried} />
      )}
      checkoutLines={[
        ...kept.map((line) => ({
          id: line.entry.id,
          name: line.entry.productName,
          ...describeCost(line.quantity, line.unitCost),
        })),
        ...cart.lines.map(({ product, quantity, unitCost }) => ({
          id: `new-${product.id}`,
          name: product.name,
          ...describeCost(quantity, unitCost),
        })),
      ]}
      totalQuantity={kept.reduce((sum, line) => sum + line.quantity, 0) + cart.totalQuantity}
      total={kept.reduce((sum, line) => sum + line.quantity * (line.unitCost ?? 0), 0) + cart.totalCost}
      error={
        wasSaveTried && isCostMissing
          ? 'Belum bisa disimpan. Isi dulu harga beli tiap barang.'
          : save.isError
            ? describeSaveFailure(save.error, purchase.noun)
            : undefined
      }
      isSaving={save.isPending}
      onSave={trySave}
    />
  )
}

/** How a line's amount came about, while its cost may still be empty. */
function describeCost(quantity: number, unitCost: number | null) {
  return unitCost === null
    ? { detail: `${quantity} × harga beli belum diisi`, amount: '–' }
    : { detail: `${quantity} × ${formatRupiah(unitCost)}`, amount: formatRupiah(quantity * unitCost) }
}
