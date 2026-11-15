import { formatNumber } from '../lib/format'
import { TextField, type TextFieldProps } from './TextField'

/** The most digits an amount can have: up to Rp 999.999.999. */
const MaxDigits = 9

type MoneyFieldProps = Omit<TextFieldProps, 'value' | 'onChange' | 'prefix' | 'type' | 'inputMode'> & {
  /** The amount in Rupiah, or null while the field is empty. */
  value: number | null
  onChange: (value: number | null) => void
}

/** A field for a whole Rupiah amount. It shows "Rp" and separates thousands while typing: `Rp 17.500`. */
export function MoneyField({ value, onChange, ...props }: MoneyFieldProps) {
  function type(text: string) {
    const digits = text.replace(/\D/g, '').slice(0, MaxDigits)
    onChange(digits === '' ? null : Number(digits))
  }

  return (
    <TextField
      prefix="Rp"
      inputMode="numeric"
      autoComplete="off"
      value={value === null ? '' : formatNumber(value)}
      onChange={(event) => type(event.target.value)}
      {...props}
    />
  )
}
