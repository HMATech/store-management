import { apiBaseUrl } from './config'
import { clearSession, getToken } from './session'

/**
 * A failed API call. `status` is the HTTP status, or 0 when no response arrived (offline, DNS, CORS).
 * `code` is the API's error code, such as `Users.IncorrectCredential`, when the response had one.
 */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | null

  constructor(status: number, code: string | null, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }

  get isNetworkError(): boolean {
    return this.status === 0
  }
}

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  /** Send the session token. Only login turns this off. */
  authenticated?: boolean
  signal?: AbortSignal
}

/**
 * Calls the API and returns the parsed JSON body (undefined for an empty one). Throws `ApiError` on any
 * failure. A 401 on an authenticated call ends the session, which sends the user back to the login screen.
 */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, authenticated = true, signal } = options

  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }
  if (authenticated) {
    const token = getToken()
    if (token !== null) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    })
  } catch (error) {
    if (isAbort(error)) {
      throw error
    }
    throw new ApiError(0, null, 'The API could not be reached')
  }

  if (!response.ok) {
    if (response.status === 401 && authenticated) {
      clearSession()
    }
    throw await toApiError(response)
  }

  const text = await response.text()
  return (text === '' ? undefined : JSON.parse(text)) as T
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem = await readProblem(response)
  return new ApiError(
    response.status,
    typeof problem.title === 'string' ? problem.title : null,
    typeof problem.detail === 'string' ? problem.detail : `Request failed with status ${response.status}`,
  )
}

/** The response's problem details, or an empty object when the body isn't a JSON object. */
async function readProblem(response: Response): Promise<Record<string, unknown>> {
  try {
    const body: unknown = await response.json()
    return typeof body === 'object' && body !== null ? (body as Record<string, unknown>) : {}
  } catch {
    return {}
  }
}

/** A request cancelled through its signal. Checked by name, because not every runtime's abort is an Error. */
function isAbort(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'name' in error && error.name === 'AbortError'
}
