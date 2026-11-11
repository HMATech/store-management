import { useMutation } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router'
import { Button } from '../components/Button'
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
    <main className="mx-auto flex min-h-dvh w-full max-w-md flex-col justify-center gap-8 px-4 py-8">
      <header className="flex flex-col gap-2">
        <h1 className="text-2xl font-bold">Masuk</h1>
        <p className="text-text-muted">Masukkan email dan kata sandi untuk mulai.</p>
      </header>

      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-6">
        <TextField
          label="Email"
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
        <TextField
          label="Kata sandi"
          type="password"
          name="password"
          autoComplete="current-password"
          enterKeyHint="done"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          error={fieldErrors.password}
        />

        {signIn.isError && (
          <p role="alert" className="rounded-lg bg-danger-bg p-4 text-danger-default">
            {describeFailure(signIn.error)}
          </p>
        )}

        <Button type="submit" disabled={signIn.isPending}>
          {signIn.isPending ? 'Sedang masuk…' : 'Masuk'}
        </Button>
      </form>
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
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return 'Tidak bisa terhubung. Periksa internet, lalu coba lagi.'
    }
    if (error.status === 400) {
      return 'Email atau kata sandi salah. Coba lagi.'
    }
  }
  return 'Ada gangguan. Coba lagi sebentar lagi.'
}
