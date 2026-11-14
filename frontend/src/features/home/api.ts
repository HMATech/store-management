import { apiRequest } from '../../lib/api'
import { deviceTimeZone, toIsoDay } from '../../lib/format'

export type DailyTotals = {
  salesTotal: number
  salesCount: number
  purchasesTotal: number
  purchasesCount: number
}

export type StockAlert = {
  id: string
  name: string
  stock: { quantity: number; status: 'Negative' | 'Low' }
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

/** The sales and purchase totals of one day, counted in the device's time zone. */
export function fetchTodayTotals(today: Date, signal?: AbortSignal): Promise<DailyTotals> {
  const day = toIsoDay(today)
  const query = new URLSearchParams({ from: day, to: day, timeZone: deviceTimeZone })
  return apiRequest<DailyTotals>(`/reports/daily-totals?${query}`, { signal })
}

/** The products whose stock needs a look: those below zero first, then those running low. */
export async function fetchStockAlerts(signal?: AbortSignal): Promise<StockAlert[]> {
  const fetchByStatus = (status: StockAlert['stock']['status']) =>
    apiRequest<Paged<StockAlert>>(`/products?stockStatus=${status}&pageSize=${ListSize}`, { signal })

  const [negative, low] = await Promise.all([fetchByStatus('Negative'), fetchByStatus('Low')])
  return [...negative.responses, ...low.responses].slice(0, ListSize)
}

/** The latest sales, newest first. */
export async function fetchRecentSales(signal?: AbortSignal): Promise<RecentSale[]> {
  const page = await apiRequest<Paged<RecentSale>>(`/transactions?kind=Sale&pageSize=${ListSize}`, { signal })
  return page.responses
}
