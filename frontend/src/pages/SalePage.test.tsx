import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatRupiah } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, requestedPaths, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

const product = (id: string, name: string, price: number, quantity: number, status = 'Normal') => ({
  id,
  name,
  price,
  stock: { quantity, status },
})
const indomie = product('p-indomie', 'Indomie Goreng', 3500, 24)
const aqua = product('p-aqua', 'Aqua 600 ml', 4000, 18)
const gula = product('p-gula', 'Gula Pasir 1 kg', 17500, 3, 'Low')
const minyak = product('p-minyak', 'Minyak Goreng 2L', 38000, -2, 'Negative')
const telur = product('p-telur', 'Telur Ayam 1 kg', 28000, 0, 'Low')
const catalogue = [indomie, aqua, gula, minyak, telur]

const page = (responses: unknown[], hasNextPage = false) => jsonResponse(200, { responses, hasNextPage })

/** An API with the catalogue, where a search filters by name as the real one does. */
function stubSaleApi(overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/products': (path) => {
      const search = new URLSearchParams(path.split('?')[1]).get('search')?.toLowerCase()
      return page(search ? catalogue.filter((item) => item.name.toLowerCase().includes(search)) : catalogue)
    },
    '/sales': () => new Response(null, { status: 201 }),
    ...overrides,
  })
}

const card = (name: string) => within(screen.getByText(name).closest('li')!)
const order = () => within(screen.getByRole('region', { name: 'Pesanan' }))
const saveButton = () => screen.getByRole('button', { name: 'Simpan Penjualan' })
const total = () => order().getByRole('status', { name: 'Total' })

async function renderSalePage() {
  const user = renderApp('/jual')
  await screen.findByText('Indomie Goreng')
  return user
}

describe('sale screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('lists the products with their price and stock', async () => {
    stubSaleApi()

    await renderSalePage()

    expect(card('Indomie Goreng').getByText(rupiah(3500))).toBeInTheDocument()
    expect(card('Indomie Goreng').getByText('Stok 24')).toBeInTheDocument()
    expect(card('Gula Pasir 1 kg').getByText('Sisa 3')).toBeInTheDocument()
    expect(card('Minyak Goreng 2L').getByText('Stok minus 2')).toBeInTheDocument()
    expect(card('Telur Ayam 1 kg').getByText('Stok habis')).toBeInTheDocument()
  })

  it('is a task screen on phones: a way back, and no bottom navigation', async () => {
    stubSaleApi()

    await renderSalePage()

    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/')
    // Only the sidebar's menu is left; the bottom navigation would be a second one.
    expect(screen.getAllByRole('navigation', { name: 'Menu utama' })).toHaveLength(1)
  })

  it('starts with an empty order that cannot be saved', async () => {
    stubSaleApi()

    await renderSalePage()

    expect(order().getByText('0 barang')).toBeInTheDocument()
    expect(total()).toHaveTextContent(rupiah(0))
    expect(order().getByText('Belum ada barang dipilih.')).toBeInTheDocument()
    expect(saveButton()).toBeDisabled()
  })

  it('adds products and totals the order', async () => {
    stubSaleApi()
    const user = await renderSalePage()

    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(screen.getByRole('button', { name: 'Tambah Aqua 600 ml' }))

    expect(within(screen.getByRole('group', { name: 'Jumlah Indomie Goreng' })).getByRole('status')).toHaveTextContent(
      '2',
    )
    expect(order().getByText('3 barang')).toBeInTheDocument()
    expect(total()).toHaveTextContent(rupiah(11000))
    expect(order().getByText(`2 × ${rupiah(3500)}`)).toBeInTheDocument()
    expect(order().getByText(rupiah(7000))).toBeInTheDocument()
    expect(saveButton()).toBeEnabled()
  })

  it('takes a product out of the order when its last one is removed', async () => {
    stubSaleApi()
    const user = await renderSalePage()
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))

    await user.click(screen.getByRole('button', { name: 'Kurangi Indomie Goreng' }))

    expect(screen.queryByRole('group', { name: 'Jumlah Indomie Goreng' })).not.toBeInTheDocument()
    expect(card('Indomie Goreng').getByRole('button', { name: 'Tambah Indomie Goreng' })).toHaveTextContent('Tambah')
    expect(total()).toHaveTextContent(rupiah(0))
    expect(saveButton()).toBeDisabled()
  })

  it('lets a product with no stock or negative stock be sold', async () => {
    stubSaleApi()
    const user = await renderSalePage()

    await user.click(screen.getByRole('button', { name: 'Tambah Minyak Goreng 2L' }))
    await user.click(screen.getByRole('button', { name: 'Tambah Telur Ayam 1 kg' }))

    expect(total()).toHaveTextContent(rupiah(66000))
    expect(saveButton()).toBeEnabled()
  })

  it('saves the order as a sale happening now, and confirms it', async () => {
    const api = stubSaleApi()
    const user = await renderSalePage()
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(screen.getByRole('button', { name: 'Tambah Aqua 600 ml' }))
    const before = Date.now()

    await user.click(saveButton())

    const dialog = within(await screen.findByRole('alertdialog', { name: 'Penjualan tersimpan' }))
    expect(dialog.getByText(`3 barang · ${rupiah(11000)}`)).toBeInTheDocument()
    expect(dialog.getByRole('link', { name: 'Kembali ke Beranda' })).toHaveAttribute('href', '/')

    const [, init] = api.mock.calls.find(([input]) => String(input).endsWith('/sales'))!
    const body = JSON.parse(String(init?.body))
    expect(init?.method).toBe('POST')
    expect(body).toMatchObject({
      title: '',
      tags: [],
      productEntries: [
        { id: 'p-indomie', quantity: 2 },
        { id: 'p-aqua', quantity: 1 },
      ],
    })
    expect(Date.parse(body.occurrenceTime)).toBeGreaterThanOrEqual(before - 1000)
    expect(Date.parse(body.occurrenceTime)).toBeLessThanOrEqual(Date.now())
  })

  it('starts a fresh order after "Catat Penjualan Lagi", with stock loaded again', async () => {
    const api = stubSaleApi()
    const user = await renderSalePage()
    await user.type(screen.getByRole('searchbox', { name: 'Cari nama barang' }), 'indomie')
    await waitFor(() => expect(screen.queryByText('Aqua 600 ml')).not.toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(saveButton())
    const productRequests = () => requestedPaths(api).filter((path) => path.includes('/products')).length
    const requestsBefore = productRequests()

    await user.click(await screen.findByRole('button', { name: 'Catat Penjualan Lagi' }))

    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    expect(screen.getByRole('searchbox', { name: 'Cari nama barang' })).toHaveValue('')
    expect(await screen.findByText('Aqua 600 ml')).toBeInTheDocument()
    expect(total()).toHaveTextContent(rupiah(0))
    expect(saveButton()).toBeDisabled()
    expect(productRequests()).toBeGreaterThan(requestsBefore)
  })

  it.each([
    [
      'no connection',
      () => Promise.reject<Response>(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung, penjualan belum tersimpan. Periksa internet, lalu coba lagi.',
    ],
    [
      'a product that was deleted',
      () => jsonResponse(404, { title: 'Sale.NonExistingProduct(s)' }),
      'Ada barang yang sudah dihapus dari daftar. Muat ulang halaman ini, lalu catat lagi.',
    ],
    ['a server error', () => jsonResponse(500, { title: 'System Error' }), 'Penjualan belum tersimpan. Coba lagi.'],
    ['a refused request', () => jsonResponse(400, { title: 'Validation' }), 'Penjualan belum tersimpan. Coba lagi.'],
  ])('keeps the order and explains %s when saving fails', async (_, respond, message) => {
    stubSaleApi({ '/sales': respond })
    const user = await renderSalePage()
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))

    await user.click(saveButton())

    expect(await order().findByRole('alert')).toHaveTextContent(message)
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
    expect(total()).toHaveTextContent(rupiah(3500))
    expect(saveButton()).toBeEnabled()
  })

  it('saves on a second try after a failure', async () => {
    let isDown = true
    stubSaleApi({
      '/sales': () => (isDown ? jsonResponse(500, {}) : new Response(null, { status: 201 })),
    })
    const user = await renderSalePage()
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))
    await user.click(saveButton())
    await order().findByRole('alert')

    isDown = false
    await user.click(saveButton())

    expect(await screen.findByRole('alertdialog', { name: 'Penjualan tersimpan' })).toBeInTheDocument()
    // The dialog hides the page behind it from assistive technology, so look past that.
    expect(screen.queryByRole('alert', { hidden: true })).not.toBeInTheDocument()
  })

  it('blocks a second save while the first is still going', async () => {
    const api = stubSaleApi({ '/sales': () => new Promise<Response>(() => {}) })
    const user = await renderSalePage()
    await user.click(screen.getByRole('button', { name: 'Tambah Indomie Goreng' }))

    await user.click(saveButton())

    const saving = await screen.findByRole('button', { name: 'Menyimpan…' })
    expect(saving).toBeDisabled()
    await user.click(saving)
    expect(requestedPaths(api).filter((path) => path.endsWith('/sales'))).toHaveLength(1)
  })

  it('searches by name once typing pauses, and keeps what was already picked', async () => {
    const api = stubSaleApi()
    const user = await renderSalePage()
    await user.click(screen.getByRole('button', { name: 'Tambah Aqua 600 ml' }))

    await user.type(screen.getByRole('searchbox', { name: 'Cari nama barang' }), '  gula ')

    await waitFor(() => expect(screen.queryByText('Indomie Goreng')).not.toBeInTheDocument())
    expect(screen.getByText('Gula Pasir 1 kg')).toBeInTheDocument()
    const searches = requestedPaths(api).filter((path) => path.includes('search='))
    expect(searches).toEqual([expect.stringContaining('search=gula')])
    expect(total()).toHaveTextContent(rupiah(4000))
    expect(order().getByText('Aqua 600 ml')).toBeInTheDocument()
  })

  it('says so when no product matches the search', async () => {
    stubSaleApi()
    const user = await renderSalePage()

    await user.type(screen.getByRole('searchbox', { name: 'Cari nama barang' }), 'rokok')

    expect(await screen.findByText('Tidak ada barang dengan nama "rokok".')).toBeInTheDocument()
  })

  it('points to the products menu when there are no products at all', async () => {
    stubSaleApi({ '/products': () => page([]) })

    renderApp('/jual')

    expect(await screen.findByText('Belum ada barang. Tambahkan dulu di menu Produk.')).toBeInTheDocument()
    expect(saveButton()).toBeDisabled()
  })

  it('offers a retry when the products cannot be loaded, and loads them on retry', async () => {
    let isDown = true
    stubSaleApi({ '/products': () => (isDown ? jsonResponse(500, {}) : page(catalogue)) })
    const user = renderApp('/jual')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')

    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    expect(await screen.findByText('Indomie Goreng')).toBeInTheDocument()
  })

  it('loads more products on demand', async () => {
    const api = stubSaleApi({
      '/products': (path) => (path.includes('page=2') ? page([gula]) : page([indomie, aqua], true)),
    })
    const user = renderApp('/jual')
    await screen.findByText('Indomie Goreng')

    expect(screen.queryByText('Gula Pasir 1 kg')).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Tampilkan lebih banyak' }))

    expect(await screen.findByText('Gula Pasir 1 kg')).toBeInTheDocument()
    expect(screen.getByText('Indomie Goreng')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Tampilkan lebih banyak' })).not.toBeInTheDocument()
    expect(requestedPaths(api).filter((path) => path.includes('/products'))).toEqual([
      expect.stringContaining('page=1&pageSize=50'),
      expect.stringContaining('page=2&pageSize=50'),
    ])
  })
})
