import clsx from 'clsx'
import { Minus, Plus } from 'lucide-react'
import { useState } from 'react'

type QuantityStepperProps = {
  /** What is being counted, for screen readers: "Indomie Goreng". */
  itemName: string
  quantity: number
  /** The most `quantity` may reach. The increase button is disabled there. */
  max: number
  /**
   * The least `quantity` may reach, where the decrease button is disabled. Leave it out where taking the last
   * one away removes the item instead.
   */
  min?: number
  /** `lg` is for a screen whose one job is this number. */
  size?: 'md' | 'lg'
  onDecrease: () => void
  onIncrease: () => void
  /**
   * Makes the number a field that can be typed in, for quantities too large to tap out. Called with `min` or
   * more, or with 1 or more when there is no `min`.
   */
  onChange?: (quantity: number) => void
}

const stepButton =
  'flex shrink-0 items-center justify-center rounded-md'
  + ' transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none'
  + ' disabled:opacity-60 disabled:active:scale-100'
  + ' focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default'

/** Changes a quantity one step at a time. Mirrors the Stepper component in Loom. */
export function QuantityStepper({
  itemName,
  quantity,
  max,
  min,
  size = 'md',
  onDecrease,
  onIncrease,
  onChange,
}: QuantityStepperProps) {
  const isLarge = size === 'lg'
  const button = clsx(stepButton, isLarge ? 'size-56' : 'size-44')
  const icon = isLarge ? 'size-28' : 'size-22'
  const number = isLarge ? 'text-5xl font-extrabold' : 'text-2xl font-bold'

  return (
    <div
      role="group"
      aria-label={`Jumlah ${itemName}`}
      className={clsx('flex shrink-0 items-center', isLarge ? 'gap-16' : 'gap-8')}
    >
      <button
        type="button"
        aria-label={`Kurangi ${itemName}`}
        onClick={onDecrease}
        disabled={min !== undefined && quantity <= min}
        className={`${button} border-medium border-primary-default bg-bg-surface text-primary-default`}
      >
        <Minus aria-hidden className={icon} />
      </button>
      {onChange === undefined ? (
        <output aria-live="polite" className={clsx('min-w-28 text-center', number)}>
          {quantity}
        </output>
      ) : (
        <QuantityInput
          itemName={itemName}
          quantity={quantity}
          min={min ?? 1}
          max={max}
          className={clsx(number, isLarge ? 'min-h-control-lg w-120' : 'min-h-touch-min w-72')}
          onChange={onChange}
        />
      )}
      <button
        type="button"
        aria-label={`Tambah ${itemName}`}
        onClick={onIncrease}
        disabled={quantity >= max}
        className={`${button} bg-primary-default text-primary-on`}
      >
        <Plus aria-hidden className={icon} />
      </button>
    </div>
  )
}

type QuantityInputProps = Pick<QuantityStepperProps, 'itemName' | 'quantity' | 'max'> & {
  min: number
  className: string
  onChange: (quantity: number) => void
}

function QuantityInput({ itemName, quantity, min, max, className, onChange }: QuantityInputProps) {
  // What is being typed. It can be empty for a moment, which the quantity itself never is.
  const [draft, setDraft] = useState<string | null>(null)
  const maxDigits = String(max).length

  function type(text: string) {
    const digits = text.replace(/\D/g, '').slice(0, maxDigits)
    const typed = Number(digits)
    if (digits === '' || typed < min) {
      setDraft(digits)
      return
    }
    const next = Math.min(typed, max)
    setDraft(String(next))
    onChange(next)
  }

  return (
    <input
      aria-label={`Ketik jumlah ${itemName}`}
      inputMode="numeric"
      autoComplete="off"
      value={draft ?? String(quantity)}
      onChange={(event) => type(event.target.value)}
      onFocus={(event) => event.target.select()}
      onBlur={() => setDraft(null)}
      className={clsx(
        'rounded-md border-medium border-border-strong bg-bg-surface px-4 text-center',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default',
        className,
      )}
    />
  )
}
