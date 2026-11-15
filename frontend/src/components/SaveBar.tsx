import { Check } from 'lucide-react'
import { Button, ButtonLink } from './Button'

type SaveBarProps = {
  /** What saving is called here: "Simpan Produk". */
  label: string
  /** Where "Batal" leads on wide screens. On phones the screen's "Kembali" does this. */
  cancelTo: string
  isSaving: boolean
  /** Why the save didn't happen, in words for the user. */
  failure: string | undefined
}

/**
 * A form's submit button with why the last save failed: pinned to the bottom on phones, and beside "Batal"
 * at the end of the form on wide screens. Mirrors the Action bar in Loom. It belongs inside the `<form>`.
 */
export function SaveBar({ label, cancelTo, isSaving, failure }: SaveBarProps) {
  return (
    <div
      className={
        'sticky bottom-0 flex flex-col gap-12 bg-bg-surface px-16 pt-14'
        + ' pb-[calc(20px+env(safe-area-inset-bottom,0px))]'
        + ' shadow-[0_-4px_16px_color-mix(in_srgb,var(--color-shadow)_8%,transparent)]'
        + ' lg:static lg:p-0 lg:shadow-none'
      }
    >
      {failure !== undefined && (
        <p role="alert" className="rounded-md bg-danger-soft p-12 text-sm font-medium text-danger-default">
          {failure}
        </p>
      )}
      <div className="flex flex-col gap-12 lg:flex-row lg:justify-end">
        <div className="hidden lg:block">
          <ButtonLink kind="secondary" to={cancelTo}>
            Batal
          </ButtonLink>
        </div>
        <Button type="submit" disabled={isSaving}>
          <Check aria-hidden className="size-24" />
          {isSaving ? 'Menyimpan…' : label}
        </Button>
      </div>
    </div>
  )
}
