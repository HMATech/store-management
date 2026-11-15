import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { getToken, setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi, TestUserId } from '../test/fixtures'
import { renderApp } from '../test/render'

type Api = ReturnType<typeof stubApi>

const passwordRule = 'Minimal 8 karakter, dengan huruf besar, huruf kecil, angka, dan tanda seperti ! atau #'

const me = { userId: TestUserId, email: 'ibu@warungortu.id', firstName: 'Ibu', lastName: null }
const bapak = { userId: 'user-2', email: 'bapak@warungortu.id', firstName: 'Bapak', lastName: 'Rahman' }

/** An API that knows the signed-in user and lists two people. */
function stubAccountApi(overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    [`/users/${TestUserId}`]: () => jsonResponse(200, me),
    '/users?': () => jsonResponse(200, { responses: [me, bapak], hasNextPage: false, totalCount: 2 }),
    '/users/register': () => new Response(null, { status: 201 }),
    ...overrides,
  })
}

const registrations = (api: Api) =>
  api.mock.calls
    .filter(([, request]) => request?.method === 'POST')
    .map(([, request]) => JSON.parse(String(request?.body)))

describe('account screen', () => {
  beforeEach(() => setToken(createJwt()))
  afterEach(() => delete document.documentElement.dataset.theme)

  it('shows who is signed in and the account menu', async () => {
    stubAccountApi()

    renderApp('/akun')

    expect(await screen.findByText('ibu@warungortu.id')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Daftar pengguna' })).toHaveAttribute('href', '/akun/pengguna')
    expect(screen.getByRole('link', { name: 'Tambah pengguna' })).toHaveAttribute('href', '/akun/pengguna/baru')
    expect(screen.getByRole('button', { name: 'Tampilan: Ikuti perangkat' })).toBeInTheDocument()
    // The top bar has no account button, which would only lead back here. The sidebar keeps its own.
    expect(screen.getAllByRole('link', { name: /^Akun/ })).toHaveLength(1)
  })

  it('steps through the looks, applies each at once and remembers it', async () => {
    stubAccountApi()
    const user = renderApp('/akun')

    await user.click(await screen.findByRole('button', { name: 'Tampilan: Ikuti perangkat' }))
    expect(document.documentElement.dataset.theme).toBe('light')
    expect(localStorage.getItem('warung.theme')).toBe('light')

    await user.click(screen.getByRole('button', { name: 'Tampilan: Terang' }))
    expect(document.documentElement.dataset.theme).toBe('dark')

    await user.click(screen.getByRole('button', { name: 'Tampilan: Gelap' }))
    expect(document.documentElement.dataset.theme).toBeUndefined()
    expect(localStorage.getItem('warung.theme')).toBeNull()
  })

  it('treats an unknown stored look as following the device', async () => {
    localStorage.setItem('warung.theme', 'ungu')
    stubAccountApi()

    renderApp('/akun')

    expect(await screen.findByRole('button', { name: 'Tampilan: Ikuti perangkat' })).toBeInTheDocument()
  })

  it('asks before signing out, and stays signed in on "Batal"', async () => {
    stubAccountApi()
    const user = renderApp('/akun')

    await user.click(await screen.findByRole('button', { name: 'Keluar' }))
    const dialog = within(await screen.findByRole('alertdialog', { name: 'Keluar dari aplikasi?' }))
    expect(
      dialog.getByText('Untuk memakai lagi, kamu perlu masuk dengan email dan kata sandi.'),
    ).toBeInTheDocument()
    await user.click(dialog.getByRole('button', { name: 'Batal' }))

    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    expect(getToken()).not.toBeNull()
  })
})

describe('user list screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('lists everyone who can sign in, marking the signed-in user', async () => {
    stubAccountApi()

    renderApp('/akun/pengguna')

    const rows = await screen.findAllByRole('listitem')
    expect(rows.map((row) => row.textContent)).toEqual([
      'Ibuibu@warungortu.idKamu',
      'Bapak Rahmanbapak@warungortu.id',
    ])
    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/akun')
    expect(screen.getByRole('link', { name: 'Tambah Pengguna' })).toHaveAttribute('href', '/akun/pengguna/baru')
  })

  it('offers a retry when the list cannot be loaded', async () => {
    let isDown = true
    stubAccountApi({
      '/users?': () =>
        isDown ? jsonResponse(500, {}) : jsonResponse(200, { responses: [me], hasNextPage: false, totalCount: 1 }),
    })
    const user = renderApp('/akun/pengguna')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')
    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    expect(await screen.findByText('ibu@warungortu.id')).toBeInTheDocument()
  })
})

describe('add user screen', () => {
  beforeEach(() => setToken(createJwt()))

  async function fill(user: ReturnType<typeof renderApp>, values: Partial<Record<string, string>>) {
    for (const [label, value] of Object.entries(values)) {
      if (value !== undefined) {
        await user.type(screen.getByLabelText(label), value)
      }
    }
  }

  const valid = { 'Nama depan': ' Bapak ', Email: ' bapak@warungortu.id ', 'Kata sandi': 'Rahasia1!' }

  it('adds the user and shows them in the list with what to do next', async () => {
    const api = stubAccountApi()
    const user = renderApp('/akun/pengguna/baru')

    await fill(user, valid)
    await user.click(screen.getByRole('button', { name: 'Simpan Pengguna' }))

    expect(await screen.findByText(/Bapak sudah ditambahkan\./)).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Daftar Pengguna', level: 1 })).toBeInTheDocument()
    expect(registrations(api)).toEqual([
      { firstName: 'Bapak', lastName: null, email: 'bapak@warungortu.id', password: 'Rahasia1!' },
    ])
  })

  it('sends the last name when one is given', async () => {
    const api = stubAccountApi()
    const user = renderApp('/akun/pengguna/baru')

    await fill(user, { ...valid, 'Nama belakang (boleh dikosongkan)': 'Rahman' })
    await user.click(screen.getByRole('button', { name: 'Simpan Pengguna' }))

    await screen.findByText(/sudah ditambahkan/)
    expect(registrations(api)[0]).toMatchObject({ lastName: 'Rahman' })
  })

  it('points out what is missing only after saving is tried, and sends nothing', async () => {
    const api = stubAccountApi()
    const user = renderApp('/akun/pengguna/baru')
    expect(screen.queryByText('Nama depan wajib diisi')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Simpan Pengguna' }))

    expect(screen.getByText('Nama depan wajib diisi')).toBeInTheDocument()
    expect(screen.getByText('Email wajib diisi')).toBeInTheDocument()
    expect(screen.getByText('Kata sandi wajib diisi')).toBeInTheDocument()
    expect(registrations(api)).toEqual([])
  })

  it.each([
    ['Nama depan', 'Bu', 'Nama depan minimal 3 huruf'],
    ['Email', 'bapak.warung', 'Email belum benar. Contoh: nama@email.com'],
    ['Kata sandi', 'rahasia1!', passwordRule],
    ['Kata sandi', 'Rahasia1', passwordRule],
    ['Kata sandi', 'Rh1!', passwordRule],
  ])('rejects "%s" of "%s"', async (label, value, message) => {
    const api = stubAccountApi()
    const user = renderApp('/akun/pengguna/baru')

    await fill(user, { ...valid, [label]: undefined })
    await user.type(screen.getByLabelText(label), value)
    await user.click(screen.getByRole('button', { name: 'Simpan Pengguna' }))

    expect(screen.getByText(message)).toBeInTheDocument()
    expect(registrations(api)).toEqual([])
  })

  it('says on the email field when the email is already in use', async () => {
    stubAccountApi({ '/users/register': () => jsonResponse(409, { title: 'Users.EmailAlreadyRegistered' }) })
    const user = renderApp('/akun/pengguna/baru')

    await fill(user, valid)
    await user.click(screen.getByRole('button', { name: 'Simpan Pengguna' }))

    expect(await screen.findByText('Email ini sudah dipakai pengguna lain.')).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toBeInvalid()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()

    await user.type(screen.getByLabelText('Email'), 'x')
    expect(screen.queryByText('Email ini sudah dipakai pengguna lain.')).not.toBeInTheDocument()
  })

  it.each([
    [
      'the API refuses it',
      () => jsonResponse(400, {}),
      'Pengguna belum tersimpan. Periksa lagi isinya, lalu coba lagi.',
    ],
    [
      'there is no connection',
      () => Promise.reject(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung, pengguna belum tersimpan. Periksa internet, lalu coba lagi.',
    ],
  ])('keeps the form and says so when %s', async (_case, respond, message) => {
    stubAccountApi({ '/users/register': respond })
    const user = renderApp('/akun/pengguna/baru')

    await fill(user, valid)
    await user.click(screen.getByRole('button', { name: 'Simpan Pengguna' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(screen.getByLabelText('Nama depan')).toHaveValue(' Bapak ')
    expect(screen.getByRole('button', { name: 'Simpan Pengguna' })).toBeEnabled()
  })
})
