import clsx from 'clsx'
import { Store } from 'lucide-react'
import { NavLink } from 'react-router'
import { AccountButton } from './AccountButton'
import { navigationItems } from './navigation'

/** The main navigation on wide screens. Mirrors the Sidebar and Sidebar item components in Loom. */
export function Sidebar() {
  return (
    <aside
      className={
        'sticky top-0 hidden h-dvh w-240 shrink-0 flex-col gap-24 border-r-thin border-border-default'
        + ' bg-bg-surface px-16 py-24 lg:flex'
      }
    >
      <div className="flex items-center gap-12 px-8 py-4">
        <span className="flex size-40 items-center justify-center rounded-md bg-primary-soft">
          <Store aria-hidden className="size-24 text-primary-default" />
        </span>
        <span className="text-xl font-bold">Warung Ortu</span>
      </div>

      <nav aria-label="Menu utama" className="flex flex-1 flex-col gap-4">
        {navigationItems.map(({ label, to, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end
            className={({ isActive }) =>
              clsx(
                'flex min-h-control-md items-center gap-12 rounded-md px-14 py-12',
                'transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none',
                'focus-visible:outline-2 focus-visible:outline-primary-default',
                isActive
                  ? 'bg-primary-soft font-bold text-primary-default'
                  : 'font-medium text-text-muted hover:bg-bg-canvas',
              )
            }
          >
            <Icon aria-hidden className="size-24" />
            {label}
          </NavLink>
        ))}
      </nav>

      <AccountButton className="self-start" />
    </aside>
  )
}
