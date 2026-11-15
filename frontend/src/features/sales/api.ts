import { apiRequest } from '../../lib/api'
import type { ProductSummary } from '../products/api'

/** What a sale needs to know about a product. */
export type SaleProduct = ProductSummary

export type SaleLine = { product: SaleProduct; quantity: number }

/**
 * Records a sale of `lines` happening now. The API prices each line at the product's current price. Sending
 * the same `id` again returns the sale already recorded, so a retry can't record it twice.
 */
export function createSale(lines: SaleLine[], id: string): Promise<void> {
  return apiRequest<void>('/sales', {
    method: 'POST',
    body: {
      id,
      title: '',
      tags: [],
      productEntries: lines.map(({ product, quantity }) => ({ id: product.id, quantity })),
      occurrenceTime: new Date().toISOString(),
    },
  })
}
