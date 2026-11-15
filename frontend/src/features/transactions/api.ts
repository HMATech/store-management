import { apiRequest } from '../../lib/api'
import type { Page } from '../../lib/usePagedList'

export type TransactionKind = 'Sale' | 'Purchase'

export type Transaction = {
  id: string
  kind: TransactionKind
  occurrenceTime: string
  /** How many units were sold or bought, over all of its products. */
  quantity: number
  /** What it is worth: the selling price of a sale, the cost of a purchase. */
  total: number
}

/** How many transactions one request loads. More are loaded on demand. */
const PageSize = 50

/** One page of the history, newest first, optionally of one kind only. */
export function fetchTransactions(
  kind: TransactionKind | undefined,
  page: number,
  signal?: AbortSignal,
): Promise<Page<Transaction>> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(PageSize) })
  if (kind !== undefined) {
    query.set('kind', kind)
  }
  return apiRequest<Page<Transaction>>(`/transactions?${query}`, { signal })
}
