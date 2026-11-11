import { Navigate, Outlet } from 'react-router'
import { useIsSignedIn } from './useIsSignedIn'

/** Shows the nested routes to a signed-in user, and sends anyone else to the login screen. */
export function RequireSignedIn() {
  return useIsSignedIn() ? <Outlet /> : <Navigate to="/login" replace />
}
