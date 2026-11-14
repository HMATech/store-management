import { useQuery } from '@tanstack/react-query'
import { Package, Plus, Search } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { ButtonLink } from '../components/Button'
import { CardList } from '../components/Card'
import { EmptyState } from '../components/EmptyState'
import { FilterChip } from '../components/FilterChip'
import { Empty, LoadFailed, Loading } from '../components/LoadState'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { TextField } from '../components/TextField'
import { fetchProductCount, type StockStatus } from '../features/products/api'
import { LoadMoreButton } from '../features/products/LoadMoreButton'
import { ProductRow } from '../features/products/ProductRow'
import { useProductPages } from '../features/products/useProductPages'
import { useDebouncedValue } from '../lib/useDebouncedValue'

/** How long typing must pause before the product list is searched. */
const SearchDelayMs = 300

/** The stock filters, by the value each has in the address (`/produk?stok=minus`). */
const stockFilters = {
  'hampir-habis': {
    label: 'Hampir habis',
    status: 'Low',
    dot: 'warning',
    whenEmpty: 'Tidak ada barang yang hampir habis.',
  },
  minus: { label: 'Minus', status: 'Negative', dot: 'danger', whenEmpty: 'Tidak ada barang yang stoknya minus.' },
} as const satisfies Record<string, { label: string; status: StockStatus; dot: string; whenEmpty: string }>

type StockFilterKey = keyof typeof stockFilters

const isStockFilterKey = (value: string | null): value is StockFilterKey => value !== null && value in stockFilters

/** Produk: every product with its price and stock, searchable and filterable by stock that needs attention. */
export function ProductsPage() {
  const [searchText, setSearchText] = useState('')
  const [params, setParams] = useSearchParams()
  const stockParam = params.get('stok')
  const stockFilter = isStockFilterKey(stockParam) ? stockParam : null
  const count = useQuery({ queryKey: ['products', 'count'], queryFn: ({ signal }) => fetchProductCount(signal) })

  function selectStockFilter(key: StockFilterKey | null) {
    setParams(key === null ? {} : { stok: key }, { replace: true })
  }

  const addProductPath = '/produk/baru'

  if (count.data === 0) {
    return (
      <Screen title="Produk" subtitle="Belum ada barang di toko" action={<AddProductLink to={addProductPath} />}>
        <ScreenBody>
          <EmptyState
            icon={Package}
            title="Belum ada barang"
            action={
              <ButtonLink to={addProductPath}>
                <Plus aria-hidden className="size-22" />
                Tambah Produk
              </ButtonLink>
            }
          >
            Tambahkan barang yang dijual di warung supaya bisa mulai mencatat penjualan.
          </EmptyState>
        </ScreenBody>
      </Screen>
    )
  }

  return (
    <Screen
      title="Produk"
      subtitle={count.data === undefined ? undefined : `${count.data} barang di toko`}
      action={<AddProductLink to={addProductPath} />}
    >
      <ScreenBody>
        <div className="flex flex-col gap-14 lg:gap-24">
          <div className="flex flex-col gap-14 lg:flex-row lg:items-center lg:gap-12">
            <div className="lg:w-400">
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
            <div role="group" aria-label="Saring menurut stok" className="flex gap-8 overflow-x-auto">
              <FilterChip isSelected={stockFilter === null} onSelect={() => selectStockFilter(null)}>
                Semua
              </FilterChip>
              {Object.entries(stockFilters).map(([key, { label, dot }]) => (
                <FilterChip
                  key={key}
                  isSelected={stockFilter === key}
                  dot={dot}
                  onSelect={() => selectStockFilter(key as StockFilterKey)}
                >
                  {label}
                </FilterChip>
              ))}
            </div>
          </div>
          <ProductList searchText={searchText} stockFilter={stockFilter} />
        </div>
      </ScreenBody>
    </Screen>
  )
}

/** The top bar's action: "Tambah" on phones, "Tambah Produk" where there is room. */
function AddProductLink({ to }: { to: string }) {
  return (
    <ButtonLink to={to} aria-label="Tambah Produk" className="shrink-0">
      <Plus aria-hidden className="size-22" />
      <span>
        Tambah<span className="hidden lg:inline"> Produk</span>
      </span>
    </ButtonLink>
  )
}

function ProductList({ searchText, stockFilter }: { searchText: string; stockFilter: StockFilterKey | null }) {
  const search = useDebouncedValue(searchText.trim(), SearchDelayMs)
  const filter = stockFilter === null ? undefined : stockFilters[stockFilter]
  const { products, isError, retry, hasMore, isLoadingMore, loadMore } = useProductPages({
    search,
    stockStatus: filter?.status,
  })

  if (isError) {
    return <LoadFailed onRetry={retry} />
  }
  if (products === undefined) {
    return <Loading />
  }
  if (products.length === 0) {
    return <Empty>{describeEmpty(search, filter)}</Empty>
  }

  return (
    <>
      <CardList>
        {products.map((product) => (
          <ProductRow key={product.id} product={product} />
        ))}
      </CardList>
      {hasMore && <LoadMoreButton isLoading={isLoadingMore} onLoadMore={loadMore} />}
    </>
  )
}

/** Why the list is empty, given what was searched for and which stock filter is on. */
function describeEmpty(search: string, filter: (typeof stockFilters)[StockFilterKey] | undefined): string {
  if (search === '') {
    return filter?.whenEmpty ?? 'Belum ada barang.'
  }
  return filter === undefined
    ? `Tidak ada barang dengan nama "${search}".`
    : `Tidak ada barang dengan nama "${search}" di "${filter.label}".`
}
