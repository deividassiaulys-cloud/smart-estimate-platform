import { cleanup, render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 017 US2: the Estimate Editor's 11 columns, the labour row and the Labour / Materials / Machinery totals,
// on spec US2-2: the worked example's wall with "Įrankiai ir įrengimai" (Machinery, 0,20 €), total 1273,97 €.

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function managerToken(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const materialLine = (id: number, materialName: string, kind: number, norm: number, waste: number, quantity: number, price: number, cost: number) => ({
  id,
  materialName,
  materialUnit: 'm²',
  kind,
  consumptionNorm: norm,
  wastePercent: waste,
  quantity,
  price,
  cost,
})

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
      hours: 28,
      materials: 405.31,
      machinery: 7,
      amount: 832.31,
      unitPrice: 23.78,
      materialLines: [
        materialLine(51, 'Gipso plokštė', 0, 2, 10, 77, 4.5, 346.5),
        materialLine(52, 'Varžtai', 0, 25, 5, 918.75, 0.02, 18.38),
        materialLine(53, 'Plėvelė', 0, 1, 5, 36.75, 1.1, 40.43),
        materialLine(54, 'Įrankiai ir įrengimai', 1, 1, 0, 35, 0.2, 7),
      ],
    },
  ],
  labour: 420,
  materials: 405.31,
  machinery: 7,
  directCosts: 832.31,
  overhead: 83.23,
  profit: 137.33,
  vat: 221.1,
  total: 1273.97,
}

beforeEach(() => {
  saveToken(managerToken())
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

async function openEditor(status = 0) {
  const shown = { ...estimate, status }
  vi.stubGlobal('fetch', vi.fn(async (path: string) => Response.json(path === '/api/estimates/9' ? shown : [])))
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await screen.findByRole('heading', { name: 'Pradinė sąmata' })
}

const cells = (row: HTMLElement) => within(row).getAllByRole('cell').map((cell) => cell.textContent?.replace(/\s+/g, ' ').trim())

function rowOf(text: string) {
  return screen.getByText(text).closest('tr')!
}

test('the table has the 11 columns of the export', async () => {
  await openEditor()

  const headings = screen.getAllByRole('columnheader').map((heading) => heading.textContent)
  expect(headings).toEqual([
    'Nr.', 'Pavadinimas', 'Mato vnt.', 'Norma', 'Atliekos %', 'Kaina', 'Kiekis', 'Suma', 'Darbas', 'Medžiagos', 'Mechanizmai', '',
  ])
})

test('the work row shows its number, unit price, amount and the three parts', async () => {
  await openEditor()

  const row = cells(rowOf('Siena A++'))
  expect([row[0], row[1], row[2], row[5], row[7], row[8], row[9], row[10]]).toEqual([
    '1', 'Siena A++', 'm²', '23,78 €', '832,31 €', '420,00 €', '405,31 €', '7,00 €',
  ])
  expect((screen.getByLabelText('Kiekis: Siena A++') as HTMLInputElement).value).toBe('35')
})

test('the labour row shows the norm, the editable rate, the hours and the labour cost', async () => {
  await openEditor()

  // Found through its labour-rate field: "Darbas" is also a column heading and a totals label.
  const row = screen.getByLabelText('Darbo įkainis, €/val.: Siena A++').closest('tr')!
  expect(cells(row).slice(1, 5)).toEqual(['Darbas', 'val.', '0,8', ''])
  expect(cells(row).slice(6, 9)).toEqual(['28', '420,00 €', '420,00 €'])
  const rate = within(row).getByLabelText('Darbo įkainis, €/val.: Siena A++') as HTMLInputElement
  expect([rate.value, rate.disabled]).toEqual(['15,00', false])
})

test('a material line shows its norm and waste, and its cost in the column of its kind', async () => {
  await openEditor()

  const board = cells(rowOf('Gipso plokštė'))
  expect([board[3], board[4], board[6], board[7], board[9], board[10]]).toEqual(['2', '10 %', '77', '346,50 €', '346,50 €', ''])
  const tools = cells(rowOf('Įrankiai ir įrengimai'))
  expect([tools[7], tools[9], tools[10]]).toEqual(['7,00 €', '', '7,00 €'])
})

test('the totals start with Darbas, Medžiagos and Mechanizmai before the direct costs', async () => {
  await openEditor()

  const terms = Array.from(document.querySelectorAll('dl dt')).map((term) => term.textContent)
  const amounts = Array.from(document.querySelectorAll('dl dd')).map((amount) => amount.textContent?.replace(/\s/g, ' '))
  expect(terms.slice(0, 4)).toEqual(['Darbas', 'Medžiagos', 'Mechanizmai', 'Tiesioginės išlaidos'])
  expect(amounts.slice(0, 4)).toEqual(['420,00 €', '405,31 €', '7,00 €', '832,31 €'])
  expect(amounts.at(-1)).toBe('1 273,97 €')
})

test('in a Confirmed estimate the labour rate is read-only', async () => {
  await openEditor(1)

  expect((screen.getByLabelText('Darbo įkainis, €/val.: Siena A++') as HTMLInputElement).disabled).toBe(true)
})
