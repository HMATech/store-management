import { LogOut } from 'lucide-react'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { useCurrentUser } from '../features/auth/useCurrentUser'
import { clearSession } from '../lib/session'

/** Akun: who is signed in, and signing out. The rest of the design's account menu comes later. */
export function AccountPage() {
  const { data: user } = useCurrentUser()

  return (
    <Screen title="Akun">
      <ScreenBody>
        <div className="flex w-full max-w-[480px] flex-col gap-16">
          {user !== undefined && (
            <Card className="flex flex-col gap-2 p-16">
              <span className="font-semibold break-words">
                {[user.firstName, user.lastName].filter(Boolean).join(' ')}
              </span>
              <span className="text-sm break-all text-text-muted">{user.email}</span>
            </Card>
          )}
          <Button kind="secondary" onClick={clearSession}>
            <LogOut aria-hidden className="size-24" />
            Keluar
          </Button>
        </div>
      </ScreenBody>
    </Screen>
  )
}
