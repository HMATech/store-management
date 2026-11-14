import { vi } from 'vitest'

/** The user id inside the tokens `createJwt` builds. */
export const TestUserId = 'user-1'

/** Builds an unsigned JWT that expires `secondsFromNow` from now (negative for one already expired). */
export function createJwt(secondsFromNow = 3600): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')

  const exp = Math.floor(Date.now() / 1000) + secondsFromNow
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({ sub: TestUserId, exp })}.signature`
}

/** A `fetch` response carrying JSON, as the API sends it. */
export function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

type Handler = () => Response | Promise<Response>

/**
 * Stubs `fetch` with one handler per API path. A request goes to the handler with the longest path that its
 * own path (and query) starts with, and gets a 404 when none matches. Returns the mock, to inspect the calls.
 */
export function stubApi(handlers: Record<string, Handler>) {
  const fetchMock = vi.fn<typeof fetch>((input) => {
    const path = String(input)
    const match = Object.keys(handlers)
      .filter((prefix) => path.includes(prefix))
      .sort((a, b) => b.length - a.length)[0]
    return Promise.resolve(match === undefined ? jsonResponse(404, {}) : handlers[match]())
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

/** The paths the stubbed API was asked for, in order. */
export function requestedPaths(fetchMock: ReturnType<typeof stubApi>): string[] {
  return fetchMock.mock.calls.map(([input]) => String(input))
}
