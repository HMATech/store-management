import { TriangleAlert } from 'lucide-react'

/** Says the profit leaves out items sold before any purchase of them was recorded, whose cost isn't known. */
export function IncompleteProfit({ quantity }: { quantity: number }) {
  return (
    <p className="flex items-start gap-12 rounded-md bg-warning-soft p-16 text-sm">
      <TriangleAlert aria-hidden className="size-22 shrink-0 text-warning-default" />
      Belum termasuk {quantity} barang yang belum ada catatan belanjanya.
    </p>
  )
}
