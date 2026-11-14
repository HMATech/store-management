import { useState } from 'react'

/** Today's date, fixed when the component first shows so it stays the same across re-renders. */
export function useToday(): Date {
  const [today] = useState(() => new Date())
  return today
}
