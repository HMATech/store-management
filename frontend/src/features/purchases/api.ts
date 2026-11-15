import { apiRequest } from '../../lib/api'
import type { ProductSummary } from '../products/api'

export type PurchaseLine = {
  product: ProductSummary
  quantity: number
  /** What one of it cost at the supplier, or null until it is filled in. */
  unitCost: number | null
}

/** A line whose cost has been filled in, so it can be saved. */
export type CostedPurchaseLine = PurchaseLine & { unitCost: number }

/**
 * Records a purchase of `lines` from `supplier`, happening now. Sending the same `id` again returns the
 * purchase already recorded, so a retry can't record it twice.
 */
export function createPurchase(supplier: string, lines: CostedPurchaseLine[], id: string): Promise<void> {
  return apiRequest<void>('/purchases', {
    method: 'POST',
    body: {
      id,
      title: supplier,
      tags: [],
      productEntries: lines.map(({ product, quantity, unitCost }) => ({ id: product.id, quantity, unitCost })),
      occurrenceTime: new Date().toISOString(),
    },
  })
}
