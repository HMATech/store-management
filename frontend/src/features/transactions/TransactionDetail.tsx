import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import clsx from 'clsx'
import { ClipboardList, ReceiptText, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { Button, ButtonLink } from '../../components/Button'
import { Card } from '../../components/Card'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { EmptyState } from '../../components/EmptyState'
import { LoadFailed, Loading } from '../../components/LoadState'
import { Notice } from '../../components/Notice'
import { Screen, ScreenBody } from '../../components/shell/Screen'
import { ApiError } from '../../lib/api'
import { formatLongDate, formatRupiah, formatTime } from '../../lib/format'

/** A recorded sale or purchase, in the shape its detail screen shows. */
export type RecordedTransaction = {
  id: string
  /** Where the goods came from, for a purchase. */
  supplier?: string
  occurrenceTime: string
  /** When it was last changed. An edit whose answer was lost compares it to tell whether the edit got through. */
  lastUpdatedTime: string
  total: number
  entries: RecordedEntry[]
}

/** One line of a recorded sale or purchase: a product at the price it was sold for or the cost paid. */
export type RecordedEntry = {
  id: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  total: number
}

/** What differs between the sale and purchase detail screens: their words, and how they reach the API. */
export type TransactionKindText = {
  /** "Penjualan" or "Belanja". */
  noun: string
  /** The query key and request that load one. */
  queryKey: string
  fetch: (id: string, signal: AbortSignal) => Promise<RecordedTransaction>
  remove: (id: string) => Promise<void>
  /** Where its items are changed. */
  editPath: (id: string) => string
  /** What deleting does to stock: "Stok barangnya akan dikembalikan." */
  stockOnDelete: string
  /** Whether the total is money in, shown in the primary colour, or money out. */
  isIncome: boolean
}

/**
 * Detail Penjualan and Detail Belanja: what one sale or purchase held, and the ways to correct its items or
 * remove it when it was recorded by mistake. Mirrors the "10 · Detail Penjualan" and "10b · Detail Belanja"
 * boards in Loom.
 */
export function TransactionDetail({ kind }: { kind: TransactionKindText }) {
  const { id = '' } = useParams()
  const transaction = useQuery({ queryKey: [kind.queryKey, id], queryFn: ({ signal }) => kind.fetch(id, signal) })
  const lower = kind.noun.toLowerCase()

  return (
    <Screen title={`Detail ${kind.noun}`} backTo="/riwayat">
      <ScreenBody>
        {transaction.isPending ? (
          <Loading />
        ) : transaction.isError ? (
          transaction.error instanceof ApiError
          && (transaction.error.status === 404 || transaction.error.status === 400) ? (
            <EmptyState
              icon={ReceiptText}
              title={`${kind.noun} tidak ditemukan`}
              action={<ButtonLink to="/riwayat">Kembali ke Riwayat</ButtonLink>}
            >
              {kind.noun} ini mungkin sudah dihapus.
            </EmptyState>
          ) : (
            <LoadFailed onRetry={() => void transaction.refetch()} />
          )
        ) : (
          <div className="flex flex-col gap-16 lg:gap-24">
            {/* Set by Ubah Barang after saving. */}
            <Notice />
            <div className="grid gap-16 lg:grid-cols-[minmax(0,1fr)_380px] lg:items-start lg:gap-24">
              <Summary transaction={transaction.data} isIncome={kind.isIncome} />
              {/* The wide column on wide screens; between the summary and the actions on phones. */}
              <div className="lg:col-start-1 lg:row-span-2 lg:row-start-1">
                <Items transaction={transaction.data} />
              </div>
              <div className="flex flex-col gap-16 lg:col-start-2">
                <ButtonLink kind="secondary" to={kind.editPath(transaction.data.id)}>
                  <ClipboardList aria-hidden className="size-24" />
                  Ubah Barang
                </ButtonLink>
                <DeleteTransaction transaction={transaction.data} kind={kind} lower={lower} />
              </div>
            </div>
          </div>
        )}
      </ScreenBody>
    </Screen>
  )
}

function Summary({ transaction, isIncome }: { transaction: RecordedTransaction; isIncome: boolean }) {
  const when = new Date(transaction.occurrenceTime)
  const quantity = transaction.entries.reduce((sum, entry) => sum + entry.quantity, 0)

  return (
    <Card className="flex flex-col gap-4 p-16 lg:col-start-2 lg:p-20">
      {transaction.supplier !== undefined && (
        <p className="font-semibold break-words">Dari {transaction.supplier}</p>
      )}
      <p className="text-xs text-text-muted">
        {formatLongDate(when)} · pukul {formatTime(when)}
      </p>
      <p className={clsx('text-5xl font-extrabold', isIncome && 'text-primary-default')}>
        {formatRupiah(transaction.total)}
      </p>
      <p className="text-sm font-medium">
        {transaction.entries.length} jenis barang · {quantity} item
      </p>
    </Card>
  )
}

function Items({ transaction }: { transaction: RecordedTransaction }) {
  return (
    <Card className="flex flex-col gap-12 p-16 lg:p-20">
      <h2 className="font-bold">Barang yang dibeli</h2>
      <ul className="flex flex-col gap-12">
        {transaction.entries.map((entry) => (
          <li key={entry.id} className="flex items-center gap-12">
            <div className="flex min-w-0 flex-1 flex-col gap-2">
              <span className="font-semibold break-words">{entry.productName}</span>
              <span className="text-xs text-text-muted">
                {entry.quantity} × {formatRupiah(entry.unitPrice)}
              </span>
            </div>
            <span className="shrink-0 font-bold">{formatRupiah(entry.total)}</span>
          </li>
        ))}
      </ul>
      <p className="flex items-center justify-between gap-12 border-t-thin border-border-default pt-12">
        <span className="font-bold">Total</span>
        <span className="text-xl font-extrabold">{formatRupiah(transaction.total)}</span>
      </p>
    </Card>
  )
}

type DeleteTransactionProps = { transaction: RecordedTransaction; kind: TransactionKindText; lower: string }

function DeleteTransaction({ transaction, kind, lower }: DeleteTransactionProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isAsking, setIsAsking] = useState(false)

  const remove = useMutation({
    mutationFn: () => kind.remove(transaction.id),
    onSuccess: () => {
      // The history, today's totals and the stock of what it held all changed.
      void queryClient.invalidateQueries()
      void navigate('/riwayat', { state: { notice: `${kind.noun} sudah dihapus.` } })
    },
  })

  function stopAsking() {
    setIsAsking(false)
    remove.reset()
  }

  const from = transaction.supplier === undefined ? '' : ` dari ${transaction.supplier}`

  return (
    <div className="flex flex-col">
      <Button kind="danger" onClick={() => setIsAsking(true)}>
        <Trash2 aria-hidden className="size-24" />
        Hapus {kind.noun}
      </Button>
      <ConfirmDialog
        isOpen={isAsking}
        title={`Hapus ${lower} ini?`}
        confirmLabel="Ya, Hapus"
        confirmIcon={Trash2}
        isWorking={remove.isPending}
        workingLabel="Menghapus…"
        failure={remove.isError ? describeDeleteFailure(remove.error, kind.noun, lower) : undefined}
        onConfirm={() => remove.mutate()}
        onCancel={stopAsking}
      >
        {kind.noun} {formatRupiah(transaction.total)}
        {from} pukul {formatTime(new Date(transaction.occurrenceTime))} akan dihapus. {kind.stockOnDelete}
      </ConfirmDialog>
    </div>
  )
}

function describeDeleteFailure(error: unknown, noun: string, lower: string): string {
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return `Tidak bisa terhubung, ${lower} belum dihapus. Periksa internet, lalu coba lagi.`
    }
    if (error.status === 404) {
      return `${noun} ini sudah dihapus. Kembali ke Riwayat.`
    }
  }
  return `${noun} belum dihapus. Coba lagi.`
}
