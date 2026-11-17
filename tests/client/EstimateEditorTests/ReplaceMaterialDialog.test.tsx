import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 020 US2: "Keisti" opens a dialog with a search field and the same-unit catalog materials.

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function managerToken(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const material = (id: number, materialName: string, materialUnit: string) => ({
  id,
  kind: 0,
  materialName,
  materialUnit,
  consumptionNorm: 1,
  wastePercent: 0,
  quantity: 35,
  price: 4.5,
  cost: 157.5,
})

const estimate = {
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status: 0,
  overheadPercent: 0,
  profitPercent: 0,
  vatPercent: 0,
  sections: [],
  unsectionedSubtotal: 157.5,
  workLines: [
    {
      id: 21,
      sectionId: null,
      workName: 'Siena A++',
      workUnit: 'm²',
      quantity: 35,
      labourNorm: 0,
      labourRate: 0,
      labourCost: 0,
      materialLines: [material(51, 'Gipso plokštė', 'm²'), material(52, 'Varžtai', 'vnt.')],
    },
  ],
  directCosts: 157.5,
  overhead: 0,
  profit: 0,
  vat: 0,
  total: 157.5,
}

const catalog = [
  { id: 1, name: 'Gipso plokštė', unit: 'm²', price: 4.5 },
  { id: 3, name: 'Plėvelė Tyvek Soft', unit: 'm²', price: 1.15 },
  { id: 2, name: 'Drėgmei atspari plokštė', unit: 'm²', price: 6.2 },
  { id: 4, name: 'Cementas', unit: 'kg', price: 0.25 },
]

let fetchMock: ReturnType<typeof vi.fn>

beforeEach(() => {
  saveToken(managerToken())
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method && init.method !== 'GET') return Response.json(estimate)
    if (path === '/api/estimates/9') return Response.json(estimate)
    if (path === '/api/materials') return Response.json(catalog)
    return Response.json([])
  })
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function patches() {
  return fetchMock.mock.calls
    .filter(([, init]) => (init as RequestInit | undefined)?.method === 'PATCH')
    .map(([path, init]) => ({ path, body: (init as RequestInit).body }))
}

async function openDialog() {
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  const opener = await screen.findByRole('button', { name: 'Pakeisti medžiagą: Gipso plokštė' })
  await userEvent.click(opener)
  return { opener, dialog: screen.getByRole('dialog', { name: 'Pakeisti medžiagą: Gipso plokštė' }) }
}

const names = (dialog: HTMLElement) =>
  within(dialog).queryAllByRole('listitem').map((item) => item.querySelector('span')?.textContent)

test('the dialog opens with the focus in the search field and lists the other same-unit materials by name', async () => {
  const { dialog } = await openDialog()

  expect(document.activeElement).toBe(within(dialog).getByLabelText('Paieška'))
  expect(names(dialog)).toEqual(['Drėgmei atspari plokštė', 'Plėvelė Tyvek Soft'])
})

test('typing filters the list ignoring letter case, and no match says so', async () => {
  const { dialog } = await openDialog()
  const search = within(dialog).getByLabelText('Paieška')

  await userEvent.type(search, 'DRĖG')
  expect(names(dialog)).toEqual(['Drėgmei atspari plokštė'])

  await userEvent.clear(search)
  await userEvent.type(search, 'nėra tokios')
  expect(within(dialog).getByText('Medžiagų nerasta.')).toBeTruthy()
})

test('choosing with a click sends the same replacement request as before and closes the dialog', async () => {
  const { dialog } = await openDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: /Plėvelė Tyvek Soft/ }))

  expect(patches()).toEqual([
    { path: '/api/estimates/9/lines/21/materials/51/material', body: JSON.stringify({ materialId: 3 }) },
  ])
  expect(screen.queryByRole('dialog')).toBeNull()
})

test('the arrow keys and Enter choose too', async () => {
  await openDialog()

  await userEvent.keyboard('{ArrowDown}{Enter}')

  expect(patches()).toEqual([
    { path: '/api/estimates/9/lines/21/materials/51/material', body: JSON.stringify({ materialId: 2 }) },
  ])
})

test('Escape and Atšaukti close without a change and give the focus back to Keisti', async () => {
  const first = await openDialog()
  await userEvent.keyboard('{Escape}')
  expect(screen.queryByRole('dialog')).toBeNull()
  expect(document.activeElement).toBe(first.opener)

  await userEvent.click(first.opener)
  await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Atšaukti' }))
  expect(screen.queryByRole('dialog')).toBeNull()
  expect(patches()).toEqual([])
})

test('a line with no other material of its unit has no Keisti button', async () => {
  await openDialog()

  expect(screen.queryByRole('button', { name: 'Pakeisti medžiagą: Varžtai' })).toBeNull()
  expect(screen.getByRole('button', { name: 'Pašalinti Varžtai' })).toBeTruthy()
})
