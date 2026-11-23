import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { getToken, saveToken } from '../../../client/src/utils/auth'

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  localStorage.clear()
  vi.stubGlobal(
    'fetch',
    vi.fn(async () =>
      Response.json({ firstName: 'Jonas', lastName: 'Jonaitis', companyName: 'Statyba UAB', role: 0 }),
    ),
  )
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

test('logging out removes the token and shows Login', async () => {
  saveToken('test-token')
  renderAt('/dashboard')
  expect(await screen.findByRole('heading', { name: 'Sveiki, Jonas!' })).toBeTruthy()

  await userEvent.click(screen.getByRole('button', { name: 'Atsijungti' }))

  expect(await screen.findByRole('heading', { name: 'Prisijungimas' })).toBeTruthy()
  expect(getToken()).toBeNull()
})

test('logging out calls the API and removes the token', async () => {
  saveToken('test-token')
  renderAt('/dashboard')
  await userEvent.click(await screen.findByRole('button', { name: 'Atsijungti' }))

  expect(await screen.findByRole('heading', { name: 'Prisijungimas' })).toBeTruthy()
  const fetchMock = vi.mocked(fetch)
  const logoutCall = fetchMock.mock.calls.find(([path]) => path === '/api/auth/logout')
  expect(logoutCall?.[1]?.method).toBe('POST')
  // The token is still sent with the logout request, so the server knows whose session to end.
  expect(new Headers(logoutCall?.[1]?.headers).get('Authorization')).toBe('Bearer test-token')
  expect(getToken()).toBeNull()
})

test('logging out still removes the token when the server cannot be reached', async () => {
  saveToken('test-token')
  renderAt('/dashboard')
  await screen.findByRole('heading', { name: 'Sveiki, Jonas!' })
  vi.mocked(fetch).mockRejectedValue(new TypeError('Failed to fetch'))

  await userEvent.click(screen.getByRole('button', { name: 'Atsijungti' }))

  expect(await screen.findByRole('heading', { name: 'Prisijungimas' })).toBeTruthy()
  expect(getToken()).toBeNull()
})

test('after logout a protected page redirects to Login', async () => {
  saveToken('test-token')
  renderAt('/dashboard')
  await userEvent.click(await screen.findByRole('button', { name: 'Atsijungti' }))
  cleanup()

  renderAt('/dashboard')

  expect(await screen.findByRole('heading', { name: 'Prisijungimas' })).toBeTruthy()
  expect(screen.queryByRole('heading', { name: 'Sveiki, Jonas!' })).toBeNull()
})
