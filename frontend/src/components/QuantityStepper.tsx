import { Minus, Plus } from 'lucide-react'
import { useState } from 'react'

type QuantityStepperProps = {
  /** What is being counted, for screen readers: "Indomie Goreng". */
  itemName: string
  quantity: number
  /** The most `quantity` may reach. The increase button is disabled there. */
  max: number
  onDecrease: () => void
  onIncrease: () => void
  /** Makes the number a field that can be typed in, for quantities too large to tap out. Called with 1 or more. */
  onChange?: (quantity: number) => void
}

const stepButton =
  'flex size-44 shrink-0 items-center justify-center rounded-md'
  + ' transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none'
  + ' disabled:opacity-60 disabled:active:scale-100'
  + ' focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default'

/** Changes a quantity one step at a time. Mirrors the Stepper component in Loom. */
export function QuantityStepper({ itemName, quantity, max, onDecrease, onIncrease, onChange }: QuantityStepperProps) {
  return (
    <div role="group" aria-label={`Jumlah ${itemName}`} className="flex shrink-0 items-center gap-8">
      <button
        type="button"
        aria-label={`Kurangi ${itemName}`}
        onClick={onDecrease}
        className={`${stepButton} border-medium border-primary-default bg-bg-surface text-primary-default`}
      >
        <Minus aria-hidden className="size-22" />
      </button>
      {onChange === undefined ? (
        <output aria-live="polite" className="min-w-28 text-center text-2xl font-bold">
          {quantity}
        </output>
      ) : (
        <QuantityInput itemName={itemName} quantity={quantity} max={max} onChange={onChange} />
      )}
      <button
        type="button"
        aria-label={`Tambah ${itemName}`}
        onClick={onIncrease}
        disabled={quantity >= max}
        className={`${stepButton} bg-primary-default text-primary-on`}
      >
        <Plus aria-hidden className="size-22" />
      </button>
    </div>
  )
}

type QuantityInputProps = Pick<QuantityStepperProps, 'itemName' | 'quantity' | 'max'> & {
  onChange: (quantity: number) => void
}

function QuantityInput({ itemName, quantity, max, onChange }: QuantityInputProps) {
  // What is being typed. It can be empty for a moment, which the quantity itself never is.
  const [draft, setDraft] = useState<string | null>(null)
  const maxDigits = String(max).length

  function type(text: string) {
    const digits = text.replace(/\D/g, '').slice(0, maxDigits)
    const typed = Number(digits)
    if (typed < 1) {
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
      className={
        'min-h-touch-min w-72 rounded-md border-medium border-border-strong bg-bg-surface px-4 text-center'
        + ' text-2xl font-bold focus-visible:outline-2 focus-visible:outline-offset-2'
        + ' focus-visible:outline-primary-default'
      }
    />
  )
}
