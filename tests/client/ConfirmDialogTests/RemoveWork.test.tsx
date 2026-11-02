import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function token(role: 'Manager' | 'Viewer'): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: role })}.signature`
}

const works = [
  { id: 1, name: 'Lubų dažymas', unit: 'm²', labourRate: 18.5, labourNorm: 0.3, lines: [] },
  { id: 2, name: 'Sienų dažymas', unit: 'm²', labourRate: 18.5, labourNorm: 0.25, lines: [] },
]

let fetchMock: ReturnType<typeof vi.fn>

function mockApi(responses: Record<string, unknown>) {
  fetchMock = vi.fn(async (path: string, init?: RequestInit) =>
    init?.method === 'DELETE' ? new Response(null, { status: 204 }) : Response.json(responses[path] ?? []),
  )
  vi.stubGlobal('fetch', fetchMock)
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function deleteCalls() {
  return fetchMock.mock.calls.filter(([, init]) => (init as RequestInit | undefined)?.method === 'DELETE')
}

async function openDeleteDialog() {
  saveToken(token('Manager'))
  mockApi({ '/api/works': works })
  render(
    <MemoryRouter initialEntries={['/works']}>
      <App />
    </MemoryRouter>,
  )
  const row = (await screen.findByText('Sienų dažymas')).closest('tr')!
  await userEvent.click(within(row).getByRole('button', { name: 'Ištrinti' }))
  return screen.getByRole('alertdialog')
}

test('cancelling the confirmation keeps the work', async () => {
  const dialog = await openDeleteDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))

  expect(screen.queryByRole('alertdialog')).toBeNull()
  expect(deleteCalls()).toHaveLength(0)
  expect(screen.getByText('Sienų dažymas')).toBeTruthy()
})

test('confirming deletes the work', async () => {
  const dialog = await openDeleteDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Ištrinti' }))

  expect(deleteCalls().map(([path]) => path)).toEqual(['/api/works/2'])
  expect(screen.queryByText('Sienų dažymas')).toBeNull()
  expect(screen.getByText('Lubų dažymas')).toBeTruthy()
})

test('a Viewer has no link to the work catalog', async () => {
  saveToken(token('Viewer'))
  mockApi({ '/api/auth/me': { firstName: 'Ona', lastName: 'Onaitė', companyName: 'Statyba UAB', role: 1 } })
  render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <App />
    </MemoryRouter>,
  )

  await screen.findByText('Sveiki, Ona!')

  expect(screen.queryByText('Darbų katalogas')).toBeNull()
})
