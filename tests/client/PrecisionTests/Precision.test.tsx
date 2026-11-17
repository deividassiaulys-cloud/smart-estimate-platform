import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 016: prices with up to 4 decimal places, norms with up to 6, kept exactly by the catalog forms and lists.

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function token(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const screws = { id: 7, name: 'Sraigtai terasoms', unit: 'vnt.', price: 0.0616 }
const film = { id: 8, name: 'Plėvelė', unit: 'm²', price: 4.5 }
const timber = { id: 9, name: 'Kalibruota mediena', unit: 'm³', price: 440 }
const barrier = { id: 3, name: 'Pelių barjeras', unit: 'm', labourRate: 25, labourNorm: 0.042002, lines: [] }

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

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

test('the materials list shows prices with 2 to 4 decimal places', async () => {
  mockApi({ '/api/materials': [screws, film] })
  open('/materials')

  expect(await screen.findByText('0,0616 €')).toBeTruthy()
  expect(screen.getByText('4,50 €')).toBeTruthy()
})

test('saving a material form unchanged keeps a 4-decimal price', async () => {
  mockApi({ '/api/materials/7': screws, '/api/units': ['vnt.', 'm²'] })
  open('/materials/7/edit')

  const price = await screen.findByLabelText('Kaina, €')
  await vi.waitFor(() => expect((price as HTMLInputElement).value).toBe('0,0616'))
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  expect(sentBodies('PUT')).toEqual([{ name: 'Sraigtai terasoms', unit: 'vnt.', price: 0.0616 }])
})

test('a price with 5 decimal places is refused and not sent', async () => {
  mockApi({ '/api/units': ['vnt.'] })
  open('/materials/new')

  await userEvent.type(await screen.findByLabelText('Pavadinimas'), 'Sraigtai')
  await userEvent.selectOptions(screen.getByLabelText('Mato vienetas'), 'vnt.')
  await userEvent.type(screen.getByLabelText('Kaina, €'), '0,06164')
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  expect(screen.getByText('Kaina turi būti 0 arba daugiau, ne daugiau kaip 4 skaitmenys po kablelio.')).toBeTruthy()
  expect(sentBodies('POST')).toHaveLength(0)
})

test('the works list shows a 6-decimal labour norm in full', async () => {
  mockApi({ '/api/works': [barrier] })
  open('/works')

  expect(await screen.findByText('0,042002')).toBeTruthy()
})

test('the work form accepts a 6-decimal consumption norm and refuses a 7-decimal one', async () => {
  mockApi({ '/api/units': ['m'], '/api/materials': [timber] })
  open('/works/new')

  await userEvent.type(await screen.findByLabelText('Pavadinimas'), 'Pelių barjeras')
  await userEvent.selectOptions(screen.getByLabelText('Mato vienetas'), 'm')
  await userEvent.type(screen.getByLabelText('Darbo įkainis, €/val.'), '25,1234')
  await userEvent.type(screen.getByLabelText('Darbo norma, val. vienetui'), '0,042002')
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti medžiagą' }))
  await userEvent.selectOptions(await screen.findByLabelText('Medžiaga (1 eilutė)'), String(timber.id))
  await userEvent.type(screen.getByLabelText('Sąnaudų norma (1 eilutė)'), '0,0000884')
  await userEvent.type(screen.getByLabelText('Atliekos (1 eilutė)'), '0')
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  expect(screen.getByText('Sąnaudų norma turi būti didesnė už 0, ne daugiau kaip 6 skaitmenys po kablelio.')).toBeTruthy()
  expect(sentBodies('POST')).toHaveLength(0)

  const norm = screen.getByLabelText('Sąnaudų norma (1 eilutė)')
  await userEvent.clear(norm)
  await userEvent.type(norm, '0,000884')
  await userEvent.click(screen.getByRole('button', { name: 'Išsaugoti' }))

  await vi.waitFor(() => expect(sentBodies('POST')).toHaveLength(1))
  const [sent] = sentBodies('POST')
  expect([sent.labourRate, sent.labourNorm, sent.lines[0].consumptionNorm]).toEqual([25.1234, 0.042002, 0.000884])
})
