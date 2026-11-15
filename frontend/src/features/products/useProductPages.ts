import { usePagedList } from '../../lib/usePagedList'
import { fetchProducts, type ProductFilter } from './api'

/** The products matching `filter`, loaded a page at a time. `products` is undefined until they have loaded. */
export function useProductPages(filter: ProductFilter) {
  const { items, ...list } = usePagedList(['products', filter.search, filter.stockStatus ?? 'all'], (page, signal) =>
    fetchProducts(filter, page, signal),
  )
  return { products: items, ...list }
}
