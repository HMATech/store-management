import { useQuery } from '@tanstack/react-query'
import { ShoppingCart } from 'lucide-react'
import { CardList, CardRow } from '../../components/Card'
import { IconChip } from '../../components/IconChip'
import { Empty, LoadFailed, Loading } from '../../components/LoadState'
import { formatOccurrence, formatRupiah } from '../../lib/format'
import { fetchRecentSales } from './api'

/** The latest sales. Rows mirror the Transaction row component in Loom (Kind Sale). */
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
        <CardRow key={sale.id}>
          <IconChip icon={ShoppingCart} />
          <div className="flex min-w-0 flex-1 flex-col gap-2">
            <span className="font-semibold">{sale.quantity} barang</span>
            <span className="text-xs text-text-muted">{formatOccurrence(new Date(sale.occurrenceTime))}</span>
          </div>
          <span className="shrink-0 font-bold text-primary-default">{formatRupiah(sale.total)}</span>
        </CardRow>
      ))}
    </CardList>
  )
}
