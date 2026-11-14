import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { useState } from 'react'
import { Empty, LoadFailed, Loading } from '../components/LoadState'
import { Screen } from '../components/shell/Screen'
import { TextField } from '../components/TextField'
import { LoadMoreButton } from '../features/products/LoadMoreButton'
import { useProductPages } from '../features/products/useProductPages'
import { createSale } from '../features/sales/api'
import { OrderPanel } from '../features/sales/OrderPanel'
import { ProductCard } from '../features/sales/ProductCard'
import { SaleSavedDialog, type SavedSale } from '../features/sales/SaleSavedDialog'
import { useCart, type Cart } from '../features/sales/useCart'
import { ApiError } from '../lib/api'
import { useDebouncedValue } from '../lib/useDebouncedValue'

/** How long typing must pause before the product list is searched. */
const SearchDelayMs = 300

/** Catat Penjualan: pick what the customer buys, see the total, and save the sale. */
export function SalePage() {
  const queryClient = useQueryClient()
  const cart = useCart()
  const [searchText, setSearchText] = useState('')
  const [savedSale, setSavedSale] = useState<SavedSale | null>(null)

  const save = useMutation({
    mutationFn: () => createSale(cart.lines),
    onSuccess: () => {
      setSavedSale({ totalQuantity: cart.totalQuantity, totalPrice: cart.totalPrice })
      cart.clear()
      // The sale changed stock, today's totals and the history.
      void queryClient.invalidateQueries()
    },
  })

  function recordAnother() {
    setSavedSale(null)
    setSearchText('')
    save.reset()
  }

  return (
    <Screen title="Catat Penjualan" backTo="/">
      <div className="flex flex-1 flex-col lg:flex-row lg:items-start lg:gap-24 lg:px-32 lg:pb-32">
        <div className="flex min-w-0 flex-1 flex-col gap-14 px-16 pb-16 lg:gap-16 lg:p-0">
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
          <p className="text-sm text-text-muted">Ketuk "Tambah" untuk memasukkan barang</p>
          <ProductList searchText={searchText} cart={cart} />
        </div>
        <OrderPanel
          cart={cart}
          isSaving={save.isPending}
          error={save.isError ? describeSaveFailure(save.error) : undefined}
          onSave={() => save.mutate()}
        />
      </div>
      <SaleSavedDialog sale={savedSale} onRecordAnother={recordAnother} />
    </Screen>
  )
}

function ProductList({ searchText, cart }: { searchText: string; cart: Cart }) {
  const search = useDebouncedValue(searchText.trim(), SearchDelayMs)
  const { products, isError, retry, hasMore, isLoadingMore, loadMore } = useProductPages({ search })

  if (isError) {
    return <LoadFailed onRetry={retry} />
  }
  if (products === undefined) {
    return <Loading />
  }
  if (products.length === 0) {
    return (
      <Empty>
        {search === ''
          ? 'Belum ada barang. Tambahkan dulu di menu Produk.'
          : `Tidak ada barang dengan nama "${search}".`}
      </Empty>
    )
  }

  return (
    <>
      <ul className="grid grid-cols-[repeat(auto-fill,minmax(min(100%,320px),1fr))] gap-14 lg:gap-16">
        {products.map((product) => (
          <ProductCard
            key={product.id}
            product={product}
            quantity={cart.quantityOf(product.id)}
            onAdd={() => cart.add(product)}
            onIncrease={() => cart.increase(product.id)}
            onDecrease={() => cart.decrease(product.id)}
          />
        ))}
      </ul>
      {hasMore && <LoadMoreButton isLoading={isLoadingMore} onLoadMore={loadMore} />}
    </>
  )
}

function describeSaveFailure(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return 'Tidak bisa terhubung, penjualan belum tersimpan. Periksa internet, lalu coba lagi.'
    }
    if (error.status === 404) {
      return 'Ada barang yang sudah dihapus dari daftar. Muat ulang halaman ini, lalu catat lagi.'
    }
  }
  return 'Penjualan belum tersimpan. Coba lagi.'
}
