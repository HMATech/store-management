import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { SaleProduct } from './api'
import { MaxQuantity, useCart } from './useCart'

const product = (id: string, price: number): SaleProduct => ({
  id,
  name: `Product ${id}`,
  price,
  stock: { quantity: 10, status: 'Normal' },
})
const indomie = product('indomie', 3500)
const aqua = product('aqua', 4000)

describe('useCart', () => {
  it('starts empty', () => {
    const { result } = renderHook(() => useCart())

    expect(result.current.lines).toEqual([])
    expect(result.current.totalQuantity).toBe(0)
    expect(result.current.totalPrice).toBe(0)
    expect(result.current.quantityOf('indomie')).toBe(0)
  })

  it('adds products in the order they were picked and totals them', () => {
    const { result } = renderHook(() => useCart())

    act(() => result.current.add(indomie))
    act(() => result.current.add(aqua))
    act(() => result.current.increase('indomie'))

    expect(result.current.lines.map((line) => [line.product.id, line.quantity])).toEqual([
      ['indomie', 2],
      ['aqua', 1],
    ])
    expect(result.current.totalQuantity).toBe(3)
    expect(result.current.totalPrice).toBe(11000)
  })

  it('adds one more when a product in the cart is added again', () => {
    const { result } = renderHook(() => useCart())

    act(() => result.current.add(indomie))
    act(() => result.current.add(indomie))

    expect(result.current.lines).toHaveLength(1)
    expect(result.current.quantityOf('indomie')).toBe(2)
  })

  it('removes a product when its last one is taken away', () => {
    const { result } = renderHook(() => useCart())
    act(() => result.current.add(indomie))
    act(() => result.current.add(aqua))

    act(() => result.current.decrease('indomie'))

    expect(result.current.lines.map((line) => line.product.id)).toEqual(['aqua'])
    expect(result.current.quantityOf('indomie')).toBe(0)
  })

  it('ignores changes to a product that is not in the cart', () => {
    const { result } = renderHook(() => useCart())
    act(() => result.current.add(indomie))

    act(() => result.current.increase('missing'))
    act(() => result.current.decrease('missing'))

    expect(result.current.lines.map((line) => [line.product.id, line.quantity])).toEqual([['indomie', 1]])
  })

  it('stops at the most one sale can hold of a product', () => {
    const { result } = renderHook(() => useCart())
    act(() => result.current.add(indomie))

    for (let i = 0; i < MaxQuantity + 5; i++) {
      act(() => result.current.increase('indomie'))
    }
    act(() => result.current.add(indomie))

    expect(result.current.quantityOf('indomie')).toBe(MaxQuantity)
  })

  it('empties when cleared', () => {
    const { result } = renderHook(() => useCart())
    act(() => result.current.add(indomie))

    act(() => result.current.clear())

    expect(result.current.lines).toEqual([])
    expect(result.current.totalPrice).toBe(0)
  })
})
