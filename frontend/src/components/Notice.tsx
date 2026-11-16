import clsx from 'clsx'
import { useLocation } from 'react-router'

/**
 * Says what was just done on the screen the user came from, such as that the stock was adjusted. That screen
 * passes it along when it navigates here: `navigate(path, { state: { notice } })`. Renders nothing without one.
 */
export function Notice({ className }: { className?: string }) {
  const notice: unknown = useLocation().state?.notice
  if (typeof notice !== 'string') {
    return null
  }
  return (
    <p role="status" className={clsx('rounded-md bg-primary-soft p-12 font-medium text-primary-default', className)}>
      {notice}
    </p>
  )
}
