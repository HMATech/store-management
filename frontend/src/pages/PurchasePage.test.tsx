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
const catalogue = [indomie, aqua, gula, minyak]

const page = (responses: unknown[], hasNextPage = false) => jsonResponse(200, { responses, hasNextPage })

/** An API with the catalogue, where a search filters by name as the real one does. */
function stubPurchaseApi(overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/products': (path) => {
      const search = new URLSearchParams(path.split('?')[1]).get('search')?.toLowerCase()
      return page(search ? catalogue.filter((item) => item.name.toLowerCase().includes(search)) : catalogue)
    },
    '/purchases': () => new Response(null, { status: 201 }),
    ...overrides,
  })
}

type User = ReturnType<typeof renderApp>

/** The product's card in the list. A picked product is also named in the basket, after the list. */
const card = (name: string) => within(screen.getAllByText(name)[0].closest('li')!)
const products = () => within(screen.getByRole('list', { name: 'Daftar barang' }))
const basket = () => within(screen.getByRole('region', { name: 'Belanjaan' }))
const saveButton = () => screen.getByRole('button', { name: 'Simpan Belanja' })
const total = () => basket().getByRole('status', { name: 'Total' })
const supplierField = () => screen.getByRole('textbox', { name: 'Belanja dari' })
const costField = (name: string) => screen.getByRole('textbox', { name: `Harga beli ${name}, per barang` })
const quantityField = (name: string) => screen.getByRole('textbox', { name: `Ketik jumlah ${name}` })
const sentPurchases = (api: ReturnType<typeof stubApi>) =>
  api.mock.calls.filter(([input]) => String(input).endsWith('/purchases'))

async function renderPurchasePage() {
  const user = renderApp('/purchases/new')
  await screen.findByText('Indomie Goreng')
  return user
}

/** Adds a product with how many were bought and what each cost. */
async function pick(user: User, name: string, quantity: number, unitCost?: number) {
  await user.click(screen.getByRole('button', { name: `Tambah ${name}` }))
  if (quantity !== 1) {
    await user.clear(quantityField(name))
    await user.type(quantityField(name), String(quantity))
  }
  if (unitCost !== undefined) {
    await user.type(costField(name), String(unitCost))
  }
}

describe('purchase screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('lists the products with their selling price and stock', async () => {
    stubPurchaseApi()

    await renderPurchasePage()

    expect(card('Indomie Goreng').getByText(`Harga jual ${rupiah(3500)}`)).toBeInTheDocument()
    expect(card('Indomie Goreng').getByText('Stok 24')).toBeInTheDocument()
    expect(card('Gula Pasir 1 kg').getByText('Sisa 3')).toBeInTheDocument()
    expect(card('Minyak Goreng 2L').getByText('Stok minus 2')).toBeInTheDocument()
  })

  it('is a task screen on phones: a way back, and no bottom navigation', async () => {
    stubPurchaseApi()

    await renderPurchasePage()

    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/')
    expect(screen.getAllByRole('navigation', { name: 'Menu utama' })).toHaveLength(1)
  })

  it('starts with nothing picked, which cannot be saved', async () => {
    stubPurchaseApi()

    await renderPurchasePage()

    expect(total()).toHaveTextContent(rupiah(0))
    expect(basket().getByText('0 barang')).toBeInTheDocument()
    expect(saveButton()).toBeDisabled()
    expect(screen.queryByRole('textbox', { name: /Harga beli/ })).not.toBeInTheDocument()
  })

  it('totals the purchase from the typed quantities and buying prices, not the selling prices', async () => {
    stubPurchaseApi()
    const user = await renderPurchasePage()

    await pick(user, 'Indomie Goreng', 40, 3000)
    await pick(user, 'Gula Pasir 1 kg', 10, 15500)

    expect(costField('Indomie Goreng')).toHaveValue('3.000')
    expect(total()).toHaveTextContent(rupiah(275000))
    expect(basket().getByText('50 barang')).toBeInTheDocument()
    expect(basket().getByText(`40 × ${rupiah(3000)}`)).toBeInTheDocument()
    expect(basket().getByText(rupiah(120000))).toBeInTheDocument()
  })

  it('changes a quantity with the buttons, and drops the product when its last one is removed', async () => {
    stubPurchaseApi()
    const user = await renderPurchasePage()
    await pick(user, 'Aqua 600 ml', 1, 2500)

    await user.click(screen.getByRole('button', { name: 'Tambah Aqua 600 ml' }))
    expect(quantityField('Aqua 600 ml')).toHaveValue('2')
    expect(total()).toHaveTextContent(rupiah(5000))

    await user.click(screen.getByRole('button', { name: 'Kurangi Aqua 600 ml' }))
    await user.click(screen.getByRole('button', { name: 'Kurangi Aqua 600 ml' }))

    expect(screen.queryByRole('textbox', { name: /Aqua/ })).not.toBeInTheDocument()
    expect(saveButton()).toBeDisabled()
  })

  it('keeps the quantity when its field is emptied, and caps what is typed', async () => {
    stubPurchaseApi()
    const user = await renderPurchasePage()
    await pick(user, 'Indomie Goreng', 12, 3000)

    await user.clear(quantityField('Indomie Goreng'))
    await user.tab()
    expect(quantityField('Indomie Goreng')).toHaveValue('12')

    await user.clear(quantityField('Indomie Goreng'))
    await user.type(quantityField('Indomie Goreng'), '0')
    await user.tab()
    expect(quantityField('Indomie Goreng')).toHaveValue('12')

    await user.clear(quantityField('Indomie Goreng'))
    await user.type(quantityField('Indomie Goreng'), '123456')
    expect(quantityField('Indomie Goreng')).toHaveValue('1234')
  })

  it('takes only digits as a buying price', async () => {
    stubPurchaseApi()
    const user = await renderPurchasePage()
    await pick(user, 'Indomie Goreng', 2)

    await user.type(costField('Indomie Goreng'), '-3rb.500,x')

    expect(costField('Indomie Goreng')).toHaveValue('3.500')
    expect(total()).toHaveTextContent(rupiah(7000))
  })

  it('saves the purchase with its supplier and buying prices, and confirms it', async () => {
    const api = stubPurchaseApi()
    const user = await renderPurchasePage()
    await user.type(supplierField(), '  Toko Sumber Rejeki ')
    await pick(user, 'Indomie Goreng', 40, 3000)
    await pick(user, 'Aqua 600 ml', 1, 0)

    const before = Date.now()
    await user.click(saveButton())

    const dialog = within(await screen.findByRole('alertdialog', { name: 'Belanja tersimpan' }))
    expect(dialog.getByText(`41 barang · ${rupiah(120000)}`)).toBeInTheDocument()
    expect(dialog.getByRole('link', { name: 'Kembali ke Beranda' })).toHaveAttribute('href', '/')

    const [[, request]] = sentPurchases(api)
    const body = JSON.parse(String(request?.body))
    expect(request?.method).toBe('POST')
    expect(body).toMatchObject({
      title: 'Toko Sumber Rejeki',
      tags: [],
      productEntries: [
        { id: 'p-indomie', quantity: 40, unitCost: 3000 },
        { id: 'p-aqua', quantity: 1, unitCost: 0 },
      ],
    })
    expect(new Date(body.occurrenceTime).getTime()).toBeGreaterThanOrEqual(before - 1000)
  })

  it('does not save without a supplier, and says what to fill in', async () => {
    const api = stubPurchaseApi()
    const user = await renderPurchasePage()
    await pick(user, 'Indomie Goreng', 5, 3000)

    await user.click(saveButton())

    expect(screen.getByText('Isi dulu belanja dari mana.')).toBeInTheDocument()
    expect(supplierField()).toBeInvalid()
    expect(basket().getByRole('alert')).toHaveTextContent('Isi dulu "Belanja dari" di atas.')
    expect(sentPurchases(api)).toHaveLength(0)

    await user.type(supplierField(), 'Pasar Induk')
    expect(screen.queryByText('Isi dulu belanja dari mana.')).not.toBeInTheDocument()
    await user.click(saveButton())

    expect(await screen.findByRole('alertdialog', { name: 'Belanja tersimpan' })).toBeInTheDocument()
  })

  it('treats a supplier of only spaces as missing', async () => {
    const api = stubPurchaseApi()
    const user = await renderPurchasePage()
    await user.type(supplierField(), '   ')
    await pick(user, 'Indomie Goreng', 5, 3000)

    await user.click(saveButton())

    expect(screen.getByText('Isi dulu belanja dari mana.')).toBeInTheDocument()
    expect(sentPurchases(api)).toHaveLength(0)
  })

  it('does not save while a picked product has no buying price, and marks which', async () => {
    const api = stubPurchaseApi()
    const user = await renderPurchasePage()
    await user.type(supplierField(), 'Pasar Induk')
    await pick(user, 'Indomie Goreng', 5, 3000)
    await pick(user, 'Gula Pasir 1 kg', 2)

    expect(basket().getByText('2 × harga beli belum diisi')).toBeInTheDocument()
    expect(screen.queryByText('Isi harga belinya dulu.')).not.toBeInTheDocument()

    await user.click(saveButton())

    expect(card('Gula Pasir 1 kg').getByText('Isi harga belinya dulu.')).toBeInTheDocument()
    expect(card('Indomie Goreng').queryByText('Isi harga belinya dulu.')).not.toBeInTheDocument()
    expect(costField('Gula Pasir 1 kg')).toBeInvalid()
    expect(basket().getByRole('alert')).toHaveTextContent('Isi dulu harga beli tiap barang yang dipilih.')
    expect(sentPurchases(api)).toHaveLength(0)

    await user.type(costField('Gula Pasir 1 kg'), '15500')
    await user.click(saveButton())

    expect(await screen.findByRole('alertdialog', { name: 'Belanja tersimpan' })).toBeInTheDocument()
  })

  it('starts a fresh purchase after "Catat Belanja Lagi", with stock loaded again', async () => {
    const api = stubPurchaseApi()
    const user = await renderPurchasePage()
    await user.type(supplierField(), 'Pasar Induk')
    await pick(user, 'Indomie Goreng', 5, 3000)
    const productRequests = requestedPaths(api).filter((path) => path.includes('/products')).length
    await user.click(saveButton())

    await user.click(await screen.findByRole('button', { name: 'Catat Belanja Lagi' }))

    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    expect(supplierField()).toHaveValue('')
    expect(total()).toHaveTextContent(rupiah(0))
    expect(saveButton()).toBeDisabled()
    expect(screen.queryByText('Isi dulu belanja dari mana.')).not.toBeInTheDocument()
    await waitFor(() =>
      expect(requestedPaths(api).filter((path) => path.includes('/products')).length).toBeGreaterThan(productRequests),
    )
  })

  it.each([
    ['the API refuses it', () => jsonResponse(400, {}), 'Belanja belum tersimpan. Coba lagi.'],
    ['the server fails', () => jsonResponse(500, {}), 'Belanja belum tersimpan. Coba lagi.'],
    [
      'a product no longer exists',
      () => jsonResponse(404, {}),
      'Ada barang yang sudah dihapus dari daftar. Muat ulang halaman ini, lalu catat lagi.',
    ],
    [
      'there is no connection',
      () => Promise.reject(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung. Periksa internet, lalu simpan lagi. Belanja tidak akan tercatat dua kali.',
    ],
  ])('keeps what was entered and says so when %s', async (_case, respond, message) => {
    stubPurchaseApi({ '/purchases': respond })
    const user = await renderPurchasePage()
    await user.type(supplierField(), 'Pasar Induk')
    await pick(user, 'Indomie Goreng', 5, 3000)

    await user.click(saveButton())

    expect(await basket().findByRole('alert')).toHaveTextContent(message)
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
    expect(supplierField()).toHaveValue('Pasar Induk')
    expect(quantityField('Indomie Goreng')).toHaveValue('5')
    expect(costField('Indomie Goreng')).toHaveValue('3.000')
    expect(saveButton()).toBeEnabled()
  })

  it('saves on a second try after a failure', async () => {
    let isDown = true
    stubPurchaseApi({
      '/purchases': () => (isDown ? jsonResponse(500, {}) : new Response(null, { status: 201 })),
    })
    const user = await renderPurchasePage()
    await user.type(supplierField(), 'Pasar Induk')
    await pick(user, 'Indomie Goreng', 5, 3000)
    await user.click(saveButton())
    await basket().findByRole('alert')

    isDown = false
    await user.click(saveButton())

    expect(await screen.findByRole('alertdialog', { name: 'Belanja tersimpan' })).toBeInTheDocument()
  })

  it('sends the same id when saving the same purchase again, and a new one once it is changed', async () => {
    let failures = 2
    const api = stubPurchaseApi({
      '/purchases': () => (failures-- > 0 ? jsonResponse(500, {}) : new Response(null, { status: 201 })),
    })
    const user = await renderPurchasePage()
    await user.type(supplierField(), 'Pasar Induk')
    await pick(user, 'Indomie Goreng', 5, 3000)
    await user.click(saveButton())
    await basket().findByRole('alert')
    await user.click(saveButton())
    await waitFor(() => expect(sentPurchases(api)).toHaveLength(2))

    await user.clear(costField('Indomie Goreng'))
    await user.type(costField('Indomie Goreng'), '2900')
    await user.click(saveButton())
    await screen.findByRole('alertdialog', { name: 'Belanja tersimpan' })

    const [first, retry, changed] = sentPurchases(api).map(([, init]) => JSON.parse(String(init?.body)).id)
    expect(retry).toBe(first)
    expect(changed).not.toBe(first)
  })

  it('blocks a second save while the first is still going', async () => {
    let finish: (response: Response) => void = () => {}
    const api = stubPurchaseApi({ '/purchases': () => new Promise<Response>((resolve) => (finish = resolve)) })
    const user = await renderPurchasePage()
    await user.type(supplierField(), 'Pasar Induk')
    await pick(user, 'Indomie Goreng', 5, 3000)

    await user.click(saveButton())

    const saving = await screen.findByRole('button', { name: 'Menyimpan…' })
    expect(saving).toBeDisabled()
    await user.click(saving)
    expect(sentPurchases(api)).toHaveLength(1)
    finish(new Response(null, { status: 201 }))
    expect(await screen.findByRole('alertdialog', { name: 'Belanja tersimpan' })).toBeInTheDocument()
  })

  it('searches by name once typing pauses, and keeps what was already picked', async () => {
    stubPurchaseApi()
    const user = await renderPurchasePage()
    await pick(user, 'Indomie Goreng', 5, 3000)

    await user.type(screen.getByRole('searchbox', { name: 'Cari nama barang' }), 'gula')

    await waitFor(() => expect(products().queryByText('Indomie Goreng')).not.toBeInTheDocument())
    expect(products().getByText('Gula Pasir 1 kg')).toBeInTheDocument()
    expect(total()).toHaveTextContent(rupiah(15000))
  })

  it('says so when no product matches the search', async () => {
    stubPurchaseApi()
    const user = await renderPurchasePage()

    await user.type(screen.getByRole('searchbox', { name: 'Cari nama barang' }), 'rokok')

    expect(await screen.findByText('Tidak ada barang dengan nama "rokok".')).toBeInTheDocument()
  })

  it('points to the products menu when there are no products at all', async () => {
    stubPurchaseApi({ '/products': () => page([]) })

    renderApp('/purchases/new')

    expect(await screen.findByText('Belum ada barang. Tambahkan dulu di menu Produk.')).toBeInTheDocument()
  })

  it('offers a retry when the products cannot be loaded, and loads them on retry', async () => {
    let isDown = true
    stubPurchaseApi({ '/products': () => (isDown ? jsonResponse(500, {}) : page(catalogue)) })
    const user = renderApp('/purchases/new')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')

    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    expect(await screen.findByText('Indomie Goreng')).toBeInTheDocument()
  })
})
