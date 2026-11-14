import type { ReactNode } from 'react'
import { AccountButton } from './AccountButton'

type ScreenProps = {
  title: string
  /** A line under the title, such as today's date. */
  subtitle?: string
  children: ReactNode
}

/**
 * A main screen inside the app shell: its top bar, then its content. Mirrors the Top bar component in Loom
 * (Type Main, Action Account). On wide screens the account button is in the sidebar instead.
 */
export function Screen({ title, subtitle, children }: ScreenProps) {
  return (
    <main className="flex flex-1 flex-col">
      <header
        className={
          'flex flex-col gap-4 px-16 pt-[calc(16px+env(safe-area-inset-top,0px))] pb-12'
          + ' lg:px-32 lg:pt-32 lg:pb-20'
        }
      >
        <div className="flex items-center justify-between gap-12">
          <h1 className="min-w-0 truncate text-4xl font-bold">{title}</h1>
          <AccountButton className="max-w-[50%] shrink-0 lg:hidden" />
        </div>
        {subtitle !== undefined && <p className="text-sm text-text-muted">{subtitle}</p>}
      </header>
      <div className="flex flex-1 flex-col px-16 pt-4 pb-16 lg:px-32 lg:pt-0 lg:pb-32">{children}</div>
    </main>
  )
}
