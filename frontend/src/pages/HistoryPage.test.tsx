import { screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatDayHeading, formatRupiah, formatTime } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, requestedPaths, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

/** A moment `daysAgo` days back at the given local time. */
function at(daysAgo: number, hour: number, minute: number): Date {
  const now = new Date()
  return new Date(now.getFullYear(), now.getMonth(), now.getDate() - daysAgo, hour, minute)
}

const transaction = (id: string, kind: 'Sale' | 'Purchase', time: Date, quantity: number, total: number) => ({
  id,
  kind,
  title: '',
  occurrenceTime: time.toISOString(),
  lineCount: 1,
  quantity,
  total,
})

/** Newest first, as the API sends them. Noon and earlier, so the times are in the past whenever the test runs. */
const history = [
  transaction('t1', 'Sale', at(0, 0, 2), 3, 27500),
  transaction('t2', 'Sale', at(0, 0, 1), 1, 4000),
  transaction('t3', 'Purchase', at(0, 0, 0), 12, 860000),
  transaction('t4', 'Sale', at(1, 20, 5), 2, 41500),
  transaction('t5', 'Purchase', at(3, 9, 30), 6, 120000),
]

const page = (responses: unknown[], hasNextPage = false) =>
  jsonResponse(200, { responses, hasNextPage, totalCount: responses.length })

/** An API with the history, filtering by kind as the real one does. */
function stubHistoryApi(items = history, overrides: Parameters<typeof stubApi>[0] = {}) {
  return stubApi({
    '/transactions': (path) => {
      const kind = new URLSearchParams(path.split('?')[1]).get('kind')
      return page(items.filter((item) => !kind || item.kind === kind))
    },
    ...overrides,
  })
}

const day = (daysAgo: number) => within(screen.getByRole('region', { name: formatDayHeading(at(daysAgo, 12, 0)) }))
const rows = () => screen.getAllByRole('listitem')
const choice = (name: string) => screen.getByRole('button', { name })

async function renderHistory(path = '/history') {
  const user = renderApp(path)
  await screen.findAllByRole('listitem')
  return user
}

describe('history screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('groups sales and purchases by day, newest first', async () => {
    stubHistoryApi()

    await renderHistory()

    const headings = screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent)
    expect(headings).toEqual(['Hari ini', formatDayHeading(at(1, 12, 0)), formatDayHeading(at(3, 12, 0))])
    expect(headings[1]).toMatch(/^Kemarin, /)
    expect(day(0).getAllByRole('listitem')).toHaveLength(3)
    expect(day(1).getAllByRole('listitem')).toHaveLength(1)
    expect(day(3).getAllByRole('listitem')).toHaveLength(1)
  })

  it('shows what each transaction was, when, how many items and its amount', async () => {
    stubHistoryApi()

    await renderHistory()

    const [sale, , purchase] = day(0).getAllByRole('listitem')
    expect(within(sale).getByText('Penjualan')).toBeInTheDocument()
    expect(within(sale).getByText(`${formatTime(at(0, 0, 2))} · 3 barang`)).toBeInTheDocument()
    expect(within(sale).getByText(`+ ${rupiah(27500)}`)).toBeInTheDocument()
    expect(within(purchase).getByText('Belanja')).toBeInTheDocument()
    expect(within(purchase).getByText(`${formatTime(at(0, 0, 0))} · 12 barang`)).toBeInTheDocument()
    expect(within(purchase).getByText(`− ${rupiah(860000)}`)).toBeInTheDocument()
  })

  it('opens a sale or a purchase from its row', async () => {
    stubHistoryApi()

    await renderHistory()

    const [sale, , purchase] = day(0).getAllByRole('listitem')
    expect(within(sale).getByRole('link')).toHaveAttribute('href', '/history/sales/t1')
    expect(within(purchase).getByRole('link')).toHaveAttribute('href', '/history/purchases/t3')
  })

  it("shows each day's money in, counting sales only", async () => {
    stubHistoryApi()

    await renderHistory()

    expect(day(0).getByText(`Masuk ${rupiah(31500)}`)).toBeInTheDocument()
    expect(day(1).getByText(`Masuk ${rupiah(41500)}`)).toBeInTheDocument()
    expect(day(3).getByText(`Masuk ${rupiah(0)}`)).toBeInTheDocument()
  })

  it.each([
    ['Penjualan', 'Sale', 3],
    ['Belanja', 'Purchase', 2],
  ])('narrows to "%s"', async (label, kind, count) => {
    const api = stubHistoryApi()
    const user = await renderHistory()

    await user.click(choice(label))

    await waitFor(() => expect(rows()).toHaveLength(count))
    expect(choice(label)).toHaveAttribute('aria-pressed', 'true')
    expect(choice('Semua')).toHaveAttribute('aria-pressed', 'false')
    expect(requestedPaths(api).some((path) => path.includes(`kind=${kind}`))).toBe(true)
  })

  it("shows each day's money out when only purchases are listed", async () => {
    stubHistoryApi()

    await renderHistory('/history?kind=purchase')

    expect(choice('Belanja')).toHaveAttribute('aria-pressed', 'true')
    expect(day(0).getByText(`Keluar ${rupiah(860000)}`)).toBeInTheDocument()
    expect(screen.queryByText(/^Masuk/)).not.toBeInTheDocument()
  })

  it('shows everything again with "Semua"', async () => {
    stubHistoryApi()
    const user = await renderHistory('/history?kind=sale')
    expect(rows()).toHaveLength(3)

    await user.click(choice('Semua'))

    await waitFor(() => expect(rows()).toHaveLength(5))
    expect(choice('Semua')).toHaveAttribute('aria-pressed', 'true')
  })

  it('ignores an unknown kind in the address', async () => {
    const api = stubHistoryApi()

    await renderHistory('/history?kind=debt')

    expect(choice('Semua')).toHaveAttribute('aria-pressed', 'true')
    expect(rows()).toHaveLength(5)
    expect(requestedPaths(api).some((path) => path.includes('kind='))).toBe(false)
  })

  it('invites recording a sale when nothing has been recorded', async () => {
    stubHistoryApi([])

    renderApp('/history')

    expect(await screen.findByRole('heading', { name: 'Belum ada transaksi' })).toBeInTheDocument()
    expect(screen.getByText('Penjualan dan belanja yang dicatat akan muncul di sini.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Catat Penjualan' })).toHaveAttribute('href', '/sales/new')
    expect(choice('Semua')).toBeInTheDocument()
  })

  it.each([
    ['Penjualan', 'Belum ada penjualan.', 'Purchase'],
    ['Belanja', 'Belum ada belanja.', 'Sale'],
  ])('says so when there is no "%s"', async (label, message, onlyKind) => {
    stubHistoryApi(history.filter((item) => item.kind === onlyKind))
    const user = await renderHistory()

    await user.click(choice(label))

    expect(await screen.findByText(message)).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Belum ada transaksi' })).not.toBeInTheDocument()
  })

  it('offers a retry when the history cannot be loaded, and loads it on retry', async () => {
    let isDown = true
    stubHistoryApi(history, { '/transactions': () => (isDown ? jsonResponse(500, {}) : page(history)) })
    const user = renderApp('/history')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')

    isDown = false
    await user.click(screen.getByRole('button', { name: 'Coba lagi' }))

    await waitFor(() => expect(rows()).toHaveLength(5))
  })

  it('loads older transactions on demand, and totals a day only once all of it is loaded', async () => {
    stubHistoryApi(history, {
      '/transactions?page=1': () => page(history.slice(0, 2), true),
      '/transactions?page=2': () => page(history.slice(2)),
    })
    const user = await renderHistory()
    expect(rows()).toHaveLength(2)
    expect(screen.queryByText(/^Masuk/)).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Tampilkan lebih banyak' }))

    await waitFor(() => expect(rows()).toHaveLength(5))
    expect(day(0).getByText(`Masuk ${rupiah(31500)}`)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Tampilkan lebih banyak' })).not.toBeInTheDocument()
  })
})
