/** Base URL of the backend API, without a trailing slash. Set through VITE_API_URL. */
export const apiBaseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/+$/, '')
