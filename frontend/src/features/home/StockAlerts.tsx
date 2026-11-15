import { useQuery } from '@tanstack/react-query'
import { TriangleAlert } from 'lucide-react'
import { Badge } from '../../components/Badge'
import { CardList, CardRow } from '../../components/Card'
import { IconChip } from '../../components/IconChip'
import { Empty, LoadFailed, Loading } from '../../components/LoadState'
import { fetchStockAlerts, type StockAlert } from './api'

/** The products whose stock is below zero, run out or running low. Rows mirror the Stock alert row component in Loom. */
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
  const { tone, badge, note } = describe(quantity, status)

  return (
    <CardRow>
      <IconChip icon={TriangleAlert} tone={tone} />
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="font-semibold break-words">{alert.name}</span>
        <span className="text-xs text-text-muted">{note}</span>
      </div>
      <Badge tone={tone}>{badge}</Badge>
    </CardRow>
  )
}

function describe(quantity: number, status: StockAlert['stock']['status']) {
  if (status === 'Negative') {
    return { tone: 'danger', badge: `Minus ${Math.abs(quantity)}`, note: 'Stok minus, perlu dihitung' } as const
  }
  if (status === 'Empty') {
    return { tone: 'danger', badge: 'Habis', note: 'Stok habis, perlu dibeli' } as const
  }
  return { tone: 'warning', badge: `Sisa ${quantity}`, note: 'Hampir habis' } as const
}
