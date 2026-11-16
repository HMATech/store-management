import { apiRequest } from '../../lib/api'
import type { RecordedTransaction } from '../transactions/TransactionDetail'

/** One product in a recorded purchase, at the cost it was bought for. */
type PurchaseEntry = {
  id: string
  productId: string
  productName: string
  unitCost: number
  quantity: number
  totalCost: number
}

type PurchaseResponse = {
  id: string
  title: string
  productEntries: PurchaseEntry[]
  totalCost: number
  occurrenceTime: string
  lastUpdatedTime: string
}

/** Where a purchase's detail screen lives. */
export const purchaseDetailPath = (id: string) => `/riwayat/belanja/${id}`

export async function fetchPurchase(id: string, signal?: AbortSignal): Promise<RecordedTransaction> {
  const purchase = await apiRequest<PurchaseResponse>(`/purchases/${id}`, { signal })
  return {
    id: purchase.id,
    supplier: purchase.title,
    occurrenceTime: purchase.occurrenceTime,
    lastUpdatedTime: purchase.lastUpdatedTime,
    total: purchase.totalCost,
    entries: purchase.productEntries.map((entry) => ({
      id: entry.id,
      productId: entry.productId,
      productName: entry.productName,
      quantity: entry.quantity,
      unitPrice: entry.unitCost,
      total: entry.totalCost,
    })),
  }
}

/** Removes a purchase from the records. Its products no longer count as stock. */
export function deletePurchase(id: string): Promise<void> {
  return apiRequest<void>(`/purchases/${id}`, { method: 'DELETE' })
}

/** Where a purchase's items are changed. */
export const editPurchasePath = (id: string) => `${purchaseDetailPath(id)}/ubah`

/** A line of a purchase being changed. A line already recorded keeps its `id`; a new one has none. */
export type PurchaseEntryChange = { id: string | null; productId: string; quantity: number; unitCost: number }

/** Replaces what a purchase holds with `entries`. Recorded lines left out are removed. */
export function updatePurchaseEntries(id: string, entries: PurchaseEntryChange[]): Promise<void> {
  return apiRequest<void>(`/purchases/${id}/entries`, { method: 'PUT', body: { productEntries: entries } })
}
