import { ChevronRight } from 'lucide-react'
import { Badge } from '../../components/Badge'
import { CardLinkRow } from '../../components/Card'
import { formatRupiah } from '../../lib/format'
import type { Product } from './api'

/** A product in the product list, opening its detail. Mirrors the Product row component in Loom (Stock). */
export function ProductRow({ product }: { product: Product }) {
  return (
    <CardLinkRow to={`/produk/${product.id}`}>
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="font-semibold break-words">{product.name}</span>
        <span className="text-xs break-words text-text-muted">{describe(product)}</span>
      </div>
      <div className="flex shrink-0 flex-col items-end gap-6">
        <span className="text-lg font-bold">{formatRupiah(product.price)}</span>
        <StockBadge stock={product.stock} />
      </div>
      <ChevronRight aria-hidden className="size-20 shrink-0 text-text-subtle" />
    </CardLinkRow>
  )
}

/** The brand and size, as in "Indofood · 85 g". A measure of 1 is left out: "Danone · botol". */
function describe({ brand, measure, measureUnit }: Product): string {
  const size = measure === 1 ? measureUnit.name : `${measure.toLocaleString('id-ID')} ${measureUnit.name}`
  return `${brand} · ${size}`
}

function StockBadge({ stock }: { stock: Product['stock'] }) {
  if (stock.status === 'Negative') {
    return <Badge tone="danger">Minus {Math.abs(stock.quantity)}</Badge>
  }
  if (stock.status === 'Low') {
    return <Badge tone="warning">{stock.quantity === 0 ? 'Habis' : `Sisa ${stock.quantity}`}</Badge>
  }
  return <Badge tone="neutral">Stok {stock.quantity}</Badge>
}
