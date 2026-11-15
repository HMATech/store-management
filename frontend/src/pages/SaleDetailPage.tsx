import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ReceiptText, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { Button, ButtonLink } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { EmptyState } from '../components/EmptyState'
import { LoadFailed, Loading } from '../components/LoadState'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { deleteSale, fetchSale, type SaleDetail } from '../features/sales/detail'
import { ApiError } from '../lib/api'
import { formatLongDate, formatRupiah, formatTime } from '../lib/format'

/** Detail Penjualan: what one sale held, and the way to remove it when it was recorded by mistake. */
export function SaleDetailPage() {
  const { id = '' } = useParams()
  const sale = useQuery({ queryKey: ['sales', id], queryFn: ({ signal }) => fetchSale(id, signal) })

  return (
    <Screen title="Detail Penjualan" backTo="/riwayat">
      <ScreenBody>
        {sale.isPending ? (
          <Loading />
        ) : sale.isError ? (
          sale.error instanceof ApiError && (sale.error.status === 404 || sale.error.status === 400) ? (
            <EmptyState
              icon={ReceiptText}
              title="Penjualan tidak ditemukan"
              action={<ButtonLink to="/riwayat">Kembali ke Riwayat</ButtonLink>}
            >
              Penjualan ini mungkin sudah dihapus.
            </EmptyState>
          ) : (
            <LoadFailed onRetry={() => void sale.refetch()} />
          )
        ) : (
          <div className="grid gap-16 lg:grid-cols-[minmax(0,1fr)_380px] lg:items-start lg:gap-24">
            <Summary sale={sale.data} />
            {/* The wide column on wide screens; between the summary and the actions on phones. */}
            <div className="lg:col-start-1 lg:row-span-2 lg:row-start-1">
              <Items sale={sale.data} />
            </div>
            <DeleteSale sale={sale.data} />
          </div>
        )}
      </ScreenBody>
    </Screen>
  )
}

function Summary({ sale }: { sale: SaleDetail }) {
  const when = new Date(sale.occurrenceTime)
  const quantity = sale.productEntries.reduce((sum, entry) => sum + entry.quantity, 0)

  return (
    <Card className="flex flex-col gap-4 p-16 lg:col-start-2 lg:p-20">
      <p className="text-xs text-text-muted">
        {formatLongDate(when)} · pukul {formatTime(when)}
      </p>
      <p className="text-5xl font-extrabold text-primary-default">{formatRupiah(sale.totalPrice)}</p>
      <p className="text-sm font-medium">
        {sale.productEntries.length} jenis barang · {quantity} item
      </p>
    </Card>
  )
}

function Items({ sale }: { sale: SaleDetail }) {
  return (
    <Card className="flex flex-col gap-12 p-16 lg:p-20">
      <h2 className="font-bold">Barang yang dibeli</h2>
      <ul className="flex flex-col gap-12">
        {sale.productEntries.map((entry) => (
          <li key={entry.id} className="flex items-center gap-12">
            <div className="flex min-w-0 flex-1 flex-col gap-2">
              <span className="font-semibold break-words">{entry.productName}</span>
              <span className="text-xs text-text-muted">
                {entry.quantity} × {formatRupiah(entry.priceValue)}
              </span>
            </div>
            <span className="shrink-0 font-bold">{formatRupiah(entry.totalPrice)}</span>
          </li>
        ))}
      </ul>
      <p className="flex items-center justify-between gap-12 border-t-thin border-border-default pt-12">
        <span className="font-bold">Total</span>
        <span className="text-xl font-extrabold">{formatRupiah(sale.totalPrice)}</span>
      </p>
    </Card>
  )
}

function DeleteSale({ sale }: { sale: SaleDetail }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isAsking, setIsAsking] = useState(false)

  const remove = useMutation({
    mutationFn: () => deleteSale(sale.id),
    onSuccess: () => {
      // The history, today's totals and the stock of what was sold all changed.
      void queryClient.invalidateQueries()
      void navigate('/riwayat', { state: { notice: 'Penjualan sudah dihapus.' } })
    },
  })

  function stopAsking() {
    setIsAsking(false)
    remove.reset()
  }

  return (
    <div className="flex flex-col lg:col-start-2">
      <Button kind="danger" onClick={() => setIsAsking(true)}>
        <Trash2 aria-hidden className="size-24" />
        Hapus Penjualan
      </Button>
      <ConfirmDialog
        isOpen={isAsking}
        title="Hapus penjualan ini?"
        confirmLabel="Ya, Hapus"
        isWorking={remove.isPending}
        failure={remove.isError ? describeDeleteFailure(remove.error) : undefined}
        onConfirm={() => remove.mutate()}
        onCancel={stopAsking}
      >
        Penjualan {formatRupiah(sale.totalPrice)} pukul {formatTime(new Date(sale.occurrenceTime))} akan dihapus.
        Stok barangnya akan dikembalikan.
      </ConfirmDialog>
    </div>
  )
}

function describeDeleteFailure(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return 'Tidak bisa terhubung, penjualan belum dihapus. Periksa internet, lalu coba lagi.'
    }
    if (error.status === 404) {
      return 'Penjualan ini sudah dihapus. Kembali ke Riwayat.'
    }
  }
  return 'Penjualan belum dihapus. Coba lagi.'
}
