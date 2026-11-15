import { apiRequest } from '../../lib/api'
import type { Page } from '../../lib/usePagedList'
import type { Product, StockStatus } from './api'

/** A product as its own screen shows it. */
export type ProductDetail = Product & { tags: string[] }

/** Where a product's stock comes from: what was bought, what was sold, and what was corrected by hand. */
export type StockBreakdown = {
  purchased: number
  sold: number
  /** The sum of the corrections; negative when more was taken off than added. */
  adjusted: number
  quantity: number
  status: StockStatus
}

/** A selling price and the time it was, or is, in force. */
export type PriceLine = {
  id: string
  value: number
  validFromTime: string
  /** When the next price replaced it; null for the price in force now. */
  validToTime: string | null
}

/** How many past prices one request loads. More are loaded on demand. */
const PricePageSize = 10

export function fetchProduct(id: string, signal?: AbortSignal): Promise<ProductDetail> {
  return apiRequest<ProductDetail>(`/products/${id}`, { signal })
}

export function fetchStockBreakdown(productId: string, signal?: AbortSignal): Promise<StockBreakdown> {
  return apiRequest<StockBreakdown>(`/stocks/${productId}`, { signal })
}

/** One page of a product's prices, the current one first. */
export function fetchPriceHistory(productId: string, page: number, signal?: AbortSignal): Promise<Page<PriceLine>> {
  const query = new URLSearchParams({
    productId,
    sortOrder: 'DESC',
    page: String(page),
    pageSize: String(PricePageSize),
  })
  return apiRequest<Page<PriceLine>>(`/products/prices?${query}`, { signal })
}

/** Gives the product a new selling price from now on. Sales already recorded keep the price they were made at. */
export function changePrice(productId: string, value: number): Promise<void> {
  return apiRequest<void>(`/products/${productId}/prices`, { method: 'POST', body: { value } })
}
