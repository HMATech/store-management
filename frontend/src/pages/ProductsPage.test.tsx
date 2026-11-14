import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatRupiah } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, requestedPaths, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

const product = (
  name: string,
  price: number,
  quantity: number,
  status: string,
  brand = 'Umum',
  measure = 1,
  unit = 'pcs',
) => ({
  id: `p-${name.toLowerCase().replace(/\s+/g, '-')}`,
  name,
  brand,
  measure,
  measureUnit: { name: unit },
  price,
  stock: { quantity, status },
})
const catalogue = [
  product('Aqua 600 ml', 4000, 18, 'Normal', 'Danone', 1, 'botol'),
  product('Gula Pasir 1 kg', 17500, 3, 'Low', 'Gulaku', 1, 'bungkus'),
  product('Indomie Goreng', 3500, 24, 'Normal', 'Indofood', 85, 'g'),
  product('Minyak Goreng 2L', 38000, -2, 'Negative', 'Bimoli', 1.5, 'liter'),
  product('Telur Ayam 1 kg', 28000, 0, 'Low'),
]

const page = (responses: unknown[], hasNextPage = false, totalCount = responses.length) =>
  jsonResponse(200, { responses, hasNextPage, totalCount })

/** An API with the catalogue, filtering by name and stock status as the real one does. */
function stubProductApi(items = catalogue, overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/products': (path) => {
      const query = new URLSearchParams(path.split('?')[1])
      const search = query.get('search')?.toLowerCase()
      const status = query.get('stockStatus')
      const matching = items.filter(
        (item) => (!search || item.name.toLowerCase().includes(search)) && (!status || item.stock.status === status),
      )
      return page(matching.slice(0, Number(query.get('pageSize'))), false, matching.length)
    },
    ...overrides,
  })
}

const row = (name: string) => within(screen.getByRole('link', { name: new RegExp(name) }))
const names = () => screen.getAllByRole('listitem').map((item) => item.querySelector('span')?.textContent)
const filter = (name: string) => screen.getByRole('button', { name })

async function renderProducts(path = '/produk') {
  const user = renderApp(path)
  await screen.findAllByRole('listitem')
  return user
}

describe('product list screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('lists the products with their brand, size, price and stock', async () => {
    stubProductApi()

    await renderProducts()

    expect(names()).toEqual(catalogue.map((item) => item.name))
    expect(row('Aqua 600 ml').getByText('Danone · botol')).toBeInTheDocument()
    expect(row('Aqua 600 ml').getByText(rupiah(4000))).toBeInTheDocument()
    expect(row('Aqua 600 ml').getByText('Stok 18')).toBeInTheDocument()
    expect(row('Indomie Goreng').getByText('Indofood · 85 g')).toBeInTheDocument()
    expect(row('Minyak Goreng 2L').getByText('Bimoli · 1,5 liter')).toBeInTheDocument()
    expect(row('Gula Pasir 1 kg').getByText('Sisa 3')).toBeInTheDocument()
    expect(row('Minyak Goreng 2L').getByText('Minus 2')).toBeInTheDocument()
    expect(row('Telur Ayam 1 kg').getByText('Habis')).toBeInTheDocument()
  })

  it('says how many products the store has, and links to adding one', async () => {
    stubProductApi()

    await renderProducts()

    expect(await screen.findByText('5 barang di toko')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Tambah Produk' })).toHaveAttribute('href', '/produk/baru')
  })

  it('keeps counting every product while a search or filter narrows the list', async () => {
    stubProductApi()
    const user = await renderProducts()

    await user.click(filter('Minus'))

    await waitFor(() => expect(names()).toEqual(['Minyak Goreng 2L']))
    expect(screen.getByText('5 barang di toko')).toBeInTheDocument()
  })

  it('opens a product from its row', async () => {
    stubProductApi()

    await renderProducts()

    expect(screen.getByRole('link', { name: /Indomie Goreng/ })).toHaveAttribute('href', '/produk/p-indomie-goreng')
  })

  it.each([
    ['Hampir habis', 'Low', ['Gula Pasir 1 kg', 'Telur Ayam 1 kg'], '/produk?stok=hampir-habis'],
    ['Minus', 'Negative', ['Minyak Goreng 2L'], '/produk?stok=minus'],
  ])('filters to "%s"', async (label, status, expected) => {
    const api = stubProductApi()
    const user = await renderProducts()

    await user.click(filter(label))

    await waitFor(() => expect(names()).toEqual(expected))
    expect(filter(label)).toHaveAttribute('aria-pressed', 'true')
    expect(filter('Semua')).toHaveAttribute('aria-pressed', 'false')
    expect(requestedPaths(api).some((path) => path.includes(`stockStatus=${status}`))).toBe(true)
  })

  it('shows everything again with "Semua"', async () => {
    stubProductApi()
    const user = await renderProducts('/produk?stok=minus')
    expect(names()).toEqual(['Minyak Goreng 2L'])

    await user.click(filter('Semua'))

    await waitFor(() => expect(names()).toHaveLength(5))
    expect(filter('Semua')).toHaveAttribute('aria-pressed', 'true')
  })

  it.each([
    ['/produk?stok=hampir-habis', 'Hampir habis'],
    ['/produk?stok=minus', 'Minus'],
  ])('opens with the filter in the address %s', async (path, label) => {
    stubProductApi()

    await renderProducts(path)

    expect(filter(label)).toHaveAttribute('aria-pressed', 'true')
  })

  it('ignores an unknown filter in the address', async () => {
    stubProductApi()

    await renderProducts('/produk?stok=banyak')

    expect(filter('Semua')).toHaveAttribute('aria-pressed', 'true')
    expect(names()).toHaveLength(5)
  })

  it('searches by name once typing pauses, within the chosen filter', async () => {
    const api = stubProductApi()
    const user = await renderProducts('/produk?stok=hampir-habis')

    await user.type(screen.getByRole('searchbox', { name: 'Cari nama barang' }), ' telur ')

    await waitFor(() => expect(names()).toEqual(['Telur Ayam 1 kg']))
    const searches = requestedPaths(api).filter((path) => path.includes('search='))
    expect(searches).toEqual([expect.stringMatching(/search=telur.*stockStatus=Low/)])
  })

  it.each([
    ['/produk', 'rokok', 'Tidak ada barang dengan nama "rokok".'],
    ['/produk?stok=minus', 'gula', 'Tidak ada barang dengan nama "gula" di "Minus".'],
  ])('says so when nothing at %s matches "%s"', async (path, text, message) => {
    stubProductApi()
    const user = await renderProducts(path)

    await user.type(screen.getByRole('searchbox', { name: 'Cari nama barang' }), text)

    expect(await screen.findByText(message)).toBeInTheDocument()
  })

  it.each([
    ['Hampir habis', 'Tidak ada barang yang hampir habis.'],
    ['Minus', 'Tidak ada barang yang stoknya minus.'],
  ])('says so when no product is "%s"', async (label, message) => {
    stubProductApi([catalogue[0]])
    const user = await renderProducts()

    await user.click(filter(label))

    expect(await screen.findByText(message)).toBeInTheDocument()
  })

  it('invites adding the first product when the store has none', async () => {
    stubProductApi([])

    renderApp('/produk')

    expect(await screen.findByRole('heading', { name: 'Belum ada barang' })).toBeInTheDocument()
    expect(screen.getByText('Belum ada barang di toko')).toBeInTheDocument()
    expect(
      screen.getByText('Tambahkan barang yang dijual di warung supaya bisa mulai mencatat penjualan.'),
    ).toBeInTheDocument()
    expect(screen.queryByRole('searchbox')).not.toBeInTheDocument()
    screen.getAllByRole('link', { name: 'Tambah Produk' }).forEach((link) => {
      expect(link).toHaveAttribute('href', '/produk/baru')
    })
  })

  it('offers a retry when the products cannot be loaded, and loads them on retry', async () => {
    let isDown = true
    stubProductApi(catalogue, {
      '/products?page=1&pageSize=50': () => (isDown ? jsonResponse(500, {}) : page(catalogue)),
    })
    const user = renderApp('/produk')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')

    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    await waitFor(() => expect(names()).toHaveLength(5))
  })

  it('still lists the products when the count cannot be loaded', async () => {
    stubProductApi(catalogue, { '/products?page=1&pageSize=1': () => jsonResponse(500, {}) })

    await renderProducts()

    expect(names()).toHaveLength(5)
    expect(screen.queryByText(/barang di toko/)).not.toBeInTheDocument()
  })

  it('loads more products on demand', async () => {
    stubProductApi(catalogue, {
      '/products?page=1&pageSize=50': () => page(catalogue.slice(0, 2), true, 5),
      '/products?page=2&pageSize=50': () => page(catalogue.slice(2), false, 5),
    })
    const user = await renderProducts()
    expect(names()).toHaveLength(2)

    await user.click(screen.getByRole('button', { name: 'Tampilkan lebih banyak' }))

    await waitFor(() => expect(names()).toHaveLength(5))
    expect(screen.queryByRole('button', { name: 'Tampilkan lebih banyak' })).not.toBeInTheDocument()
  })
})
