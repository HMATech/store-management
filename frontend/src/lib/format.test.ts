import { describe, expect, it } from 'vitest'
import { formatLongDate, formatOccurrence, formatRupiah, toIsoDay } from './format'

/** Intl puts a no-break space after "Rp". */
const plain = (text: string) => text.replace(/ /g, ' ')

describe('formatRupiah', () => {
  it.each([
    [0, 'Rp 0'],
    [4000, 'Rp 4.000'],
    [1245000, 'Rp 1.245.000'],
    [1999.6, 'Rp 2.000'],
    [-15000, '-Rp 15.000'],
    [12345678901, 'Rp 12.345.678.901'],
  ])('writes %d as %s', (amount, expected) => {
    expect(plain(formatRupiah(amount))).toBe(expected)
  })
})

describe('formatLongDate', () => {
  it('writes the weekday, day, month and year in Indonesian', () => {
    expect(formatLongDate(new Date(2026, 10, 8))).toBe('Minggu, 8 November 2026')
  })
})

describe('formatOccurrence', () => {
  const now = new Date(2026, 10, 8, 12, 0)

  it('writes only the time for something today', () => {
    expect(formatOccurrence(new Date(2026, 10, 8, 10, 42), now)).toBe('Pukul 10.42')
  })

  it('writes the first minute of today as today', () => {
    expect(formatOccurrence(new Date(2026, 10, 8, 0, 0), now)).toBe('Pukul 00.00')
  })

  it('adds the date for something on another day', () => {
    expect(formatOccurrence(new Date(2026, 10, 7, 23, 59), now)).toBe('7 Nov, 23.59')
  })
})

describe('toIsoDay', () => {
  it.each([
    [new Date(2026, 10, 8, 23, 59), '2026-11-08'],
    [new Date(2026, 0, 1, 0, 0), '2026-01-01'],
    [new Date(2026, 11, 31, 12, 0), '2026-12-31'],
  ])('writes the local day of %s as %s', (date, expected) => {
    expect(toIsoDay(date)).toBe(expected)
  })
})
