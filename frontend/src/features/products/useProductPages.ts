import { keepPreviousData, useInfiniteQuery } from '@tanstack/react-query'
import { fetchProducts, type ProductFilter } from './api'

/**
 * The products matching `filter`, loaded a page at a time. `products` is everything loaded so far, or
 * undefined until the first page arrives or while an empty list from the previous filter is being replaced.
 */
export function useProductPages(filter: ProductFilter) {
  const query = useInfiniteQuery({
    queryKey: ['products', filter.search, filter.stockStatus ?? 'all'],
    queryFn: ({ pageParam, signal }) => fetchProducts(filter, pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage, _pages, lastPageParam) => (lastPage.hasNextPage ? lastPageParam + 1 : undefined),
    // Keep showing the last results while a new filter loads, so the list doesn't blink on every change.
    placeholderData: keepPreviousData,
  })

  const loaded = query.data?.pages.flatMap((page) => page.responses)
  // An empty list left over from the previous filter says nothing about the one still loading.
  const isStaleEmpty = query.isPlaceholderData && loaded?.length === 0

  return {
    products: isStaleEmpty ? undefined : loaded,
    isError: query.isError,
    retry: () => void query.refetch(),
    hasMore: query.hasNextPage,
    isLoadingMore: query.isFetchingNextPage,
    loadMore: () => void query.fetchNextPage(),
  }
}
