import { AlertDialog } from '@base-ui/react/alert-dialog'
import { Trash2, TriangleAlert } from 'lucide-react'
import type { ReactNode } from 'react'
import { Button } from './Button'

type ConfirmDialogProps = {
  isOpen: boolean
  /** The question: "Hapus penjualan ini?" */
  title: string
  /** What will happen, in a sentence or two. */
  children: ReactNode
  /** The action that goes ahead: "Ya, Hapus". */
  confirmLabel: string
  /** True while the action runs. Both buttons wait for it. */
  isWorking: boolean
  /** Why the action failed, in words for the user. The dialog stays open to show it. */
  failure?: string
  onConfirm: () => void
  onCancel: () => void
}

/**
 * Asks before something that can't be undone, such as deleting a sale. It stays until one of its two actions
 * is chosen. Mirrors the Confirm dialog component in Loom.
 */
export function ConfirmDialog({
  isOpen,
  title,
  children,
  confirmLabel,
  isWorking,
  failure,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <AlertDialog.Root open={isOpen}>
      <AlertDialog.Portal>
        <AlertDialog.Backdrop className="fixed inset-0 bg-bg-inverse/50" />
        <AlertDialog.Popup
          className={
            'fixed top-1/2 left-1/2 flex w-[calc(100%-48px)] max-w-[400px] -translate-x-1/2 -translate-y-1/2'
            + ' flex-col gap-16 rounded-xl bg-bg-surface px-20 py-24'
            + ' shadow-[0_12px_32px_color-mix(in_srgb,var(--color-shadow)_18%,transparent)]'
          }
        >
          <span className="flex size-56 items-center justify-center rounded-full bg-danger-soft">
            <TriangleAlert aria-hidden className="size-28 text-danger-default" />
          </span>
          <AlertDialog.Title className="text-2xl font-bold">{title}</AlertDialog.Title>
          <AlertDialog.Description className="text-text-muted">{children}</AlertDialog.Description>
          {failure !== undefined && (
            <p role="alert" className="rounded-md bg-danger-soft p-12 text-sm font-medium text-danger-default">
              {failure}
            </p>
          )}
          <div className="flex flex-col gap-10">
            <Button kind="danger" onClick={onConfirm} disabled={isWorking}>
              <Trash2 aria-hidden className="size-24" />
              {isWorking ? 'Menghapus…' : confirmLabel}
            </Button>
            <Button kind="secondary" onClick={onCancel} disabled={isWorking}>
              Batal
            </Button>
          </div>
        </AlertDialog.Popup>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  )
}
