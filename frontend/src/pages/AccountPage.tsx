import { Eye, LogOut, User, UserPlus, Users } from 'lucide-react'
import { useState } from 'react'
import { Card, CardList } from '../components/Card'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { MenuRow } from '../components/MenuRow'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { useCurrentUser } from '../features/auth/useCurrentUser'
import { fullName } from '../features/users/api'
import { clearSession } from '../lib/session'
import { nextTheme, setTheme, useTheme, type Theme } from '../lib/theme'

const themeNames: Record<Theme, string> = { system: 'Ikuti perangkat', light: 'Terang', dark: 'Gelap' }

/** Akun: who is signed in, the people who can sign in, how the app looks, and signing out. */
export function AccountPage() {
  const { data: user } = useCurrentUser()
  const theme = useTheme()
  const [isAskingToSignOut, setIsAskingToSignOut] = useState(false)

  return (
    <Screen title="Akun" hasAccountButton={false}>
      <ScreenBody>
        <div className="flex w-full max-w-560 flex-col gap-16">
          {user !== undefined && (
            <Card className="flex items-center gap-12 p-16">
              <span className="flex size-56 shrink-0 items-center justify-center rounded-full bg-primary-soft">
                <User aria-hidden className="size-28 text-primary-default" />
              </span>
              <p className="flex min-w-0 flex-col gap-2">
                <span className="text-xl font-bold break-words">{fullName(user)}</span>
                <span className="text-sm break-all text-text-muted">{user.email}</span>
              </p>
            </Card>
          )}
          <CardList>
            <MenuRow icon={Users} to="/akun/pengguna">
              Daftar pengguna
            </MenuRow>
            <MenuRow icon={UserPlus} to="/akun/pengguna/baru">
              Tambah pengguna
            </MenuRow>
            {/* Each tap moves to the next look; the row says which one is on. */}
            <MenuRow icon={Eye} onSelect={() => setTheme(nextTheme(theme))}>
              Tampilan: {themeNames[theme]}
            </MenuRow>
          </CardList>
          <CardList>
            <MenuRow icon={LogOut} tone="danger" onSelect={() => setIsAskingToSignOut(true)}>
              Keluar
            </MenuRow>
          </CardList>
        </div>
        <ConfirmDialog
          isOpen={isAskingToSignOut}
          title="Keluar dari aplikasi?"
          confirmLabel="Ya, Keluar"
          confirmIcon={LogOut}
          onConfirm={clearSession}
          onCancel={() => setIsAskingToSignOut(false)}
        >
          Untuk memakai lagi, kamu perlu masuk dengan email dan kata sandi.
        </ConfirmDialog>
      </ScreenBody>
    </Screen>
  )
}
