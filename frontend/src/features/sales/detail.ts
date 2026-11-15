import { apiRequest } from '../../lib/api'

/** One product in a recorded sale, at the price it was sold for. */
export type SaleEntry = {
  id: string
  productName: string
  priceValue: number
  quantity: number
  totalPrice: number
}

/** A recorded sale with what was sold. */
export type SaleDetail = {
  id: string
  productEntries: SaleEntry[]
  totalPrice: number
  occurrenceTime: string
}

/** Where a sale's detail screen lives. */
export const saleDetailPath = (id: string) => `/riwayat/penjualan/${id}`

export function fetchSale(id: string, signal?: AbortSignal): Promise<SaleDetail> {
  return apiRequest<SaleDetail>(`/sales/${id}`, { signal })
}

/** Removes a sale from the records. Its products count as stock again. */
export function deleteSale(id: string): Promise<void> {
  return apiRequest<void>(`/sales/${id}`, { method: 'DELETE' })
}
