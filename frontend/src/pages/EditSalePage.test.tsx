import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatLongDate, formatRupiah } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

type Api = ReturnType<typeof stubApi>

const soldAt = new Date(2026, 10, 8, 10, 42)
const recordedAt = '2026-11-08T03:42:00Z'

/** A sale of 2 Indomie at an old price of Rp 3.000, and 1 Gula. */
const saleUpdatedAt = (lastUpdatedTime: string) => ({
  id: 's1',
  title: '',
  tags: [],
  productEntries: [
    {
      id: 'e1',
      productId: 'p-indomie',
      productName: 'Indomie Goreng',
      priceId: 'old',
      priceValue: 3000,
      quantity: 2,
      totalPrice: 6000,
    },
    {
      id: 'e2',
      productId: 'p-gula',
      productName: 'Gula Pasir 1 kg',
      priceId: 'g1',
      priceValue: 17500,
      quantity: 1,
      totalPrice: 17500,
    },
  ],
  totalPrice: 23500,
  occurrenceTime: soldAt.toISOString(),
  createdTime: recordedAt,
  lastUpdatedTime,
})

const product = (id: string, name: string, price: number) => ({
  id,
  name,
  price,
  stock: { quantity: 10, status: 'Normal' },
})
const catalogue = [product('p-indomie', 'Indomie Goreng', 3500), product('p-aqua', 'Aqua 600 ml', 4000)]

/** An API that knows the sale, and answers a change of its entries with `onUpdate`. */
function stubSaleApi(
  onUpdate: () => Response | Promise<Response> = () => jsonResponse(200, {}),
  sale: () => unknown = () => saleUpdatedAt(recordedAt),
) {
  return stubApi({
    '/sales/s1/entries': onUpdate,
    '/sales/s1': () => jsonResponse(200, sale()),
    '/products': () => jsonResponse(200, { responses: catalogue, hasNextPage: false }),
  })
}

const updates = (api: Api) =>
  api.mock.calls
    .filter(([input, init]) => String(input).endsWith('/sales/s1/entries') && init?.method === 'PUT')
    .map(([, init]) => JSON.parse(String(init?.body)) as { productEntries: unknown[] })

const recorded = () => within(screen.getByRole('region', { name: 'Barang di penjualan ini' }))
const toAdd = () => within(screen.getByRole('region', { name: 'Tambah barang lain' }))
const line = (name: string) => within(recorded().getByText(name).closest('li')!)
const total = () =>
  within(screen.getByRole('region', { name: 'Setelah diubah' })).getByRole('status', { name: 'Total' })
const saveButton = () => screen.getByRole('button', { name: 'Simpan Perubahan' })

async function renderEdit() {
  const user = renderApp('/history/sales/s1/edit')
  await screen.findByRole('region', { name: 'Barang di penjualan ini' })
  await toAdd().findByText('Aqua 600 ml')
  return user
}

describe('edit sale screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('shows the recorded lines at the price they were sold for', async () => {
    stubSaleApi()

    await renderEdit()

    expect(screen.getByRole('heading', { name: 'Ubah Barang', level: 1 })).toBeInTheDocument()
    expect(screen.getByText(`${formatLongDate(soldAt)} · pukul 10.42`)).toBeInTheDocument()
    expect(line('Indomie Goreng').getByText(rupiah(3000))).toBeInTheDocument()
    expect(line('Indomie Goreng').getByRole('status')).toHaveTextContent('2')
    expect(total()).toHaveTextContent(rupiah(23500))
    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/history/sales/s1')
  })

  it('saves a corrected quantity, a removed line and an added product, then says so on the detail', async () => {
    const api = stubSaleApi()
    const user = await renderEdit()

    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(line('Gula Pasir 1 kg').getByRole('button', { name: 'Hapus Gula Pasir 1 kg' }))
    await user.click(toAdd().getByRole('button', { name: 'Tambah Aqua 600 ml' }))

    // 3 Indomie stay at the old Rp 3.000; Aqua is added at its current Rp 4.000.
    expect(total()).toHaveTextContent(rupiah(13000))
    await user.click(saveButton())

    expect(await screen.findByText('Perubahan sudah disimpan.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Detail Penjualan', level: 1 })).toBeInTheDocument()
    expect(updates(api)).toEqual([
      {
        productEntries: [
          { id: 'e1', productId: 'p-indomie', quantity: 3 },
          { id: null, productId: 'p-aqua', quantity: 1 },
        ],
      },
    ])
  })

  it('brings a removed line back as it was recorded', async () => {
    stubSaleApi()
    const user = await renderEdit()

    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Hapus Indomie Goreng' }))
    expect(line('Indomie Goreng').getByText('Akan dihapus')).toBeInTheDocument()
    expect(total()).toHaveTextContent(rupiah(17500))

    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Batal hapus Indomie Goreng' }))

    expect(line('Indomie Goreng').getByRole('status')).toHaveTextContent('2')
    expect(total()).toHaveTextContent(rupiah(23500))
  })

  it('keeps a recorded line at one or more, so removing it is a choice of its own', async () => {
    stubSaleApi()
    const user = await renderEdit()

    expect(line('Gula Pasir 1 kg').getByRole('button', { name: 'Kurangi Gula Pasir 1 kg' })).toBeDisabled()
    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Kurangi Indomie Goreng' }))

    expect(line('Indomie Goreng').getByRole('status')).toHaveTextContent('1')
    expect(line('Indomie Goreng').getByRole('button', { name: 'Kurangi Indomie Goreng' })).toBeDisabled()
  })

  it('refuses to save a sale with nothing left in it', async () => {
    const api = stubSaleApi()
    const user = await renderEdit()

    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Hapus Indomie Goreng' }))
    await user.click(line('Gula Pasir 1 kg').getByRole('button', { name: 'Hapus Gula Pasir 1 kg' }))

    expect(screen.getByText(/Sisakan paling tidak satu barang. Kalau penjualan ini salah catat/)).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Sisakan paling tidak satu barang untuk menyimpan.')
    expect(saveButton()).toBeDisabled()
    expect(updates(api)).toEqual([])
  })

  it('keeps the changes and says so when there is no connection', async () => {
    stubSaleApi(() => Promise.reject(new TypeError('Failed to fetch')))
    const user = await renderEdit()

    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(saveButton())

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Tidak bisa terhubung. Periksa internet, lalu simpan lagi. Perubahan tidak akan tercatat dua kali.',
    )
    expect(line('Indomie Goreng').getByRole('status')).toHaveTextContent('3')
    expect(saveButton()).toBeEnabled()
  })

  it('does not add the new lines twice when a save whose answer was lost got through', async () => {
    let lastUpdatedTime = recordedAt
    let isOffline = true
    const api = stubSaleApi(
      () => {
        // The change reaches the API, but its answer never comes back.
        lastUpdatedTime = '2026-11-08T05:00:00Z'
        return isOffline ? Promise.reject(new TypeError('Failed to fetch')) : jsonResponse(200, {})
      },
      () => saleUpdatedAt(lastUpdatedTime),
    )
    const user = await renderEdit()

    await user.click(toAdd().getByRole('button', { name: 'Tambah Aqua 600 ml' }))
    await user.click(saveButton())
    await screen.findByRole('alert')
    isOffline = false
    await user.click(saveButton())

    expect(await screen.findByText('Perubahan sudah disimpan.')).toBeInTheDocument()
    expect(updates(api)).toHaveLength(1)
  })

  it('sends the change again when the lost save did not get through', async () => {
    let isOffline = true
    const api = stubSaleApi(() =>
      isOffline ? Promise.reject(new TypeError('Failed to fetch')) : jsonResponse(200, {}),
    )
    const user = await renderEdit()

    await user.click(toAdd().getByRole('button', { name: 'Tambah Aqua 600 ml' }))
    await user.click(saveButton())
    await screen.findByRole('alert')
    isOffline = false
    await user.click(saveButton())

    expect(await screen.findByText('Perubahan sudah disimpan.')).toBeInTheDocument()
    expect(updates(api)).toHaveLength(2)
  })

  it('says what happened when the sale or one of its products is gone', async () => {
    stubSaleApi(() => jsonResponse(404, { title: 'Sale.NotFound' }))
    const user = await renderEdit()

    await user.click(saveButton())

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Penjualan ini atau salah satu barangnya sudah dihapus. Kembali ke Riwayat untuk melihatnya lagi.',
    )
  })

  it('asks to try again when saving fails on the server', async () => {
    stubSaleApi(() => jsonResponse(500, {}))
    const user = await renderEdit()

    await user.click(saveButton())

    expect(await screen.findByRole('alert')).toHaveTextContent('Perubahan penjualan belum tersimpan. Coba lagi.')
  })

  it('says when the sale no longer exists', async () => {
    stubApi({ '/sales/s1': () => jsonResponse(404, {}) })

    renderApp('/history/sales/s1/edit')

    expect(await screen.findByText('Penjualan tidak ditemukan')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Kembali ke Riwayat' })).toHaveAttribute('href', '/history')
  })

  it('offers to try again when the sale cannot be loaded', async () => {
    let fails = true
    stubApi({
      '/sales/s1': () => (fails ? jsonResponse(500, {}) : jsonResponse(200, saleUpdatedAt(recordedAt))),
      '/products': () => jsonResponse(200, { responses: catalogue, hasNextPage: false }),
    })
    const user = renderApp('/history/sales/s1/edit')

    const retry = await screen.findByRole('button', { name: 'Coba lagi' })
    fails = false
    await user.click(retry)

    await waitFor(() => expect(screen.getByRole('region', { name: 'Barang di penjualan ini' })).toBeInTheDocument())
  })

  it('treats a malformed sale as one that cannot be loaded', async () => {
    stubApi({ '/sales/s1': () => new Response('not json', { status: 200 }) })

    renderApp('/history/sales/s1/edit')

    expect(await screen.findByRole('button', { name: 'Coba lagi' })).toBeInTheDocument()
  })
})
