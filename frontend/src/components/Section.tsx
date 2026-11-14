import { ChevronRight } from 'lucide-react'
import { useId, type ReactNode } from 'react'
import { Link } from 'react-router'

type SectionProps = {
  title: string
  /** A link at the end of the header, such as "Lihat semua". */
  link?: { label: string; to: string }
  children: ReactNode
}

/** A titled block of a screen. Its header mirrors the Section header component in Loom. */
export function Section({ title, link, children }: SectionProps) {
  const titleId = useId()

  return (
    <section aria-labelledby={titleId} className="flex flex-col gap-10">
      <div className="flex min-h-touch-min items-center justify-between gap-12">
        <h2 id={titleId} className="text-xl font-bold">
          {title}
        </h2>
        {link !== undefined && (
          <Link
            to={link.to}
            className={
              'flex min-h-touch-min items-center gap-2 rounded-sm pl-8 text-sm font-semibold text-primary-default'
              + ' focus-visible:outline-2 focus-visible:outline-primary-default'
            }
          >
            {link.label}
            <ChevronRight aria-hidden className="size-20" />
          </Link>
        )}
      </div>
      {children}
    </section>
  )
}
