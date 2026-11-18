import { screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { formatDayHeading, formatDayRange, formatRupiah, toIsoDay } from '../lib/format'
import { setToken } from '../lib/session'
import { createJwt, jsonResponse, requestedPaths, stubApi } from '../test/fixtures'
import { renderApp } from '../test/render'

/** The amount as the screen shows it. Testing Library reads the no-break space after "Rp" as a space. */
const rupiah = (amount: number) => formatRupiah(amount).replace(/ /g, ' ')

const today = new Date()
const daysAgo = (count: number) => new Date(today.getFullYear(), today.getMonth(), today.getDate() - count)

const day = (ago: number, salesTotal: number, grossProfit: number, uncostedQuantity = 0) => ({
  date: toIsoDay(daysAgo(ago)),
  salesTotal,
  salesCount: salesTotal === 0 ? 0 : 20,
  purchasesTotal: 0,
  purchasesCount: 0,
  costOfSales: salesTotal - grossProfit,
  grossProfit,
  uncostedQuantity,
})

/** A week, oldest day first as the API sends it, with nothing sold two days ago. */
const days = [
  day(6, 548000, 83518),
  day(5, 411700, 68610),
  day(4, 693000, 101767),
  day(3, 1010500, 144716, 3),
  day(2, 0, 0),
  day(1, 598500, 82447),
  day(0, 616000, 77286),
]

const sum = (field: 'salesTotal' | 'costOfSales' | 'grossProfit' | 'uncostedQuantity') =>
  days.reduce((total, d) => total + d[field], 0)

const week = {
  salesTotal: sum('salesTotal'),
  salesCount: 120,
  purchasesTotal: 0,
  purchasesCount: 0,
  costOfSales: sum('costOfSales'),
  grossProfit: sum('grossProfit'),
  uncostedQuantity: sum('uncostedQuantity'),
  days,
}

const rows = () => within(screen.getByRole('list')).getAllByRole('listitem')

describe('profit screen', () => {
  beforeEach(() => setToken(createJwt()))

  it('shows the profit of the last seven days and how much of each Rp 100 sold it was', async () => {
    stubApi({ '/reports/daily-totals': () => jsonResponse(200, week) })

    renderApp('/profit')

    expect(await screen.findByText('Untung 7 hari terakhir')).toBeInTheDocument()
    expect(screen.getByText(formatDayRange(daysAgo(6), today))).toBeInTheDocument()
    expect(screen.getAllByText(rupiah(558344))).toHaveLength(2)
    expect(screen.getByText('Rata-rata untung Rp 14 dari tiap Rp 100 penjualan')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Kembali' })).toHaveAttribute('href', '/')
  })

  it('asks for the last seven days, today included, in the device time zone', async () => {
    const api = stubApi({ '/reports/daily-totals': () => jsonResponse(200, week) })

    renderApp('/profit')
    await screen.findByText('Per hari')

    const request = requestedPaths(api).find((path) => path.includes('/reports/daily-totals'))
    expect(request).toContain(`from=${toIsoDay(daysAgo(6))}&to=${toIsoDay(today)}`)
    expect(request).toContain(`timeZone=${encodeURIComponent(Intl.DateTimeFormat().resolvedOptions().timeZone)}`)
  })

  it('lists each day newest first with what was sold and its profit', async () => {
    stubApi({ '/reports/daily-totals': () => jsonResponse(200, week) })

    renderApp('/profit')
    await screen.findByText('Per hari')

    const list = rows()
    expect(list).toHaveLength(7)
    expect(list[0]).toHaveTextContent(`Hari iniTerjual ${rupiah(616000)}${rupiah(77286)}`)
    expect(list[1]).toHaveTextContent(`${formatDayHeading(daysAgo(1), today)}Terjual ${rupiah(598500)}${rupiah(82447)}`)
    expect(list[2]).toHaveTextContent(`Belum ada penjualan${rupiah(0)}`)
    expect(list[6]).toHaveTextContent(rupiah(83518))
  })

  it('points out the days and the week whose profit leaves out items without a recorded purchase', async () => {
    stubApi({ '/reports/daily-totals': () => jsonResponse(200, week) })

    renderApp('/profit')
    await screen.findByText('Per hari')

    expect(rows()[3]).toHaveTextContent('3 barang belum dihitung')
    expect(rows().filter((row) => row.textContent?.includes('belum dihitung'))).toHaveLength(1)
    expect(screen.getByText('Belum termasuk 3 barang yang belum ada catatan belanjanya.')).toBeInTheDocument()
  })

  it('says so when the week sold below cost', async () => {
    const loss = { ...week, costOfSales: 110000, grossProfit: -10000, uncostedQuantity: 0, days: [] }
    stubApi({ '/reports/daily-totals': () => jsonResponse(200, loss) })

    renderApp('/profit')

    expect(await screen.findByText('Rata-rata rugi Rp 10 dari tiap Rp 100 penjualan')).toBeInTheDocument()
  })

  it('leaves out the average when nothing with a known cost was sold', async () => {
    const nothing = { ...week, costOfSales: 0, grossProfit: 0, uncostedQuantity: 0, days: [] }
    stubApi({ '/reports/daily-totals': () => jsonResponse(200, nothing) })

    renderApp('/profit')

    await screen.findByText('Untung 7 hari terakhir')
    expect(screen.queryByText(/dari tiap Rp 100/)).not.toBeInTheDocument()
  })

  it('offers to try again when the totals cannot be loaded', async () => {
    let isDown = true
    stubApi({
      '/reports/daily-totals': () =>
        isDown ? Promise.reject(new TypeError('Failed to fetch')) : jsonResponse(200, week),
    })
    const user = renderApp('/profit')

    const retry = await screen.findByRole('button', { name: 'Coba lagi' })
    isDown = false
    await user.click(retry)

    expect(await screen.findByText('Per hari')).toBeInTheDocument()
  })

  it('treats malformed totals as ones that cannot be loaded', async () => {
    stubApi({ '/reports/daily-totals': () => jsonResponse(200, { ...week, days: [{ date: '2026-11-18' }] }) })

    renderApp('/profit')

    expect(await screen.findByRole('alert')).toHaveTextContent('Tidak bisa memuat.')
  })
})
