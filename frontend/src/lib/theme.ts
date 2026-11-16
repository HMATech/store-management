import { useSyncExternalStore } from 'react'

/** How the app looks: as the device says, or always light or dark. */
export type Theme = 'system' | 'light' | 'dark'

const StorageKey = 'store.theme'
const themes: Theme[] = ['system', 'light', 'dark']
const listeners = new Set<() => void>()

/** The chosen theme. Anything unreadable or unknown in storage means following the device. */
export function getTheme(): Theme {
  try {
    const stored = localStorage.getItem(StorageKey)
    return themes.find((theme) => theme === stored) ?? 'system'
  } catch {
    return 'system'
  }
}

/** Chooses a theme, shows it at once, and remembers it on this device. */
export function setTheme(theme: Theme): void {
  try {
    if (theme === 'system') {
      localStorage.removeItem(StorageKey)
    } else {
      localStorage.setItem(StorageKey, theme)
    }
  } catch {
    // Storage can be blocked. The theme then still applies until the app is closed.
  }
  applyTheme(theme)
  listeners.forEach((listener) => listener())
}

/** Puts the theme on `<html>`, where the design tokens read it. Call it once when the app starts. */
export function applyTheme(theme: Theme = getTheme()): void {
  if (theme === 'system') {
    delete document.documentElement.dataset.theme
  } else {
    document.documentElement.dataset.theme = theme
  }
}

/** The theme after `theme` in the order a single button steps through. */
export function nextTheme(theme: Theme): Theme {
  return themes[(themes.indexOf(theme) + 1) % themes.length]
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

/** The chosen theme, kept current when it changes. */
export function useTheme(): Theme {
  return useSyncExternalStore(subscribe, getTheme)
}
