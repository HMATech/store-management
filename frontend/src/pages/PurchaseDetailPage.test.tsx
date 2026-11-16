import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatLongDate, formatRupiah } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

type Api = ReturnType<typeof stubApi>

const boughtAt = new Date(2026, 10, 8, 8, 30)
const purchase = {
  id: 'b1',
  title: 'Toko Sumber Rejeki',
  tags: [],
  productEntries: [
    { id: 'e1', productId: 'p1', productName: 'Indomie Goreng', unitCost: 3000, quantity: 40, totalCost: 120000 },
    { id: 'e2', productId: 'p2', productName: 'Gula Pasir 1 kg', unitCost: 15500, quantity: 10, totalCost: 155000 },
  ],
  totalCost: 275000,
  occurrenceTime: boughtAt.toISOString(),
}

/** An API that knows the purchase and answers a delete with `onDelete`. */
function stubPurchaseApi(onDelete: () => Response | Promise<Response> = () => new Response(null, { status: 200 })) {
  const api: Api = stubApi({
    '/purchases/b1': () =>
      api.mock.calls.at(-1)?.[1]?.method === 'DELETE' ? onDelete() : jsonResponse(200, purchase),
    '/transactions': () => jsonResponse(200, { responses: [], hasNextPage: false, totalCount: 0 }),
  })
  return api
}

const deletions = (api: Api) => api.mock.calls.filter(([, request]) => request?.method === 'DELETE')

async function renderPurchase() {
  const user = renderApp('/history/purchases/b1')
  await screen.findByText('Barang yang dibeli')
  return user
}

async function askToDelete(user: ReturnType<typeof renderApp>) {
  await user.click(screen.getByRole('button', { name: 'Hapus Belanja' }))
  return within(await screen.findByRole('alertdialog', { name: 'Hapus belanja ini?' }))
}

describe('purchase detail screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('shows where it came from, when, its cost and how much was bought', async () => {
    stubPurchaseApi()

    await renderPurchase()

    expect(screen.getByRole('heading', { name: 'Detail Belanja', level: 1 })).toBeInTheDocument()
    expect(screen.getByText('Dari Toko Sumber Rejeki')).toBeInTheDocument()
    expect(screen.getByText(`${formatLongDate(boughtAt)} · pukul 08.30`)).toBeInTheDocument()
    expect(screen.getByText('2 jenis barang · 50 item')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/history')
  })

  it('lists what was bought at the cost paid', async () => {
    stubPurchaseApi()

    await renderPurchase()

    expect(screen.getAllByRole('listitem').map((line) => line.textContent)).toEqual([
      `Indomie Goreng40 × ${formatRupiah(3000)}${formatRupiah(120000)}`,
      `Gula Pasir 1 kg10 × ${formatRupiah(15500)}${formatRupiah(155000)}`,
    ])
    expect(screen.getByText('Total').parentElement).toHaveTextContent(rupiah(275000))
  })

  it('offers to change its items', async () => {
    stubPurchaseApi()

    await renderPurchase()

    expect(screen.getByRole('link', { name: 'Ubah Barang' })).toHaveAttribute('href', '/history/purchases/b1/edit')
  })

  it('asks before deleting, then deletes and says so in the history', async () => {
    const api = stubPurchaseApi()
    const user = await renderPurchase()

    const dialog = await askToDelete(user)
    expect(
      dialog.getByText(
        `Belanja ${rupiah(275000)} dari Toko Sumber Rejeki pukul 08.30 akan dihapus. Stok barangnya akan dikurangi lagi.`,
      ),
    ).toBeInTheDocument()
    await user.click(dialog.getByRole('button', { name: 'Ya, Hapus' }))

    expect(await screen.findByText('Belanja sudah dihapus.')).toBeInTheDocument()
    expect(deletions(api)).toHaveLength(1)
  })

  it('keeps the purchase on "Batal"', async () => {
    const api = stubPurchaseApi()
    const user = await renderPurchase()
    const dialog = await askToDelete(user)

    await user.click(dialog.getByRole('button', { name: 'Batal' }))

    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    expect(deletions(api)).toHaveLength(0)
  })

  it.each([
    ['the server fails', () => jsonResponse(500, {}), 'Belanja belum dihapus. Coba lagi.'],
    [
      'there is no connection',
      () => Promise.reject(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung, belanja belum dihapus. Periksa internet, lalu coba lagi.',
    ],
  ])('stays open and says so when %s', async (_case, respond, message) => {
    stubPurchaseApi(respond)
    const user = await renderPurchase()
    const dialog = await askToDelete(user)

    await user.click(dialog.getByRole('button', { name: 'Ya, Hapus' }))

    expect(await dialog.findByRole('alert')).toHaveTextContent(message)
  })

  it('says the purchase is not there on a 404', async () => {
    stubApi({ '/purchases/b1': () => jsonResponse(404, {}) })

    renderApp('/history/purchases/b1')

    expect(await screen.findByRole('heading', { name: 'Belanja tidak ditemukan' })).toBeInTheDocument()
  })
})
