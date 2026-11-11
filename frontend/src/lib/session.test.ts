import { describe, expect, it, vi } from 'vitest'
import { createJwt } from '../test/fixtures'
import { clearSession, getToken, setToken, subscribe } from './session'

describe('session', () => {
  it('has no token before signing in', () => {
    expect(getToken()).toBeNull()
  })

  it('returns the token that was set', () => {
    const token = createJwt()

    setToken(token)

    expect(getToken()).toBe(token)
  })

  it('treats an expired token as no session', () => {
    setToken(createJwt(-1))

    expect(getToken()).toBeNull()
  })

  it.each(['not-a-jwt', 'a.b.c', `x.${btoa('{"sub":"1"}')}.y`, `x.${btoa('{"exp":"soon"}')}.y`])(
    'treats the unreadable token %s as no session',
    (token) => {
      setToken(token)

      expect(getToken()).toBeNull()
    },
  )

  it('forgets the token when the session is cleared', () => {
    setToken(createJwt())

    clearSession()

    expect(getToken()).toBeNull()
  })

  it('tells subscribers when the session starts and ends', () => {
    const listener = vi.fn()
    const unsubscribe = subscribe(listener)

    setToken(createJwt())
    clearSession()

    expect(listener).toHaveBeenCalledTimes(2)
    unsubscribe()
  })

  it('does not tell subscribers when there was no session to clear', () => {
    const listener = vi.fn()
    const unsubscribe = subscribe(listener)

    clearSession()

    expect(listener).not.toHaveBeenCalled()
    unsubscribe()
  })

  it('stops telling a subscriber that unsubscribed', () => {
    const listener = vi.fn()
    subscribe(listener)()

    setToken(createJwt())

    expect(listener).not.toHaveBeenCalled()
  })

  it('keeps the session in memory when storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('blocked', 'SecurityError')
    })
    const token = createJwt()

    setToken(token)

    expect(getToken()).toBe(token)
    clearSession()
    expect(getToken()).toBeNull()
  })
})
