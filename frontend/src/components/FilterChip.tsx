import clsx from 'clsx'
import type { ReactNode } from 'react'

type FilterChipProps = {
  isSelected: boolean
  /** A coloured dot before the label, for a filter that stands for a status. */
  dot?: 'warning' | 'danger'
  onSelect: () => void
  children: ReactNode
}

/** One choice in a row of filters. Mirrors the Filter chip component in Loom (State, and its Dot). */
export function FilterChip({ isSelected, dot, onSelect, children }: FilterChipProps) {
  return (
    <button
      type="button"
      aria-pressed={isSelected}
      onClick={onSelect}
      className={clsx(
        'flex min-h-touch-min shrink-0 items-center gap-6 rounded-full px-14 py-12 text-sm font-semibold',
        'transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default',
        isSelected
          ? 'bg-bg-inverse text-text-inverse'
          : 'border-thin border-border-default bg-bg-surface text-text-default',
      )}
    >
      {dot !== undefined && (
        <span
          aria-hidden
          className={clsx('size-8 rounded-full', dot === 'warning' ? 'bg-warning-default' : 'bg-danger-default')}
        />
      )}
      {children}
    </button>
  )
}
