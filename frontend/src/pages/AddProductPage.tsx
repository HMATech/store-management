import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Info, Package } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { Button, ButtonLink } from '../components/Button'
import { InfoNote } from '../components/InfoNote'
import { MoneyField } from '../components/MoneyField'
import { Screen } from '../components/shell/Screen'
import { SuggestField } from '../components/SuggestField'
import { TextField } from '../components/TextField'
import { createProduct, fetchCatalog, NoBrandName, type NewProduct } from '../features/products/catalog'
import { ApiError } from '../lib/api'

/** The longest name a product, brand, unit or label can be given. */
const MaxNameLength = 100

/** What the form holds while it is filled in: text as typed, and the price once it is a number. */
type Draft = { name: string; brand: string; measure: string; unit: string; price: number | null; label: string }

const emptyDraft: Draft = { name: '', brand: '', measure: '', unit: '', price: null, label: '' }

type Problems = Partial<Record<'name' | 'measure' | 'unit' | 'price', string>>

/** Tambah Produk: describe a new product and what it sells for. */
export function AddProductPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [draft, setDraft] = useState(emptyDraft)
  // Missing fields are pointed out only once saving was tried, not while the form is still being filled in.
  const [wasSaveTried, setWasSaveTried] = useState(false)
  // Suggestions are a help, not a need: the form works without them, so a failed load isn't shown.
  const catalog = useQuery({ queryKey: ['catalog'], queryFn: ({ signal }) => fetchCatalog(signal) })

  const problems = wasSaveTried ? findProblems(draft) : {}
  const names = (items: { name: string }[] | undefined) => items?.map((item) => item.name) ?? []
  const set = (change: Partial<Draft>) => setDraft((current) => ({ ...current, ...change }))

  const save = useMutation({
    mutationFn: (product: NewProduct) =>
      createProduct(product, { brands: catalog.data?.brands ?? [], units: catalog.data?.units ?? [] }),
    onSuccess: (_result, product) => {
      // The product list, its count and the suggestions all changed.
      void queryClient.invalidateQueries()
      void navigate('/produk', { state: { addedProduct: product.name } })
    },
  })

  function trySave(event: FormEvent) {
    event.preventDefault()
    setWasSaveTried(true)
    const product = toNewProduct(draft)
    if (product !== null) {
      save.mutate(product)
    }
  }

  const failure = save.isError ? describeSaveFailure(save.error) : undefined

  return (
    <Screen title="Tambah Produk" backTo="/produk">
      <form noValidate onSubmit={trySave} className="flex flex-1 flex-col lg:px-32 lg:pb-32">
        <div
          className={
            'flex flex-1 flex-col lg:max-w-720 lg:flex-none lg:gap-20 lg:rounded-lg lg:border-thin'
            + ' lg:border-border-default lg:bg-bg-surface lg:p-24'
          }
        >
          <div className="flex flex-1 flex-col gap-20 px-16 pb-16 lg:p-0">
            <div className="grid gap-20 lg:grid-cols-2 lg:gap-x-16">
              <div className="lg:col-span-2">
                <TextField
                  label="Nama barang"
                  icon={Package}
                  placeholder="Contoh: Indomie Goreng"
                  autoComplete="off"
                  maxLength={MaxNameLength}
                  value={draft.name}
                  onChange={(event) => set({ name: event.target.value })}
                  error={problems.name}
                />
              </div>
              <SuggestField
                label="Merek (boleh dikosongkan)"
                placeholder="Contoh: Indofood"
                maxLength={MaxNameLength}
                suggestions={names(catalog.data?.brands).filter((name) => name !== NoBrandName)}
                value={draft.brand}
                onChange={(brand) => set({ brand })}
              />
              <div className="grid grid-cols-2 gap-12 lg:contents">
                <TextField
                  label="Ukuran"
                  placeholder="1"
                  inputMode="decimal"
                  autoComplete="off"
                  maxLength={8}
                  value={draft.measure}
                  onChange={(event) => set({ measure: event.target.value })}
                  error={problems.measure}
                />
                <SuggestField
                  label="Satuan"
                  placeholder="bungkus"
                  maxLength={MaxNameLength}
                  suggestions={names(catalog.data?.units)}
                  value={draft.unit}
                  onChange={(unit) => set({ unit })}
                  error={problems.unit}
                />
              </div>
              <MoneyField
                label="Harga jual"
                placeholder="0"
                value={draft.price}
                onChange={(price) => set({ price })}
                error={problems.price}
              />
              <div className="lg:col-start-2 lg:row-start-2">
                <SuggestField
                  label="Label (boleh dikosongkan)"
                  placeholder="Contoh: Mi instan"
                  maxLength={MaxNameLength}
                  suggestions={names(catalog.data?.labels)}
                  value={draft.label}
                  onChange={(label) => set({ label })}
                />
              </div>
            </div>
            <InfoNote icon={Info}>Harga bisa diubah kapan saja. Stok awal dicatat lewat Catat Belanja.</InfoNote>
          </div>
          <SaveBar isSaving={save.isPending} failure={failure} />
        </div>
      </form>
    </Screen>
  )
}

type SaveBarProps = { isSaving: boolean; failure: string | undefined }

/** The save button with why the last save failed: pinned to the bottom on phones, inside the card on wide screens. */
function SaveBar({ isSaving, failure }: SaveBarProps) {
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
        {/* On phones "Kembali" at the top does this. */}
        <div className="hidden lg:block">
          <ButtonLink kind="secondary" to="/produk">
            Batal
          </ButtonLink>
        </div>
        <Button type="submit" disabled={isSaving}>
          <Check aria-hidden className="size-24" />
          {isSaving ? 'Menyimpan…' : 'Simpan Produk'}
        </Button>
      </div>
    </div>
  )
}

/** The size as a number: "1,5" and "1.5" are both one and a half, and an empty size is 1. Null when it isn't one. */
function parseMeasure(text: string): number | null {
  const trimmed = text.trim()
  if (trimmed === '') {
    return 1
  }
  if (!/^\d+([.,]\d+)?$/.test(trimmed)) {
    return null
  }
  const measure = Number(trimmed.replace(',', '.'))
  return measure > 0 ? measure : null
}

function findProblems(draft: Draft): Problems {
  const problems: Problems = {}
  if (draft.name.trim() === '') {
    problems.name = 'Nama barang wajib diisi'
  }
  if (parseMeasure(draft.measure) === null) {
    problems.measure = 'Isi dengan angka, contoh: 85 atau 1,5'
  }
  if (draft.unit.trim() === '') {
    problems.unit = 'Satuan wajib diisi, contoh: bungkus'
  }
  if (draft.price === null) {
    problems.price = 'Harga jual wajib diisi, contoh: 3500'
  }
  return problems
}

/** The product to save, or null while the form has a problem. */
function toNewProduct(draft: Draft): NewProduct | null {
  const measure = parseMeasure(draft.measure)
  if (Object.keys(findProblems(draft)).length > 0 || measure === null || draft.price === null) {
    return null
  }
  return {
    name: draft.name.trim(),
    brand: draft.brand.trim(),
    measure,
    unit: draft.unit.trim(),
    price: draft.price,
    label: draft.label.trim(),
  }
}

function describeSaveFailure(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return 'Tidak bisa terhubung, barang belum tersimpan. Periksa internet, lalu coba lagi.'
    }
    if (error.status === 409) {
      return 'Barang dengan nama dan merek ini sudah ada. Ganti namanya, atau buka barang itu di menu Produk.'
    }
  }
  return 'Barang belum tersimpan. Coba lagi.'
}
