import clsx from 'clsx'
import { useId, type InputHTMLAttributes } from 'react'

type TextFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> & {
  label: string
  /** Shown under the field, which is then marked invalid. */
  error?: string
}

/** A labelled input. Mirrors the Text field component in Loom (Type Text/Password, and its Error state). */
export function TextField({ label, error, className, ...props }: TextFieldProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div className="flex flex-col gap-2">
      <label htmlFor={id} className="font-medium">
        {label}
      </label>
      <input
        id={id}
        aria-invalid={error !== undefined}
        aria-describedby={error !== undefined ? errorId : undefined}
        className={clsx(
          'min-h-control-lg rounded-lg border bg-bg-surface px-4 text-text-default',
          'focus-visible:outline-2 focus-visible:outline-primary-default',
          error !== undefined ? 'border-danger-default' : 'border-border-default',
          className,
        )}
        {...props}
      />
      {error !== undefined && (
        <p id={errorId} className="text-sm text-danger-default">
          {error}
        </p>
      )}
    </div>
  )
}
