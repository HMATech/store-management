import { useQuery } from '@tanstack/react-query'
import { TriangleAlert } from 'lucide-react'
import { Badge } from '../../components/Badge'
import { CardList, CardRow } from '../../components/Card'
import { IconChip } from '../../components/IconChip'
import { Empty, LoadFailed, Loading } from '../../components/LoadState'
import { fetchStockAlerts, type StockAlert } from './api'

/** The products whose stock is below zero or running low. Rows mirror the Stock alert row component in Loom. */
export function StockAlerts() {
  const alerts = useQuery({ queryKey: ['stock-alerts'], queryFn: ({ signal }) => fetchStockAlerts(signal) })

  if (alerts.isPending) {
    return <Loading />
  }
  if (alerts.isError) {
    return <LoadFailed onRetry={() => void alerts.refetch()} />
  }
  if (alerts.data.length === 0) {
    return <Empty>Semua stok aman.</Empty>
  }

  return (
    <CardList>
      {alerts.data.map((alert) => (
        <StockAlertRow key={alert.id} alert={alert} />
      ))}
    </CardList>
  )
}

function StockAlertRow({ alert }: { alert: StockAlert }) {
  const { quantity, status } = alert.stock
  const isNegative = status === 'Negative'
  const tone = isNegative ? 'danger' : 'warning'

  return (
    <CardRow>
      <IconChip icon={TriangleAlert} tone={tone} />
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="font-semibold break-words">{alert.name}</span>
        <span className="text-xs text-text-muted">{describe(quantity, isNegative)}</span>
      </div>
      <Badge tone={tone}>{isNegative ? `Minus ${Math.abs(quantity)}` : `Sisa ${quantity}`}</Badge>
    </CardRow>
  )
}

function describe(quantity: number, isNegative: boolean): string {
  if (isNegative) {
    return 'Stok minus, perlu dihitung'
  }
  return quantity === 0 ? 'Habis' : 'Hampir habis'
}
