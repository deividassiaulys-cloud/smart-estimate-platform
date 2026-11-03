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

const workLine = (id: number, workName: string, labourCost: number) => ({
  id,
  workName,
  workUnit: 'm²',
  quantity: 35,
  labourNorm: 0.8,
  labourRate: 15,
  labourCost,
  materialLines: [],
})

const estimate = {
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status: 0,
  overheadPercent: 0,
  profitPercent: 0,
  vatPercent: 21,
  workLines: [workLine(21, 'Siena A++', 420), workLine(22, 'Lubų dažymas', 100)],
  directCosts: 520,
  overhead: 0,
  profit: 0,
  vat: 109.2,
  total: 629.2,
}

// What the server answers after removing line 22: the recalculated estimate.
const afterRemoval = { ...estimate, workLines: [estimate.workLines[0]], directCosts: 420, vat: 88.2, total: 508.2 }

let fetchMock: ReturnType<typeof vi.fn>

beforeEach(() => {
  saveToken(managerToken())
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method === 'DELETE') return Response.json(afterRemoval)
    return Response.json(path === '/api/estimates/9' ? estimate : [])
  })
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function deleteCalls() {
  return fetchMock.mock.calls.filter(([, init]) => (init as RequestInit | undefined)?.method === 'DELETE')
}

async function openRemoveDialog() {
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await userEvent.click(await screen.findByRole('button', { name: 'Pašalinti Lubų dažymas' }))
  return screen.getByRole('alertdialog')
}

test('cancelling the confirmation keeps the work line', async () => {
  const dialog = await openRemoveDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))

  expect(screen.queryByRole('alertdialog')).toBeNull()
  expect(deleteCalls()).toHaveLength(0)
  expect(screen.getByText('Lubų dažymas')).toBeTruthy()
})

test('confirming removes the work line and shows the recalculated totals', async () => {
  const dialog = await openRemoveDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Pašalinti' }))

  expect(deleteCalls().map(([path]) => path)).toEqual(['/api/estimates/9/lines/22'])
  expect(screen.queryByText('Lubų dažymas')).toBeNull()
  expect(screen.getByText('Siena A++')).toBeTruthy()
  // The total comes from the server's answer; the browser does not calculate it.
  expect(screen.getByText(/508,20/)).toBeTruthy()
})
