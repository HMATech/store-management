import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { newRequestId, useRetrySafeId } from './requestId'

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/

describe('newRequestId', () => {
  it('makes a version 4 UUID', () => {
    expect(newRequestId()).toMatch(uuid)
  })

  it('makes a different one each time', () => {
    expect(new Set(Array.from({ length: 50 }, newRequestId)).size).toBe(50)
  })
})

describe('useRetrySafeId', () => {
  it('keeps the id while the inputs stay the same, and changes it when one changes', () => {
    const { result } = renderHook(() => useRetrySafeId())
    const lines = [{ quantity: 1 }]

    const first = result.current(['Pasar', lines])
    expect(result.current(['Pasar', lines])).toBe(first)
    expect(result.current(['Toko', lines])).not.toBe(first)
  })

  it('treats a new list with the same contents as a change', () => {
    const { result } = renderHook(() => useRetrySafeId())

    const first = result.current([[{ quantity: 1 }]])

    expect(result.current([[{ quantity: 1 }]])).not.toBe(first)
  })
})
