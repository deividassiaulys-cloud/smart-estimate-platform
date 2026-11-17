import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 019 US1: the catalog sections (FR-030), e.g. "A++ siena" with works and coefficients.

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function token(role: 'Manager' | 'Viewer' = 'Manager'): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: role })}.signature`
}

const works = [
  { id: 1, name: 'Tašelis 28x45 mm', unit: 'm²', labourRate: 25, labourNorm: 0.042, lines: [] },
  { id: 2, name: 'Pelių barjeras', unit: 'm', labourRate: 25, labourNorm: 0.11, lines: [] },
]
const wall = {
  id: 7,
  name: 'A++ siena',
  unit: 'm²',
  lines: [
    { id: 1, workId: 1, workName: 'Tašelis 28x45 mm', workUnit: 'm²', coefficient: 1 },
    { id: 2, workId: 2, workName: 'Pelių barjeras', workUnit: 'm', coefficient: 0.4 },
  ],
}

let fetchMock: ReturnType<typeof vi.fn>

function mockApi(responses: Record<string, unknown>, answer?: (init: RequestInit) => Response) {
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method === 'DELETE') return new Response(null, { status: 204 })
    if (init?.method === 'POST' || init?.method === 'PUT')
      return answer ? answer(init) : Response.json({ id: 9, ...JSON.parse(String(init.body)) })
    return Response.json(responses[path] ?? [])
  })
  vi.stubGlobal('fetch', fetchMock)
}

function sent(method: string) {
  return fetchMock.mock.calls
    .filter(([, init]) => (init as RequestInit | undefined)?.method === method)
    .map(([path, init]) => ({ path, body: (init as RequestInit).body ? JSON.parse(String((init as RequestInit).body)) : null }))
}

function open(path: string, role: 'Manager' | 'Viewer' = 'Manager') {
  saveToken(token(role))
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

async function fillNewSection() {
  await userEvent.type(await screen.findByLabelText('Pavadinimas'), 'A++ siena')
  await userEvent.selectOptions(screen.getByLabelText('Mato vienetas'), 'm²')
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti darbą' }))
  await userEvent.selectOptions(screen.getByLabelText('Darbas (1 eilutė)'), '1')
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti darbą' }))
  await userEvent.selectOptions(screen.getByLabelText('Darbas (2 eilutė)'), '2')
  const coefficient = screen.getByLabelText('Koeficientas (2 eilutė)')
  await userEvent.clear(coefficient)
  await userEvent.type(coefficient, '0,4')
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

test('creating a section sends its name, unit and works in order', async () => {
  mockApi({ '/api/units': ['m²', 'm'], '/api/works': works })
  open('/catalog-sections/new')

  await fillNewSection()
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  await vi.waitFor(() => expect(sent('POST')).toHaveLength(1))
  expect(sent('POST')[0]).toEqual({
    path: '/api/catalog-sections',
    body: { name: 'A++ siena', unit: 'm²', lines: [{ workId: 1, coefficient: 1 }, { workId: 2, coefficient: 0.4 }] },
  })
})

test('a coefficient of 0 or with 7 decimal places is refused and nothing is sent', async () => {
  mockApi({ '/api/units': ['m²', 'm'], '/api/works': works })
  open('/catalog-sections/new')
  await fillNewSection()
  const coefficient = screen.getByLabelText('Koeficientas (2 eilutė)')

  for (const value of ['0', '0,0000001']) {
    await userEvent.clear(coefficient)
    await userEvent.type(coefficient, value)
    await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))
    expect(screen.getByText('Koeficientas turi būti didesnis už 0, ne daugiau kaip 6 skaitmenys po kablelio.')).toBeTruthy()
  }
  expect(sent('POST')).toHaveLength(0)
})

test('a section without works is refused', async () => {
  mockApi({ '/api/units': ['m²'], '/api/works': works })
  open('/catalog-sections/new')

  await userEvent.type(await screen.findByLabelText('Pavadinimas'), 'Tuščias')
  await userEvent.selectOptions(screen.getByLabelText('Mato vienetas'), 'm²')
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  expect(screen.getAllByText('Pridėkite bent vieną darbą.').length).toBeGreaterThan(0)
  expect(sent('POST')).toHaveLength(0)
})

test('editing loads the works and their coefficients', async () => {
  mockApi({ '/api/units': ['m²', 'm'], '/api/works': works, '/api/catalog-sections/7': wall })
  open('/catalog-sections/7/edit')

  const second = await screen.findByLabelText('Koeficientas (2 eilutė)')
  expect((second as HTMLInputElement).value).toBe('0,4')
  expect((screen.getByLabelText('Darbas (1 eilutė)') as HTMLSelectElement).value).toBe('1')
})

test('a duplicate name shows a Lithuanian message', async () => {
  mockApi({ '/api/units': ['m²', 'm'], '/api/works': works }, () => Response.json({ status: 409 }, { status: 409 }))
  open('/catalog-sections/new')

  await fillNewSection()
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  expect(await screen.findByText('Skyrius su tokiu pavadinimu ir mato vienetu jau yra.')).toBeTruthy()
})

test('the list shows the number of works, and deleting asks for confirmation first', async () => {
  mockApi({ '/api/catalog-sections': [wall] })
  open('/catalog-sections')

  const row = (await screen.findByText('A++ siena')).closest('tr')!
  expect(within(row).getByText('2')).toBeTruthy()
  await userEvent.click(within(row).getByRole('button', { name: 'Ištrinti' }))
  const dialog = screen.getByRole('alertdialog')
  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))
  expect(sent('DELETE')).toHaveLength(0)

  await userEvent.click(within(row).getByRole('button', { name: 'Ištrinti' }))
  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Ištrinti' }))
  expect(sent('DELETE').map((call) => call.path)).toEqual(['/api/catalog-sections/7'])
})

test('a work used in a section shows which sections block its deletion', async () => {
  mockApi({ '/api/works': works })
  fetchMock.mockImplementation(async (path: string, init?: RequestInit) =>
    init?.method === 'DELETE'
      ? Response.json({ status: 409, sections: ['A++ siena'] }, { status: 409 })
      : Response.json(path === '/api/works' ? works : []),
  )
  open('/works')

  const row = (await screen.findByText('Pelių barjeras')).closest('tr')!
  await userEvent.click(within(row).getByRole('button', { name: 'Ištrinti' }))
  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Ištrinti' }))

  expect(await screen.findByText('Darbo negalima ištrinti, nes jis naudojamas skyriuose: A++ siena.')).toBeTruthy()
})

test('a Viewer has no Skyriai link', async () => {
  mockApi({})
  open('/dashboard', 'Viewer')

  await screen.findAllByRole('link', { name: 'Projektai' })
  expect(screen.queryByRole('link', { name: 'Skyriai' })).toBeNull()
})
