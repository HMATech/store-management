import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { App } from './App'
import { clearSession, setToken } from './lib/session'
import { createJwt, jsonResponse } from './test/fixtures'

function renderApp(path = '/') {
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return userEvent.setup()
}

function mockLogin(response: Response | Error | DOMException) {
  const fetchMock = vi.fn<typeof fetch>(() =>
    response instanceof Response ? Promise.resolve(response) : Promise.reject(response),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

async function fillAndSubmit(user: ReturnType<typeof userEvent.setup>, email: string, password: string) {
  if (email !== '') {
    await user.type(screen.getByLabelText('Email'), email)
  }
  if (password !== '') {
    await user.type(screen.getByLabelText('Kata sandi'), password)
  }
  await user.click(screen.getByRole('button', { name: 'Masuk' }))
}

describe('routing by session', () => {
  it('sends a signed-out visitor to the login screen', () => {
    renderApp('/')

    expect(screen.getByRole('heading', { name: 'Warung Ortu' })).toBeInTheDocument()
  })

  it('sends a visitor with an expired token to the login screen', () => {
    setToken(createJwt(-1))

    renderApp('/')

    expect(screen.getByRole('heading', { name: 'Warung Ortu' })).toBeInTheDocument()
  })

  it('shows the home screen to a signed-in user', () => {
    setToken(createJwt())

    renderApp('/')

    expect(screen.getByRole('heading', { name: 'Beranda' })).toBeInTheDocument()
  })

  it('takes a signed-in user away from the login screen', () => {
    setToken(createJwt())

    renderApp('/login')

    expect(screen.getByRole('heading', { name: 'Beranda' })).toBeInTheDocument()
  })

  it('sends an unknown address to the home screen', () => {
    setToken(createJwt())

    renderApp('/tidak-ada')

    expect(screen.getByRole('heading', { name: 'Beranda' })).toBeInTheDocument()
  })

  it('returns to the login screen after signing out', async () => {
    setToken(createJwt())
    const user = renderApp('/')

    await user.click(screen.getByRole('button', { name: 'Keluar' }))

    expect(await screen.findByRole('heading', { name: 'Warung Ortu' })).toBeInTheDocument()
  })

  it('returns to the login screen when the session ends elsewhere', async () => {
    setToken(createJwt())
    renderApp('/')

    clearSession()

    expect(await screen.findByRole('heading', { name: 'Warung Ortu' })).toBeInTheDocument()
  })
})

describe('signing in', () => {
  it('opens the home screen with the right email and password', async () => {
    const fetchMock = mockLogin(jsonResponse(200, { id: 'user-1', jwt: createJwt() }))
    const user = renderApp('/login')

    await fillAndSubmit(user, '  admin@store.local ', 'Admin123!')

    expect(await screen.findByRole('heading', { name: 'Beranda' })).toBeInTheDocument()
    const [url, init] = fetchMock.mock.calls[0]
    expect(String(url)).toMatch(/\/users\/login$/)
    expect(init?.body).toBe('{"email":"admin@store.local","password":"Admin123!"}')
  })

  it.each([
    ['', 'Admin123!', 'Email belum diisi.'],
    ['   ', 'Admin123!', 'Email belum diisi.'],
    ['admin', 'Admin123!', 'Email belum benar. Contoh: nama@email.com'],
    ['admin@store.local', '', 'Kata sandi belum diisi.'],
  ])('asks to fix email "%s" and password "%s" without calling the API', async (email, password, message) => {
    const fetchMock = mockLogin(jsonResponse(200, {}))
    const user = renderApp('/login')

    await fillAndSubmit(user, email, password)

    expect(screen.getByText(message)).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('marks the field that needs fixing as invalid', async () => {
    mockLogin(jsonResponse(200, {}))
    const user = renderApp('/login')

    await fillAndSubmit(user, '', '')

    expect(screen.getByLabelText('Email')).toBeInvalid()
    expect(screen.getByLabelText('Email')).toHaveAccessibleDescription('Email belum diisi.')
    expect(screen.getByLabelText('Kata sandi')).toBeInvalid()
  })

  it.each([
    [
      'a wrong email or password',
      jsonResponse(400, { title: 'Users.IncorrectCredential' }),
      'Email atau kata sandi salah. Coba lagi.',
    ],
    ['no connection', new TypeError('Failed to fetch'), 'Tidak bisa terhubung. Periksa internet, lalu coba lagi.'],
    ['a server error', jsonResponse(500, { title: 'System Error' }), 'Ada gangguan. Coba lagi sebentar lagi.'],
    ['a response without a token', jsonResponse(200, {}), 'Ada gangguan. Coba lagi sebentar lagi.'],
  ])('stays on the login screen and explains %s', async (_, response, message) => {
    mockLogin(response)
    const user = renderApp('/login')

    await fillAndSubmit(user, 'admin@store.local', 'salah')

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(screen.getByRole('heading', { name: 'Warung Ortu' })).toBeInTheDocument()
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
    const fetchMock = mockLogin(jsonResponse(200, {}))
    const user = renderApp('/login')
    await user.type(screen.getByLabelText('Email'), 'admin@store.local')
    await user.type(screen.getByLabelText('Kata sandi'), 'Admin123!')

    await user.click(screen.getByRole('button', { name: 'Lihat' }))

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('blocks a second submit while signing in', async () => {
    const fetchMock = vi.fn<typeof fetch>(() => new Promise(() => {}))
    vi.stubGlobal('fetch', fetchMock)
    const user = renderApp('/login')

    await fillAndSubmit(user, 'admin@store.local', 'Admin123!')

    const button = await screen.findByRole('button', { name: 'Sedang masuk…' })
    expect(button).toBeDisabled()
    await user.type(screen.getByLabelText('Kata sandi'), '{Enter}')
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
