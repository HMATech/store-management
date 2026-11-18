import { apiRequest } from '../../lib/api'
import { deviceTimeZone, toIsoDay } from '../../lib/format'

/**
 * Money in and out over some days. `costOfSales` is what the items sold cost the store (by moving average), and
 * `grossProfit` is their selling price minus that cost. Both leave out items sold before any purchase of them was
 * recorded; `uncostedQuantity` counts those, so the profit can be shown as incomplete.
 */
export type Totals = {
  salesTotal: number
  salesCount: number
  purchasesTotal: number
  purchasesCount: number
  costOfSales: number
  grossProfit: number
  uncostedQuantity: number
}

/** One day's totals. `date` is the day as `2026-11-18`. */
export type DayTotals = Totals & { date: string }

export type DailyTotals = Totals & { days: DayTotals[] }

const numberFields = [
  'salesTotal',
  'salesCount',
  'purchasesTotal',
  'purchasesCount',
  'costOfSales',
  'grossProfit',
  'uncostedQuantity',
] as const

const hasTotals = (value: unknown): value is Totals =>
  typeof value === 'object'
  && value !== null
  && numberFields.every((field) => typeof (value as Record<string, unknown>)[field] === 'number')

/**
 * The totals of each day from `from` to `to`, both included, counted in the device's time zone. Throws when the
 * answer isn't shaped as expected, so the screen shows that it couldn't load rather than wrong numbers.
 */
export async function fetchDailyTotals(from: Date, to: Date, signal?: AbortSignal): Promise<DailyTotals> {
  const query = new URLSearchParams({ from: toIsoDay(from), to: toIsoDay(to), timeZone: deviceTimeZone })
  const totals = await apiRequest<unknown>(`/reports/daily-totals?${query}`, { signal })

  const days = (totals as { days?: unknown } | null)?.days
  if (!hasTotals(totals) || !Array.isArray(days) || !days.every((day) => hasTotals(day) && 'date' in day)) {
    throw new Error('The daily totals were malformed')
  }
  return totals as DailyTotals
}

/** How much of each Rp 100 sold was profit, rounded; null when nothing with a known cost was sold. */
export function profitPerHundred({ grossProfit, costOfSales }: Pick<Totals, 'grossProfit' | 'costOfSales'>) {
  const costedSales = grossProfit + costOfSales
  return costedSales > 0 ? Math.round((grossProfit / costedSales) * 100) : null
}
