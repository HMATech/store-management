import { useRef } from 'react'

/**
 * A random UUID (version 4). `crypto.randomUUID` only exists on HTTPS and localhost, and the app is also opened
 * over plain HTTP on the shop's network, so this builds one from `crypto.getRandomValues`, which works anywhere.
 */
export function newRequestId(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(16))
  bytes[6] = (bytes[6] & 0x0f) | 0x40
  bytes[8] = (bytes[8] & 0x3f) | 0x80
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('')
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`
}

/**
 * Gives the id to save something under, so the API can recognise a retry and not record it twice. Saving the
 * same thing again gets the same id; anything changed since the last try gets a new one, so an edit after a
 * failed save is recorded as it now is rather than taken for the earlier try. "The same" means every input is
 * identical (`===`) to the last call's.
 */
export function useRetrySafeId(): (inputs: readonly unknown[]) => string {
  const last = useRef<{ inputs: readonly unknown[]; id: string } | null>(null)

  return (inputs) => {
    const previous = last.current
    const isSame =
      previous !== null
      && previous.inputs.length === inputs.length
      && previous.inputs.every((input, index) => input === inputs[index])
    if (!isSame) {
      last.current = { inputs, id: newRequestId() }
    }
    return last.current!.id
  }
}
