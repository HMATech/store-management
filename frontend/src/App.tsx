import { Navigate, Route, Routes } from 'react-router'
import { AppShell } from './components/shell/AppShell'
import { RequireSignedIn } from './features/auth/RequireSignedIn'
import { AddProductPage } from './pages/AddProductPage'
import { AddUserPage } from './pages/AddUserPage'
import { AdjustStockPage } from './pages/AdjustStockPage'
import { AccountPage } from './pages/AccountPage'
import { EditPurchasePage } from './pages/EditPurchasePage'
import { EditSalePage } from './pages/EditSalePage'
import { HistoryPage } from './pages/HistoryPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { ProductsPage } from './pages/ProductsPage'
import { ProfitPage } from './pages/ProfitPage'
import { PurchaseDetailPage } from './pages/PurchaseDetailPage'
import { PurchasePage } from './pages/PurchasePage'
import { UsersPage } from './pages/UsersPage'
import { SaleDetailPage } from './pages/SaleDetailPage'
import { SalePage } from './pages/SalePage'

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireSignedIn />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/history" element={<HistoryPage />} />
          <Route path="/account" element={<AccountPage />} />
        </Route>
        <Route element={<AppShell bottomNavigation={false} />}>
          <Route path="/sales/new" element={<SalePage />} />
          <Route path="/profit" element={<ProfitPage />} />
          <Route path="/purchases/new" element={<PurchasePage />} />
          <Route path="/products/new" element={<AddProductPage />} />
          <Route path="/products/:id" element={<ProductDetailPage />} />
          <Route path="/products/:id/stock" element={<AdjustStockPage />} />
          <Route path="/history/sales/:id" element={<SaleDetailPage />} />
          <Route path="/history/sales/:id/edit" element={<EditSalePage />} />
          <Route path="/history/purchases/:id" element={<PurchaseDetailPage />} />
          <Route path="/history/purchases/:id/edit" element={<EditPurchasePage />} />
          <Route path="/account/users" element={<UsersPage />} />
          <Route path="/account/users/new" element={<AddUserPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
