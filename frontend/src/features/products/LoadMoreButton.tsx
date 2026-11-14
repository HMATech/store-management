import { Button } from '../../components/Button'

type LoadMoreButtonProps = { isLoading: boolean; onLoadMore: () => void }

/** Loads the next page of a list that is shown a page at a time. */
export function LoadMoreButton({ isLoading, onLoadMore }: LoadMoreButtonProps) {
  return (
    <Button kind="secondary" onClick={onLoadMore} disabled={isLoading}>
      {isLoading ? 'Memuat…' : 'Tampilkan lebih banyak'}
    </Button>
  )
}
