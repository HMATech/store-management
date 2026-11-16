import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatLongDate, formatRupiah, formatTime } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

type Api = ReturnType<typeof stubApi>
type User = ReturnType<typeof renderApp>

const soldAt = new Date(2026, 10, 8, 10, 42)
const entry = (id: string, productName: string, priceValue: number, quantity: number) => ({
  id,
  productId: `p-${id}`,
  productName,
  priceId: `pr-${id}`,
  priceValue,
  quantity,
  totalPrice: priceValue * quantity,
})
const sale = {
  id: 's1',
  title: '',
  tags: [],
  productEntries: [
    entry('e1', 'Gula Pasir 1 kg', 17500, 1),
    entry('e2', 'Aqua 600 ml', 4000, 1),
    entry('e3', 'Kopi Kapal Api', 2000, 3),
  ],
  totalPrice: 27500,
  occurrenceTime: soldAt.toISOString(),
}

/** An API that knows the sale, lets it be deleted, and has an empty history. */
function stubSaleApi(overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/sales/s1': () => jsonResponse(200, sale),
    '/transactions': () => jsonResponse(200, { responses: [], hasNextPage: false, totalCount: 0 }),
    ...overrides,
  })
}

const deletions = (api: Api) =>
  api.mock.calls.filter(([, request]) => request?.method === 'DELETE').map(([input]) => String(input))

async function renderSale() {
  const user = renderApp('/riwayat/penjualan/s1')
  await screen.findByText('Barang yang dibeli')
  return user
}

async function askToDelete(user: User) {
  await user.click(screen.getByRole('button', { name: 'Hapus Penjualan' }))
  return within(await screen.findByRole('alertdialog', { name: 'Hapus penjualan ini?' }))
}

describe('sale detail screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('shows when the sale was made, its total, and how much was sold', async () => {
    stubSaleApi()

    await renderSale()

    expect(screen.getByText(`${formatLongDate(soldAt)} · pukul 10.42`)).toBeInTheDocument()
    expect(screen.getByText('3 jenis barang · 5 item')).toBeInTheDocument()
    expect(screen.getAllByText(rupiah(27500))).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/riwayat')
    const menu = within(screen.getByRole('navigation', { name: 'Menu utama' }))
    expect(menu.getByRole('link', { name: 'Riwayat' })).toHaveAttribute('aria-current', 'page')
    expect(menu.getByRole('link', { name: 'Beranda' })).not.toHaveAttribute('aria-current')
  })

  it('lists what was sold at the price it was sold for', async () => {
    stubSaleApi()

    await renderSale()

    const lines = screen.getAllByRole('listitem').map((line) => line.textContent)
    expect(lines).toEqual([
      `Gula Pasir 1 kg1 × ${formatRupiah(17500)}${formatRupiah(17500)}`,
      `Aqua 600 ml1 × ${formatRupiah(4000)}${formatRupiah(4000)}`,
      `Kopi Kapal Api3 × ${formatRupiah(2000)}${formatRupiah(6000)}`,
    ])
    expect(screen.getByText('Total').parentElement).toHaveTextContent(rupiah(27500))
  })

  it('offers to change its items', async () => {
    stubSaleApi()

    await renderSale()

    expect(screen.getByRole('link', { name: 'Ubah Barang' })).toHaveAttribute('href', '/riwayat/penjualan/s1/ubah')
  })

  it('asks before deleting, saying what will happen', async () => {
    const api = stubSaleApi()
    const user = await renderSale()

    const dialog = await askToDelete(user)

    expect(
      dialog.getByText(
        `Penjualan ${rupiah(27500)} pukul ${formatTime(soldAt)} akan dihapus. Stok barangnya akan dikembalikan.`,
      ),
    ).toBeInTheDocument()
    expect(deletions(api)).toEqual([])
  })

  it('keeps the sale on "Batal"', async () => {
    const api = stubSaleApi()
    const user = await renderSale()
    const dialog = await askToDelete(user)

    await user.click(dialog.getByRole('button', { name: 'Batal' }))

    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    expect(deletions(api)).toEqual([])
    expect(screen.getByText('Barang yang dibeli')).toBeInTheDocument()
  })

  it('deletes the sale on "Ya, Hapus" and says so in the history', async () => {
    const api = stubSaleApi({
      '/sales/s1': (path) =>
        api.mock.calls.at(-1)?.[1]?.method === 'DELETE' && path.endsWith('/sales/s1')
          ? new Response(null, { status: 200 })
          : jsonResponse(200, sale),
    })
    const user = await renderSale()
    const dialog = await askToDelete(user)

    await user.click(dialog.getByRole('button', { name: 'Ya, Hapus' }))

    expect(await screen.findByText('Penjualan sudah dihapus.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Riwayat', level: 1 })).toBeInTheDocument()
    expect(deletions(api)).toHaveLength(1)
    expect(deletions(api)[0]).toMatch(/\/sales\/s1$/)
  })

  it.each([
    ['the server fails', () => jsonResponse(500, {}), 'Penjualan belum dihapus. Coba lagi.'],
    ['the sale is already gone', () => jsonResponse(404, {}), 'Penjualan ini sudah dihapus. Kembali ke Riwayat.'],
    [
      'there is no connection',
      () => Promise.reject(new TypeError('Failed to fetch')),
      'Tidak bisa terhubung, penjualan belum dihapus. Periksa internet, lalu coba lagi.',
    ],
  ])('stays open and says so when %s', async (_case, respond, message) => {
    const api = stubSaleApi({
      '/sales/s1': () => (api.mock.calls.at(-1)?.[1]?.method === 'DELETE' ? respond() : jsonResponse(200, sale)),
    })
    const user = await renderSale()
    const dialog = await askToDelete(user)

    await user.click(dialog.getByRole('button', { name: 'Ya, Hapus' }))

    expect(await dialog.findByRole('alert')).toHaveTextContent(message)
    expect(dialog.getByRole('button', { name: 'Ya, Hapus' })).toBeEnabled()
    expect(screen.getByRole('heading', { name: 'Detail Penjualan', level: 1, hidden: true })).toBeInTheDocument()
  })

  it('forgets a failed try when the question is asked again', async () => {
    const api = stubSaleApi({
      '/sales/s1': () =>
        api.mock.calls.at(-1)?.[1]?.method === 'DELETE' ? jsonResponse(500, {}) : jsonResponse(200, sale),
    })
    const user = await renderSale()
    let dialog = await askToDelete(user)
    await user.click(dialog.getByRole('button', { name: 'Ya, Hapus' }))
    await dialog.findByRole('alert')

    await user.click(dialog.getByRole('button', { name: 'Batal' }))
    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    dialog = await askToDelete(user)

    expect(dialog.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('blocks a second delete while the first is still going', async () => {
    let finish: (response: Response) => void = () => {}
    const api = stubSaleApi({
      '/sales/s1': () =>
        api.mock.calls.at(-1)?.[1]?.method === 'DELETE'
          ? new Promise<Response>((resolve) => (finish = resolve))
          : jsonResponse(200, sale),
    })
    const user = await renderSale()
    const dialog = await askToDelete(user)

    await user.click(dialog.getByRole('button', { name: 'Ya, Hapus' }))

    const working = await dialog.findByRole('button', { name: 'Menghapus…' })
    expect(working).toBeDisabled()
    expect(dialog.getByRole('button', { name: 'Batal' })).toBeDisabled()
    await user.click(working)
    expect(deletions(api)).toHaveLength(1)
    finish(new Response(null, { status: 200 }))
    expect(await screen.findByText('Penjualan sudah dihapus.')).toBeInTheDocument()
  })

  it.each([404, 400])('says the sale is not there on a %d', async (status) => {
    stubSaleApi({ '/sales/s1': () => jsonResponse(status, {}) })

    renderApp('/riwayat/penjualan/s1')

    expect(await screen.findByRole('heading', { name: 'Penjualan tidak ditemukan' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Kembali ke Riwayat' })).toHaveAttribute('href', '/riwayat')
    expect(screen.queryByRole('button', { name: 'Hapus Penjualan' })).not.toBeInTheDocument()
  })

  it('offers a retry when the sale cannot be loaded, and loads it on retry', async () => {
    let isDown = true
    stubSaleApi({ '/sales/s1': () => (isDown ? jsonResponse(500, {}) : jsonResponse(200, sale)) })
    const user = renderApp('/riwayat/penjualan/s1')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')

    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    expect(await screen.findByText('Barang yang dibeli')).toBeInTheDocument()
  })
})
