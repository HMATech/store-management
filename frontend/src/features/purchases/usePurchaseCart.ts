import { useMemo, useState } from 'react'
import type { ProductSummary } from '../products/api'
import type { CostedPurchaseLine, PurchaseLine } from './api'

/** The most of one product a single purchase can hold. */
export const MaxQuantity = 9999

/** The products picked for the purchase being recorded, in the order they were added. */
export function usePurchaseCart() {
  const [lines, setLines] = useState<PurchaseLine[]>([])

  return useMemo(() => {
    const change = (productId: string, update: (line: PurchaseLine) => PurchaseLine) =>
      setLines((current) =>
        current.map((line) => (line.product.id === productId ? update(line) : line)).filter((line) => line.quantity > 0),
      )
    const step = (productId: string, by: number) =>
      change(productId, (line) => ({ ...line, quantity: Math.min(line.quantity + by, MaxQuantity) }))

    return {
      lines,
      totalQuantity: lines.reduce((sum, line) => sum + line.quantity, 0),
      /** What the purchase costs so far. A line whose cost isn't filled in yet counts as nothing. */
      totalCost: lines.reduce((sum, line) => sum + line.quantity * (line.unitCost ?? 0), 0),
      /** The lines ready to save, or null while any of them still lacks its cost. */
      costedLines: lines.every(hasCost) ? lines : null,
      lineOf: (productId: string) => lines.find((line) => line.product.id === productId),
      /** Puts one of `product` in the purchase, with its cost still to be filled in. */
      add: (product: ProductSummary) =>
        setLines((current) =>
          current.some((line) => line.product.id === product.id)
            ? current
            : [...current, { product, quantity: 1, unitCost: null }],
        ),
      increase: (productId: string) => step(productId, 1),
      /** Takes one away. The product leaves the purchase when none are left. */
      decrease: (productId: string) => step(productId, -1),
      /** Sets how many, between 1 and the most a purchase can hold. */
      setQuantity: (productId: string, quantity: number) =>
        change(productId, (line) => ({ ...line, quantity: Math.max(1, Math.min(quantity, MaxQuantity)) })),
      setUnitCost: (productId: string, unitCost: number | null) => change(productId, (line) => ({ ...line, unitCost })),
      clear: () => setLines([]),
    }
  }, [lines])
}

export type PurchaseCart = ReturnType<typeof usePurchaseCart>

function hasCost(line: PurchaseLine): line is CostedPurchaseLine {
  return line.unitCost !== null
}
