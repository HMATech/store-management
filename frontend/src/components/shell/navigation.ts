import { Home, Package, ReceiptText, ShoppingCart, type LucideIcon } from 'lucide-react'

export type NavigationItem = { label: string; to: string; icon: LucideIcon }

/** The app's main destinations, shown in the bottom navigation on phones and the sidebar on wide screens. */
export const navigationItems: NavigationItem[] = [
  { label: 'Beranda', to: '/', icon: Home },
  { label: 'Jual', to: '/sales/new', icon: ShoppingCart },
  { label: 'Produk', to: '/products', icon: Package },
  { label: 'Riwayat', to: '/history', icon: ReceiptText },
]
