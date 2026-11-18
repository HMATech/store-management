import { apiRequest } from '../../lib/api'
import { fetchDailyTotals, type DailyTotals } from '../reports/api'

export type StockAlert = {
  id: string
  name: string
  stock: { quantity: number; status: 'Negative' | 'Empty' | 'Low' }
}

export type RecentSale = {
  id: string
  occurrenceTime: string
  quantity: number
  total: number
}

type Paged<T> = { responses: T[] }

/** How many rows the home screen's lists show. The full lists are on their own screens. */
const ListSize = 3

/** The totals of one day, counted in the device's time zone. */
export function fetchTodayTotals(today: Date, signal?: AbortSignal): Promise<DailyTotals> {
  return fetchDailyTotals(today, today, signal)
}

/** The products whose stock needs a look: those below zero first, then those run out, then those running low. */
export async function fetchStockAlerts(signal?: AbortSignal): Promise<StockAlert[]> {
  const fetchByStatus = (status: StockAlert['stock']['status']) =>
    apiRequest<Paged<StockAlert>>(`/products?stockStatus=${status}&pageSize=${ListSize}`, { signal })

  const pages = await Promise.all([fetchByStatus('Negative'), fetchByStatus('Empty'), fetchByStatus('Low')])
  return pages.flatMap((page) => page.responses).slice(0, ListSize)
}

/** The latest sales, newest first. */
export async function fetchRecentSales(signal?: AbortSignal): Promise<RecentSale[]> {
  const page = await apiRequest<Paged<RecentSale>>(`/transactions?kind=Sale&pageSize=${ListSize}`, { signal })
  return page.responses
}
