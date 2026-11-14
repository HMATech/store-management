import { cva, type VariantProps } from 'class-variance-authority'
import type { LucideIcon } from 'lucide-react'

/** A round, tinted icon at the start of a row. Mirrors the Icon chip component in Loom, with its Tone. */
const chip = cva('flex size-40 shrink-0 items-center justify-center rounded-full', {
  variants: {
    tone: {
      primary: 'bg-primary-soft text-primary-default',
      warning: 'bg-warning-soft text-warning-default',
      danger: 'bg-danger-soft text-danger-default',
    },
  },
  defaultVariants: { tone: 'primary' },
})

type IconChipProps = VariantProps<typeof chip> & { icon: LucideIcon }

export function IconChip({ icon: Icon, tone }: IconChipProps) {
  return (
    <span className={chip({ tone })}>
      <Icon aria-hidden className="size-20" />
    </span>
  )
}
