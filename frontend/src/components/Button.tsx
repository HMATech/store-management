import { cva, type VariantProps } from 'class-variance-authority'
import type { ButtonHTMLAttributes } from 'react'
import { Link, type LinkProps } from 'react-router'

/** Mirrors the Button component in Loom, with its Kind variants. It hugs its label; pass `w-full` to fill. */
const button = cva(
  [
    'inline-flex min-h-control-lg items-center justify-center gap-10 rounded-md px-20 py-16',
    'text-lg font-semibold',
    'transition-transform duration-100 ease-out motion-reduce:transition-none',
    'active:scale-[0.97] disabled:opacity-60 disabled:active:scale-100',
    'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default',
  ],
  {
    variants: {
      kind: {
        primary: 'bg-primary-default text-primary-on',
        secondary: 'border-medium border-primary-default bg-bg-surface text-primary-default',
        danger: 'bg-danger-default text-danger-on',
      },
    },
    defaultVariants: { kind: 'primary' },
  },
)

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & VariantProps<typeof button>

export function Button({ kind, className, type = 'button', ...props }: ButtonProps) {
  return <button type={type} className={button({ kind, className })} {...props} />
}

type ButtonLinkProps = LinkProps & VariantProps<typeof button>

/** A link that looks like a Button, for actions that open another screen. */
export function ButtonLink({ kind, className, ...props }: ButtonLinkProps) {
  return <Link className={button({ kind, className })} {...props} />
}
