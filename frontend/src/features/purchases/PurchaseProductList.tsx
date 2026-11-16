import { LoadMoreButton } from '../../components/LoadMoreButton'
import { Empty, LoadFailed, Loading } from '../../components/LoadState'
import { MoneyField } from '../../components/MoneyField'
import { useDebouncedValue } from '../../lib/useDebouncedValue'
import { ProductCard } from '../products/ProductCard'
import { useProductPages } from '../products/useProductPages'
import { MaxQuantity, type PurchaseCart } from './usePurchaseCart'

/** How long typing must pause before the product list is searched. */
const SearchDelayMs = 300

type PurchaseProductListProps = {
  searchText: string
  cart: PurchaseCart
  /** Points out a picked product whose cost is still empty, once saving was tried. */
  showMissingCosts: boolean
}

/**
 * The products that can be added to a purchase, found by `searchText`. A picked one asks how many were bought
 * and what each cost.
 */
export function PurchaseProductList({ searchText, cart, showMissingCosts }: PurchaseProductListProps) {
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
