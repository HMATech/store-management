import { useSyncExternalStore } from 'react'
import { getToken, subscribe } from '../../lib/session'

/** Whether there is a live session. Re-renders when the user signs in or out, or the API rejects the token. */
export function useIsSignedIn(): boolean {
  return useSyncExternalStore(subscribe, () => getToken() !== null)
}
