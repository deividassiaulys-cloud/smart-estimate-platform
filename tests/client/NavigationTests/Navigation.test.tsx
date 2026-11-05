import { act, cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken, sessionExpiredEvent } from '../../../client/src/utils/auth'

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function token(role: 'Manager' | 'Viewer'): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: role })}.signature`
}

const me = { firstName: 'Jonas', lastName: 'Jonaitis', companyName: 'Statyba UAB', role: 0 }

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(async (path: string) => Response.json(path === '/api/auth/me' ? me : [])))
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function renderAt(path: string, role?: 'Manager' | 'Viewer') {
  if (role) saveToken(token(role))
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

function menuLinks() {
  return within(screen.getByRole('navigation', { name: 'Pagrindinis meniu' }))
    .getAllByRole('link')
    .map((link) => link.textContent)
}

test("a Manager's menu shows the company name and every Manager item", async () => {
  renderAt('/projects', 'Manager')

  expect(await screen.findByText('Statyba UAB')).toBeTruthy()
  expect(menuLinks()).toEqual(['Pagrindinis', 'Projektai', 'Medžiagos', 'Darbai', 'Komanda'])
  expect(screen.getAllByRole('button', { name: 'Atsijungti' })).toHaveLength(1)
})

test("a Viewer's menu shows only Pagrindinis and Projektai", async () => {
  renderAt('/dashboard', 'Viewer')

  await screen.findByText('Statyba UAB')
  expect(menuLinks()).toEqual(['Pagrindinis', 'Projektai'])
})

test('the current page is marked', async () => {
  renderAt('/projects', 'Manager')

  await screen.findByText('Statyba UAB')
  const nav = within(screen.getByRole('navigation', { name: 'Pagrindinis meniu' }))
  expect(nav.getByRole('link', { name: 'Projektai' }).getAttribute('aria-current')).toBe('page')
  expect(nav.getByRole('link', { name: 'Medžiagos' }).getAttribute('aria-current')).toBeNull()
})

test('the Dashboard has exactly one logout, which leads to Login', async () => {
  renderAt('/dashboard', 'Manager')
  await screen.findByRole('heading', { name: 'Sveiki, Jonas!' })

  await userEvent.click(screen.getByRole('button', { name: 'Atsijungti' }))

  expect(await screen.findByRole('heading', { name: 'Prisijungimas' })).toBeTruthy()
  expect(screen.queryByRole('navigation', { name: 'Pagrindinis meniu' })).toBeNull()
})

test('the mobile menu opens, closes with Escape, and closes after choosing an item', async () => {
  renderAt('/dashboard', 'Manager')
  await screen.findByText('Statyba UAB')
  const menuButton = screen.getByRole('button', { name: 'Meniu' })

  await userEvent.click(menuButton)
  expect(menuButton.getAttribute('aria-expanded')).toBe('true')
  expect(within(screen.getByRole('dialog', { name: 'Meniu' })).getAllByRole('link')).toHaveLength(5)

  await userEvent.keyboard('{Escape}')
  expect(screen.queryByRole('dialog', { name: 'Meniu' })).toBeNull()
  expect(document.activeElement).toBe(menuButton)

  await userEvent.click(menuButton)
  await userEvent.click(within(screen.getByRole('dialog', { name: 'Meniu' })).getByRole('link', { name: 'Darbai' }))
  expect(await screen.findByRole('heading', { name: 'Darbų katalogas' })).toBeTruthy()
  expect(screen.queryByRole('dialog', { name: 'Meniu' })).toBeNull()
})

test('a session that expires while the mobile menu is open shows Login without a menu', async () => {
  renderAt('/dashboard', 'Manager')
  await screen.findByText('Statyba UAB')
  await userEvent.click(screen.getByRole('button', { name: 'Meniu' }))

  act(() => {
    window.dispatchEvent(new Event(sessionExpiredEvent))
  })

  expect(await screen.findByRole('heading', { name: 'Prisijungimas' })).toBeTruthy()
  expect(screen.queryByRole('dialog', { name: 'Meniu' })).toBeNull()
  expect(screen.queryByRole('navigation', { name: 'Pagrindinis meniu' })).toBeNull()
})

test.each(['/', '/login', '/register'])('%s has no menu', async (path) => {
  renderAt(path)

  await screen.findAllByRole('heading')
  expect(screen.queryByRole('navigation', { name: 'Pagrindinis meniu' })).toBeNull()
  expect(screen.queryByRole('button', { name: 'Meniu' })).toBeNull()
})
