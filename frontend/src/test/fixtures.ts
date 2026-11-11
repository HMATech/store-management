/** Builds an unsigned JWT that expires `secondsFromNow` from now (negative for one already expired). */
export function createJwt(secondsFromNow = 3600): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')

  const exp = Math.floor(Date.now() / 1000) + secondsFromNow
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({ sub: 'user-1', exp })}.signature`
}

/** A `fetch` response carrying JSON, as the API sends it. */
export function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}
