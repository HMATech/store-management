import { ChevronRight, ShoppingCart } from 'lucide-react'
import { Link } from 'react-router'

/** The home screen's one primary action. Mirrors the Action card component in Loom. */
export function RecordSaleCard() {
  return (
    <Link
      to="/jual"
      className={
        'flex min-h-80 items-center gap-14 rounded-lg bg-primary-default p-14 pr-16 text-primary-on'
        + ' shadow-[0_6px_16px_color-mix(in_srgb,var(--color-primary-default)_28%,transparent)]'
        + ' transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none'
        + ' focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default'
      }
    >
      <span className="flex size-52 shrink-0 items-center justify-center rounded-md bg-bg-surface">
        <ShoppingCart aria-hidden className="size-28 text-primary-default" />
      </span>
      <span className="flex min-w-0 flex-1 flex-col gap-2">
        <span className="text-xl font-bold">Catat Penjualan</span>
        <span className="text-xs text-primary-on-muted">Ketuk setiap ada pembeli</span>
      </span>
      <ChevronRight aria-hidden className="size-24 shrink-0" />
    </Link>
  )
}
