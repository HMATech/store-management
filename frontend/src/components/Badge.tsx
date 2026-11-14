import { cva, type VariantProps } from 'class-variance-authority'
import type { ReactNode } from 'react'

/** A short status label. Mirrors the Badge component in Loom, with its Tone. */
const badge = cva('shrink-0 rounded-full px-10 py-6 text-xs font-semibold whitespace-nowrap', {
  variants: {
    tone: {
      warning: 'bg-warning-soft text-warning-default',
      danger: 'bg-danger-soft text-danger-default',
    },
  },
})

type BadgeProps = Required<VariantProps<typeof badge>> & { children: ReactNode }

export function Badge({ tone, children }: BadgeProps) {
  return <span className={badge({ tone })}>{children}</span>
}
