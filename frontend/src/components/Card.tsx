import clsx from 'clsx'
import type { ReactNode } from 'react'
import { Link } from 'react-router'

const surface = 'rounded-lg border-thin border-border-default bg-bg-surface'

/** A bordered surface for a block of content. */
export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={clsx(surface, className)}>{children}</div>
}

/** A card holding rows, with a divider between them. Use `CardRow` for each row. */
export function CardList({ children }: { children: ReactNode }) {
  return <ul className={clsx(surface, 'flex flex-col')}>{children}</ul>
}

/** The line between a row and the one above it, inset from the card's edges. */
const rowDivider =
  'relative not-first:before:absolute not-first:before:inset-x-16 not-first:before:top-0'
  + ' not-first:before:h-px not-first:before:bg-border-default'

const rowLayout = 'flex items-center gap-12 px-16 py-12'

export function CardRow({ children }: { children: ReactNode }) {
  return <li className={clsx(rowDivider, rowLayout)}>{children}</li>
}

/** A row that opens another screen when tapped anywhere on it. */
export function CardLinkRow({ to, children }: { to: string; children: ReactNode }) {
  return (
    <li className={rowDivider}>
      <Link
        to={to}
        className={clsx(
          rowLayout,
          'rounded-lg focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-primary-default',
        )}
      >
        {children}
      </Link>
    </li>
  )
}
