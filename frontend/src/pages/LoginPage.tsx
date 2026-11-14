import { useMutation } from '@tanstack/react-query'
import { Mail, Store, User } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router'
import { Button } from '../components/Button'
import { InfoNote } from '../components/InfoNote'
import { PasswordField } from '../components/PasswordField'
import { TextField } from '../components/TextField'
import { login } from '../features/auth/login'
import { useIsSignedIn } from '../features/auth/useIsSignedIn'
import { ApiError } from '../lib/api'

type FieldErrors = { email?: string; password?: string }

export function LoginPage() {
  const isSignedIn = useIsSignedIn()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const signIn = useMutation({ mutationFn: () => login(email.trim(), password) })

  // The API answers a wrong email or password with a 400. The design shows that on the password field.
  const isRefused = signIn.error instanceof ApiError && signIn.error.status === 400

  if (isSignedIn) {
    return <Navigate to="/" replace />
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (signIn.isPending) {
      return
    }

    const errors = validate(email, password)
    setFieldErrors(errors)
    if (Object.keys(errors).length === 0) {
      signIn.mutate()
    }
  }

  return (
    <main className="min-h-dvh bg-bg-surface">
      <div className="mx-auto flex min-h-dvh w-full max-w-[420px] flex-col justify-center gap-32 px-24 py-40">
        <header className="flex flex-col items-center gap-12 text-center">
          <div className="flex size-80 items-center justify-center rounded-xl bg-primary-soft">
            <Store aria-hidden className="size-40 text-primary-default" />
          </div>
          <h1 className="text-5xl font-bold">Warung Ortu</h1>
          <p className="text-text-muted">Masuk untuk mulai mencatat jualan</p>
        </header>

        <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-20">
          <TextField
            label="Email"
            icon={Mail}
            type="email"
            name="email"
            autoComplete="username"
            autoCapitalize="none"
            autoCorrect="off"
            enterKeyHint="next"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            error={fieldErrors.email}
          />
          <PasswordField
            label="Kata sandi"
            name="password"
            autoComplete="current-password"
            enterKeyHint="done"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            error={fieldErrors.password ?? (isRefused ? 'Email atau kata sandi salah. Coba lagi.' : undefined)}
          />

          {signIn.isError && !isRefused && (
            <p role="alert" className="rounded-md bg-danger-soft p-16 font-medium text-danger-default">
              {describeFailure(signIn.error)}
            </p>
          )}

          <Button type="submit" className="w-full" disabled={signIn.isPending}>
            {signIn.isPending ? 'Sedang masuk…' : 'Masuk'}
          </Button>
        </form>

        <InfoNote icon={User}>
          Lupa kata sandi? Minta tolong pemilik akun untuk mengaturnya ulang.
        </InfoNote>
      </div>
    </main>
  )
}

function validate(email: string, password: string): FieldErrors {
  const errors: FieldErrors = {}
  if (email.trim() === '') {
    errors.email = 'Email belum diisi.'
  } else if (!/^\S+@\S+$/.test(email.trim())) {
    errors.email = 'Email belum benar. Contoh: nama@email.com'
  }
  if (password === '') {
    errors.password = 'Kata sandi belum diisi.'
  }
  return errors
}

function describeFailure(error: unknown): string {
  return error instanceof ApiError && error.isNetworkError
    ? 'Tidak bisa terhubung. Periksa internet, lalu coba lagi.'
    : 'Ada gangguan. Coba lagi sebentar lagi.'
}
