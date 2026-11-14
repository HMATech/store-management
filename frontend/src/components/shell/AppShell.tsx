import { Outlet } from 'react-router'
import { BottomNavigation } from './BottomNavigation'
import { Sidebar } from './Sidebar'

type AppShellProps = {
  /** Off for task screens, which use the whole phone screen and have a back button instead. */
  bottomNavigation?: boolean
}

/** The frame around every signed-in screen: a sidebar on wide screens, a bottom navigation on phones. */
export function AppShell({ bottomNavigation = true }: AppShellProps) {
  return (
    <div className="flex min-h-dvh flex-col lg:flex-row">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <Outlet />
      </div>
      {bottomNavigation && <BottomNavigation />}
    </div>
  )
}
