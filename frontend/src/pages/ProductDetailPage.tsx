import { useQuery } from '@tanstack/react-query'
import clsx from 'clsx'
import { ClipboardList, PackageX, ReceiptText } from 'lucide-react'
import { useState } from 'react'
import { useLocation, useParams } from 'react-router'
import { Badge } from '../components/Badge'
import { Button, ButtonLink } from '../components/Button'
import { Card } from '../components/Card'
import { EmptyState } from '../components/EmptyState'
import { LoadMoreButton } from '../components/LoadMoreButton'
import { Empty, LoadFailed, Loading } from '../components/LoadState'
import { Notice } from '../components/Notice'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { describeProduct, type StockStatus } from '../features/products/api'
import { ChangePriceDialog } from '../features/products/ChangePriceDialog'
import {
  fetchPriceHistory,
  fetchProduct,
  fetchStockBreakdown,
  type PriceLine,
  type ProductDetail,
} from '../features/products/detail'
import { ApiError } from '../lib/api'
import { formatDay, formatDayRange, formatNumber, formatRupiah } from '../lib/format'
import { usePagedList } from '../lib/usePagedList'

/** Detail Produk: one product's price, stock and past prices, with the ways to change them. */
export function ProductDetailPage() {
  const { id = '' } = useParams()
  // Set by a screen that changed this product and came back, such as Sesuaikan Stok.
  const hasNotice = typeof useLocation().state?.notice === 'string'
  const product = useQuery({ queryKey: ['products', id], queryFn: ({ signal }) => fetchProduct(id, signal) })

  return (
    <Screen title="Detail Produk" backTo="/produk">
      <ScreenBody>
        {product.isPending ? (
          <Loading />
        ) : product.isError ? (
          isNotFound(product.error) ? (
            <EmptyState
              icon={PackageX}
              title="Barang tidak ditemukan"
              action={<ButtonLink to="/produk">Kembali ke Produk</ButtonLink>}
            >
              Barang ini mungkin sudah dihapus.
            </EmptyState>
          ) : (
            <LoadFailed onRetry={() => void product.refetch()} />
          )
        ) : (
          <div className="grid gap-14 lg:grid-cols-2 lg:items-start lg:gap-24">
            <Notice className="lg:col-span-2" />
            <ProductCard product={product.data} />
            {/* Beside the product on wide screens, between it and the prices on phones. */}
            <div className={clsx('lg:col-start-2 lg:row-span-2', hasNotice ? 'lg:row-start-2' : 'lg:row-start-1')}>
              <StockCard productId={id} />
            </div>
            <PriceHistory productId={id} />
          </div>
        )}
      </ScreenBody>
    </Screen>
  )
}

/** A product that isn't there: an id the API doesn't know (404), or one that isn't an id at all (400). */
function isNotFound(error: unknown): boolean {
  return error instanceof ApiError && (error.status === 404 || error.status === 400)
}

function ProductCard({ product }: { product: ProductDetail }) {
  const [isChangingPrice, setIsChangingPrice] = useState(false)
  const description = [describeProduct(product), ...product.tags].join(' · ')

  return (
    <Card className="flex flex-col gap-12 p-16 lg:p-20">
      <div className="flex flex-col gap-2">
        <h2 className="text-3xl font-bold break-words">{product.name}</h2>
        <p className="text-sm break-words text-text-muted">{description}</p>
      </div>
      <div className="flex flex-col gap-12 lg:flex-row lg:items-end lg:justify-between">
        <p className="flex flex-col gap-2">
          <span className="text-xs text-text-muted">Harga jual</span>
          <span className="text-5xl font-extrabold">{formatRupiah(product.price)}</span>
        </p>
        <Button kind="secondary" onClick={() => setIsChangingPrice(true)}>
          <ReceiptText aria-hidden className="size-24" />
          Ubah Harga
        </Button>
      </div>
      <ChangePriceDialog product={product} isOpen={isChangingPrice} onClose={() => setIsChangingPrice(false)} />
    </Card>
  )
}

const stockBadges: Record<StockStatus, { tone: 'warning' | 'danger'; label: string } | undefined> = {
  Normal: undefined,
  Low: { tone: 'warning', label: 'Hampir habis' },
  Negative: { tone: 'danger', label: 'Minus' },
}

function StockCard({ productId }: { productId: string }) {
  const stock = useQuery({
    queryKey: ['products', productId, 'stock'],
    queryFn: ({ signal }) => fetchStockBreakdown(productId, signal),
  })

  if (stock.isPending) {
    return <Loading />
  }
  if (stock.isError) {
    return <LoadFailed onRetry={() => void stock.refetch()} />
  }

  const { quantity, status, purchased, sold, adjusted } = stock.data
  const badge = stockBadges[status]

  return (
    <Card className="flex flex-col gap-12 p-16 lg:p-20">
      <div className="flex min-h-32 items-center justify-between gap-12">
        <h2 className="font-semibold text-text-muted">Stok sekarang</h2>
        {badge !== undefined && <Badge tone={badge.tone}>{badge.label}</Badge>}
      </div>
      <p className="flex items-end gap-8">
        <span className="text-5xl font-extrabold">{formatNumber(quantity)}</span>
        <span className="pb-4 font-semibold text-text-muted">barang</span>
      </p>
      <dl className="flex gap-8 rounded-md bg-bg-canvas p-12">
        <Count label="Dibeli" value={purchased} />
        <Count label="Terjual" value={sold} />
        <Count label="Disesuaikan" value={adjusted} />
      </dl>
      <ButtonLink to={`/produk/${productId}/stok`}>
        <ClipboardList aria-hidden className="size-24" />
        Sesuaikan Stok
      </ButtonLink>
    </Card>
  )
}

function Count({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex min-w-0 flex-1 flex-col gap-2">
      <dt className="text-xs text-text-muted">{label}</dt>
      <dd className="text-2xl font-bold">{formatNumber(value)}</dd>
    </div>
  )
}

function PriceHistory({ productId }: { productId: string }) {
  const { items, isError, retry, hasMore, isLoadingMore, loadMore } = usePagedList(
    ['products', productId, 'prices'],
    (page, signal) => fetchPriceHistory(productId, page, signal),
  )

  return (
    <section aria-labelledby="price-history" className="flex flex-col gap-10">
      <h2 id="price-history" className="text-xl font-bold">
        Riwayat harga
      </h2>
      {isError ? (
        <LoadFailed onRetry={retry} />
      ) : items === undefined ? (
        <Loading />
      ) : items.length === 0 ? (
        <Empty>Belum ada riwayat harga.</Empty>
      ) : (
        <>
          <Card className="px-16 py-4">
            <ul className="flex flex-col">
              {items.map((line) => (
                <PriceRow key={line.id} line={line} />
              ))}
            </ul>
          </Card>
          {hasMore && <LoadMoreButton isLoading={isLoadingMore} onLoadMore={loadMore} />}
        </>
      )}
    </section>
  )
}

function PriceRow({ line }: { line: PriceLine }) {
  const from = new Date(line.validFromTime)
  const isCurrent = line.validToTime === null

  return (
    <li className="flex items-center gap-12 py-12">
      <div className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="text-lg font-semibold">{formatRupiah(line.value)}</span>
        <span className="text-xs text-text-muted">
          {line.validToTime === null ? `Sejak ${formatDay(from)}` : formatDayRange(from, new Date(line.validToTime))}
        </span>
      </div>
      {isCurrent && <Badge tone="primary">Harga sekarang</Badge>}
    </li>
  )
}
