import clsx from 'clsx'
import { User } from 'lucide-react'
import { Link } from 'react-router'
import { useCurrentUser } from '../../features/auth/useCurrentUser'

/**
 * Opens the account screen, labelled with the signed-in user's first name ("Akun" until it is known).
 * Mirrors the Account button component in Loom.
 */
export function AccountButton({ className }: { className?: string }) {
  const { data: user } = useCurrentUser()
  const name = user?.firstName.trim()

  return (
    <Link
      to="/akun"
      aria-label={name ? `Akun ${name}` : 'Akun'}
      className={clsx(
        'flex min-h-touch-min max-w-full items-center gap-6 rounded-full border-thin border-border-default',
        'bg-bg-surface py-4 pr-12 pl-6 text-sm font-semibold',
        'transition-transform duration-100 ease-out active:scale-[0.97] motion-reduce:transition-none',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-default',
        className,
      )}
    >
      <span className="flex size-36 shrink-0 items-center justify-center rounded-full bg-primary-soft">
        <User aria-hidden className="size-20 text-primary-default" />
      </span>
      <span className="truncate">{name || 'Akun'}</span>
    </Link>
  )
}
