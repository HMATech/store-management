import { apiRequest } from '../../lib/api'
import { setToken } from '../../lib/session'

/**
 * Signs in through `POST /users/login` and starts the session. Throws `ApiError` when the API refuses, and
 * an `Error` when its answer carries no token.
 */
export async function login(email: string, password: string): Promise<void> {
  const response = await apiRequest<{ jwt?: unknown } | undefined>('/users/login', {
    method: 'POST',
    body: { email, password },
    authenticated: false,
  })

  if (typeof response?.jwt !== 'string' || response.jwt === '') {
    throw new Error('The login response had no token')
  }

  setToken(response.jwt)
}
