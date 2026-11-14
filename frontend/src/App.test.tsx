import { screen } from '@testing-library/react'
import type userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { clearSession, setToken } from './lib/session'
import { createJwt, jsonResponse, requestedPaths, stubApi } from './test/fixtures'
import { renderApp } from './test/render'

type User = ReturnType<typeof userEvent.setup>

const loginButton = () => screen.findByRole('button', { name: 'Masuk' })
const homeAction = () => screen.findByRole('link', { name: /Catat Penjualan/ })

async function fillAndSubmit(user: User, email: string, password: string) {
  if (email !== '') {
    await user.type(screen.getByLabelText('Email'), email)
  }
  if (password !== '') {
    await user.type(screen.getByLabelText('Kata sandi'), password)
  }
  await user.click(screen.getByRole('button', { name: 'Masuk' }))
}

describe('routing by session', () => {
  it('sends a signed-out visitor to the login screen', async () => {
    renderApp('/')

    expect(await loginButton()).toBeInTheDocument()
  })

  it('sends a visitor with an expired token to the login screen', async () => {
    setToken(createJwt(-1))

    renderApp('/')

    expect(await loginButton()).toBeInTheDocument()
  })

  it('shows the home screen to a signed-in user', async () => {
    stubApi({})
    setToken(createJwt())

    renderApp('/')

    expect(await homeAction()).toBeInTheDocument()
  })

  it('takes a signed-in user away from the login screen', async () => {
    stubApi({})
    setToken(createJwt())

    renderApp('/login')

    expect(await homeAction()).toBeInTheDocument()
  })

  it('sends an unknown address to the home screen', async () => {
    stubApi({})
    setToken(createJwt())

    renderApp('/tidak-ada')

    expect(await homeAction()).toBeInTheDocument()
  })

  it('returns to the login screen after signing out from the account screen', async () => {
    stubApi({})
    setToken(createJwt())
    const user = renderApp('/akun')

    await user.click(screen.getByRole('button', { name: 'Keluar' }))

    expect(await loginButton()).toBeInTheDocument()
  })

  it('returns to the login screen when the session ends elsewhere', async () => {
    stubApi({})
    setToken(createJwt())
    renderApp('/')

    clearSession()

    expect(await loginButton()).toBeInTheDocument()
  })

  it('returns to the login screen when the API rejects the token', async () => {
    stubApi({ '/reports/daily-totals': () => new Response(null, { status: 401 }) })
    setToken(createJwt())

    renderApp('/')

    expect(await loginButton()).toBeInTheDocument()
  })
})

describe('signing in', () => {
  it('opens the home screen with the right email and password', async () => {
    const api = stubApi({ '/users/login': () => jsonResponse(200, { id: 'user-1', jwt: createJwt() }) })
    const user = renderApp('/login')

    await fillAndSubmit(user, '  admin@store.local ', 'Admin123!')

    expect(await homeAction()).toBeInTheDocument()
    expect(requestedPaths(api)[0]).toMatch(/\/users\/login$/)
    expect(api.mock.calls[0][1]?.body).toBe('{"email":"admin@store.local","password":"Admin123!"}')
  })

  it.each([
    ['', 'Admin123!', 'Email belum diisi.'],
    ['   ', 'Admin123!', 'Email belum diisi.'],
    ['admin', 'Admin123!', 'Email belum benar. Contoh: nama@email.com'],
    ['admin@store.local', '', 'Kata sandi belum diisi.'],
  ])('asks to fix email "%s" and password "%s" without calling the API', async (email, password, message) => {
    const api = stubApi({})
    const user = renderApp('/login')

    await fillAndSubmit(user, email, password)

    expect(screen.getByText(message)).toBeInTheDocument()
    expect(api).not.toHaveBeenCalled()
  })

  it('marks the field that needs fixing as invalid', async () => {
    stubApi({})
    const user = renderApp('/login')

    await fillAndSubmit(user, '', '')

    expect(screen.getByLabelText('Email')).toBeInvalid()
    expect(screen.getByLabelText('Email')).toHaveAccessibleDescription('Email belum diisi.')
    expect(screen.getByLabelText('Kata sandi')).toBeInvalid()
  })

  it('shows a wrong email or password on the password field', async () => {
    stubApi({ '/users/login': () => jsonResponse(400, { title: 'Users.IncorrectCredential' }) })
    const user = renderApp('/login')

    await fillAndSubmit(user, 'admin@store.local', 'salah')

    const password = screen.getByLabelText('Kata sandi')
    await vi.waitFor(() => expect(password).toBeInvalid())
    expect(password).toHaveAccessibleDescription('Email atau kata sandi salah. Coba lagi.')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Masuk' })).toBeInTheDocument()
  })

  it.each([
    [
      'no connection',
      () => Promise.reject<Response>(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung. Periksa internet, lalu coba lagi.',
    ],
    ['a server error', () => jsonResponse(500, { title: 'System Error' }), 'Ada gangguan. Coba lagi sebentar lagi.'],
    ['a response without a token', () => jsonResponse(200, {}), 'Ada gangguan. Coba lagi sebentar lagi.'],
  ])('stays on the login screen and explains %s', async (_, respond, message) => {
    stubApi({ '/users/login': respond })
    const user = renderApp('/login')

    await fillAndSubmit(user, 'admin@store.local', 'Admin123!')

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(screen.getByLabelText('Kata sandi')).toBeValid()
    expect(screen.getByRole('button', { name: 'Masuk' })).toBeInTheDocument()
  })

  it('hides the password until asked to show it, and hides it again', async () => {
    const user = renderApp('/login')
    const password = screen.getByLabelText('Kata sandi')
    await user.type(password, 'Admin123!')

    expect(password).toHaveAttribute('type', 'password')

    await user.click(screen.getByRole('button', { name: 'Lihat' }))
    expect(password).toHaveAttribute('type', 'text')
    expect(password).toHaveValue('Admin123!')

    await user.click(screen.getByRole('button', { name: 'Sembunyikan' }))
    expect(password).toHaveAttribute('type', 'password')
  })

  it('does not submit the form when showing the password', async () => {
    const api = stubApi({})
    const user = renderApp('/login')
    await user.type(screen.getByLabelText('Email'), 'admin@store.local')
    await user.type(screen.getByLabelText('Kata sandi'), 'Admin123!')

    await user.click(screen.getByRole('button', { name: 'Lihat' }))

    expect(api).not.toHaveBeenCalled()
  })

  it('blocks a second submit while signing in', async () => {
    const api = stubApi({ '/users/login': () => new Promise<Response>(() => {}) })
    const user = renderApp('/login')

    await fillAndSubmit(user, 'admin@store.local', 'Admin123!')

    const button = await screen.findByRole('button', { name: 'Sedang masuk…' })
    expect(button).toBeDisabled()
    await user.type(screen.getByLabelText('Kata sandi'), '{Enter}')
    expect(api).toHaveBeenCalledTimes(1)
  })
})
