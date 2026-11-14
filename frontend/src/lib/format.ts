/** Formatting for the store's locale. Dates and times are shown in the device's time zone. */

const Locale = 'id-ID'

const rupiah = new Intl.NumberFormat(Locale, { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 })
const longDate = new Intl.DateTimeFormat(Locale, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
const shortDate = new Intl.DateTimeFormat(Locale, { day: 'numeric', month: 'short' })
const time = new Intl.DateTimeFormat(Locale, { hour: '2-digit', minute: '2-digit' })

/** The device's IANA time zone, such as `Asia/Jakarta`. The API counts days in the zone it is given. */
export const deviceTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone

/** `1245000` as `Rp 1.245.000`. */
export function formatRupiah(amount: number): string {
  return rupiah.format(amount)
}

/** A date as `Minggu, 8 November 2026`. */
export function formatLongDate(date: Date): string {
  return longDate.format(date)
}

/** When something happened: `Pukul 10.42` for today, and `8 Nov, 10.42` for another day. */
export function formatOccurrence(occurredAt: Date, now: Date = new Date()): string {
  return toIsoDay(occurredAt) === toIsoDay(now)
    ? `Pukul ${time.format(occurredAt)}`
    : `${shortDate.format(occurredAt)}, ${time.format(occurredAt)}`
}

/** The local calendar day as `2026-11-08`, the form the API takes for `from` and `to`. */
export function toIsoDay(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}
