import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 017 US1: a catalog material's kind, Medžiaga (0) or Mechanizmas (1), set in the form and shown in the list.

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function token(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const tools = { id: 4, name: 'Įrankiai ir įrengimai', unit: 'vnt.', kind: 1, price: 0.2 }
const board = { id: 5, name: 'Gipso plokštė', unit: 'm²', kind: 0, price: 4.5 }

let fetchMock: ReturnType<typeof vi.fn>

function mockApi(responses: Record<string, unknown>) {
  fetchMock = vi.fn(async (path: string, init?: RequestInit) =>
    init?.method === 'POST' || init?.method === 'PUT'
      ? Response.json({ id: 1, ...JSON.parse(String(init.body)) })
      : Response.json(responses[path] ?? []),
  )
  vi.stubGlobal('fetch', fetchMock)
}

function sentBodies(method: string) {
  return fetchMock.mock.calls
    .filter(([, init]) => (init as RequestInit | undefined)?.method === method)
    .map(([, init]) => JSON.parse(String((init as RequestInit).body)))
}

function open(path: string) {
  saveToken(token())
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

async function fillNewMaterial() {
  await userEvent.type(await screen.findByLabelText('Pavadinimas'), 'Įrankiai ir įrengimai')
  await userEvent.selectOptions(screen.getByLabelText('Mato vienetas'), 'vnt.')
  await userEvent.type(screen.getByLabelText('Kaina, €'), '0,20')
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

test('a new material is a Medžiaga unless another kind is chosen', async () => {
  mockApi({ '/api/units': ['vnt.'] })
  open('/materials/new')

  await fillNewMaterial()
  expect((screen.getByLabelText('Rūšis') as HTMLSelectElement).value).toBe('0')
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  await vi.waitFor(() => expect(sentBodies('POST')).toHaveLength(1))
  expect(sentBodies('POST')[0].kind).toBe(0)
})

test('choosing Mechanizmas sends kind 1', async () => {
  mockApi({ '/api/units': ['vnt.'] })
  open('/materials/new')

  await fillNewMaterial()
  await userEvent.selectOptions(screen.getByLabelText('Rūšis'), 'Mechanizmas')
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  await vi.waitFor(() => expect(sentBodies('POST')).toHaveLength(1))
  expect(sentBodies('POST')[0].kind).toBe(1)
})

test('editing a material loads its stored kind', async () => {
  mockApi({ '/api/units': ['vnt.'], '/api/materials/4': tools })
  open('/materials/4/edit')

  const kind = await screen.findByLabelText('Rūšis')
  await vi.waitFor(() => expect((kind as HTMLSelectElement).value).toBe('1'))
})

test('the materials list shows each kind', async () => {
  mockApi({ '/api/materials': [tools, board] })
  open('/materials')

  const toolsRow = (await screen.findByText('Įrankiai ir įrengimai')).closest('tr')!
  const boardRow = screen.getByText('Gipso plokštė').closest('tr')!
  expect(within(toolsRow).getByText('Mechanizmas')).toBeTruthy()
  expect(within(boardRow).getByText('Medžiaga')).toBeTruthy()
})
