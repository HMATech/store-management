import { Check } from 'lucide-react'
import { formatRupiah } from '../lib/format'
import { Button } from './Button'

export type CheckoutLine = {
  id: string
  name: string
  /** How the amount came about, as in "3 × Rp 3.500". */
  detail: string
  /** The line's amount as shown, already formatted. */
  amount: string
}

type CheckoutPanelProps = {
  /** The panel's name: its heading on wide screens, and what screen readers call it. */
  title: string
  description: string
  /** What the wide panel says while nothing is picked. */
  emptyText: string
  lines: CheckoutLine[]
  totalLabel: string
  totalQuantity: number
  total: number
  /** Why the last save didn't happen, in words for the user. */
  error?: string
  saveLabel: string
  isSaving: boolean
  onSave: () => void
}

/**
 * The total of what was picked and its save button: a bar pinned to the bottom on phones, and a panel that
 * also lists the picked products on wide screens. Mirrors the Checkout bar component and the desktop Order
 * panel in Loom.
 */
export function CheckoutPanel({
  title,
  description,
  emptyText,
  lines,
  totalLabel,
  totalQuantity,
  total,
  error,
  saveLabel,
  isSaving,
  onSave,
}: CheckoutPanelProps) {
  const isEmpty = lines.length === 0

  return (
    <section
      aria-label={title}
      className={
        'sticky bottom-0 flex flex-col bg-bg-surface'
        + ' shadow-[0_-4px_16px_color-mix(in_srgb,var(--color-shadow)_8%,transparent)]'
        + ' lg:top-32 lg:bottom-auto lg:w-380 lg:shrink-0 lg:rounded-lg lg:border-thin'
        + ' lg:border-border-default lg:shadow-none'
      }
    >
      <div className="hidden flex-col lg:flex">
        <div className="flex flex-col gap-4 p-20">
          <h2 className="text-2xl font-bold">{title}</h2>
          <p className="text-xs break-words text-text-muted">{description}</p>
        </div>
        {isEmpty ? (
          <p className="px-20 pb-16 text-sm text-text-muted">{emptyText}</p>
        ) : (
          <ul className="flex max-h-[40dvh] flex-col gap-12 overflow-y-auto overscroll-contain px-20 pt-4 pb-20">
            {lines.map((line) => (
              <li key={line.id} className="flex items-center gap-12">
                <div className="flex min-w-0 flex-1 flex-col gap-2">
                  <span className="font-semibold break-words">{line.name}</span>
                  <span className="text-xs text-text-muted">{line.detail}</span>
                </div>
                <span className="shrink-0 font-bold">{line.amount}</span>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="flex flex-col gap-12 px-16 pt-14 pb-[calc(20px+env(safe-area-inset-bottom,0px))] lg:pb-20">
        <div className="flex items-end justify-between gap-12">
          <div className="flex flex-col gap-2 text-sm">
            <span className="text-text-muted">{totalLabel}</span>
            <span className="font-semibold">{totalQuantity} barang</span>
          </div>
          <output aria-label="Total" className="text-5xl font-extrabold">
            {formatRupiah(total)}
          </output>
        </div>
        {error !== undefined && (
          <p role="alert" className="rounded-md bg-danger-soft p-12 text-sm font-medium text-danger-default">
            {error}
          </p>
        )}
        <Button onClick={onSave} disabled={isEmpty || isSaving}>
          <Check aria-hidden className="size-24" />
          {isSaving ? 'Menyimpan…' : saveLabel}
        </Button>
      </div>
    </section>
  )
}
