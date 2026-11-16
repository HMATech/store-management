import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { ApiError } from '../../lib/api'
import type { EditKindText } from './EditEntries'
import type { RecordedTransaction } from './TransactionDetail'

/**
 * Saves the changed items with `send`, then goes back to the detail screen and says so there. A save whose
 * answer was lost may have got through, and sending its new lines again would add them twice. So the retry
 * first reads the sale or purchase again, and counts it as saved when it changed since this screen loaded it.
 */
export function useSaveEntries<Entries>(
  kind: EditKindText,
  recorded: RecordedTransaction,
  send: (entries: Entries) => Promise<void>,
) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const wasAnswerLost = useRef(false)
  // As it was when the lines being changed were read; a refetch while changing them mustn't move it.
  const [loadedVersion] = useState(recorded.lastUpdatedTime)

  return useMutation({
    mutationFn: async (entries: Entries) => {
      if (wasAnswerLost.current) {
        const current = await kind.fetch(recorded.id)
        if (current.lastUpdatedTime !== loadedVersion) {
          return
        }
      }
      await send(entries)
    },
    onError: (error) => {
      wasAnswerLost.current = error instanceof ApiError && error.isNetworkError
    },
    onSuccess: () => {
      // Its detail, the history, the day's totals and the stock of what it holds all changed.
      void queryClient.invalidateQueries()
      void navigate(kind.detailPath(recorded.id), { replace: true, state: { notice: 'Perubahan sudah disimpan.' } })
    },
  })
}

/** Why the change wasn't saved, in words for the user. */
export function describeSaveFailure(error: unknown, noun: string): string {
  const lower = noun.toLowerCase()
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return 'Tidak bisa terhubung. Periksa internet, lalu simpan lagi. Perubahan tidak akan tercatat dua kali.'
    }
    if (error.status === 404) {
      return `${noun} ini atau salah satu barangnya sudah dihapus. Kembali ke Riwayat untuk melihatnya lagi.`
    }
  }
  return `Perubahan ${lower} belum tersimpan. Coba lagi.`
}
