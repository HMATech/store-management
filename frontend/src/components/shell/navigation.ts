import { Home, Package, ReceiptText, ShoppingCart, type LucideIcon } from 'lucide-react'

/** `opens` lists screens outside the destination's route that it leads to, which keep it marked in the sidebar. */
export type NavigationItem = { label: string; to: string; icon: LucideIcon; opens?: string[] }

/** The app's main destinations, shown in the bottom navigation on phones and the sidebar on wide screens. */
export const navigationItems: NavigationItem[] = [
  { label: 'Beranda', to: '/', icon: Home, opens: ['/profit'] },
  { label: 'Jual', to: '/sales/new', icon: ShoppingCart },
  { label: 'Produk', to: '/products', icon: Package },
  { label: 'Riwayat', to: '/history', icon: ReceiptText },
]
