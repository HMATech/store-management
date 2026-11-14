import clsx from 'clsx'
import type { ReactNode } from 'react'

const surface = 'rounded-lg border-thin border-border-default bg-bg-surface'

/** A bordered surface for a block of content. */
export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={clsx(surface, className)}>{children}</div>
}

/** A card holding rows, with a divider between them. Use `CardRow` for each row. */
export function CardList({ children }: { children: ReactNode }) {
  return <ul className={clsx(surface, 'flex flex-col')}>{children}</ul>
}

export function CardRow({ children }: { children: ReactNode }) {
  return (
    <li
      className={
        'relative flex items-center gap-12 px-16 py-12'
        + ' not-first:before:absolute not-first:before:inset-x-16 not-first:before:top-0'
        + ' not-first:before:h-px not-first:before:bg-border-default'
      }
    >
      {children}
    </li>
  )
}
