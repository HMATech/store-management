import { keepPreviousData, useInfiniteQuery, type QueryKey } from '@tanstack/react-query'

/** One page of a list, as the API sends it. */
export type Page<T> = { responses: T[]; hasNextPage: boolean; totalCount: number }

/**
 * A list from the API, loaded a page at a time. `items` is everything loaded so far, or undefined until the
 * first page arrives or while an empty list from the previous `queryKey` is being replaced.
 */
export function usePagedList<T>(queryKey: QueryKey, fetchPage: (page: number, signal: AbortSignal) => Promise<Page<T>>) {
  const query = useInfiniteQuery({
    queryKey,
    queryFn: ({ pageParam, signal }) => fetchPage(pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage, _pages, lastPageParam) => (lastPage.hasNextPage ? lastPageParam + 1 : undefined),
    // Keep showing the last results while a new filter loads, so the list doesn't blink on every change.
    placeholderData: keepPreviousData,
  })

  const loaded = query.data?.pages.flatMap((page) => page.responses)
  // An empty list left over from the previous filter says nothing about the one still loading.
  const isStaleEmpty = query.isPlaceholderData && loaded?.length === 0

  return {
    items: isStaleEmpty ? undefined : loaded,
    isError: query.isError,
    retry: () => void query.refetch(),
    hasMore: query.hasNextPage,
    isLoadingMore: query.isFetchingNextPage,
    loadMore: () => void query.fetchNextPage(),
  }
}
