import { apiRequest } from '../../lib/api'

export type StockStatus = 'Normal' | 'Low' | 'Negative'

export type Product = {
  id: string
  name: string
  brand: string
  measure: number
  measureUnit: { name: string }
  price: number
  stock: { quantity: number; status: StockStatus }
}

export type ProductPage = { responses: Product[]; hasNextPage: boolean; totalCount: number }

/** Which products to list: those whose name contains `search`, and optionally only one stock status. */
export type ProductFilter = { search: string; stockStatus?: StockStatus }

/** How many products one request loads. More are loaded on demand. */
const PageSize = 50

/** One page of products by name. */
export function fetchProducts(filter: ProductFilter, page: number, signal?: AbortSignal): Promise<ProductPage> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(PageSize) })
  if (filter.search !== '') {
    query.set('search', filter.search)
  }
  if (filter.stockStatus !== undefined) {
    query.set('stockStatus', filter.stockStatus)
  }
  return apiRequest<ProductPage>(`/products?${query}`, { signal })
}

/** How many products the store has in all. */
export async function fetchProductCount(signal?: AbortSignal): Promise<number> {
  const page = await apiRequest<ProductPage>('/products?page=1&pageSize=1', { signal })
  return page.totalCount
}
