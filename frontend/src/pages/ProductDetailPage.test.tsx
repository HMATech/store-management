import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatRupiah } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

type Api = ReturnType<typeof stubApi>

const gula = {
  id: 'p-gula',
  name: 'Gula Pasir 1 kg',
  brand: 'Gulaku',
  measure: 1,
  measureUnit: { id: 1, name: 'kg' },
  price: 17500,
  tags: ['Sembako'],
  stock: { quantity: 3, status: 'Low' },
}
const stock = { purchased: 12, sold: 9, adjusted: 0, quantity: 3, status: 'Low' }
/** Local noon, so the day shown doesn't depend on the time zone the test runs in. */
const day = (year: number, month: number, date: number) => new Date(year, month - 1, date, 12).toISOString()
const prices = [
  { id: 'pr3', value: 17500, validFromTime: day(2026, 11, 1), validToTime: null },
  { id: 'pr2', value: 16000, validFromTime: day(2026, 8, 15), validToTime: day(2026, 11, 1) },
  { id: 'pr1', value: 15500, validFromTime: day(2025, 12, 20), validToTime: day(2026, 8, 15) },
]

const page = (responses: unknown[], hasNextPage = false) =>
  jsonResponse(200, { responses, hasNextPage, totalCount: responses.length })

/** An API that knows the product, its stock and its prices. */
function stubDetailApi(overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/products/p-gula': () => jsonResponse(200, gula),
    '/stocks/p-gula': () => jsonResponse(200, stock),
    '/products/prices': () => page(prices),
    ...overrides,
  })
}

const priceChanges = (api: Api) =>
  api.mock.calls
    .filter(([, request]) => request?.method === 'POST')
    .map(([input, request]) => [String(input).replace(/^.*\/api/, ''), JSON.parse(String(request?.body))])

async function renderDetail() {
  const user = renderApp('/produk/p-gula')
  await screen.findByRole('heading', { name: 'Gula Pasir 1 kg' })
  return user
}

async function openChangePrice(user: ReturnType<typeof renderApp>) {
  await user.click(screen.getByRole('button', { name: 'Ubah Harga' }))
  return within(await screen.findByRole('dialog', { name: 'Ubah harga jual' }))
}

describe('product detail screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('shows the product with its brand, size, labels and price', async () => {
    stubDetailApi()

    await renderDetail()

    expect(screen.getByText('Gulaku · kg · Sembako')).toBeInTheDocument()
    expect(screen.getByText('Harga jual').parentElement).toHaveTextContent(rupiah(17500))
    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/produk')
    const menu = within(screen.getByRole('navigation', { name: 'Menu utama' }))
    expect(menu.getByRole('link', { name: 'Produk' })).toHaveAttribute('aria-current', 'page')
  })

  it('shows the stock, where it comes from, and the way to adjust it', async () => {
    stubDetailApi()

    await renderDetail()

    expect(await screen.findByText('Hampir habis')).toBeInTheDocument()
    expect(screen.getByText('barang').parentElement).toHaveTextContent('3')
    expect(screen.getByText('Dibeli').parentElement).toHaveTextContent('12')
    expect(screen.getByText('Terjual').parentElement).toHaveTextContent('9')
    expect(screen.getByText('Disesuaikan').parentElement).toHaveTextContent('0')
    expect(screen.getByRole('link', { name: 'Sesuaikan Stok' })).toHaveAttribute('href', '/produk/p-gula/stok')
  })

  it.each([
    ['Normal', 40, null],
    ['Empty', 0, 'Habis'],
    ['Negative', -2, 'Minus'],
  ])('marks a stock that is %s', async (status, quantity, label) => {
    stubDetailApi({ '/stocks/p-gula': () => jsonResponse(200, { ...stock, quantity, status, adjusted: -5 }) })

    await renderDetail()

    expect(await screen.findByText('Disesuaikan')).toBeInTheDocument()
    expect(screen.getByText('Disesuaikan').parentElement).toHaveTextContent('-5')
    expect(screen.queryByText('Hampir habis')).not.toBeInTheDocument()
    if (label === null) {
      expect(screen.queryByText('Minus')).not.toBeInTheDocument()
    } else {
      expect(screen.getByText(label)).toBeInTheDocument()
    }
  })

  it('lists the past prices, the current one first and marked', async () => {
    const api = stubDetailApi()

    await renderDetail()

    const history = within(screen.getByRole('region', { name: 'Riwayat harga' }))
    const rows = await history.findAllByRole('listitem')
    // `textContent` keeps the no-break space after "Rp", so it is compared with the amount as formatted.
    expect(rows.map((row) => row.textContent)).toEqual([
      `${formatRupiah(17500)}Sejak 1 Nov 2026Harga sekarang`,
      `${formatRupiah(16000)}15 Agu – 1 Nov 2026`,
      `${formatRupiah(15500)}20 Des 2025 – 15 Agu 2026`,
    ])
    expect(api.mock.calls.some(([input]) => /prices\?productId=p-gula&sortOrder=DESC/.test(String(input)))).toBe(true)
  })

  it('loads older prices on demand', async () => {
    stubDetailApi({
      '/products/prices?productId=p-gula&sortOrder=DESC&page=1': () => page(prices.slice(0, 2), true),
      '/products/prices?productId=p-gula&sortOrder=DESC&page=2': () => page(prices.slice(2)),
    })
    const user = await renderDetail()
    const history = within(screen.getByRole('region', { name: 'Riwayat harga' }))
    expect(await history.findAllByRole('listitem')).toHaveLength(2)

    await user.click(screen.getByRole('button', { name: 'Tampilkan lebih banyak' }))

    await waitFor(() => expect(history.getAllByRole('listitem')).toHaveLength(3))
  })

  it('keeps the product on screen when the stock or the prices cannot be loaded', async () => {
    let isDown = true
    stubDetailApi({
      '/stocks/p-gula': () => (isDown ? jsonResponse(500, {}) : jsonResponse(200, stock)),
      '/products/prices': () => jsonResponse(500, {}),
    })
    const user = await renderDetail()

    await waitFor(() => expect(screen.getAllByRole('alert')).toHaveLength(2))
    expect(screen.getByText('Harga jual').parentElement).toHaveTextContent(rupiah(17500))

    isDown = false
    await user.click(screen.getAllByRole('button', { name: 'Coba lagi' })[0])

    expect(await screen.findByText('Dibeli')).toBeInTheDocument()
  })

  it.each([404, 400])('says the product is not there on a %d', async (status) => {
    stubDetailApi({ '/products/p-gula': () => jsonResponse(status, {}) })

    renderApp('/produk/p-gula')

    expect(await screen.findByRole('heading', { name: 'Barang tidak ditemukan' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Kembali ke Produk' })).toHaveAttribute('href', '/produk')
    expect(screen.queryByText('Stok sekarang')).not.toBeInTheDocument()
  })

  it('offers a retry when the product cannot be loaded, and loads it on retry', async () => {
    let isDown = true
    stubDetailApi({ '/products/p-gula': () => (isDown ? jsonResponse(500, {}) : jsonResponse(200, gula)) })
    const user = renderApp('/produk/p-gula')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')

    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    expect(await screen.findByRole('heading', { name: 'Gula Pasir 1 kg' })).toBeInTheDocument()
  })

  describe('changing the price', () => {
    it('shows the current price and what a change means', async () => {
      stubDetailApi()
      const user = await renderDetail()

      const dialog = await openChangePrice(user)

      expect(dialog.getByText('Gula Pasir 1 kg')).toBeInTheDocument()
      expect(dialog.getByText('Harga sekarang').parentElement).toHaveTextContent(rupiah(17500))
      expect(dialog.getByLabelText('Harga baru')).toHaveValue('')
      expect(
        dialog.getByText('Harga baru berlaku mulai sekarang. Penjualan yang sudah dicatat tetap memakai harga lama.'),
      ).toBeInTheDocument()
    })

    it('saves the new price, closes, and shows it', async () => {
      let price = 17500
      const api = stubDetailApi({
        '/products/p-gula': () => jsonResponse(200, { ...gula, price }),
        '/products/p-gula/prices': () => {
          price = 18000
          return jsonResponse(200, {})
        },
      })
      const user = await renderDetail()
      const dialog = await openChangePrice(user)

      await user.type(dialog.getByLabelText('Harga baru'), '18000')
      await user.click(dialog.getByRole('button', { name: 'Simpan Harga' }))

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
      expect(priceChanges(api)).toEqual([['/products/p-gula/prices', { value: 18000 }]])
      await waitFor(() => expect(screen.getByText('Harga jual').parentElement).toHaveTextContent(rupiah(18000)))
    })

    it.each([
      ['nothing', '', 'Harga baru wajib diisi, contoh: 18000'],
      ['the current price', '17500', 'Harga baru sama dengan harga sekarang.'],
    ])('does not save %s', async (_case, typed, message) => {
      const api = stubDetailApi()
      const user = await renderDetail()
      const dialog = await openChangePrice(user)

      if (typed !== '') {
        await user.type(dialog.getByLabelText('Harga baru'), typed)
      }
      expect(dialog.queryByText(message)).not.toBeInTheDocument()
      await user.click(dialog.getByRole('button', { name: 'Simpan Harga' }))

      expect(dialog.getByText(message)).toBeInTheDocument()
      expect(dialog.getByLabelText('Harga baru')).toBeInvalid()
      expect(priceChanges(api)).toEqual([])
    })

    it('closes without saving on "Batal", and opens empty again', async () => {
      const api = stubDetailApi()
      const user = await renderDetail()
      const dialog = await openChangePrice(user)
      await user.type(dialog.getByLabelText('Harga baru'), '18000')

      await user.click(dialog.getByRole('button', { name: 'Batal' }))

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
      expect(priceChanges(api)).toEqual([])
      const reopened = await openChangePrice(user)
      expect(reopened.getByLabelText('Harga baru')).toHaveValue('')
    })

    it.each([
      ['the server fails', () => jsonResponse(500, {}), 'Harga belum berubah. Coba lagi.'],
      ['the product is gone', () => jsonResponse(404, {}), 'Barang ini sudah dihapus. Kembali ke menu Produk.'],
      [
        'there is no connection',
        () => Promise.reject(new TypeError('Failed to fetch')),
        'Tidak bisa terhubung, harga belum berubah. Periksa internet, lalu coba lagi.',
      ],
    ])('stays open and says so when %s', async (_case, respond, message) => {
      stubDetailApi({ '/products/p-gula/prices': respond })
      const user = await renderDetail()
      const dialog = await openChangePrice(user)

      await user.type(dialog.getByLabelText('Harga baru'), '18000')
      await user.click(dialog.getByRole('button', { name: 'Simpan Harga' }))

      expect(await dialog.findByRole('alert')).toHaveTextContent(message)
      expect(dialog.getByLabelText('Harga baru')).toHaveValue('18.000')
      expect(dialog.getByRole('button', { name: 'Simpan Harga' })).toBeEnabled()
    })
  })
})
