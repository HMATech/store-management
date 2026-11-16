import { ApiError, apiRequest } from '../../lib/api'

/** A brand, unit or label: something a product is described with, known to the API by its id. */
export type Named = { id: number; name: string }

/** The brands, units and labels already in use, to suggest while a product is typed in. */
export type Catalog = { brands: Named[]; units: Named[]; labels: Named[] }

/** How many brands one request asks for. A small shop has far fewer. */
const BrandPageSize = 200

/** The brand a product gets when none is typed, for goods sold without one such as eggs or loose rice. */
export const NoBrandName = 'Umum'

export async function fetchCatalog(signal?: AbortSignal): Promise<Catalog> {
  const [brands, units, labels] = await Promise.all([
    fetchBrands(signal),
    apiRequest<Named[]>('/products/units', { signal }),
    apiRequest<Named[]>('/products/tags', { signal }),
  ])
  return { brands, units, labels }
}

async function fetchBrands(signal?: AbortSignal): Promise<Named[]> {
  const page = await apiRequest<{ responses: Named[] }>(`/brands?pageSize=${BrandPageSize}`, { signal })
  return page.responses
}

export type NewProduct = {
  name: string
  /** The brand's name; empty for a product without one. */
  brand: string
  /** How much one of it holds, in `unit`: the 85 of "85 g". */
  measure: number
  unit: string
  price: number
  /** A label to group it by, or empty. */
  label: string
}

/**
 * Adds a product. Its brand and unit are given by name: one that already exists is reused whatever its
 * letter case, and a new one is added first.
 */
export async function createProduct(product: NewProduct, known: Pick<Catalog, 'brands' | 'units'>): Promise<void> {
  const [brandId, measureUnitId] = await Promise.all([
    findOrCreateBrand(product.brand === '' ? NoBrandName : product.brand, known.brands),
    findOrCreateUnit(product.unit, known.units),
  ])
  await apiRequest<void>('/products', {
    method: 'POST',
    body: {
      name: product.name,
      brandId,
      measure: product.measure,
      measureUnitId,
      price: product.price,
      tags: product.label === '' ? [] : [product.label],
    },
  })
}

const isNamed = (name: string) => (item: Named) => item.name.toLowerCase() === name.toLowerCase()

async function findOrCreateBrand(name: string, known: Named[]): Promise<number> {
  const existing = known.find(isNamed(name))
  if (existing !== undefined) {
    return existing.id
  }
  // The API answers a new brand with its address only, so the brand is read back to learn its id.
  await ignoringConflict(() => apiRequest<void>('/brands', { method: 'POST', body: { name } }))
  const added = (await fetchBrands()).find(isNamed(name))
  if (added === undefined) {
    throw new ApiError(500, null, `Brand "${name}" was not found after it was added`)
  }
  return added.id
}

async function findOrCreateUnit(name: string, known: Named[]): Promise<number> {
  const existing = known.find(isNamed(name))
  if (existing !== undefined) {
    return existing.id
  }
  const created = await ignoringConflict(() =>
    apiRequest<Named>('/products/units', { method: 'POST', body: { name } }),
  )
  const added = created ?? (await apiRequest<Named[]>('/products/units')).find(isNamed(name))
  if (added === undefined) {
    throw new ApiError(500, null, `Unit "${name}" was not found after it was added`)
  }
  return added.id
}

/** Runs `add`, treating "it already exists" as done: someone else, or an earlier try, added it first. */
async function ignoringConflict<T>(add: () => Promise<T>): Promise<T | undefined> {
  try {
    return await add()
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      return undefined
    }
    throw error
  }
}
