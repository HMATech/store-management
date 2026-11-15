import { useQuery } from '@tanstack/react-query'
import { apiRequest } from '../../lib/api'
import { getUserId } from '../../lib/session'
import type { User } from '../users/api'

/** The signed-in user, from `GET /users/{id}`. */
export function useCurrentUser() {
  const userId = getUserId()

  return useQuery({
    queryKey: ['users', userId],
    queryFn: ({ signal }) => apiRequest<User>(`/users/${userId}`, { signal }),
    enabled: userId !== null,
    staleTime: Infinity,
  })
}
