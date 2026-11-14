import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

type EmptyStateProps = {
  icon: LucideIcon
  title: string
  children: ReactNode
  /** What to do about it, such as a button that adds the first item. */
  action?: ReactNode
}

/** Fills a screen that has nothing to show yet, and says how to start. Mirrors the Empty state component in Loom. */
export function EmptyState({ icon: Icon, title, children, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center gap-12 px-24 py-32 text-center">
      <span className="flex size-72 items-center justify-center rounded-full bg-primary-soft">
        <Icon aria-hidden className="size-36 text-primary-default" />
      </span>
      <h2 className="text-2xl font-bold">{title}</h2>
      <p className="max-w-[320px] text-text-muted">{children}</p>
      {action}
    </div>
  )
}
