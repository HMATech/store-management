import { cva, type VariantProps } from 'class-variance-authority'
import type { ButtonHTMLAttributes } from 'react'

/** Variants mirror the Button component in Loom (Kind). */
const button = cva(
  [
    'inline-flex min-h-control-lg w-full items-center justify-center rounded-lg px-6 text-lg font-bold',
    'transition-[transform,background-color] duration-100 ease-out motion-reduce:transition-none',
    'active:scale-[0.97] disabled:opacity-60 disabled:active:scale-100',
    'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default',
  ],
  {
    variants: {
      kind: {
        primary: 'bg-primary-default text-text-on-primary hover:bg-primary-pressed active:bg-primary-pressed',
        secondary: 'border border-border-default bg-bg-surface text-text-default',
      },
    },
    defaultVariants: { kind: 'primary' },
  },
)

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & VariantProps<typeof button>

export function Button({ kind, className, type = 'button', ...props }: ButtonProps) {
  return <button type={type} className={button({ kind, className })} {...props} />
}
