import { LoadMoreButton } from '../../components/LoadMoreButton'
import { Empty, LoadFailed, Loading } from '../../components/LoadState'
import { useDebouncedValue } from '../../lib/useDebouncedValue'
import { ProductCard } from '../products/ProductCard'
import { useProductPages } from '../products/useProductPages'
import { MaxQuantity, type Cart } from './useCart'

/** How long typing must pause before the product list is searched. */
const SearchDelayMs = 300

/** The products that can be added to a sale, found by `searchText`, each with how many are in `cart`. */
export function SaleProductList({ searchText, cart }: { searchText: string; cart: Cart }) {
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
            max={MaxQuantity}
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
