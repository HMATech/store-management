import { useQuery } from '@tanstack/react-query'
import { ChevronRight, ShoppingCart } from 'lucide-react'
import { CardLinkRow, CardList } from '../../components/Card'
import { IconChip } from '../../components/IconChip'
import { Empty, LoadFailed, Loading } from '../../components/LoadState'
import { formatOccurrence, formatRupiah } from '../../lib/format'
import { saleDetailPath } from '../sales/detail'
import { fetchRecentSales } from './api'

/** The latest sales, each opening its detail. Rows mirror the Transaction row component in Loom (Kind Sale). */
export function RecentSales() {
  const sales = useQuery({ queryKey: ['recent-sales'], queryFn: ({ signal }) => fetchRecentSales(signal) })

  if (sales.isPending) {
    return <Loading />
  }
  if (sales.isError) {
    return <LoadFailed onRetry={() => void sales.refetch()} />
  }
  if (sales.data.length === 0) {
    return <Empty>Belum ada penjualan.</Empty>
  }

  return (
    <CardList>
      {sales.data.map((sale) => (
        <CardLinkRow key={sale.id} to={saleDetailPath(sale.id)}>
          <IconChip icon={ShoppingCart} />
          <div className="flex min-w-0 flex-1 flex-col gap-2">
            <span className="font-semibold">{sale.quantity} barang</span>
            <span className="text-xs text-text-muted">{formatOccurrence(new Date(sale.occurrenceTime))}</span>
          </div>
          <span className="shrink-0 font-bold text-primary-default">{formatRupiah(sale.total)}</span>
          <ChevronRight aria-hidden className="size-20 shrink-0 text-text-subtle" />
        </CardLinkRow>
      ))}
    </CardList>
  )
}
