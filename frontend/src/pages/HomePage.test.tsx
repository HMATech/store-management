import { screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatLongDate, formatRupiah, toIsoDay } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, requestedPaths, stubApi, TestUserId } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/\u00a0/g, ' ')

const totals = { salesTotal: 1245000, salesCount: 38, purchasesTotal: 860000, purchasesCount: 2 }
const paged = (responses: unknown[]) => () => jsonResponse(200, { responses })
const product = (id: string, name: string, quantity: number, status: string) => ({
  id,
  name,
  stock: { quantity, status },
})

/** An API where everything the home screen asks for succeeds; pass handlers to override some. */
function stubHomeApi(overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/reports/daily-totals': () => jsonResponse(200, totals),
    '/products?stockStatus=Negative': paged([product('p1', 'Minyak Goreng 2L', -2, 'Negative')]),
    '/products?stockStatus=Low': paged([product('p2', 'Gula Pasir 1 kg', 3, 'Low')]),
    '/transactions': paged([{ id: 's1', occurrenceTime: new Date().toISOString(), quantity: 3, total: 27500 }]),
    [`/users/${TestUserId}`]: () => jsonResponse(200, { userId: TestUserId, email: 'ibu@warung.id', firstName: 'Ibu' }),
    ...overrides,
  })
}

const section = (name: string) => within(screen.getByRole('region', { name }))

describe('home screen', () => {
  beforeEach(() => setToken(createJwt()))

  it("shows today's date and the way to the daily tasks", async () => {
    stubHomeApi()

    renderApp('/')

    expect(screen.getByText(formatLongDate(new Date()))).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Catat Penjualan/ })).toHaveAttribute('href', '/jual')
    expect(screen.getByRole('link', { name: 'Catat Belanja' })).toHaveAttribute('href', '/belanja')
    expect(screen.getByRole('link', { name: 'Cek Stok' })).toHaveAttribute('href', '/produk')
    expect(await screen.findByText('Hari ini')).toBeInTheDocument()
  })

  it("shows today's money in and out", async () => {
    stubHomeApi()

    renderApp('/')

    expect(await screen.findByText(rupiah(1245000))).toBeInTheDocument()
    expect(screen.getByText('dari 38 penjualan')).toBeInTheDocument()
    expect(screen.getByText(rupiah(860000))).toBeInTheDocument()
    expect(screen.getByText('dari 2 belanja')).toBeInTheDocument()
  })

  it("asks for today's totals in the device's time zone", async () => {
    const api = stubHomeApi()

    renderApp('/')
    await screen.findByText('Hari ini')

    const today = toIsoDay(new Date())
    const request = requestedPaths(api).find((path) => path.includes('/reports/daily-totals'))
    expect(request).toContain(`from=${today}&to=${today}`)
    expect(request).toContain(`timeZone=${encodeURIComponent(Intl.DateTimeFormat().resolvedOptions().timeZone)}`)
  })

  it('shows zero totals on a day with no transactions', async () => {
    stubHomeApi({
      '/reports/daily-totals': () =>
        jsonResponse(200, { salesTotal: 0, salesCount: 0, purchasesTotal: 0, purchasesCount: 0 }),
    })

    renderApp('/')

    expect(await screen.findByText('dari 0 penjualan')).toBeInTheDocument()
    expect(screen.getAllByText(rupiah(0))).toHaveLength(2)
  })

  it('lists stock below zero before stock running low', async () => {
    stubHomeApi()

    renderApp('/')

    const rows = await section('Stok perlu dicek').findAllByRole('listitem')
    expect(rows).toHaveLength(2)
    expect(rows[0]).toHaveTextContent('Minyak Goreng 2L')
    expect(rows[0]).toHaveTextContent('Stok minus, perlu dihitung')
    expect(rows[0]).toHaveTextContent('Minus 2')
    expect(rows[1]).toHaveTextContent('Gula Pasir 1 kg')
    expect(rows[1]).toHaveTextContent('Hampir habis')
    expect(rows[1]).toHaveTextContent('Sisa 3')
  })

  it('says a product with nothing left is out of stock', async () => {
    stubHomeApi({
      '/products?stockStatus=Negative': paged([]),
      '/products?stockStatus=Low': paged([product('p3', 'Telur', 0, 'Low')]),
    })

    renderApp('/')

    const row = await section('Stok perlu dicek').findByRole('listitem')
    expect(row).toHaveTextContent('Habis')
    expect(row).toHaveTextContent('Sisa 0')
  })

  it('shows at most three stock alerts', async () => {
    stubHomeApi({
      '/products?stockStatus=Negative': paged([
        product('n1', 'Minyak', -1, 'Negative'),
        product('n2', 'Beras', -4, 'Negative'),
      ]),
      '/products?stockStatus=Low': paged([product('l1', 'Gula', 1, 'Low'), product('l2', 'Telur', 2, 'Low')]),
    })

    renderApp('/')

    const rows = await section('Stok perlu dicek').findAllByRole('listitem')
    expect(rows.map((row) => row.textContent)).toEqual([
      expect.stringContaining('Minyak'),
      expect.stringContaining('Beras'),
      expect.stringContaining('Gula'),
    ])
  })

  it('says so when no stock needs checking', async () => {
    stubHomeApi({ '/products?stockStatus=Negative': paged([]), '/products?stockStatus=Low': paged([]) })

    renderApp('/')

    expect(await section('Stok perlu dicek').findByText('Semua stok aman.')).toBeInTheDocument()
  })

  it('lists the latest sales with their time and total', async () => {
    stubHomeApi()

    renderApp('/')

    const row = await section('Penjualan terakhir').findByRole('listitem')
    expect(row).toHaveTextContent('3 barang')
    expect(row).toHaveTextContent(/Pukul \d\d\.\d\d/)
    expect(row).toHaveTextContent(rupiah(27500))
  })

  it('says so when there are no sales yet', async () => {
    stubHomeApi({ '/transactions': paged([]) })

    renderApp('/')

    expect(await section('Penjualan terakhir').findByText('Belum ada penjualan.')).toBeInTheDocument()
  })

  it('keeps the rest of the screen when one part fails to load, and loads it on retry', async () => {
    let isDown = true
    stubHomeApi({
      '/transactions': () =>
        isDown
          ? jsonResponse(500, { title: 'System Error' })
          : jsonResponse(200, {
              responses: [{ id: 's1', occurrenceTime: new Date().toISOString(), quantity: 1, total: 4000 }],
            }),
    })
    const user = renderApp('/')

    const sales = section('Penjualan terakhir')
    expect(await sales.findByRole('alert')).toHaveTextContent('Tidak bisa memuat. Periksa internet, lalu coba lagi.')
    expect(await screen.findByText(rupiah(1245000))).toBeInTheDocument()
    expect(await section('Stok perlu dicek').findAllByRole('listitem')).toHaveLength(2)

    isDown = false
    await user.click(sales.getByRole('button', { name: 'Coba lagi' }))

    expect(await sales.findByText('1 barang')).toBeInTheDocument()
    expect(sales.queryByRole('alert')).not.toBeInTheDocument()
  })

  it.each([
    ['the totals', '/reports/daily-totals'],
    ['the stock alerts', '/products?stockStatus=Low'],
  ])('offers a retry when %s cannot be reached', async (_, path) => {
    stubHomeApi({ [path]: () => Promise.reject<Response>(new TypeError('Failed to fetch')) })

    renderApp('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')
    expect(screen.getByRole('button', { name: 'Coba lagi' })).toBeInTheDocument()
  })
})

describe('app shell', () => {
  beforeEach(() => setToken(createJwt()))

  it("labels the account button with the user's first name", async () => {
    stubHomeApi()

    renderApp('/')

    const links = await screen.findAllByRole('link', { name: 'Akun Ibu' })
    expect(links[0]).toHaveAttribute('href', '/akun')
  })

  it('labels the account button "Akun" when the user cannot be loaded', async () => {
    stubHomeApi({ [`/users/${TestUserId}`]: () => jsonResponse(500, {}) })

    renderApp('/')
    await screen.findByText('Hari ini')

    expect(screen.getAllByRole('link', { name: 'Akun' })[0]).toHaveTextContent('Akun')
  })

  it('marks the current destination in the navigation and moves between screens', async () => {
    stubHomeApi()
    const user = renderApp('/')
    const menu = within(screen.getAllByRole('navigation', { name: 'Menu utama' })[0])

    expect(menu.getByRole('link', { name: 'Beranda' })).toHaveAttribute('aria-current', 'page')
    expect(menu.getByRole('link', { name: 'Produk' })).not.toHaveAttribute('aria-current')

    await user.click(menu.getByRole('link', { name: 'Produk' }))

    expect(await screen.findByRole('heading', { name: 'Produk', level: 1 })).toBeInTheDocument()
    expect(screen.getByText('Halaman ini masih dibuat.')).toBeInTheDocument()
    expect(menu.getByRole('link', { name: 'Produk' })).toHaveAttribute('aria-current', 'page')
    expect(menu.getByRole('link', { name: 'Beranda' })).not.toHaveAttribute('aria-current')
  })

  it.each([
    ['/jual', 'Catat Penjualan'],
    ['/belanja', 'Catat Belanja'],
    ['/produk', 'Produk'],
    ['/riwayat', 'Riwayat'],
  ])('has a screen at %s', async (path, title) => {
    stubHomeApi()

    renderApp(path)

    expect(await screen.findByRole('heading', { name: title, level: 1 })).toBeInTheDocument()
  })

  it('shows who is signed in on the account screen', async () => {
    stubHomeApi({
      [`/users/${TestUserId}`]: () =>
        jsonResponse(200, { userId: TestUserId, email: 'ibu@warung.id', firstName: 'Siti', lastName: 'Aminah' }),
    })

    renderApp('/akun')

    expect(await screen.findByText('Siti Aminah')).toBeInTheDocument()
    expect(screen.getByText('ibu@warung.id')).toBeInTheDocument()
  })
})
