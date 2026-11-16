import { deletePurchase, editPurchasePath, fetchPurchase } from '../features/purchases/detail'
import { TransactionDetail, type TransactionKindText } from '../features/transactions/TransactionDetail'

const purchase: TransactionKindText = {
  noun: 'Belanja',
  queryKey: 'purchases',
  fetch: fetchPurchase,
  remove: deletePurchase,
  editPath: editPurchasePath,
  stockOnDelete: 'Stok barangnya akan dikurangi lagi.',
  isIncome: false,
}

/** Detail Belanja: what one purchase held and where it came from, and the ways to correct or remove it. */
export function PurchaseDetailPage() {
  return <TransactionDetail kind={purchase} />
}
