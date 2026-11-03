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

const draft = {
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status: 0,
  overheadPercent: 0,
  profitPercent: 0,
  vatPercent: 21,
  workLines: [
    { id: 21, workName: 'Siena A++', workUnit: 'm²', quantity: 35, labourNorm: 0.8, labourRate: 15, labourCost: 420, materialLines: [] },
  ],
  directCosts: 420,
  overhead: 0,
  profit: 0,
  vat: 88.2,
  total: 508.2,
}
const confirmed = { ...draft, status: 1 }

let fetchMock: ReturnType<typeof vi.fn>

beforeEach(() => {
  saveToken(managerToken())
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (path === '/api/estimates/9/confirm') return Response.json(confirmed)
    if (path === '/api/estimates/9/draft') return Response.json(draft)
    return Response.json(path === '/api/estimates/9' ? draft : [])
  })
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function patchCalls() {
  return fetchMock.mock.calls
    .filter(([, init]) => (init as RequestInit | undefined)?.method === 'PATCH')
    .map(([path]) => path)
}

async function openEditor() {
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await screen.findByText('Juodraštis')
}

test('confirming asks first; cancelling sends nothing', async () => {
  await openEditor()

  await userEvent.click(screen.getByRole('button', { name: 'Patvirtinti' }))
  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Atšaukti' }))

  expect(patchCalls()).toEqual([])
  expect(screen.getByText('Juodraštis')).toBeTruthy()
})

test('a confirmed estimate is read-only, and returning it to Draft makes it editable again', async () => {
  await openEditor()

  await userEvent.click(screen.getByRole('button', { name: 'Patvirtinti' }))
  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Patvirtinti' }))

  expect(patchCalls()).toEqual(['/api/estimates/9/confirm'])
  expect(await screen.findByText('Patvirtinta')).toBeTruthy()
  expect(screen.queryByRole('button', { name: 'Pridėti darbą' })).toBeNull()
  expect(screen.queryByRole('button', { name: 'Pašalinti Siena A++' })).toBeNull()
  expect((screen.getByLabelText('Kiekis: Siena A++') as HTMLInputElement).disabled).toBe(true)
  expect((screen.getByLabelText('PVM, %') as HTMLInputElement).disabled).toBe(true)

  await userEvent.click(screen.getByRole('button', { name: 'Grąžinti į juodraštį' }))
  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Grąžinti' }))

  expect(patchCalls()).toEqual(['/api/estimates/9/confirm', '/api/estimates/9/draft'])
  expect(await screen.findByText('Juodraštis')).toBeTruthy()
  expect(screen.getByRole('button', { name: 'Pridėti darbą' })).toBeTruthy()
  expect((screen.getByLabelText('Kiekis: Siena A++') as HTMLInputElement).disabled).toBe(false)
})

test('a change rejected because the status changed elsewhere reloads the estimate as read-only', async () => {
  fetchMock.mockImplementation(async (path: string, init?: RequestInit) => {
    if (init?.method === 'PATCH') return Response.json({ detail: 'The estimate is confirmed and cannot be changed.' }, { status: 409 })
    // The first read is the stale Draft page; the reload after the 409 sees the estimate as it now is.
    const reads = fetchMock.mock.calls.filter(([p]) => p === '/api/estimates/9').length
    return Response.json(path === '/api/estimates/9' ? (reads > 1 ? confirmed : draft) : [])
  })
  await openEditor()

  const quantity = screen.getByLabelText('Kiekis: Siena A++')
  await userEvent.clear(quantity)
  await userEvent.type(quantity, '40{Enter}')

  expect(await screen.findByText('Sąmatos būsena jau pasikeitė, todėl pakeitimas neišsaugotas.')).toBeTruthy()
  expect(await screen.findByText('Patvirtinta')).toBeTruthy()
  expect((screen.getByLabelText('Kiekis: Siena A++') as HTMLInputElement).disabled).toBe(true)
})
