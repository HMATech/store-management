import { apiRequest } from '../../lib/api'
import type { RecordedTransaction } from '../transactions/TransactionDetail'

/** One product in a recorded sale, at the price it was sold for. */
type SaleEntry = { id: string; productName: string; priceValue: number; quantity: number; totalPrice: number }

type SaleResponse = { id: string; productEntries: SaleEntry[]; totalPrice: number; occurrenceTime: string }

/** Where a sale's detail screen lives. */
export const saleDetailPath = (id: string) => `/riwayat/penjualan/${id}`

export async function fetchSale(id: string, signal?: AbortSignal): Promise<RecordedTransaction> {
  const sale = await apiRequest<SaleResponse>(`/sales/${id}`, { signal })
  return {
    id: sale.id,
    occurrenceTime: sale.occurrenceTime,
    total: sale.totalPrice,
    entries: sale.productEntries.map((entry) => ({
      id: entry.id,
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
