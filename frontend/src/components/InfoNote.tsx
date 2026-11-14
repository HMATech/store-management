import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

type InfoNoteProps = {
  icon: LucideIcon
  children: ReactNode
}

/** A quiet hint with an icon. Mirrors the Info note component in Loom. */
export function InfoNote({ icon: Icon, children }: InfoNoteProps) {
  return (
    <div className="flex items-start gap-12 rounded-md bg-bg-canvas p-16">
      <Icon aria-hidden className="size-22 shrink-0 text-text-muted" />
      <p className="text-sm text-text-muted">{children}</p>
    </div>
  )
}
