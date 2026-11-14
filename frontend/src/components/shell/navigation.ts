import { Home, Package, ReceiptText, ShoppingCart, type LucideIcon } from 'lucide-react'

export type NavigationItem = { label: string; to: string; icon: LucideIcon }

/** The app's main destinations, shown in the bottom navigation on phones and the sidebar on wide screens. */
export const navigationItems: NavigationItem[] = [
  { label: 'Beranda', to: '/', icon: Home },
  { label: 'Jual', to: '/jual', icon: ShoppingCart },
  { label: 'Produk', to: '/produk', icon: Package },
  { label: 'Riwayat', to: '/riwayat', icon: ReceiptText },
]
