import { toIsoDay } from '../../lib/format'
import type { Transaction } from './api'

export type TransactionDay = {
  /** The local calendar day, as `2026-11-08`. */
  day: string
  date: Date
  transactions: Transaction[]
  salesTotal: number
  purchasesTotal: number
}

/** Groups transactions by the local day they happened on, keeping their order. */
export function groupByDay(transactions: Transaction[]): TransactionDay[] {
  const days = new Map<string, TransactionDay>()
  for (const transaction of transactions) {
    const date = new Date(transaction.occurrenceTime)
    const day = toIsoDay(date)
    const group = days.get(day) ?? { day, date, transactions: [], salesTotal: 0, purchasesTotal: 0 }
    group.transactions.push(transaction)
    if (transaction.kind === 'Sale') {
      group.salesTotal += transaction.total
    } else {
      group.purchasesTotal += transaction.total
    }
    days.set(day, group)
  }
  return [...days.values()]
}
