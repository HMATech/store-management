import { apiRequest } from '../../lib/api'
import type { Page } from '../../lib/usePagedList'

/** Someone who can sign in to the app. */
export type User = { userId: string; email: string; firstName: string; lastName: string | null }

export type NewUser = { firstName: string; lastName: string; email: string; password: string }

/** How many users one request loads. More are loaded on demand. */
const PageSize = 50

/** What the API asks of a new user, so the form can say it before sending. */
export const MinFirstNameLength = 3
export const MinPasswordLength = 8

/** A user's name as shown: first and last name, without a gap when there is no last name. */
export function fullName(user: Pick<User, 'firstName' | 'lastName'>): string {
  return [user.firstName, user.lastName].filter(Boolean).join(' ')
}

/** One page of the people who can sign in. */
export function fetchUsers(page: number, signal?: AbortSignal): Promise<Page<User>> {
  return apiRequest<Page<User>>(`/users?page=${page}&pageSize=${PageSize}`, { signal })
}

/** Adds someone who can sign in with this email and password. */
export function registerUser(user: NewUser): Promise<void> {
  return apiRequest<void>('/users/register', {
    method: 'POST',
    body: {
      firstName: user.firstName,
      lastName: user.lastName === '' ? null : user.lastName,
      email: user.email,
      password: user.password,
    },
  })
}

/** Whether a password has everything the API asks for: long enough, with each kind of character. */
export function isStrongPassword(password: string): boolean {
  return (
    password.length >= MinPasswordLength
    && /[a-z]/.test(password)
    && /[A-Z]/.test(password)
    && /[0-9]/.test(password)
    && /[#?!@$%^&*-]/.test(password)
  )
}
