import { ClipboardList, Truck } from 'lucide-react'
import { ButtonLink } from '../components/Button'
import { Section } from '../components/Section'
import { Screen, ScreenBody } from '../components/shell/Screen'
import { RecentSales } from '../features/home/RecentSales'
import { RecordSaleCard } from '../features/home/RecordSaleCard'
import { StockAlerts } from '../features/home/StockAlerts'
import { TodaySummary } from '../features/home/TodaySummary'
import { formatLongDate } from '../lib/format'
import { useToday } from '../lib/useToday'

/** Beranda: today at a glance, and the way into the daily tasks. One column on phones, two on wide screens. */
export function HomePage() {
  const today = useToday()

  return (
    <Screen title="Warung Ortu" subtitle={formatLongDate(today)}>
      <ScreenBody>
        <div className="flex flex-col gap-16 lg:flex-row lg:items-start lg:gap-24">
          <div className="flex flex-col gap-16 lg:flex-1">
            <RecordSaleCard />
            <TodaySummary />
            <div className="flex gap-12">
              <ButtonLink kind="secondary" to="/belanja" className="min-w-0 flex-1 gap-8 px-6 whitespace-nowrap">
                <Truck aria-hidden className="size-24 shrink-0" />
                Catat Belanja
              </ButtonLink>
              <ButtonLink kind="secondary" to="/produk" className="min-w-0 flex-1 gap-8 px-6 whitespace-nowrap">
                <ClipboardList aria-hidden className="size-24 shrink-0" />
                Cek Stok
              </ButtonLink>
            </div>
          </div>
          <div className="flex flex-col gap-16 lg:flex-1 lg:gap-24">
            <Section title="Stok perlu dicek" link={{ label: 'Lihat semua', to: '/produk' }}>
              <StockAlerts />
            </Section>
            <Section title="Penjualan terakhir" link={{ label: 'Riwayat', to: '/riwayat' }}>
              <RecentSales />
            </Section>
          </div>
        </div>
      </ScreenBody>
    </Screen>
  )
}
