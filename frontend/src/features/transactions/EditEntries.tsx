import { useQuery } from '@tanstack/react-query'
import { ReceiptText, Search, TriangleAlert } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useParams } from 'react-router'
import { ButtonLink } from '../../components/Button'
import { CheckoutPanel, type CheckoutLine } from '../../components/CheckoutPanel'
import { EmptyState } from '../../components/EmptyState'
import { InfoNote } from '../../components/InfoNote'
import { LoadFailed, Loading } from '../../components/LoadState'
import { Section } from '../../components/Section'
import { Screen } from '../../components/shell/Screen'
import { TextField } from '../../components/TextField'
import { ApiError } from '../../lib/api'
import { formatLongDate, formatTime } from '../../lib/format'
import type { RecordedTransaction } from './TransactionDetail'

/** What differs between changing a sale's items and a purchase's: their words, and how they reach the API. */
export type EditKindText = {
  /** "Penjualan" or "Belanja". */
  noun: string
  /** The query key of the detail screen, whose data the change makes stale. */
  queryKey: string
  fetch: (id: string, signal?: AbortSignal) => Promise<RecordedTransaction>
  detailPath: (id: string) => string
}

/**
 * Ubah Barang: loads the sale or purchase fresh, then lets `children` change its items. Mirrors the
 * "13 · Ubah Barang Penjualan" and "14 · Ubah Barang Belanja" boards in Loom.
 */
export function EditEntriesScreen({
  kind,
  children,
}: {
  kind: EditKindText
  children: (recorded: RecordedTransaction) => ReactNode
}) {
  const { id = '' } = useParams()
  // Its own key, read fresh every time: a retry compares against when it was last changed, which must be current.
  const transaction = useQuery({
    queryKey: [kind.queryKey, id, 'edit'],
    queryFn: ({ signal }) => kind.fetch(id, signal),
    gcTime: 0,
  })
  const backTo = kind.detailPath(id)

  if (transaction.isPending || transaction.isError) {
    return (
      <Screen title="Ubah Barang" backTo={backTo}>
        <div className="flex flex-1 flex-col px-16 pb-16 lg:px-32 lg:pb-32">
          {transaction.isPending ? (
            <Loading />
          ) : isNotFound(transaction.error) ? (
            <EmptyState
              icon={ReceiptText}
              title={`${kind.noun} tidak ditemukan`}
              action={<ButtonLink to="/riwayat">Kembali ke Riwayat</ButtonLink>}
            >
              {kind.noun} ini mungkin sudah dihapus.
            </EmptyState>
          ) : (
            <LoadFailed onRetry={() => void transaction.refetch()} />
          )}
        </div>
      </Screen>
    )
  }

  return (
    <Screen title="Ubah Barang" subtitle={describe(transaction.data)} backTo={backTo}>
      {children(transaction.data)}
    </Screen>
  )
}

type EditEntriesLayoutProps = {
  kind: EditKindText
  recorded: RecordedTransaction
  /** The recorded lines, as `RecordedLineCard`s. */
  recordedLines: ReactNode
  /** True when every line is removed and nothing is added, which can't be saved. */
  isEmpty: boolean
  /** What changing a line does, said under the lines. */
  note: string
  /** The products that can be added, found by the search text. */
  products: (searchText: string) => ReactNode
  checkoutLines: CheckoutLine[]
  totalQuantity: number
  total: number
  /** Why it can't be or wasn't saved, in words for the user. */
  error?: string
  isSaving: boolean
  onSave: () => void
}

/** The parts every Ubah Barang screen has: the recorded lines, products to add, and the new total. */
export function EditEntriesLayout({
  kind,
  recorded,
  recordedLines,
  isEmpty,
  note,
  products,
  checkoutLines,
  totalQuantity,
  total,
  error,
  isSaving,
  onSave,
}: EditEntriesLayoutProps) {
  const [searchText, setSearchText] = useState('')
  const lower = kind.noun.toLowerCase()

  return (
    <div className="flex flex-1 flex-col lg:flex-row lg:items-start lg:gap-24 lg:px-32 lg:pb-32">
      <div className="flex min-w-0 flex-1 flex-col gap-20 px-16 pb-16 lg:gap-24 lg:p-0">
        <Section title={`Barang di ${lower} ini`}>
          <ul className="grid grid-cols-[repeat(auto-fill,minmax(min(100%,320px),1fr))] items-start gap-14 lg:gap-16">
            {recordedLines}
          </ul>
          {isEmpty && (
            <p className="text-xs text-danger-default">
              Sisakan paling tidak satu barang. Kalau {lower} ini salah catat, hapus dari layar Detail {kind.noun}.
            </p>
          )}
          <InfoNote icon={TriangleAlert}>{note}</InfoNote>
        </Section>
        <Section title="Tambah barang lain">
          <div className="flex flex-col gap-14 lg:gap-16">
            <TextField
              label="Cari nama barang"
              hideLabel
              icon={Search}
              type="search"
              placeholder="Cari nama barang…"
              enterKeyHint="search"
              autoCorrect="off"
              value={searchText}
              onChange={(event) => setSearchText(event.target.value)}
            />
            {products(searchText)}
          </div>
        </Section>
      </div>
      <CheckoutPanel
        title="Setelah diubah"
        description={
          recorded.supplier === undefined ? `Isi ${lower} ini kalau disimpan` : `Dari ${recorded.supplier}`
        }
        emptyText="Belum ada barang."
        lines={checkoutLines}
        totalLabel="Total setelah diubah"
        totalQuantity={totalQuantity}
        total={total}
        error={isEmpty ? 'Sisakan paling tidak satu barang untuk menyimpan.' : error}
        saveLabel="Simpan Perubahan"
        isSaving={isSaving}
        onSave={onSave}
      />
    </div>
  )
}

function describe(transaction: RecordedTransaction): string {
  const when = new Date(transaction.occurrenceTime)
  const time = `${formatLongDate(when)} · pukul ${formatTime(when)}`
  return transaction.supplier === undefined ? time : `Dari ${transaction.supplier} · ${time}`
}

/** A sale or purchase that isn't there: an id the API doesn't know (404), or one that isn't an id at all (400). */
function isNotFound(error: unknown): boolean {
  return error instanceof ApiError && (error.status === 404 || error.status === 400)
}
