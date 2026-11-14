/**
 * The signed-in session: the JWT from `POST /users/login`. Everything that reads or writes the token goes
 * through this module, so the storage can later move to secure native storage without touching callers.
 */

const StorageKey = 'warung.session.token'

type Listener = () => void

const listeners = new Set<Listener>()

/** The stored token, or null when there is none or it has expired. */
export function getToken(): string | null {
  const token = readStoredToken()
  return token === null || isExpired(token) ? null : token
}

/** The signed-in user's id (the token's `sub` claim), or null when there is no session. */
export function getUserId(): string | null {
  const token = getToken()
  const sub = token === null ? undefined : readClaims(token)?.sub
  return typeof sub === 'string' && sub !== '' ? sub : null
}

export function setToken(token: string): void {
  try {
    localStorage.setItem(StorageKey, token)
  } catch {
    // Storage can be blocked (private mode); the session then lasts until the page is reloaded.
    memoryToken = token
  }
  notify()
}

export function clearSession(): void {
  const hadToken = readStoredToken() !== null
  memoryToken = null
  try {
    localStorage.removeItem(StorageKey)
  } catch {
    // Nothing was stored there if storage is blocked.
  }
  if (hadToken) {
    notify()
  }
}

/** Calls `listener` whenever the session starts or ends. Returns the unsubscribe function. */
export function subscribe(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

let memoryToken: string | null = null

function readStoredToken(): string | null {
  try {
    return localStorage.getItem(StorageKey) ?? memoryToken
  } catch {
    return memoryToken
  }
}

function notify(): void {
  listeners.forEach((listener) => listener())
}

/** True when the token's `exp` claim has passed. A token that can't be read counts as expired. */
function isExpired(token: string): boolean {
  const exp = readClaims(token)?.exp
  return typeof exp !== 'number' || exp * 1000 <= Date.now()
}

/** The token's payload, or null when it can't be read. */
function readClaims(token: string): Record<string, unknown> | null {
  const payload = token.split('.')[1]
  if (payload === undefined) {
    return null
  }

  try {
    const claims: unknown = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))
    return typeof claims === 'object' && claims !== null ? (claims as Record<string, unknown>) : null
  } catch {
    return null
  }
}
