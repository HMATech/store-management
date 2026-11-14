import clsx from 'clsx'
import { NavLink } from 'react-router'
import { navigationItems } from './navigation'

/** The main navigation on phones. Mirrors the Bottom navigation and Nav tab components in Loom. */
export function BottomNavigation() {
  return (
    <nav
      aria-label="Menu utama"
      className={
        'sticky bottom-0 flex border-t-thin border-border-default bg-bg-surface px-8 pt-8'
        + ' pb-[calc(12px+env(safe-area-inset-bottom,0px))] lg:hidden'
      }
    >
      {navigationItems.map(({ label, to, icon: Icon }) => (
        <NavLink
          key={to}
          to={to}
          end
          className={({ isActive }) =>
            clsx(
              'group flex min-h-touch-min flex-1 flex-col items-center gap-4 rounded-md text-xs',
              'focus-visible:outline-2 focus-visible:outline-primary-default',
              isActive ? 'font-bold text-primary-default' : 'font-medium text-text-muted',
            )
          }
        >
          <span
            className={
              'flex h-32 w-64 items-center justify-center rounded-full'
              + ' transition-transform duration-100 ease-out group-active:scale-[0.97]'
              + ' group-aria-[current=page]:bg-primary-soft motion-reduce:transition-none'
            }
          >
            <Icon aria-hidden className="size-24" />
          </span>
          {label}
        </NavLink>
      ))}
    </nav>
  )
}
