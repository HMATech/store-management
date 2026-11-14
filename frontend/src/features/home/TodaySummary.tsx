import { useQuery } from '@tanstack/react-query'
import clsx from 'clsx'
import { Card } from '../../components/Card'
import { LoadFailed, Loading } from '../../components/LoadState'
import { formatRupiah, toIsoDay } from '../../lib/format'
import { useToday } from '../../lib/useToday'
import { fetchTodayTotals } from './api'

/** Today's money in and out. Mirrors the Daily summary component in Loom. */
export function TodaySummary() {
  const today = useToday()
  const totals = useQuery({
    queryKey: ['daily-totals', toIsoDay(today)],
    queryFn: ({ signal }) => fetchTodayTotals(today, signal),
  })

  if (totals.isPending) {
    return <Loading />
  }
  if (totals.isError) {
    return <LoadFailed onRetry={() => void totals.refetch()} />
  }

  const { salesTotal, salesCount, purchasesTotal, purchasesCount } = totals.data

  return (
    <Card className="flex flex-col gap-12 p-16">
      <h2 className="text-sm font-semibold text-text-muted">Hari ini</h2>
      <dl className="flex gap-12">
        <Amount label="Uang masuk" amount={salesTotal} note={`dari ${salesCount} penjualan`} emphasized />
        <div aria-hidden className="w-px bg-border-default" />
        <Amount label="Uang keluar" amount={purchasesTotal} note={`dari ${purchasesCount} belanja`} />
      </dl>
    </Card>
  )
}

type AmountProps = { label: string; amount: number; note: string; emphasized?: boolean }

function Amount({ label, amount, note, emphasized = false }: AmountProps) {
  return (
    <div className="flex min-w-0 flex-1 flex-col gap-2">
      <dt className="text-xs text-text-muted">{label}</dt>
      <dd className="flex flex-col gap-2">
        <span className={clsx('text-2xl font-bold break-words', emphasized && 'text-primary-default')}>
          {formatRupiah(amount)}
        </span>
        <span className="text-xs text-text-muted">{note}</span>
      </dd>
    </div>
  )
}
