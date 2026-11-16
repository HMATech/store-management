import { apiRequest } from '../../lib/api'
import type { RecordedTransaction } from '../transactions/TransactionDetail'

/** One product in a recorded sale, at the price it was sold for. */
type SaleEntry = {
  id: string
  productId: string
  productName: string
  priceValue: number
  quantity: number
  totalPrice: number
}

type SaleResponse = {
  id: string
  productEntries: SaleEntry[]
  totalPrice: number
  occurrenceTime: string
  lastUpdatedTime: string
}

/** Where a sale's detail screen lives. */
export const saleDetailPath = (id: string) => `/history/sales/${id}`

export async function fetchSale(id: string, signal?: AbortSignal): Promise<RecordedTransaction> {
  const sale = await apiRequest<SaleResponse>(`/sales/${id}`, { signal })
  return {
    id: sale.id,
    occurrenceTime: sale.occurrenceTime,
    lastUpdatedTime: sale.lastUpdatedTime,
    total: sale.totalPrice,
    entries: sale.productEntries.map((entry) => ({
      id: entry.id,
      productId: entry.productId,
      productName: entry.productName,
      quantity: entry.quantity,
      unitPrice: entry.priceValue,
      total: entry.totalPrice,
    })),
  }
}

/** Removes a sale from the records. Its products count as stock again. */
export function deleteSale(id: string): Promise<void> {
  return apiRequest<void>(`/sales/${id}`, { method: 'DELETE' })
}

/** Where a sale's items are changed. */
export const editSalePath = (id: string) => `${saleDetailPath(id)}/edit`

/** A line of a sale being changed. A line already recorded keeps its `id` and price; a new one has none. */
export type SaleEntryChange = { id: string | null; productId: string; quantity: number }

/**
 * Replaces what a sale holds with `entries`: recorded lines left out are removed, and new lines are priced at
 * the product's current price.
 */
export function updateSaleEntries(id: string, entries: SaleEntryChange[]): Promise<void> {
  return apiRequest<void>(`/sales/${id}/entries`, { method: 'PUT', body: { productEntries: entries } })
}
