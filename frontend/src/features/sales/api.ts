import { apiRequest } from '../../lib/api'

export type SaleProduct = {
  id: string
  name: string
  price: number
  stock: { quantity: number; status: 'Normal' | 'Low' | 'Negative' }
}

export type ProductPage = { responses: SaleProduct[]; hasNextPage: boolean }

export type SaleLine = { product: SaleProduct; quantity: number }

/** How many products one request loads. More are loaded on demand. */
const PageSize = 50

/** One page of products by name, optionally only those whose name contains `search`. */
export function fetchProducts(search: string, page: number, signal?: AbortSignal): Promise<ProductPage> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(PageSize) })
  if (search !== '') {
    query.set('search', search)
  }
  return apiRequest<ProductPage>(`/products?${query}`, { signal })
}

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
