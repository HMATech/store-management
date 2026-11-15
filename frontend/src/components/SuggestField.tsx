import { Autocomplete } from '@base-ui/react/autocomplete'
import clsx from 'clsx'
import { ChevronDown } from 'lucide-react'
import { useId, useState } from 'react'

type SuggestFieldProps = {
  label: string
  value: string
  onChange: (value: string) => void
  /** What can be picked instead of typed. Anything else may still be typed. */
  suggestions: string[]
  placeholder?: string
  maxLength?: number
  /** Shown under the field, which is then marked invalid. */
  error?: string
}

/**
 * A text field that offers what was used before, and still takes anything typed. Looks like the Text field
 * component in Loom with its chevron (Type Select).
 */
export function SuggestField({ label, value, onChange, suggestions, placeholder, maxLength, error }: SuggestFieldProps) {
  const id = useId()
  const errorId = `${id}-error`
  const hasError = error !== undefined
  const [isOpen, setIsOpen] = useState(false)
  // While its list is open the rest of the page is hidden from screen readers, so it opens only with something
  // to show.
  const typed = value.trim().toLowerCase()
  const hasMatch = suggestions.some((suggestion) => suggestion.toLowerCase().includes(typed))

  return (
    <div className="flex flex-col gap-8">
      <label htmlFor={id} className="text-sm font-semibold">
        {label}
      </label>
      <Autocomplete.Root
        items={suggestions}
        value={value}
        onValueChange={onChange}
        open={isOpen && hasMatch}
        onOpenChange={setIsOpen}
        openOnInputClick
      >
        <Autocomplete.InputGroup
          className={clsx(
            'flex min-h-control-lg items-center gap-12 rounded-md bg-bg-surface px-16',
            'focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-primary-default',
            hasError ? 'border-thick border-danger-default' : 'border-medium border-border-strong',
          )}
        >
          <Autocomplete.Input
            id={id}
            placeholder={placeholder}
            maxLength={maxLength}
            autoComplete="off"
            aria-invalid={hasError}
            aria-describedby={hasError ? errorId : undefined}
            className={
              'min-w-0 flex-1 self-stretch bg-transparent py-14 text-lg outline-none placeholder:text-text-subtle'
            }
          />
          <ChevronDown aria-hidden className="size-24 shrink-0 text-text-subtle" />
        </Autocomplete.InputGroup>
        <Autocomplete.Portal>
          <Autocomplete.Positioner sideOffset={6} className="z-10 outline-none">
            <Autocomplete.Popup
              className={
                'w-(--anchor-width) max-w-(--available-width) rounded-md border-thin border-border-default'
                + ' bg-bg-surface shadow-[0_8px_24px_color-mix(in_srgb,var(--color-shadow)_14%,transparent)]'
              }
            >
              <Autocomplete.List
                className={'max-h-[min(280px,var(--available-height))] overflow-y-auto overscroll-contain p-4'}
              >
                {(suggestion: string) => (
                  <Autocomplete.Item
                    key={suggestion}
                    value={suggestion}
                    className={
                      'flex min-h-touch-min cursor-default items-center rounded-sm px-12 py-10 select-none'
                      + ' data-highlighted:bg-primary-soft'
                    }
                  >
                    {suggestion}
                  </Autocomplete.Item>
                )}
              </Autocomplete.List>
            </Autocomplete.Popup>
          </Autocomplete.Positioner>
        </Autocomplete.Portal>
      </Autocomplete.Root>
      {hasError && (
        <p id={errorId} className="text-xs font-medium text-danger-default">
          {error}
        </p>
      )}
    </div>
  )
}
