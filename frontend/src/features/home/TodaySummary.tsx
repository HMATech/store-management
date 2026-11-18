import { useQuery } from '@tanstack/react-query'
import clsx from 'clsx'
import { ChevronRight } from 'lucide-react'
import { Link } from 'react-router'
import { Card } from '../../components/Card'
import { LoadFailed, Loading } from '../../components/LoadState'
import { formatRupiah, toIsoDay } from '../../lib/format'
import { useToday } from '../../lib/useToday'
import { IncompleteProfit } from '../reports/IncompleteProfit'
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

  const { salesTotal, salesCount, purchasesTotal, purchasesCount, grossProfit, uncostedQuantity } = totals.data

  return (
    <Card className="flex flex-col gap-12 p-16">
      <h2 className="text-sm font-semibold text-text-muted">Hari ini</h2>
      <dl className="flex gap-12">
        <Amount label="Uang masuk" amount={salesTotal} note={`dari ${salesCount} penjualan`} emphasized />
        <div aria-hidden className="w-px bg-border-default" />
        <Amount label="Uang keluar" amount={purchasesTotal} note={`dari ${purchasesCount} belanja`} />
      </dl>
      <div className="flex items-center gap-12 border-t-thin border-border-default pt-12">
        <dl className="flex min-w-0 flex-1">
          <Amount label="Untung" amount={grossProfit} note="setelah dikurangi modal" emphasized />
        </dl>
        <Link
          to="/profit"
          className={
            'flex min-h-touch-min shrink-0 items-center gap-2 rounded-sm pl-8 text-sm font-semibold'
            + ' text-primary-default focus-visible:outline-2 focus-visible:outline-primary-default'
          }
        >
          Lihat 7 hari
          <ChevronRight aria-hidden className="size-20" />
        </Link>
      </div>
      {uncostedQuantity > 0 && <IncompleteProfit quantity={uncostedQuantity} />}
    </Card>
  )
}

type AmountProps = { label: string; amount: number; note: string; emphasized?: boolean }

function Amount({ label, amount, note, emphasized = false }: AmountProps) {
  return (
    <div className="flex min-w-0 flex-1 flex-col gap-2">
      <dt className="text-xs text-text-muted">{label}</dt>
      <dd className="flex flex-col gap-2">
        <span
          className={clsx(
            'text-2xl font-bold break-words',
            amount < 0 ? 'text-danger-default' : emphasized && 'text-primary-default',
          )}
        >
          {formatRupiah(amount)}
        </span>
        <span className="text-xs text-text-muted">{note}</span>
      </dd>
    </div>
  )
}
