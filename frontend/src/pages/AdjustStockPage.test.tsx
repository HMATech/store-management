import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

type Api = ReturnType<typeof stubApi>
type User = ReturnType<typeof renderApp>

const gula = {
  id: 'p-gula',
  name: 'Gula Pasir 1 kg',
  brand: 'Gulaku',
  measure: 1,
  measureUnit: { id: 1, name: 'kg' },
  price: 17500,
  tags: [],
  stock: { quantity: 3, status: 'Low' },
}
const stockOf = (quantity: number) => ({
  productName: 'Gula Pasir 1 kg',
  purchased: 12,
  sold: 12 - quantity,
  adjusted: 0,
  quantity,
  status: 'Low',
})

/** An API where the product has `quantity` in stock and adjustments are accepted. */
function stubStockApi(quantity = 3, overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/stocks/p-gula': () => jsonResponse(200, stockOf(quantity)),
    '/stocks/adjustments': () => new Response(null, { status: 201 }),
    '/products/p-gula': () => jsonResponse(200, gula),
    '/products/prices': () => jsonResponse(200, { responses: [], hasNextPage: false, totalCount: 0 }),
    ...overrides,
  })
}

const adjustments = (api: Api) =>
  api.mock.calls
    .filter(([, request]) => request?.method === 'POST')
    .map(([input, request]) => [String(input).replace(/^.*\/api/, ''), JSON.parse(String(request?.body))])

const countField = () => screen.getByRole('textbox', { name: 'Ketik jumlah di rak' })
const difference = () => screen.getByRole('status')
const saveButton = () => screen.getByRole('button', { name: 'Simpan Penyesuaian' })
const reason = (name: string) => screen.getByRole('button', { name })

async function renderAdjustStock() {
  const user = renderApp('/produk/p-gula/stok')
  await screen.findByRole('heading', { name: 'Gula Pasir 1 kg' })
  return user
}

async function count(user: User, quantity: number) {
  await user.clear(countField())
  await user.type(countField(), String(quantity))
}

describe('adjust stock screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('starts from what the app has on record, with nothing to adjust', async () => {
    stubStockApi()

    await renderAdjustStock()

    expect(screen.getByText('Tercatat di aplikasi: 3 barang')).toBeInTheDocument()
    expect(screen.getByText('Berapa jumlah sebenarnya di rak?')).toBeInTheDocument()
    expect(countField()).toHaveValue('3')
    expect(difference()).toHaveTextContent('Sama dengan yang tercatat')
    expect(screen.queryByText('Kenapa berbeda?')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/produk/p-gula')
    expect(screen.getByRole('link', { name: 'Batal' })).toHaveAttribute('href', '/produk/p-gula')
  })

  it('shows how much less is on the shelf, and asks why', async () => {
    stubStockApi()
    const user = await renderAdjustStock()

    await user.click(screen.getByRole('button', { name: 'Kurangi di rak' }))
    await user.click(screen.getByRole('button', { name: 'Kurangi di rak' }))

    expect(difference()).toHaveTextContent('Selisih: kurang 2 barang')
    expect(difference()).toHaveTextContent('Stok di aplikasi akan jadi 1')
    const reasons = within(screen.getByRole('group', { name: 'Kenapa berbeda?' }))
    expect(reasons.getAllByRole('button').map((button) => button.textContent)).toEqual([
      'Rusak / kedaluwarsa',
      'Hilang',
      'Salah hitung',
      'Lainnya',
    ])
  })

  it('shows how much more is on the shelf, with reasons that fit', async () => {
    stubStockApi()
    const user = await renderAdjustStock()

    await user.click(screen.getByRole('button', { name: 'Tambah di rak' }))

    expect(difference()).toHaveTextContent('Selisih: lebih 1 barang')
    expect(difference()).toHaveTextContent('Stok di aplikasi akan jadi 4')
    const reasons = within(screen.getByRole('group', { name: 'Kenapa berbeda?' }))
    expect(reasons.getAllByRole('button').map((button) => button.textContent)).toEqual([
      'Salah hitung',
      'Belum dicatat',
      'Lainnya',
    ])
  })

  it('does not count below zero', async () => {
    stubStockApi(1)
    const user = await renderAdjustStock()

    await user.click(screen.getByRole('button', { name: 'Kurangi di rak' }))

    expect(countField()).toHaveValue('0')
    expect(screen.getByRole('button', { name: 'Kurangi di rak' })).toBeDisabled()
    expect(difference()).toHaveTextContent('Selisih: kurang 1 barang')
  })

  it('saves what is missing as a negative adjustment with its reason, and returns to the product', async () => {
    const api = stubStockApi()
    const user = await renderAdjustStock()
    await count(user, 1)
    await user.click(reason('Rusak / kedaluwarsa'))
    expect(reason('Rusak / kedaluwarsa')).toHaveAttribute('aria-pressed', 'true')

    const before = Date.now()
    await user.click(saveButton())

    expect(await screen.findByText('Stok sudah disesuaikan.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Detail Produk', level: 1 })).toBeInTheDocument()
    const [[path, body]] = adjustments(api)
    expect(path).toBe('/stocks/adjustments')
    expect(body).toMatchObject({ productId: 'p-gula', quantity: -2, reason: 'Rusak / kedaluwarsa' })
    expect(new Date(body.occurrenceTime).getTime()).toBeGreaterThanOrEqual(before - 1000)
  })

  it('saves a surplus as a positive adjustment', async () => {
    const api = stubStockApi()
    const user = await renderAdjustStock()
    await count(user, 10)
    await user.click(reason('Belum dicatat'))

    await user.click(saveButton())

    await screen.findByText('Stok sudah disesuaikan.')
    expect(adjustments(api)[0][1]).toMatchObject({ quantity: 7, reason: 'Belum dicatat' })
  })

  it('brings a negative stock up to what was counted', async () => {
    const api = stubStockApi(-2)
    const user = await renderAdjustStock()
    expect(screen.getByText('Tercatat di aplikasi: -2 barang')).toBeInTheDocument()
    expect(countField()).toHaveValue('0')
    expect(difference()).toHaveTextContent('Selisih: lebih 2 barang')

    await user.click(reason('Salah hitung'))
    await user.click(saveButton())

    await screen.findByText('Stok sudah disesuaikan.')
    expect(adjustments(api)[0][1]).toMatchObject({ quantity: 2 })
  })

  it('does not save when nothing differs', async () => {
    const api = stubStockApi()
    const user = await renderAdjustStock()

    await user.click(saveButton())

    expect(screen.getByRole('alert')).toHaveTextContent('Jumlahnya sama dengan yang tercatat')
    expect(adjustments(api)).toEqual([])
  })

  it('does not save without a reason, and says so', async () => {
    const api = stubStockApi()
    const user = await renderAdjustStock()
    await count(user, 1)
    expect(screen.queryByText('Pilih dulu kenapa jumlahnya berbeda.')).not.toBeInTheDocument()

    await user.click(saveButton())

    expect(screen.getByText('Pilih dulu kenapa jumlahnya berbeda.')).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Pilih dulu kenapa jumlahnya berbeda.')
    expect(adjustments(api)).toEqual([])
  })

  it('asks for the reason again when the count turns from less to more', async () => {
    const api = stubStockApi()
    const user = await renderAdjustStock()
    await count(user, 1)
    await user.click(reason('Hilang'))

    await count(user, 5)
    await user.click(saveButton())

    expect(screen.getByText('Pilih dulu kenapa jumlahnya berbeda.')).toBeInTheDocument()
    expect(adjustments(api)).toEqual([])
  })

  it.each([
    ['the server fails', () => jsonResponse(500, {}), 'Stok belum disesuaikan. Coba lagi.'],
    ['the product is gone', () => jsonResponse(404, {}), 'Barang ini sudah dihapus. Kembali ke menu Produk.'],
    [
      'there is no connection',
      () => Promise.reject(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung, stok belum disesuaikan. Periksa internet, lalu coba lagi.',
    ],
  ])('keeps the count and says so when %s', async (_case, respond, message) => {
    stubStockApi(3, { '/stocks/adjustments': respond })
    const user = await renderAdjustStock()
    await count(user, 1)
    await user.click(reason('Hilang'))

    await user.click(saveButton())

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(countField()).toHaveValue('1')
    expect(reason('Hilang')).toHaveAttribute('aria-pressed', 'true')
    expect(saveButton()).toBeEnabled()
  })

  it('blocks a second save while the first is still going', async () => {
    let finish: (response: Response) => void = () => {}
    const api = stubStockApi(3, {
      '/stocks/adjustments': () => new Promise<Response>((resolve) => (finish = resolve)),
    })
    const user = await renderAdjustStock()
    await count(user, 1)
    await user.click(reason('Hilang'))

    await user.click(saveButton())

    const saving = await screen.findByRole('button', { name: 'Menyimpan…' })
    expect(saving).toBeDisabled()
    await user.click(saving)
    expect(adjustments(api)).toHaveLength(1)
    finish(new Response(null, { status: 201 }))
    expect(await screen.findByText('Stok sudah disesuaikan.')).toBeInTheDocument()
  })

  it.each([404, 400])('says the product is not there on a %d', async (status) => {
    stubStockApi(3, { '/stocks/p-gula': () => jsonResponse(status, {}) })

    renderApp('/produk/p-gula/stok')

    expect(await screen.findByRole('heading', { name: 'Barang tidak ditemukan' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Simpan Penyesuaian' })).not.toBeInTheDocument()
  })

  it('offers a retry when the stock cannot be loaded, and loads it on retry', async () => {
    let isDown = true
    stubStockApi(3, { '/stocks/p-gula': () => (isDown ? jsonResponse(500, {}) : jsonResponse(200, stockOf(3))) })
    const user = renderApp('/produk/p-gula/stok')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')

    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    await waitFor(() => expect(countField()).toHaveValue('3'))
  })
})
