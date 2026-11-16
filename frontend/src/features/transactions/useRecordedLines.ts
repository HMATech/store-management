import { useMemo, useState } from 'react'
import type { RecordedEntry } from './TransactionDetail'

/** A line already recorded in the sale or purchase being changed. */
export type RecordedLine<Extra extends object = object> = Extra & {
  entry: RecordedEntry
  quantity: number
  isRemoved: boolean
}

/**
 * The recorded lines of a sale or purchase being changed, starting from `entries`. `extraOf` adds what a line
 * of that kind can also change, such as a purchase's cost. A quantity stays between 1 and `max`; taking a line
 * out is done with `remove`, which puts it back as it was recorded, so undoing a removal restores the recorded line.
 */
export function useRecordedLines<Extra extends object>(
  entries: RecordedEntry[],
  extraOf: (entry: RecordedEntry) => Extra,
  max: number,
) {
  const [recorded] = useState<RecordedLine<Extra>[]>(() =>
    entries.map((entry) => ({ ...extraOf(entry), entry, quantity: entry.quantity, isRemoved: false })),
  )
  const [lines, setLines] = useState(recorded)

  return useMemo(() => {
    const change = (entryId: string, update: (line: RecordedLine<Extra>) => RecordedLine<Extra>) =>
      setLines((current) => current.map((line) => (line.entry.id === entryId ? update(line) : line)))
    // A line recorded with more than `max`, which only the API could do, can still be kept as it is.
    const maxOf = (line: RecordedLine<Extra>) => Math.max(max, line.entry.quantity)
    const clamp = (line: RecordedLine<Extra>, quantity: number) => Math.max(1, Math.min(quantity, maxOf(line)))
    const recordedLine = (entryId: string) => recorded.find((line) => line.entry.id === entryId)

    return {
      lines,
      /** The lines that stay in the sale or purchase. */
      keptLines: lines.filter((line) => !line.isRemoved),
      maxOf,
      setQuantity: (entryId: string, quantity: number) =>
        change(entryId, (line) => ({ ...line, quantity: clamp(line, quantity) })),
      increase: (entryId: string) => change(entryId, (line) => ({ ...line, quantity: clamp(line, line.quantity + 1) })),
      decrease: (entryId: string) => change(entryId, (line) => ({ ...line, quantity: clamp(line, line.quantity - 1) })),
      update: (entryId: string, extra: Partial<Extra>) => change(entryId, (line) => ({ ...line, ...extra })),
      remove: (entryId: string) =>
        change(entryId, (line) => ({ ...(recordedLine(entryId) ?? line), isRemoved: true })),
      restore: (entryId: string) => change(entryId, (line) => ({ ...line, isRemoved: false })),
    }
  }, [lines, recorded, max])
}

export type RecordedLines<Extra extends object = object> = ReturnType<typeof useRecordedLines<Extra>>
