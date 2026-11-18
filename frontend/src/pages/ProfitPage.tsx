import { useQuery } from '@tanstack/react-query'
import clsx from 'clsx'
import { Info } from 'lucide-react'
import { Card } from '../components/Card'
import { InfoNote } from '../components/InfoNote'
import { LoadFailed, Loading } from '../components/LoadState'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { fetchDailyTotals, profitPerHundred, type DailyTotals, type DayTotals } from '../features/reports/api'
import { IncompleteProfit } from '../features/reports/IncompleteProfit'
import { formatDayHeading, formatDayRange, formatRupiah, toIsoDay } from '../lib/format'
import { useToday } from '../lib/useToday'

/** How many days the screen covers, today included. */
const Days = 7

/** A day from the API (`2026-11-18`) as a local date. */
const toDate = (day: string) => {
  const [year, month, date] = day.split('-').map(Number)
  return new Date(year, month - 1, date)
}

/**
 * Profit over the last seven days: the week's total, then each day newest first. Mirrors boards 15 and D15 in
 * Loom. The days' list is the wide column on wide screens.
 */
export function ProfitPage() {
  const today = useToday()
  const from = new Date(today.getFullYear(), today.getMonth(), today.getDate() - (Days - 1))
  const totals = useQuery({
    queryKey: ['daily-totals', toIsoDay(from), toIsoDay(today)],
    queryFn: ({ signal }) => fetchDailyTotals(from, today, signal),
  })

  return (
    <Screen title="Untung" backTo="/">
      <ScreenBody>
        {totals.isPending ? (
          <Loading />
        ) : totals.isError ? (
          <LoadFailed onRetry={() => void totals.refetch()} />
        ) : (
          <div
            className={
              'grid gap-16 lg:grid-cols-[minmax(0,1fr)_380px] lg:grid-rows-[auto_1fr] lg:items-start lg:gap-24'
            }
          >
            <WeekSummary totals={totals.data} from={from} to={today} />
            <div className="lg:col-start-1 lg:row-span-2 lg:row-start-1">
              <DayList totals={totals.data} today={today} />
            </div>
            <div className="lg:col-start-2">
              <InfoNote icon={Info}>
                Untung dihitung dari harga jual dikurangi modal rata-rata tiap barang. Barang yang belum ada catatan
                belanjanya belum dihitung.
              </InfoNote>
            </div>
          </div>
        )}
      </ScreenBody>
    </Screen>
  )
}

function WeekSummary({ totals, from, to }: { totals: DailyTotals; from: Date; to: Date }) {
  const perHundred = profitPerHundred(totals)

  return (
    <Card className="flex flex-col gap-4 p-16 lg:col-start-2 lg:p-20">
      <h2 className="font-semibold">Untung {Days} hari terakhir</h2>
      <p className="text-xs text-text-muted">{formatDayRange(from, to)}</p>
      <p
        className={clsx(
          'text-5xl font-extrabold break-words',
          totals.grossProfit < 0 ? 'text-danger-default' : 'text-primary-default',
        )}
      >
        {formatRupiah(totals.grossProfit)}
      </p>
      {perHundred !== null && (
        <p className="text-sm font-medium">
          {perHundred < 0
            ? `Rata-rata rugi ${formatRupiah(-perHundred)} dari tiap Rp 100 penjualan`
            : `Rata-rata untung ${formatRupiah(perHundred)} dari tiap Rp 100 penjualan`}
        </p>
      )}
      {totals.uncostedQuantity > 0 && (
        <div className="pt-8">
          <IncompleteProfit quantity={totals.uncostedQuantity} />
        </div>
      )}
    </Card>
  )
}

function DayList({ totals, today }: { totals: DailyTotals; today: Date }) {
  const newestFirst = [...totals.days].sort((a, b) => b.date.localeCompare(a.date))

  return (
    <Card className="flex flex-col gap-12 p-16 lg:p-20">
      <h2 className="font-bold">Per hari</h2>
      <ul className="flex flex-col gap-12">
        {newestFirst.map((day) => (
          <DayRow key={day.date} day={day} today={today} />
        ))}
      </ul>
      <p className="flex items-center justify-between gap-12 border-t-thin border-border-default pt-12">
        <span className="font-bold">Total {Days} hari</span>
        <span className="text-xl font-extrabold">{formatRupiah(totals.grossProfit)}</span>
      </p>
    </Card>
  )
}

function DayRow({ day, today }: { day: DayTotals; today: Date }) {
  return (
    <li className="flex items-center gap-12">
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="font-semibold break-words">{formatDayHeading(toDate(day.date), today)}</span>
        <span className="text-xs text-text-muted">
          {day.salesCount === 0 ? 'Belum ada penjualan' : `Terjual ${formatRupiah(day.salesTotal)}`}
        </span>
        {day.uncostedQuantity > 0 && (
          <span className="text-xs text-warning-default">{day.uncostedQuantity} barang belum dihitung</span>
        )}
      </div>
      <span className={clsx('shrink-0 font-bold', day.grossProfit < 0 && 'text-danger-default')}>
        {formatRupiah(day.grossProfit)}
      </span>
    </li>
  )
}
