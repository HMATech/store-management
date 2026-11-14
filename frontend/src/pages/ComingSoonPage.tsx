import { Empty } from '../components/LoadState'
import { Screen } from '../components/shell/Screen'

/** Stands in for a screen that hasn't been built yet, so the navigation already works. */
export function ComingSoonPage({ title }: { title: string }) {
  return (
    <Screen title={title}>
      <Empty>Halaman ini masih dibuat.</Empty>
    </Screen>
  )
}
