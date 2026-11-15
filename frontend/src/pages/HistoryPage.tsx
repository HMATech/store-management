import { Plus, ReceiptText } from 'lucide-react'
import { useSearchParams } from 'react-router'
import { ButtonLink } from '../components/Button'
import { CardList } from '../components/Card'
import { EmptyState } from '../components/EmptyState'
import { LoadMoreButton } from '../components/LoadMoreButton'
import { Empty, LoadFailed, Loading } from '../components/LoadState'
import { SegmentedControl } from '../components/SegmentedControl'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { fetchTransactions, type TransactionKind } from '../features/transactions/api'
import { groupByDay, type TransactionDay } from '../features/transactions/groupByDay'
import { TransactionRow } from '../features/transactions/TransactionRow'
import { formatDayHeading, formatRupiah } from '../lib/format'
import { usePagedList } from '../lib/usePagedList'
import { useToday } from '../lib/useToday'

/** The kinds the history can be narrowed to, by the value each has in the address (`/riwayat?jenis=belanja`). */
const kindFilters = {
  penjualan: { label: 'Penjualan', kind: 'Sale', whenEmpty: 'Belum ada penjualan.' },
  belanja: { label: 'Belanja', kind: 'Purchase', whenEmpty: 'Belum ada belanja.' },
} as const satisfies Record<string, { label: string; kind: TransactionKind; whenEmpty: string }>

type KindFilterKey = keyof typeof kindFilters
type KindChoice = KindFilterKey | 'semua'

const isKindFilterKey = (value: string | null): value is KindFilterKey => value !== null && value in kindFilters

const kindOptions: { value: KindChoice; label: string }[] = [
  { value: 'semua', label: 'Semua' },
  ...Object.entries(kindFilters).map(([value, { label }]) => ({ value: value as KindFilterKey, label })),
]

/** Riwayat: every sale and purchase, newest first and grouped by day. */
export function HistoryPage() {
  const [params, setParams] = useSearchParams()
  const kindParam = params.get('jenis')
  const choice: KindChoice = isKindFilterKey(kindParam) ? kindParam : 'semua'

  return (
    <Screen title="Riwayat" subtitle="Semua penjualan dan belanja">
      <ScreenBody>
        <div className="flex flex-col gap-14 lg:gap-24">
          <SegmentedControl
            label="Pilih jenis transaksi"
            options={kindOptions}
            value={choice}
            onChange={(value) => setParams(value === 'semua' ? {} : { jenis: value }, { replace: true })}
            className="lg:w-420"
          />
          <TransactionDays filter={choice === 'semua' ? undefined : kindFilters[choice]} />
        </div>
      </ScreenBody>
    </Screen>
  )
}

function TransactionDays({ filter }: { filter: (typeof kindFilters)[KindFilterKey] | undefined }) {
  const today = useToday()
  const kind = filter?.kind
  const { items, isError, retry, hasMore, isLoadingMore, loadMore } = usePagedList(
    ['transactions', kind ?? 'all'],
    (page, signal) => fetchTransactions(kind, page, signal),
  )

  if (isError) {
    return <LoadFailed onRetry={retry} />
  }
  if (items === undefined) {
    return <Loading />
  }
  if (items.length === 0) {
    return filter === undefined ? (
      <EmptyState
        icon={ReceiptText}
        title="Belum ada transaksi"
        action={
          <ButtonLink to="/jual">
            <Plus aria-hidden className="size-22" />
            Catat Penjualan
          </ButtonLink>
        }
      >
        Penjualan dan belanja yang dicatat akan muncul di sini.
      </EmptyState>
    ) : (
      <Empty>{filter.whenEmpty}</Empty>
    )
  }

  const days = groupByDay(items)

  return (
    <>
      <div className="grid gap-14 lg:grid-cols-2 lg:items-start lg:gap-24">
        {days.map((day, index) => (
          <section key={day.day} aria-labelledby={`day-${day.day}`} className="flex flex-col gap-14 lg:gap-10">
            <div className="flex items-end justify-between gap-12 px-2 pt-6">
              <h2 id={`day-${day.day}`} className="text-lg font-bold">
                {formatDayHeading(day.date, today)}
              </h2>
              {/* The oldest day on screen may continue on the next page, so its total isn't known yet. */}
              {(!hasMore || index < days.length - 1) && <DayTotal day={day} kind={kind} />}
            </div>
            <CardList>
              {day.transactions.map((transaction) => (
                <TransactionRow key={transaction.id} transaction={transaction} />
              ))}
            </CardList>
          </section>
        ))}
      </div>
      {hasMore && <LoadMoreButton isLoading={isLoadingMore} onLoadMore={loadMore} />}
    </>
  )
}

/** The day's money in, or its money out when only purchases are listed. */
function DayTotal({ day, kind }: { day: TransactionDay; kind: TransactionKind | undefined }) {
  return kind === 'Purchase' ? (
    <span className="shrink-0 text-xs font-semibold text-text-muted">Keluar {formatRupiah(day.purchasesTotal)}</span>
  ) : (
    <span className="shrink-0 text-xs font-semibold text-primary-default">Masuk {formatRupiah(day.salesTotal)}</span>
  )
}
