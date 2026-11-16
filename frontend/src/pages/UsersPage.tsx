import { Plus, User as UserIcon } from 'lucide-react'
import { Badge } from '../components/Badge'
import { ButtonLink } from '../components/Button'
import { CardList, CardRow } from '../components/Card'
import { IconChip } from '../components/IconChip'
import { LoadMoreButton } from '../components/LoadMoreButton'
import { Empty, LoadFailed, Loading } from '../components/LoadState'
import { Notice } from '../components/Notice'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { fetchUsers, fullName } from '../features/users/api'
import { getUserId } from '../lib/session'
import { usePagedList } from '../lib/usePagedList'

/** Users: everyone who can sign in to the app. */
export function UsersPage() {
  const currentUserId = getUserId()
  const { items, isError, retry, hasMore, isLoadingMore, loadMore } = usePagedList(['users', 'list'], (page, signal) =>
    fetchUsers(page, signal),
  )

  return (
    <Screen title="Daftar Pengguna" backTo="/account">
      <ScreenBody>
        <div className="flex w-full max-w-560 flex-col gap-14">
          <Notice />
          {isError ? (
            <LoadFailed onRetry={retry} />
          ) : items === undefined ? (
            <Loading />
          ) : items.length === 0 ? (
            <Empty>Belum ada pengguna.</Empty>
          ) : (
            <>
              <CardList>
                {items.map((user) => (
                  <CardRow key={user.userId}>
                    <IconChip icon={UserIcon} />
                    <div className="flex min-w-0 flex-1 flex-col gap-2">
                      <span className="font-semibold break-words">{fullName(user)}</span>
                      <span className="text-xs break-all text-text-muted">{user.email}</span>
                    </div>
                    {user.userId === currentUserId && <Badge tone="primary">Kamu</Badge>}
                  </CardRow>
                ))}
              </CardList>
              {hasMore && <LoadMoreButton isLoading={isLoadingMore} onLoadMore={loadMore} />}
            </>
          )}
          <ButtonLink kind="secondary" to="/account/users/new">
            <Plus aria-hidden className="size-22" />
            Tambah Pengguna
          </ButtonLink>
        </div>
      </ScreenBody>
    </Screen>
  )
}
