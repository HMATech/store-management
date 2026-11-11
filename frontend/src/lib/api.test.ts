import { describe, expect, it, vi } from 'vitest'
import { createJwt, jsonResponse } from '../test/fixtures'
import { ApiError, apiRequest } from './api'
import { getToken, setToken } from './session'

function mockFetch(response: Response | Error | DOMException) {
  const fetchMock = vi.fn<typeof fetch>(() =>
    response instanceof Response ? Promise.resolve(response) : Promise.reject(response),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function sentHeaders(fetchMock: ReturnType<typeof mockFetch>): Record<string, string> {
  return fetchMock.mock.calls[0][1]?.headers as Record<string, string>
}

async function failureOf(request: Promise<unknown>): Promise<ApiError> {
  const error = await request.catch((caught: unknown) => caught)
  expect(error).toBeInstanceOf(ApiError)
  return error as ApiError
}

describe('apiRequest', () => {
  it('returns the parsed JSON body', async () => {
    mockFetch(jsonResponse(200, { id: 'p1' }))

    await expect(apiRequest('/products/p1')).resolves.toEqual({ id: 'p1' })
  })

  it('returns undefined for an empty body', async () => {
    mockFetch(new Response(null, { status: 204 }))

    await expect(apiRequest('/products/p1', { method: 'DELETE' })).resolves.toBeUndefined()
  })

  it('sends the session token as a bearer token', async () => {
    const token = createJwt()
    setToken(token)
    const fetchMock = mockFetch(jsonResponse(200, {}))

    await apiRequest('/products')

    expect(sentHeaders(fetchMock).Authorization).toBe(`Bearer ${token}`)
  })

  it('sends no token when the call is not authenticated', async () => {
    setToken(createJwt())
    const fetchMock = mockFetch(jsonResponse(200, {}))

    await apiRequest('/users/login', { method: 'POST', body: {}, authenticated: false })

    expect(sentHeaders(fetchMock).Authorization).toBeUndefined()
  })

  it('sends no token when the stored one has expired', async () => {
    setToken(createJwt(-1))
    const fetchMock = mockFetch(jsonResponse(200, {}))

    await apiRequest('/products')

    expect(sentHeaders(fetchMock).Authorization).toBeUndefined()
  })

  it('sends the body as JSON', async () => {
    const fetchMock = mockFetch(jsonResponse(200, {}))

    await apiRequest('/brands', { method: 'POST', body: { name: 'Indomie' } })

    const init = fetchMock.mock.calls[0][1]
    expect(init?.method).toBe('POST')
    expect(init?.body).toBe('{"name":"Indomie"}')
    expect(sentHeaders(fetchMock)['Content-Type']).toBe('application/json')
  })

  it('throws the status, code and detail from problem details', async () => {
    mockFetch(jsonResponse(400, { title: 'Users.IncorrectCredential', detail: 'Incorrect email or password' }))

    const error = await failureOf(apiRequest('/users/login', { method: 'POST', body: {} }))

    expect(error.status).toBe(400)
    expect(error.code).toBe('Users.IncorrectCredential')
    expect(error.message).toBe('Incorrect email or password')
    expect(error.isNetworkError).toBe(false)
  })

  it.each([
    ['a body that is not JSON', new Response('<html>Bad Gateway</html>', { status: 502 })],
    ['an empty body', new Response(null, { status: 502 })],
    ['a JSON body that is not an object', jsonResponse(502, 'oops')],
  ])('throws the status alone for %s', async (_, response) => {
    mockFetch(response)

    const error = await failureOf(apiRequest('/products'))

    expect(error.status).toBe(502)
    expect(error.code).toBeNull()
  })

  it('throws a network error when no response arrives', async () => {
    mockFetch(new TypeError('Failed to fetch'))

    const error = await failureOf(apiRequest('/products'))

    expect(error.isNetworkError).toBe(true)
  })

  it('lets a cancelled request reject as an abort, not as an API error', async () => {
    mockFetch(new DOMException('Aborted', 'AbortError'))

    await expect(apiRequest('/products')).rejects.toMatchObject({ name: 'AbortError' })
  })

  it('ends the session when the API rejects the token', async () => {
    setToken(createJwt())
    mockFetch(new Response(null, { status: 401 }))

    const error = await failureOf(apiRequest('/products'))

    expect(error.status).toBe(401)
    expect(getToken()).toBeNull()
  })

  it('keeps the session when an unauthenticated call gets a 401', async () => {
    const token = createJwt()
    setToken(token)
    mockFetch(new Response(null, { status: 401 }))

    await failureOf(apiRequest('/users/login', { method: 'POST', body: {}, authenticated: false }))

    expect(getToken()).toBe(token)
  })

  it('keeps the session on other failures', async () => {
    const token = createJwt()
    setToken(token)
    mockFetch(jsonResponse(500, { title: 'System Error' }))

    await failureOf(apiRequest('/products'))

    expect(getToken()).toBe(token)
  })
})
