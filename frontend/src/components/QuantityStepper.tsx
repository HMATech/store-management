import { Minus, Plus } from 'lucide-react'

type QuantityStepperProps = {
  /** What is being counted, for screen readers: "Indomie Goreng". */
  itemName: string
  quantity: number
  /** The most `quantity` may reach. The increase button is disabled there. */
  max: number
  onDecrease: () => void
  onIncrease: () => void
}

const stepButton =
  'flex size-44 shrink-0 items-center justify-center rounded-md'
  + ' transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none'
  + ' disabled:opacity-60 disabled:active:scale-100'
  + ' focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default'

/** Changes a quantity one step at a time. Mirrors the Stepper component in Loom. */
export function QuantityStepper({ itemName, quantity, max, onDecrease, onIncrease }: QuantityStepperProps) {
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
      <output aria-live="polite" className="min-w-28 text-center text-2xl font-bold">
        {quantity}
      </output>
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
