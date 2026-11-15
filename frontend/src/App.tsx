import { Navigate, Route, Routes } from 'react-router'
import { AppShell } from './components/shell/AppShell'
import { RequireSignedIn } from './features/auth/RequireSignedIn'
import { AddProductPage } from './pages/AddProductPage'
import { AdjustStockPage } from './pages/AdjustStockPage'
import { AccountPage } from './pages/AccountPage'
import { HistoryPage } from './pages/HistoryPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { ProductsPage } from './pages/ProductsPage'
import { PurchasePage } from './pages/PurchasePage'
import { SalePage } from './pages/SalePage'

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireSignedIn />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/produk" element={<ProductsPage />} />
          <Route path="/riwayat" element={<HistoryPage />} />
          <Route path="/akun" element={<AccountPage />} />
        </Route>
        <Route element={<AppShell bottomNavigation={false} />}>
          <Route path="/jual" element={<SalePage />} />
          <Route path="/belanja" element={<PurchasePage />} />
          <Route path="/produk/baru" element={<AddProductPage />} />
          <Route path="/produk/:id" element={<ProductDetailPage />} />
          <Route path="/produk/:id/stok" element={<AdjustStockPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
