import { useMemo, useState } from 'react'
import type { SaleLine, SaleProduct } from './api'

/** The most of one product a single sale can hold. Keeps a stuck finger from recording thousands. */
export const MaxQuantity = 999

/** The products picked for the sale being recorded, in the order they were added. */
export function useCart() {
  const [lines, setLines] = useState<SaleLine[]>([])

  return useMemo(
    () => ({
      lines,
      totalQuantity: lines.reduce((sum, line) => sum + line.quantity, 0),
      totalPrice: lines.reduce((sum, line) => sum + line.quantity * line.product.price, 0),
      quantityOf: (productId: string) => lines.find((line) => line.product.id === productId)?.quantity ?? 0,
      /** Puts one of `product` in the cart, or adds one more if it is already there. */
      add: (product: SaleProduct) =>
        setLines((current) =>
          current.some((line) => line.product.id === product.id)
            ? withQuantityChanged(current, product.id, 1)
            : [...current, { product, quantity: 1 }],
        ),
      increase: (productId: string) => setLines((current) => withQuantityChanged(current, productId, 1)),
      /** Takes one away. The product leaves the cart when none are left. */
      decrease: (productId: string) => setLines((current) => withQuantityChanged(current, productId, -1)),
      clear: () => setLines([]),
    }),
    [lines],
  )
}

export type Cart = ReturnType<typeof useCart>

function withQuantityChanged(lines: SaleLine[], productId: string, change: number): SaleLine[] {
  return lines
    .map((line) =>
      line.product.id === productId
        ? { ...line, quantity: Math.min(line.quantity + change, MaxQuantity) }
        : line,
    )
    .filter((line) => line.quantity > 0)
}
