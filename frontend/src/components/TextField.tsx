import clsx from 'clsx'
import type { LucideIcon } from 'lucide-react'
import { useId, type InputHTMLAttributes, type ReactNode } from 'react'

export type TextFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> & {
  label: string
  /** Keeps the label for screen readers only, for a field whose placeholder and icon say what it is. */
  hideLabel?: boolean
  /** Decorative icon at the start of the field. */
  icon?: LucideIcon
  /** A control at the end of the field, such as the password field's show button. */
  trailing?: ReactNode
  /** Shown under the field, which is then marked invalid. */
  error?: string
}

/** A labelled input. Mirrors the Text field component in Loom (Type Text or Search, and its Error state). */
export function TextField({
  label,
  hideLabel = false,
  icon: Icon,
  trailing,
  error,
  className,
  ...props
}: TextFieldProps) {
  const id = useId()
  const errorId = `${id}-error`
  const hasError = error !== undefined

  return (
    <div className="flex flex-col gap-8">
      <label htmlFor={id} className={hideLabel ? 'sr-only' : 'text-sm font-semibold'}>
        {label}
      </label>
      <div
        className={clsx(
          'flex min-h-control-lg items-center gap-12 rounded-md bg-bg-surface px-16',
          'focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-primary-default',
          hasError ? 'border-thick border-danger-default' : 'border-medium border-border-strong',
        )}
      >
        {Icon !== undefined && <Icon aria-hidden className="size-22 shrink-0 text-text-subtle" />}
        <input
          id={id}
          aria-invalid={hasError}
          aria-describedby={hasError ? errorId : undefined}
          className={clsx(
            'min-w-0 flex-1 self-stretch bg-transparent py-14 text-lg outline-none',
            'placeholder:text-text-subtle',
            className,
          )}
          {...props}
        />
        {trailing}
      </div>
      {hasError && (
        <p id={errorId} className="text-xs font-medium text-danger-default">
          {error}
        </p>
      )}
    </div>
  )
}
