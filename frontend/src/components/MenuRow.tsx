import clsx from 'clsx'
import { ChevronRight, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { IconChip } from './IconChip'

type MenuRowProps = {
  icon: LucideIcon
  /** `danger` is for a row that ends or removes something, such as signing out. */
  tone?: 'primary' | 'danger'
  children: ReactNode
} & ({ to: string; onSelect?: never } | { onSelect: () => void; to?: never })

const row =
  'flex min-h-control-lg w-full items-center gap-12 rounded-lg px-16 py-12 text-left font-semibold'
  + ' focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-primary-default'

/** The line between a row and the one above it, inset from the card's edges. */
const divider =
  'relative not-first:before:absolute not-first:before:inset-x-16 not-first:before:top-0'
  + ' not-first:before:h-px not-first:before:bg-border-default'

/**
 * A row of a menu inside a `CardList`: it opens a screen when given `to`, or does something on the spot when
 * given `onSelect`. Mirrors the Menu row component in Loom.
 */
export function MenuRow({ icon, tone = 'primary', to, onSelect, children }: MenuRowProps) {
  const content = (
    <>
      <IconChip icon={icon} tone={tone} />
      <span className={clsx('min-w-0 flex-1 break-words', tone === 'danger' && 'text-danger-default')}>
        {children}
      </span>
      {to !== undefined && <ChevronRight aria-hidden className="size-20 shrink-0 text-text-subtle" />}
    </>
  )

  return (
    <li className={divider}>
      {to !== undefined ? (
        <Link to={to} className={row}>
          {content}
        </Link>
      ) : (
        <button type="button" onClick={onSelect} className={row}>
          {content}
        </button>
      )}
    </li>
  )
}
