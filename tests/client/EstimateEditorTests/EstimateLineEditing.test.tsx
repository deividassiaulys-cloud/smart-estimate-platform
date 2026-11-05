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

const materialLine = (id: number, materialName: string, materialUnit: string, price: number, cost: number) => ({
  id,
  materialName,
  materialUnit,
  consumptionNorm: 2,
  wastePercent: 10,
  quantity: 77,
  price,
  cost,
})

// The worked example (spec 007): "Siena A++" 35 m² with three materials, total 1263.26.
const estimate = {
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status: 0,
  overheadPercent: 10,
  profitPercent: 15,
  vatPercent: 21,
  workLines: [
    {
      id: 21,
      workName: 'Siena A++',
      workUnit: 'm²',
      quantity: 35,
      labourNorm: 0.8,
      labourRate: 15,
      labourCost: 420,
      materialLines: [
        materialLine(51, 'Gipso plokštė', 'm²', 4.5, 346.5),
        materialLine(52, 'Varžtai', 'vnt.', 0.02, 18.38),
        materialLine(53, 'Plėvelė', 'm²', 1.1, 40.43),
      ],
    },
  ],
  directCosts: 825.31,
  overhead: 82.53,
  profit: 136.18,
  vat: 219.24,
  total: 1263.26,
}

const catalog = [
  { id: 1, name: 'Gipso plokštė', unit: 'm²', price: 4.5 },
  { id: 2, name: 'Drėgmei atspari plokštė', unit: 'm²', price: 6.2 },
  { id: 3, name: 'Cementas', unit: 'kg', price: 0.25 },
]

// What the server answers to a change (only the fields the tests look at differ from the estimate).
const recalculated = { ...estimate, total: 1391.84 }

let fetchMock: ReturnType<typeof vi.fn>
let answer: (path: string, init?: RequestInit) => Response

beforeEach(() => {
  saveToken(managerToken())
  answer = () => Response.json(recalculated)
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method && init.method !== 'GET') return answer(path, init)
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

async function openEditor(status = 0) {
  if (status !== 0) {
    const confirmed = { ...estimate, status }
    fetchMock.mockImplementation(async (path: string) => Response.json(path === '/api/estimates/9' ? confirmed : []))
  }
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await screen.findByRole('heading', { name: 'Pradinė sąmata' })
}

function changes() {
  return fetchMock.mock.calls
    .filter(([, init]) => (init as RequestInit | undefined)?.method && (init as RequestInit).method !== 'GET')
    .map(([path, init]) => ({ path, method: (init as RequestInit).method, body: (init as RequestInit).body }))
}

const problem = (status: number) => () => Response.json({ status, detail: 'Server text XYZ' }, { status })

// ---- US1: prices ----

test('Enter in the labour rate saves it and shows the recalculated estimate', async () => {
  await openEditor()
  const rate = screen.getByLabelText('Darbo įkainis, €/val.: Siena A++')
  expect((rate as HTMLInputElement).value).toBe('15,00')

  await userEvent.clear(rate)
  await userEvent.type(rate, '18{Enter}')

  expect(changes()).toEqual([
    { path: '/api/estimates/9/lines/21/labour-rate', method: 'PATCH', body: JSON.stringify({ labourRate: 18 }) },
  ])
  expect(await screen.findByText('1 391,84 €')).toBeTruthy()
})

test('leaving a material price field saves the price', async () => {
  await openEditor()
  const price = screen.getByLabelText('Kaina: Gipso plokštė')

  await userEvent.clear(price)
  await userEvent.type(price, '4')
  await userEvent.tab()

  expect(changes()).toEqual([
    { path: '/api/estimates/9/lines/21/materials/51/price', method: 'PATCH', body: JSON.stringify({ price: 4 }) },
  ])
})

test('an invalid price shows a Lithuanian message and is not sent', async () => {
  await openEditor()
  const price = screen.getByLabelText('Kaina: Gipso plokštė')

  await userEvent.clear(price)
  await userEvent.type(price, '-1{Enter}')

  expect(screen.getByText('Kaina turi būti 0 arba didesnė, ne daugiau kaip 2 skaitmenys po kablelio.')).toBeTruthy()
  expect(changes()).toEqual([])
})

test("a price the server rejects shows the field's message, not the server's text", async () => {
  answer = problem(400)
  await openEditor()
  const price = screen.getByLabelText('Kaina: Gipso plokštė')

  await userEvent.clear(price)
  await userEvent.type(price, '4{Enter}')

  expect(await screen.findByText('Kaina turi būti 0 arba didesnė, ne daugiau kaip 2 skaitmenys po kablelio.')).toBeTruthy()
  expect(document.body.textContent).not.toContain('XYZ')
})

test('only prices and quantity can be edited: no field for norms or waste', async () => {
  await openEditor()

  const fields = screen.getAllByRole('textbox').map((field) => field.getAttribute('aria-label') ?? '')

  expect(fields.filter((label) => /Kiekis|Kaina|įkainis/.test(label))).toHaveLength(5)
  expect(fields.some((label) => /norma|nuostol/i.test(label))).toBe(false)
})

test('a Confirmed estimate has no editable prices, no replacement and no material removal, and loads no catalog', async () => {
  await openEditor(1)

  expect((screen.getByLabelText('Darbo įkainis, €/val.: Siena A++') as HTMLInputElement).disabled).toBe(true)
  expect((screen.getByLabelText('Kaina: Gipso plokštė') as HTMLInputElement).disabled).toBe(true)
  expect(screen.queryByRole('button', { name: 'Pašalinti Plėvelė' })).toBeNull()
  expect(screen.queryByLabelText('Pakeisti medžiagą: Gipso plokštė')).toBeNull()
  expect(fetchMock.mock.calls.some(([path]) => path === '/api/materials')).toBe(false)
})

// ---- US2: removal ----

test('cancelling the material removal sends nothing', async () => {
  await openEditor()
  await userEvent.click(screen.getByRole('button', { name: 'Pašalinti Plėvelė' }))
  const dialog = screen.getByRole('alertdialog')
  expect(within(dialog).getByText('Pašalinti medžiagą „Plėvelė“ iš šios sąmatos?')).toBeTruthy()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))

  expect(changes()).toEqual([])
  expect(screen.getByText('Plėvelė')).toBeTruthy()
})

test('confirming the material removal deletes it and shows the returned estimate', async () => {
  const withoutFilm = {
    ...estimate,
    workLines: [{ ...estimate.workLines[0], materialLines: estimate.workLines[0].materialLines.slice(0, 2) }],
  }
  answer = () => Response.json(withoutFilm)
  await openEditor()
  await userEvent.click(screen.getByRole('button', { name: 'Pašalinti Plėvelė' }))

  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Pašalinti' }))

  expect(changes()).toEqual([{ path: '/api/estimates/9/lines/21/materials/53', method: 'DELETE', body: undefined }])
  expect(await screen.findByText('Varžtai')).toBeTruthy()
  expect(screen.queryByText('Plėvelė')).toBeNull()
})

test('a removal refused because the estimate was confirmed elsewhere shows the status message', async () => {
  answer = problem(409)
  await openEditor()
  await userEvent.click(screen.getByRole('button', { name: 'Pašalinti Plėvelė' }))

  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Pašalinti' }))

  expect(await screen.findByText('Sąmatos būsena jau pasikeitė, todėl pakeitimas neišsaugotas.')).toBeTruthy()
  expect(document.body.textContent).not.toContain('XYZ')
})

// ---- US3: replacement ----

test('the replacement list offers only other materials with the same unit', async () => {
  await openEditor()

  const select = await screen.findByLabelText('Pakeisti medžiagą: Gipso plokštė')
  const options = within(select).getAllByRole('option').map((option) => option.textContent?.replace(/\s/g, ' '))

  expect(options).toEqual(['Pakeisti medžiagą…', 'Drėgmei atspari plokštė (6,20 €)'])
  // No other "vnt." material in the catalog: the screws line offers no replacement.
  expect(screen.queryByLabelText('Pakeisti medžiagą: Varžtai')).toBeNull()
})

test('choosing a replacement sends it and shows the returned line', async () => {
  const replaced = {
    ...estimate,
    workLines: [
      {
        ...estimate.workLines[0],
        materialLines: [materialLine(51, 'Drėgmei atspari plokštė', 'm²', 6.2, 477.4), ...estimate.workLines[0].materialLines.slice(1)],
      },
    ],
  }
  answer = () => Response.json(replaced)
  await openEditor()

  await userEvent.selectOptions(await screen.findByLabelText('Pakeisti medžiagą: Gipso plokštė'), '2')

  expect(changes()).toEqual([
    { path: '/api/estimates/9/lines/21/materials/51/material', method: 'PATCH', body: JSON.stringify({ materialId: 2 }) },
  ])
  expect(await screen.findByText('477,40 €')).toBeTruthy()
})

test('a refused replacement shows a Lithuanian message', async () => {
  answer = problem(400)
  await openEditor()

  await userEvent.selectOptions(await screen.findByLabelText('Pakeisti medžiagą: Gipso plokštė'), '2')

  expect(await screen.findByText('Pasirinkite to paties mato vieneto medžiagą iš katalogo.')).toBeTruthy()
  expect(document.body.textContent).not.toContain('XYZ')
})

test('a replacement refused because the estimate was confirmed elsewhere shows the status message', async () => {
  answer = problem(409)
  await openEditor()

  await userEvent.selectOptions(await screen.findByLabelText('Pakeisti medžiagą: Gipso plokštė'), '2')

  expect(await screen.findByText('Sąmatos būsena jau pasikeitė, todėl pakeitimas neišsaugotas.')).toBeTruthy()
})
