import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import clsx from 'clsx'
import { CircleCheck, PackageX, TriangleAlert } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router'
import { ButtonLink } from '../components/Button'
import { Card } from '../components/Card'
import { EmptyState } from '../components/EmptyState'
import { FilterChip } from '../components/FilterChip'
import { LoadFailed, Loading } from '../components/LoadState'
import { QuantityStepper } from '../components/QuantityStepper'
import { SaveBar } from '../components/SaveBar'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { adjustStock, fetchStockBreakdown, type StockBreakdown } from '../features/products/detail'
import { ApiError } from '../lib/api'
import { formatNumber } from '../lib/format'

/** The most stock that can be counted on the shelf. */
const MaxQuantity = 99999

/** Why the shelf holds less than the app says, and why it holds more. The chosen one is saved as the reason. */
const reasonsWhenLess = ['Rusak / kedaluwarsa', 'Hilang', 'Salah hitung', 'Lainnya']
const reasonsWhenMore = ['Salah hitung', 'Belum dicatat', 'Lainnya']

/** Adjust Stock: count what is really on the shelf, and record the difference with its reason. */
export function AdjustStockPage() {
  const { id = '' } = useParams()
  const productPath = `/products/${id}`
  const stock = useQuery({
    queryKey: ['products', id, 'stock'],
    queryFn: ({ signal }) => fetchStockBreakdown(id, signal),
  })

  return (
    <Screen title="Sesuaikan Stok" backTo={productPath}>
      {stock.isPending ? (
        <ScreenBody>
          <Loading />
        </ScreenBody>
      ) : stock.isError ? (
        <ScreenBody>
          {stock.error instanceof ApiError && (stock.error.status === 404 || stock.error.status === 400) ? (
            <EmptyState
              icon={PackageX}
              title="Barang tidak ditemukan"
              action={<ButtonLink to="/products">Kembali ke Produk</ButtonLink>}
            >
              Barang ini mungkin sudah dihapus.
            </EmptyState>
          ) : (
            <LoadFailed onRetry={() => void stock.refetch()} />
          )}
        </ScreenBody>
      ) : (
        <AdjustStockForm productId={id} productPath={productPath} stock={stock.data} />
      )}
    </Screen>
  )
}

type AdjustStockFormProps = { productId: string; productPath: string; stock: StockBreakdown }

function AdjustStockForm({ productId, productPath, stock }: AdjustStockFormProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  // What the app had on record when counting began. The difference is worked out against this.
  const [recorded] = useState(stock.quantity)
  const [counted, setCounted] = useState(Math.max(recorded, 0))
  const [reason, setReason] = useState<string | null>(null)
  // A missing reason is pointed out only once saving was tried.
  const [wasSaveTried, setWasSaveTried] = useState(false)

  const difference = counted - recorded
  const reasons = difference < 0 ? reasonsWhenLess : reasonsWhenMore
  const chosenReason = reason !== null && reasons.includes(reason) ? reason : null

  const save = useMutation({
    mutationFn: (why: string) => adjustStock(productId, difference, why),
    onSuccess: () => {
      // The product's stock, the product lists and the home screen's alerts all changed.
      void queryClient.invalidateQueries()
      void navigate(productPath, { state: { notice: 'Stok sudah disesuaikan.' } })
    },
  })

  function trySave(event: FormEvent) {
    event.preventDefault()
    setWasSaveTried(true)
    if (difference !== 0 && chosenReason !== null) {
      save.mutate(chosenReason)
    }
  }

  return (
    <form noValidate onSubmit={trySave} className="flex flex-1 flex-col lg:px-32 lg:pb-32">
      <div
        className={
          'flex flex-1 flex-col lg:max-w-560 lg:flex-none lg:gap-20 lg:rounded-lg lg:border-thin'
          + ' lg:border-border-default lg:bg-bg-surface lg:p-24'
        }
      >
        <div className="flex flex-1 flex-col gap-16 px-16 pb-16 lg:p-0">
          <Card className="flex flex-col gap-2 p-16 lg:border-none lg:p-0">
            <h2 className="text-xl font-bold break-words">{stock.productName}</h2>
            <p className="text-sm text-text-muted">Tercatat di aplikasi: {formatNumber(recorded)} barang</p>
          </Card>
          <div className="flex flex-col items-center gap-16 py-8">
            <p className="text-center text-xl font-bold">
              Berapa jumlah sebenarnya di rak?
            </p>
            <QuantityStepper
              itemName="di rak"
              size="lg"
              quantity={counted}
              min={0}
              max={MaxQuantity}
              onDecrease={() => setCounted((current) => Math.max(current - 1, 0))}
              onIncrease={() => setCounted((current) => Math.min(current + 1, MaxQuantity))}
              onChange={setCounted}
            />
          </div>
          <Difference difference={difference} counted={counted} />
          {difference !== 0 && (
            <fieldset className="flex flex-col gap-10">
              <legend className="mb-10 text-sm font-semibold">Kenapa berbeda?</legend>
              <div className="flex flex-wrap gap-8">
                {reasons.map((option) => (
                  <FilterChip key={option} isSelected={chosenReason === option} onSelect={() => setReason(option)}>
                    {option}
                  </FilterChip>
                ))}
              </div>
              {wasSaveTried && chosenReason === null && (
                <p className="text-xs font-medium text-danger-default">Pilih dulu kenapa jumlahnya berbeda.</p>
              )}
            </fieldset>
          )}
        </div>
        <SaveBar
          label="Simpan Penyesuaian"
          cancelTo={productPath}
          isSaving={save.isPending}
          failure={describeProblem(wasSaveTried, difference, chosenReason, save.error)}
        />
      </div>
    </form>
  )
}

/** What saving will do to the stock, mirroring the Difference note in Loom. */
function Difference({ difference, counted }: { difference: number; counted: number }) {
  const isSame = difference === 0
  const Icon = isSame ? CircleCheck : TriangleAlert

  return (
    <div
      role="status"
      className={clsx(
        'flex items-center gap-12 rounded-md px-16 py-12',
        isSame ? 'bg-bg-sunken' : difference < 0 ? 'bg-danger-soft' : 'bg-warning-soft',
      )}
    >
      <Icon
        aria-hidden
        className={clsx(
          'size-24 shrink-0',
          isSame ? 'text-text-muted' : difference < 0 ? 'text-danger-default' : 'text-warning-default',
        )}
      />
      <p className="flex flex-col gap-2">
        {isSame ? (
          <>
            <span className="font-bold">Sama dengan yang tercatat</span>
            <span className="text-xs">Tidak ada yang perlu disesuaikan.</span>
          </>
        ) : (
          <>
            <span className={clsx('font-bold', difference < 0 ? 'text-danger-default' : 'text-warning-default')}>
              Selisih: {difference < 0 ? 'kurang' : 'lebih'} {formatNumber(Math.abs(difference))} barang
            </span>
            <span className="text-xs">Stok di aplikasi akan jadi {formatNumber(counted)}</span>
          </>
        )}
      </p>
    </div>
  )
}

/** Why the adjustment isn't saved, in words for the user: nothing to save, something to choose, or a failed save. */
function describeProblem(
  wasSaveTried: boolean,
  difference: number,
  reason: string | null,
  saveError: unknown,
): string | undefined {
  if (wasSaveTried && difference === 0) {
    return 'Jumlahnya sama dengan yang tercatat, jadi tidak ada yang disimpan.'
  }
  if (wasSaveTried && reason === null) {
    return 'Belum bisa disimpan. Pilih dulu kenapa jumlahnya berbeda.'
  }
  if (saveError instanceof ApiError) {
    if (saveError.isNetworkError) {
      return 'Tidak bisa terhubung, stok belum disesuaikan. Periksa internet, lalu coba lagi.'
    }
    if (saveError.status === 404) {
      return 'Barang ini sudah dihapus. Kembali ke menu Produk.'
    }
  }
  return saveError === null ? undefined : 'Stok belum disesuaikan. Coba lagi.'
}
