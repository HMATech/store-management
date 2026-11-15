import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, Truck } from 'lucide-react'
import { useState } from 'react'
import { CheckoutPanel } from '../components/CheckoutPanel'
import { LoadMoreButton } from '../components/LoadMoreButton'
import { Empty, LoadFailed, Loading } from '../components/LoadState'
import { MoneyField } from '../components/MoneyField'
import { SavedDialog, type SavedSummary } from '../components/SavedDialog'
import { Screen } from '../components/shell/Screen'
import { TextField } from '../components/TextField'
import { ProductCard } from '../features/products/ProductCard'
import { useProductPages } from '../features/products/useProductPages'
import { createPurchase, type CostedPurchaseLine } from '../features/purchases/api'
import { MaxQuantity, usePurchaseCart, type PurchaseCart } from '../features/purchases/usePurchaseCart'
import { ApiError } from '../lib/api'
import { formatRupiah } from '../lib/format'
import { useRetrySafeId } from '../lib/requestId'
import { useDebouncedValue } from '../lib/useDebouncedValue'

/** How long typing must pause before the product list is searched. */
const SearchDelayMs = 300

/** The longest supplier name the field takes. */
const MaxSupplierLength = 100

/** Catat Belanja: say where the goods came from, how many of each and what each cost, and save the purchase. */
export function PurchasePage() {
  const queryClient = useQueryClient()
  const cart = usePurchaseCart()
  const [supplier, setSupplier] = useState('')
  const [searchText, setSearchText] = useState('')
  // Missing fields are pointed out only once saving was tried, not while the form is still being filled in.
  const [wasSaveTried, setWasSaveTried] = useState(false)
  const [saved, setSaved] = useState<SavedSummary | null>(null)
  const purchaseIdFor = useRetrySafeId()

  const supplierName = supplier.trim()
  const isSupplierMissing = supplierName === ''
  const isCostMissing = cart.costedLines === null

  const save = useMutation({
    // Saving again after a failure sends the same id, so a purchase that did reach the API isn't recorded twice.
    mutationFn: (lines: CostedPurchaseLine[]) =>
      createPurchase(supplierName, lines, purchaseIdFor([supplierName, lines])),
    onSuccess: () => {
      setSaved({ totalQuantity: cart.totalQuantity, total: cart.totalCost })
      cart.clear()
      setWasSaveTried(false)
      // The purchase changed stock, today's totals and the history.
      void queryClient.invalidateQueries()
    },
  })

  function trySave() {
    setWasSaveTried(true)
    if (cart.costedLines !== null && !isSupplierMissing) {
      save.mutate(cart.costedLines)
    }
  }

  function recordAnother() {
    setSaved(null)
    setSupplier('')
    setSearchText('')
    save.reset()
  }

  return (
    <Screen title="Catat Belanja" backTo="/">
      <div className="flex flex-1 flex-col lg:flex-row lg:items-start lg:gap-24 lg:px-32 lg:pb-32">
        <div className="flex min-w-0 flex-1 flex-col gap-14 px-16 pb-16 lg:gap-16 lg:p-0">
          <div className="grid gap-14 lg:grid-cols-2 lg:items-end lg:gap-16">
            <TextField
              label="Belanja dari"
              icon={Truck}
              placeholder="Nama toko atau pasar"
              autoComplete="off"
              maxLength={MaxSupplierLength}
              value={supplier}
              onChange={(event) => setSupplier(event.target.value)}
              error={wasSaveTried && isSupplierMissing ? 'Isi dulu belanja dari mana.' : undefined}
            />
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
          </div>
          <p className="text-sm text-text-muted">Masukkan jumlah barang yang dibeli dan harga belinya</p>
          <ProductList searchText={searchText} cart={cart} showMissingCosts={wasSaveTried} />
        </div>
        <CheckoutPanel
          title="Belanjaan"
          description={isSupplierMissing ? 'Barang yang dibeli untuk toko' : `Dari ${supplierName}`}
          emptyText="Belum ada barang dipilih."
          lines={cart.lines.map(({ product, quantity, unitCost }) => ({
            id: product.id,
            name: product.name,
            detail: unitCost === null ? `${quantity} × harga beli belum diisi` : `${quantity} × ${formatRupiah(unitCost)}`,
            amount: unitCost === null ? '–' : formatRupiah(quantity * unitCost),
          }))}
          totalLabel="Total belanja ke toko"
          totalQuantity={cart.totalQuantity}
          total={cart.totalCost}
          error={describeProblem(wasSaveTried, isSupplierMissing, isCostMissing, save.error)}
          saveLabel="Simpan Belanja"
          isSaving={save.isPending}
          onSave={trySave}
        />
      </div>
      <SavedDialog title="Belanja tersimpan" saved={saved} againLabel="Catat Belanja Lagi" onAgain={recordAnother} />
    </Screen>
  )
}

type ProductListProps = { searchText: string; cart: PurchaseCart; showMissingCosts: boolean }

function ProductList({ searchText, cart, showMissingCosts }: ProductListProps) {
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
      <ul
        aria-label="Daftar barang"
        className="grid grid-cols-[repeat(auto-fill,minmax(min(100%,320px),1fr))] items-start gap-14 lg:gap-16"
      >
        {products.map((product) => {
          const line = cart.lineOf(product.id)
          return (
            <ProductCard
              key={product.id}
              product={product}
              quantity={line?.quantity ?? 0}
              max={MaxQuantity}
              priceLabel="Harga jual"
              quantityLabel="Jumlah dibeli"
              onAdd={() => cart.add(product)}
              onIncrease={() => cart.increase(product.id)}
              onDecrease={() => cart.decrease(product.id)}
              onQuantityChange={(quantity) => cart.setQuantity(product.id, quantity)}
            >
              <MoneyField
                label="Harga beli per barang"
                aria-label={`Harga beli ${product.name}, per barang`}
                placeholder="0"
                value={line?.unitCost ?? null}
                onChange={(unitCost) => cart.setUnitCost(product.id, unitCost)}
                error={showMissingCosts && line?.unitCost === null ? 'Isi harga belinya dulu.' : undefined}
              />
            </ProductCard>
          )
        })}
      </ul>
      {hasMore && <LoadMoreButton isLoading={isLoadingMore} onLoadMore={loadMore} />}
    </>
  )
}

/** Why the purchase isn't saved, in words for the user: something still to fill in, or a save that failed. */
function describeProblem(
  wasSaveTried: boolean,
  isSupplierMissing: boolean,
  isCostMissing: boolean,
  saveError: unknown,
): string | undefined {
  if (wasSaveTried && isSupplierMissing) {
    return 'Belum bisa disimpan. Isi dulu "Belanja dari" di atas.'
  }
  if (wasSaveTried && isCostMissing) {
    return 'Belum bisa disimpan. Isi dulu harga beli tiap barang yang dipilih.'
  }
  if (saveError instanceof ApiError) {
    if (saveError.isNetworkError) {
      return 'Tidak bisa terhubung. Periksa internet, lalu simpan lagi. Belanja tidak akan tercatat dua kali.'
    }
    if (saveError.status === 404) {
      return 'Ada barang yang sudah dihapus dari daftar. Muat ulang halaman ini, lalu catat lagi.'
    }
  }
  return saveError === null ? undefined : 'Belanja belum tersimpan. Coba lagi.'
}
