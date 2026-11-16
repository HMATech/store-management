import { screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatRupiah } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

type Api = ReturnType<typeof stubApi>

const entry = (id: string, productId: string, productName: string, unitCost: number, quantity: number) => ({
  id,
  productId,
  productName,
  unitCost,
  quantity,
  totalCost: unitCost * quantity,
})

const purchase = {
  id: 'b1',
  title: 'Toko Sumber Rejeki',
  tags: [],
  productEntries: [
    entry('e1', 'p-indomie', 'Indomie Goreng', 3000, 40),
    entry('e2', 'p-gula', 'Gula Pasir 1 kg', 15500, 10),
  ],
  totalCost: 275000,
  occurrenceTime: new Date(2026, 10, 7, 9, 15).toISOString(),
  createdTime: '2026-11-07T02:15:00Z',
  lastUpdatedTime: '2026-11-07T02:15:00Z',
}

const product = (id: string, name: string, price: number) => ({
  id,
  name,
  price,
  stock: { quantity: 10, status: 'Normal' },
})
const catalogue = [product('p-aqua', 'Aqua 600 ml', 4000)]

function stubPurchaseApi(onUpdate: () => Response | Promise<Response> = () => jsonResponse(200, {})) {
  return stubApi({
    '/purchases/b1/entries': onUpdate,
    '/purchases/b1': () => jsonResponse(200, purchase),
    '/products': () => jsonResponse(200, { responses: catalogue, hasNextPage: false }),
  })
}

const updates = (api: Api) =>
  api.mock.calls
    .filter(([input, init]) => String(input).endsWith('/purchases/b1/entries') && init?.method === 'PUT')
    .map(([, init]) => JSON.parse(String(init?.body)) as { productEntries: unknown[] })

const recorded = () => within(screen.getByRole('region', { name: 'Barang di belanja ini' }))
const toAdd = () => within(screen.getByRole('region', { name: 'Tambah barang lain' }))
const line = (name: string) => within(recorded().getByText(name).closest('li')!)
const total = () =>
  within(screen.getByRole('region', { name: 'Setelah diubah' })).getByRole('status', { name: 'Total' })
const saveButton = () => screen.getByRole('button', { name: 'Simpan Perubahan' })

async function renderEdit() {
  const user = renderApp('/riwayat/belanja/b1/ubah')
  await screen.findByRole('region', { name: 'Barang di belanja ini' })
  await toAdd().findByText('Aqua 600 ml')
  return user
}

describe('edit purchase screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('shows where it came from and each line with its quantity and cost', async () => {
    stubPurchaseApi()

    await renderEdit()

    expect(screen.getByText(/^Dari Toko Sumber Rejeki · .* · pukul 09\.15$/)).toBeInTheDocument()
    expect(line('Indomie Goreng').getByRole('textbox', { name: 'Ketik jumlah Indomie Goreng' })).toHaveValue('40')
    expect(line('Indomie Goreng').getByRole('textbox', { name: 'Harga beli Indomie Goreng, per barang' })).toHaveValue(
      '3.000',
    )
    expect(total()).toHaveTextContent(rupiah(275000))
  })

  it('saves a corrected quantity and cost, a removed line and an added product', async () => {
    const api = stubPurchaseApi()
    const user = await renderEdit()

    const quantity = line('Indomie Goreng').getByRole('textbox', { name: 'Ketik jumlah Indomie Goreng' })
    await user.clear(quantity)
    await user.type(quantity, '48')
    const cost = line('Indomie Goreng').getByRole('textbox', { name: 'Harga beli Indomie Goreng, per barang' })
    await user.clear(cost)
    await user.type(cost, '2900')
    await user.click(line('Gula Pasir 1 kg').getByRole('button', { name: 'Hapus Gula Pasir 1 kg' }))
    await user.click(toAdd().getByRole('button', { name: 'Tambah Aqua 600 ml' }))
    await user.type(toAdd().getByRole('textbox', { name: 'Harga beli Aqua 600 ml, per barang' }), '2500')

    expect(total()).toHaveTextContent(rupiah(48 * 2900 + 2500))
    await user.click(saveButton())

    expect(await screen.findByText('Perubahan sudah disimpan.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Detail Belanja', level: 1 })).toBeInTheDocument()
    expect(updates(api)).toEqual([
      {
        productEntries: [
          { id: 'e1', productId: 'p-indomie', quantity: 48, unitCost: 2900 },
          { id: null, productId: 'p-aqua', quantity: 1, unitCost: 2500 },
        ],
      },
    ])
  })

  it('points out a cost left empty and sends nothing', async () => {
    const api = stubPurchaseApi()
    const user = await renderEdit()

    await user.clear(line('Indomie Goreng').getByRole('textbox', { name: 'Harga beli Indomie Goreng, per barang' }))
    expect(screen.queryByText('Isi harga belinya dulu.')).not.toBeInTheDocument()
    await user.click(saveButton())

    expect(line('Indomie Goreng').getByText('Isi harga belinya dulu.')).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Belum bisa disimpan. Isi dulu harga beli tiap barang.')
    expect(updates(api)).toEqual([])
  })

  it('points out an added product whose cost is empty', async () => {
    const api = stubPurchaseApi()
    const user = await renderEdit()

    await user.click(toAdd().getByRole('button', { name: 'Tambah Aqua 600 ml' }))
    await user.click(saveButton())

    expect(toAdd().getByText('Isi harga belinya dulu.')).toBeInTheDocument()
    expect(updates(api)).toEqual([])
  })

  it('brings a removed line back with the quantity and cost it was recorded with', async () => {
    stubPurchaseApi()
    const user = await renderEdit()

    const cost = line('Indomie Goreng').getByRole('textbox', { name: 'Harga beli Indomie Goreng, per barang' })
    await user.clear(cost)
    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Hapus Indomie Goreng' }))
    expect(line('Indomie Goreng').getByText(`40 × ${rupiah(3000)}`)).toBeInTheDocument()

    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Batal hapus Indomie Goreng' }))

    expect(line('Indomie Goreng').getByRole('textbox', { name: 'Harga beli Indomie Goreng, per barang' })).toHaveValue(
      '3.000',
    )
    expect(total()).toHaveTextContent(rupiah(275000))
  })

  it('refuses to save a purchase with nothing left in it', async () => {
    stubPurchaseApi()
    const user = await renderEdit()

    await user.click(line('Indomie Goreng').getByRole('button', { name: 'Hapus Indomie Goreng' }))
    await user.click(line('Gula Pasir 1 kg').getByRole('button', { name: 'Hapus Gula Pasir 1 kg' }))

    expect(screen.getByText(/Kalau belanja ini salah catat, hapus dari layar Detail Belanja/)).toBeInTheDocument()
    expect(saveButton()).toBeDisabled()
  })

  it('keeps the changes when saving fails', async () => {
    stubPurchaseApi(() => jsonResponse(500, {}))
    const user = await renderEdit()

    await user.click(line('Gula Pasir 1 kg').getByRole('button', { name: 'Hapus Gula Pasir 1 kg' }))
    await user.click(saveButton())

    expect(await screen.findByRole('alert')).toHaveTextContent('Perubahan belanja belum tersimpan. Coba lagi.')
    expect(line('Gula Pasir 1 kg').getByText('Akan dihapus')).toBeInTheDocument()
  })
})
