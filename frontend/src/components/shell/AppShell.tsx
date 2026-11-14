import { Outlet } from 'react-router'
import { BottomNavigation } from './BottomNavigation'
import { Sidebar } from './Sidebar'

/** The frame around every signed-in screen: a sidebar on wide screens, a bottom navigation on phones. */
export function AppShell() {
  return (
    <div className="flex min-h-dvh flex-col lg:flex-row">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <Outlet />
      </div>
      <BottomNavigation />
    </div>
  )
}
