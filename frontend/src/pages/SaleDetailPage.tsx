import { deleteSale, editSalePath, fetchSale } from '../features/sales/detail'
import { TransactionDetail, type TransactionKindText } from '../features/transactions/TransactionDetail'

const sale: TransactionKindText = {
  noun: 'Penjualan',
  queryKey: 'sales',
  fetch: fetchSale,
  remove: deleteSale,
  editPath: editSalePath,
  stockOnDelete: 'Stok barangnya akan dikembalikan.',
  isIncome: true,
}

/** Detail Penjualan: what one sale held, and the ways to correct it or remove it when it was recorded by mistake. */
export function SaleDetailPage() {
  return <TransactionDetail kind={sale} />
}
