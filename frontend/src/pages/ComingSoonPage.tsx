import { Empty } from '../components/LoadState'
import { Screen, ScreenBody } from '../components/shell/Screen'

/** Stands in for a screen that hasn't been built yet, so the navigation already works. */
export function ComingSoonPage({ title }: { title: string }) {
  return (
    <Screen title={title}>
      <ScreenBody>
        <Empty>Halaman ini masih dibuat.</Empty>
      </ScreenBody>
    </Screen>
  )
}
