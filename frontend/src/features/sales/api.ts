import { apiRequest } from '../../lib/api'
import type { Product } from '../products/api'

/** What a sale needs to know about a product. */
export type SaleProduct = Pick<Product, 'id' | 'name' | 'price' | 'stock'>

export type SaleLine = { product: SaleProduct; quantity: number }

/** Records a sale of `lines` happening now. The API prices each line at the product's current price. */
export function createSale(lines: SaleLine[]): Promise<void> {
  return apiRequest<void>('/sales', {
    method: 'POST',
    body: {
      title: '',
      tags: [],
      productEntries: lines.map(({ product, quantity }) => ({ id: product.id, quantity })),
      occurrenceTime: new Date().toISOString(),
    },
  })
}
