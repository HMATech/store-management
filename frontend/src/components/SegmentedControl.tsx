import clsx from 'clsx'

type SegmentedControlProps<T extends string> = {
  /** What the choice is about, for screen readers. */
  label: string
  options: readonly { value: T; label: string }[]
  value: T
  onChange: (value: T) => void
  className?: string
}

/** A row of choices where exactly one is on. Mirrors the Segmented option component in Loom (State). */
export function SegmentedControl<T extends string>({
  label,
  options,
  value,
  onChange,
  className,
}: SegmentedControlProps<T>) {
  return (
    <div role="group" aria-label={label} className={clsx('flex gap-4 rounded-md bg-bg-sunken p-4', className)}>
      {options.map((option) => {
        const isSelected = option.value === value
        return (
          <button
            key={option.value}
            type="button"
            aria-pressed={isSelected}
            onClick={() => onChange(option.value)}
            className={clsx(
              'min-h-touch-min min-w-0 flex-1 rounded-md px-8 py-12 text-sm',
              'transition-colors duration-150 ease-out motion-reduce:transition-none',
              'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default',
              isSelected ? 'bg-bg-surface font-bold text-text-default shadow-sm' : 'font-medium text-text-muted',
            )}
          >
            {option.label}
          </button>
        )
      })}
    </div>
  )
}
