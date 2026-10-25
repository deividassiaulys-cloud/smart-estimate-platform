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

test('after logout a protected page redirects to Login', async () => {
  saveToken('test-token')
  renderAt('/dashboard')
  await userEvent.click(await screen.findByRole('button', { name: 'Atsijungti' }))
  cleanup()

  renderAt('/dashboard')

  expect(await screen.findByRole('heading', { name: 'Prisijungimas' })).toBeTruthy()
  expect(screen.queryByRole('heading', { name: 'Sveiki, Jonas!' })).toBeNull()
})
