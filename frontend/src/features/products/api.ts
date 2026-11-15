import { apiRequest } from '../../lib/api'
import { formatNumber } from '../../lib/format'

/** As the API judges a stock: `Low` is 1 up to the product's minimum, `Empty` exactly 0, `Negative` below 0. */
export type StockStatus = 'Normal' | 'Low' | 'Empty' | 'Negative'

export type Product = {
  id: string
  name: string
  brand: string
  measure: number
  measureUnit: { name: string }
  price: number
  stock: { quantity: number; status: StockStatus }
}

/** The brand and size, as in "Indofood · 85 g". A measure of 1 is left out: "Danone · botol". */
export function describeProduct({ brand, measure, measureUnit }: Pick<Product, 'brand' | 'measure' | 'measureUnit'>) {
  const size = measure === 1 ? measureUnit.name : `${formatNumber(measure)} ${measureUnit.name}`
  return `${brand} · ${size}`
}

/** What a screen that picks products needs to know about one. */
export type ProductSummary = Pick<Product, 'id' | 'name' | 'price' | 'stock'>

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
