import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Info, Mail, User } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { InfoNote } from '../components/InfoNote'
import { PasswordField } from '../components/PasswordField'
import { SaveBar } from '../components/SaveBar'
import { Screen } from '../components/shell/Screen'
import { TextField } from '../components/TextField'
import {
  isStrongPassword,
  MinFirstNameLength,
  MinPasswordLength,
  registerUser,
  type NewUser,
} from '../features/users/api'
import { ApiError } from '../lib/api'

/** What a password needs, as the form says it. */
const PasswordRule =
  `Minimal ${MinPasswordLength} karakter, dengan huruf besar, huruf kecil, angka, dan tanda seperti ! atau #`

/** The longest a name or email can be typed. */
const MaxLength = 100

const emptyUser: NewUser = { firstName: '', lastName: '', email: '', password: '' }

type Problems = Partial<Record<'firstName' | 'email' | 'password', string>>

/** Tambah Pengguna: let someone else sign in, with an email and a password to hand to them. */
export function AddUserPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [user, setUser] = useState(emptyUser)
  // Problems are pointed out only once saving was tried, not while the form is still being filled in.
  const [wasSaveTried, setWasSaveTried] = useState(false)

  const set = (change: Partial<NewUser>) => setUser((current) => ({ ...current, ...change }))
  const entered: NewUser = {
    ...user,
    firstName: user.firstName.trim(),
    lastName: user.lastName.trim(),
    email: user.email.trim(),
  }

  const save = useMutation({
    mutationFn: registerUser,
    onSuccess: (_result, saved) => {
      void queryClient.invalidateQueries()
      void navigate('/akun/pengguna', {
        state: {
          notice: `${saved.firstName} sudah ditambahkan. Berikan email dan kata sandinya supaya dia bisa masuk.`,
        },
      })
    },
  })

  // The API answers an email that is already in use with a conflict. That belongs on the email field.
  const isEmailTaken =
    save.error instanceof ApiError && save.error.status === 409 && save.variables?.email === entered.email
  const problems: Problems = wasSaveTried ? findProblems(entered) : {}

  function trySave(event: FormEvent) {
    event.preventDefault()
    setWasSaveTried(true)
    if (Object.keys(findProblems(entered)).length === 0) {
      save.mutate(entered)
    }
  }

  return (
    <Screen title="Tambah Pengguna" backTo="/akun">
      <form noValidate onSubmit={trySave} className="flex flex-1 flex-col lg:px-32 lg:pb-32">
        <div
          className={
            'flex flex-1 flex-col lg:max-w-560 lg:flex-none lg:gap-20 lg:rounded-lg lg:border-thin'
            + ' lg:border-border-default lg:bg-bg-surface lg:p-24'
          }
        >
          <div className="flex flex-1 flex-col gap-20 px-16 pb-16 lg:p-0">
            <TextField
              label="Nama depan"
              icon={User}
              autoComplete="off"
              maxLength={MaxLength}
              value={user.firstName}
              onChange={(event) => set({ firstName: event.target.value })}
              error={problems.firstName}
            />
            <TextField
              label="Nama belakang (boleh dikosongkan)"
              autoComplete="off"
              maxLength={MaxLength}
              value={user.lastName}
              onChange={(event) => set({ lastName: event.target.value })}
            />
            <TextField
              label="Email"
              icon={Mail}
              type="email"
              inputMode="email"
              autoComplete="off"
              autoCapitalize="none"
              autoCorrect="off"
              spellCheck={false}
              maxLength={MaxLength}
              value={user.email}
              onChange={(event) => set({ email: event.target.value })}
              error={problems.email ?? (isEmailTaken ? 'Email ini sudah dipakai pengguna lain.' : undefined)}
            />
            <PasswordField
              label="Kata sandi"
              autoComplete="new-password"
              maxLength={MaxLength}
              value={user.password}
              onChange={(event) => set({ password: event.target.value })}
              error={problems.password}
            />
            <InfoNote icon={Info}>
              Kata sandi minimal {MinPasswordLength} karakter, dengan huruf besar, huruf kecil, angka, dan tanda
              seperti ! atau #. Berikan email dan kata sandi ini ke pengguna baru supaya dia bisa masuk.
            </InfoNote>
          </div>
          <SaveBar
            label="Simpan Pengguna"
            cancelTo="/akun"
            isSaving={save.isPending}
            failure={save.isError && !isEmailTaken ? describeSaveFailure(save.error) : undefined}
          />
        </div>
      </form>
    </Screen>
  )
}

function findProblems(user: NewUser): Problems {
  const problems: Problems = {}
  if (user.firstName === '') {
    problems.firstName = 'Nama depan wajib diisi'
  } else if (user.firstName.length < MinFirstNameLength) {
    problems.firstName = `Nama depan minimal ${MinFirstNameLength} huruf`
  }
  if (user.email === '') {
    problems.email = 'Email wajib diisi'
  } else if (!/^[^\s@]+@[^\s@]+$/.test(user.email)) {
    problems.email = 'Email belum benar. Contoh: nama@email.com'
  }
  if (user.password === '') {
    problems.password = 'Kata sandi wajib diisi'
  } else if (!isStrongPassword(user.password)) {
    problems.password = PasswordRule
  }
  return problems
}

function describeSaveFailure(error: unknown): string {
  if (error instanceof ApiError && error.isNetworkError) {
    return 'Tidak bisa terhubung, pengguna belum tersimpan. Periksa internet, lalu coba lagi.'
  }
  return 'Pengguna belum tersimpan. Periksa lagi isinya, lalu coba lagi.'
}
