import { apiRequest } from '../../lib/api'
import type { RecordedTransaction } from '../transactions/TransactionDetail'

/** One product in a recorded purchase, at the cost it was bought for. */
type PurchaseEntry = { id: string; productName: string; unitCost: number; quantity: number; totalCost: number }

type PurchaseResponse = {
  id: string
  title: string
  productEntries: PurchaseEntry[]
  totalCost: number
  occurrenceTime: string
}

/** Where a purchase's detail screen lives. */
export const purchaseDetailPath = (id: string) => `/riwayat/belanja/${id}`

export async function fetchPurchase(id: string, signal?: AbortSignal): Promise<RecordedTransaction> {
  const purchase = await apiRequest<PurchaseResponse>(`/purchases/${id}`, { signal })
  return {
    id: purchase.id,
    supplier: purchase.title,
    occurrenceTime: purchase.occurrenceTime,
    total: purchase.totalCost,
    entries: purchase.productEntries.map((entry) => ({
      id: entry.id,
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
