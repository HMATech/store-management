import { ArrowLeft } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { AccountButton } from './AccountButton'

type ScreenProps = {
  title: string
  /** A line under the title, such as today's date. */
  subtitle?: string
  /**
   * Where "Kembali" leads. Give it to a task screen, one entered to do a single job: on phones it then shows
   * the back button instead of the account button. Pair it with `<AppShell bottomNavigation={false}>`.
   */
  backTo?: string
  /** The screen's main action, shown beside the title in place of the account button. */
  action?: ReactNode
  children: ReactNode
}

/**
 * A screen inside the app shell: its top bar, then its content. Mirrors the Top bar component in Loom
 * (Type Main with the account button or an action button, or Type Back). On wide screens the sidebar has
 * the account button and the navigation, so neither of those shows.
 */
export function Screen({ title, subtitle, backTo, action, children }: ScreenProps) {
  return (
    <main className="flex flex-1 flex-col">
      <header
        className={
          'flex flex-col gap-4 px-16 pt-[calc(16px+env(safe-area-inset-top,0px))] pb-12'
          + ' lg:px-32 lg:pt-32 lg:pb-20'
        }
      >
        {backTo !== undefined && (
          <Link
            to={backTo}
            className={
              'flex min-h-control-md items-center gap-6 self-start rounded-md py-12 pr-12 pl-4 font-semibold'
              + ' transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none'
              + ' focus-visible:outline-2 focus-visible:outline-primary-default lg:hidden'
            }
          >
            <ArrowLeft aria-hidden className="size-24" />
            Kembali
          </Link>
        )}
        <div className="flex items-center justify-between gap-12">
          <h1 className="min-w-0 truncate text-4xl font-bold">{title}</h1>
          {action}
          {action === undefined && backTo === undefined && (
            <AccountButton className="max-w-[50%] shrink-0 lg:hidden" />
          )}
        </div>
        {subtitle !== undefined && <p className="text-sm text-text-muted">{subtitle}</p>}
      </header>
      {children}
    </main>
  )
}

/** The padded area under a screen's top bar. Screens that pin something to the bottom lay out their own. */
export function ScreenBody({ children }: { children: ReactNode }) {
  return <div className="flex flex-1 flex-col px-16 pt-4 pb-16 lg:px-32 lg:pt-0 lg:pb-32">{children}</div>
}
