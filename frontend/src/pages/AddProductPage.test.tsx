import { screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

type User = ReturnType<typeof renderApp>
type Api = ReturnType<typeof stubApi>

const brands = [
  { id: 1, name: 'Umum' },
  { id: 2, name: 'Indofood' },
]
const units = [
  { id: 1, name: 'pcs' },
  { id: 2, name: 'gram' },
]
const labels = [{ id: 1, name: 'Mi instan' }]

/** An API that knows some brands, units and labels, accepts new ones, and lists no products. */
function stubProductApi(overrides: Parameters<typeof stubApi>[0] = {}) {
  const knownBrands = [...brands]
  return stubApi({
    '/brands': (path) => {
      if (path.endsWith('/brands')) {
        knownBrands.push({ id: 9, name: 'Sedaap' })
        return new Response(null, { status: 201 })
      }
      return jsonResponse(200, { responses: knownBrands })
    },
    '/products/units': () => jsonResponse(200, units),
    '/products/tags': () => jsonResponse(200, labels),
    '/products': () => jsonResponse(200, { responses: [], hasNextPage: false, totalCount: 3 }),
    ...overrides,
  })
}

const field = (name: string | RegExp) => screen.getByLabelText(name)
const saveButton = () => screen.getByRole('button', { name: 'Simpan Produk' })

/** The requests that changed something, as `[path, body]`. */
const writes = (api: Api) =>
  api.mock.calls
    .filter(([, request]) => request?.method === 'POST')
    .map(([input, request]) => [String(input).replace(/^.*\/api/, ''), JSON.parse(String(request?.body))])

/** Waits until the brands, units and labels have been asked for, so a save can match names against them. */
const catalogLoaded = (api: Api) =>
  waitFor(() =>
    expect(api.mock.calls.filter(([input]) => /\/(brands|units|tags)/.test(String(input)))).toHaveLength(3),
  )

async function renderAddProduct() {
  const user = renderApp('/products/new')
  await screen.findByRole('heading', { name: 'Tambah Produk', level: 1 })
  return user
}

async function fill(user: User, values: { name?: string; brand?: string; measure?: string; unit?: string; price?: string; label?: string }) {
  const targets: [string | RegExp, string | undefined][] = [
    ['Nama barang', values.name],
    [/^Merek/, values.brand],
    ['Ukuran', values.measure],
    ['Satuan', values.unit],
    ['Harga jual', values.price],
    [/^Label/, values.label],
  ]
  for (const [label, value] of targets) {
    if (value !== undefined) {
      await user.type(field(label), value)
    }
  }
  // Leaves the last field, which closes its list of suggestions.
  await user.tab()
}

describe('add product screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('is a task screen with a way back to the products', async () => {
    stubProductApi()

    await renderAddProduct()

    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/products')
    expect(screen.getByRole('link', { name: 'Batal' })).toHaveAttribute('href', '/products')
    expect(screen.getAllByRole('navigation', { name: 'Menu utama' })).toHaveLength(1)
    expect(
      screen.getByText('Harga bisa diubah kapan saja. Stok awal dicatat lewat Catat Belanja.'),
    ).toBeInTheDocument()
  })

  it('saves a product with a known brand and unit, then shows it was added', async () => {
    const api = stubProductApi()
    const user = await renderAddProduct()
    await catalogLoaded(api)

    await fill(user, {
      name: ' Indomie Goreng ',
      brand: 'indofood',
      measure: '85',
      unit: 'Gram',
      price: '3500',
      label: 'Mi instan',
    })
    expect(field('Harga jual')).toHaveValue('3.500')
    await user.click(saveButton())

    expect(await screen.findByRole('status')).toHaveTextContent('Indomie Goreng sudah ditambahkan.')
    expect(screen.getByRole('heading', { name: 'Produk', level: 1 })).toBeInTheDocument()
    expect(writes(api)).toEqual([
      [
        '/products',
        { name: 'Indomie Goreng', brandId: 2, measure: 85, measureUnitId: 2, price: 3500, tags: ['Mi instan'] },
      ],
    ])
  })

  it('adds a brand and a unit that are new, then the product with them', async () => {
    const api = stubProductApi({
      '/products/units': (path) =>
        path.endsWith('/units') && api.mock.calls.at(-1)?.[1]?.method === 'POST'
          ? jsonResponse(201, { id: 7, name: 'bungkus' })
          : jsonResponse(200, units),
    })
    const user = await renderAddProduct()
    await catalogLoaded(api)

    await fill(user, { name: 'Mie Sedaap', brand: 'Sedaap', unit: 'bungkus', price: '3000' })
    await user.click(saveButton())

    expect(await screen.findByRole('status')).toHaveTextContent('Mie Sedaap sudah ditambahkan.')
    const sent = writes(api)
    expect(sent).toContainEqual(['/brands', { name: 'Sedaap' }])
    expect(sent).toContainEqual(['/products/units', { name: 'bungkus' }])
    expect(sent.at(-1)).toEqual([
      '/products',
      { name: 'Mie Sedaap', brandId: 9, measure: 1, measureUnitId: 7, price: 3000, tags: [] },
    ])
  })

  it('files a product without a brand under "Umum", with a size of 1 when none is given', async () => {
    const api = stubProductApi()
    const user = await renderAddProduct()
    await catalogLoaded(api)

    await fill(user, { name: 'Telur Ayam', unit: 'pcs', price: '2500' })
    await user.click(saveButton())

    await screen.findByRole('status')
    expect(writes(api)).toEqual([
      ['/products', { name: 'Telur Ayam', brandId: 1, measure: 1, measureUnitId: 1, price: 2500, tags: [] }],
    ])
  })

  it('reads a size written with a comma', async () => {
    const api = stubProductApi()
    const user = await renderAddProduct()
    await catalogLoaded(api)

    await fill(user, { name: 'Minyak Goreng', measure: '1,5', unit: 'pcs', price: '38000' })
    await user.click(saveButton())

    await screen.findByRole('status')
    expect(writes(api)[0][1]).toMatchObject({ measure: 1.5 })
  })

  it('points out what is missing only after saving is tried, and sends nothing', async () => {
    const api = stubProductApi()
    const user = await renderAddProduct()
    expect(screen.queryByText('Nama barang wajib diisi')).not.toBeInTheDocument()

    await user.click(saveButton())

    expect(screen.getByText('Nama barang wajib diisi')).toBeInTheDocument()
    expect(screen.getByText('Satuan wajib diisi, contoh: bungkus')).toBeInTheDocument()
    expect(screen.getByText('Harga jual wajib diisi, contoh: 3500')).toBeInTheDocument()
    expect(field('Nama barang')).toBeInvalid()
    expect(field('Harga jual')).toBeInvalid()
    expect(writes(api)).toEqual([])

    await user.type(field('Nama barang'), 'Kopi')
    expect(screen.queryByText('Nama barang wajib diisi')).not.toBeInTheDocument()
  })

  it.each(['abc', '0', '1,2,3', '-5'])('rejects "%s" as a size', async (measure) => {
    const api = stubProductApi()
    const user = await renderAddProduct()

    await fill(user, { name: 'Kopi', measure, unit: 'pcs', price: '2000' })
    await user.click(saveButton())

    expect(screen.getByText('Isi dengan angka, contoh: 85 atau 1,5')).toBeInTheDocument()
    expect(writes(api)).toEqual([])
  })

  it('treats a name of only spaces as missing', async () => {
    const api = stubProductApi()
    const user = await renderAddProduct()

    await fill(user, { name: '   ', unit: 'pcs', price: '2000' })
    await user.click(saveButton())

    expect(screen.getByText('Nama barang wajib diisi')).toBeInTheDocument()
    expect(writes(api)).toEqual([])
  })

  it.each([
    [
      'the product already exists',
      () => jsonResponse(409, { title: 'Products.AlreadyExist' }),
      'Barang dengan nama dan merek ini sudah ada. Ganti namanya, atau buka barang itu di menu Produk.',
    ],
    ['the API refuses it', () => jsonResponse(400, {}), 'Barang belum tersimpan. Coba lagi.'],
    ['the server fails', () => jsonResponse(500, {}), 'Barang belum tersimpan. Coba lagi.'],
    [
      'there is no connection',
      () => Promise.reject(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung, barang belum tersimpan. Periksa internet, lalu coba lagi.',
    ],
  ])('keeps the form and says so when %s', async (_case, respond, message) => {
    const api = stubProductApi({
      '/products': (path) =>
        path.endsWith('/products') ? respond() : jsonResponse(200, { responses: [], hasNextPage: false }),
    })
    const user = await renderAddProduct()
    await catalogLoaded(api)

    await fill(user, { name: 'Kopi', unit: 'pcs', price: '2000' })
    await user.click(saveButton())

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(screen.getByRole('heading', { name: 'Tambah Produk', level: 1 })).toBeInTheDocument()
    expect(field('Nama barang')).toHaveValue('Kopi')
    expect(field('Harga jual')).toHaveValue('2.000')
    expect(saveButton()).toBeEnabled()
  })

  it('still saves when the suggestions could not be loaded', async () => {
    let isDown = true
    const api = stubProductApi({
      '/products/units': () => (isDown ? jsonResponse(500, {}) : jsonResponse(200, units)),
    })
    const user = await renderAddProduct()
    await catalogLoaded(api)
    isDown = false
    api.mockClear()

    await fill(user, { name: 'Kopi', unit: 'pcs', price: '2000' })
    await user.click(saveButton())

    // With nothing known, the brand and unit are asked for by adding them; both already exist.
    expect(await screen.findByRole('status')).toHaveTextContent('Kopi sudah ditambahkan.')
  })

  it('blocks a second save while the first is still going', async () => {
    let finish: (response: Response) => void = () => {}
    const api = stubProductApi({
      '/products': (path) =>
        path.endsWith('/products')
          ? new Promise<Response>((resolve) => (finish = resolve))
          : jsonResponse(200, { responses: [], hasNextPage: false, totalCount: 1 }),
    })
    const user = await renderAddProduct()
    await catalogLoaded(api)

    await fill(user, { name: 'Kopi', unit: 'pcs', price: '2000' })
    await user.click(saveButton())

    const saving = await screen.findByRole('button', { name: 'Menyimpan…' })
    expect(saving).toBeDisabled()
    await user.click(saving)
    expect(writes(api)).toHaveLength(1)
    finish(new Response(null, { status: 201 }))
    expect(await screen.findByRole('status')).toBeInTheDocument()
  })
})
