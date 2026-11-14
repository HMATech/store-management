import { Eye, EyeOff, Lock } from 'lucide-react'
import { useState } from 'react'
import { TextField, type TextFieldProps } from './TextField'

type PasswordFieldProps = Omit<TextFieldProps, 'type' | 'icon' | 'trailing'>

/** A password input that can show what was typed. Mirrors the Text field component in Loom (Type Password). */
export function PasswordField(props: PasswordFieldProps) {
  const [isShown, setIsShown] = useState(false)
  const ToggleIcon = isShown ? EyeOff : Eye

  return (
    <TextField
      {...props}
      type={isShown ? 'text' : 'password'}
      icon={Lock}
      trailing={
        <button
          type="button"
          aria-pressed={isShown}
          onClick={() => setIsShown((shown) => !shown)}
          className={
            'flex min-h-touch-min shrink-0 items-center gap-6 rounded-sm p-4 text-sm font-semibold'
            + ' text-primary-default focus-visible:outline-2 focus-visible:outline-primary-default'
          }
        >
          <ToggleIcon aria-hidden className="size-20" />
          {isShown ? 'Sembunyikan' : 'Lihat'}
        </button>
      }
    />
  )
}
