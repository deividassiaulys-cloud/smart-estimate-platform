import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function managerToken(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const team = [
  { id: 1, firstName: 'Jonas', lastName: 'Jonaitis', email: 'jonas@example.com', role: 0 },
  { id: 2, firstName: 'Ona', lastName: 'Onaitė', email: 'ona@example.com', role: 1 },
]

let fetchMock: ReturnType<typeof vi.fn>

beforeEach(() => {
  localStorage.clear()
  saveToken(managerToken())
  fetchMock = vi.fn(async (_path: string, init?: RequestInit) =>
    init?.method === 'DELETE' ? new Response(null, { status: 204 }) : Response.json(team),
  )
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

function deleteCalls() {
  return fetchMock.mock.calls.filter(([, init]) => (init as RequestInit | undefined)?.method === 'DELETE')
}

async function openRemoveDialog() {
  render(
    <MemoryRouter initialEntries={['/team']}>
      <App />
    </MemoryRouter>,
  )
  const row = (await screen.findByText('ona@example.com')).closest('tr')!
  await userEvent.click(within(row).getByRole('button', { name: 'Pašalinti' }))
  return screen.getByRole('alertdialog')
}

test('cancelling the confirmation keeps the Viewer', async () => {
  const dialog = await openRemoveDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))

  expect(screen.queryByRole('alertdialog')).toBeNull()
  expect(deleteCalls()).toHaveLength(0)
  expect(screen.getByText('ona@example.com')).toBeTruthy()
})

test('pressing Escape cancels the confirmation', async () => {
  await openRemoveDialog()

  await userEvent.keyboard('{Escape}')

  expect(screen.queryByRole('alertdialog')).toBeNull()
  expect(deleteCalls()).toHaveLength(0)
})

test('confirming removes the Viewer', async () => {
  const dialog = await openRemoveDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Pašalinti' }))

  expect(deleteCalls().map(([path]) => path)).toEqual(['/api/team/viewers/2'])
  expect(screen.queryByText('ona@example.com')).toBeNull()
  expect(screen.getByText('jonas@example.com')).toBeTruthy()
})

test('the Manager row has no remove action', async () => {
  render(
    <MemoryRouter initialEntries={['/team']}>
      <App />
    </MemoryRouter>,
  )
  const managerRow = (await screen.findByText('jonas@example.com')).closest('tr')!

  expect(within(managerRow).queryByRole('button', { name: 'Pašalinti' })).toBeNull()
})
