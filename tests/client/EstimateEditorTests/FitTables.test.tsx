import { cleanup, render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 020: long names wrap (2 lines, full text as a tooltip and in the page) and the small actions keep
// accessible names that name their item. The width itself needs a real browser (manual check, SC-020-1).

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function managerToken(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const longName = (prefix: string) => `${prefix} ${'labai ilgas pavadinimas '.repeat(8)}`.slice(0, 200).trim()
const workName = longName('Darbas')
const materialName = longName('Medžiaga')
const sectionName = longName('Skyrius')

const estimate = (status: number) => ({
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status,
  overheadPercent: 0,
  profitPercent: 0,
  vatPercent: 0,
  sections: [{ id: 5, name: sectionName, unit: 'm²', quantity: 10, subtotal: 45 }],
  unsectionedSubtotal: 0,
  workLines: [
    {
      id: 21,
      sectionId: 5,
      workName,
      workUnit: 'm²',
      quantity: 10,
      labourNorm: 0,
      labourRate: 0,
      labourCost: 0,
      materialLines: [
        { id: 51, materialName, materialUnit: 'm²', consumptionNorm: 1, wastePercent: 0, quantity: 10, price: 4.5, cost: 45 },
      ],
    },
  ],
  directCosts: 45,
  overhead: 0,
  profit: 0,
  vat: 0,
  total: 45,
})

const catalog = [
  { id: 1, name: materialName, unit: 'm²', price: 4.5 },
  { id: 2, name: 'Kita plokštė', unit: 'm²', price: 5 },
]

function open(path: string, responses: Record<string, unknown>) {
  saveToken(managerToken())
  vi.stubGlobal('fetch', vi.fn(async (url: string) => Response.json(responses[url] ?? [])))
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

test('long work, material and section names are shown in full with the full name as a tooltip', async () => {
  open('/estimates/9', { '/api/estimates/9': estimate(0), '/api/materials': catalog })

  for (const name of [workName, materialName, sectionName]) {
    // The section name is also an option of the add-work form's section choice; the table cell is the one with a title.
    const shown = (await screen.findAllByText(name)).find((element) => element.hasAttribute('title'))
    expect(shown?.getAttribute('title')).toBe(name)
    expect(shown?.className).toContain('line-clamp-2')
  }
})

test('the small editor actions still name their item for screen readers', async () => {
  open('/estimates/9', { '/api/estimates/9': estimate(0), '/api/materials': catalog })

  const replace = await screen.findByRole('button', { name: `Pakeisti medžiagą: ${materialName}` })
  expect(replace.textContent).toBe('Keisti')
  expect(screen.getByRole('button', { name: `Pašalinti ${materialName}` }).textContent).toBe('Šalinti')
  expect(screen.getByRole('button', { name: `Pašalinti ${workName}` }).textContent).toBe('Šalinti')
  expect(screen.getByRole('button', { name: `Pašalinti skyrių ${sectionName}` }).textContent).toBe('Šalinti')
})

test('a Confirmed estimate shows no actions', async () => {
  open('/estimates/9', { '/api/estimates/9': estimate(1) })

  await screen.findByText(workName)
  expect(screen.queryByRole('button', { name: /^(Pakeisti medžiagą|Pašalinti)/ })).toBeNull()
})

test('the materials and works lists wrap long names and keep their row actions', async () => {
  open('/materials', { '/api/materials': [{ id: 1, name: materialName, unit: 'm²', price: 4.5 }] })
  const material = await screen.findByText(materialName)
  expect(material.getAttribute('title')).toBe(materialName)
  const row = material.closest('tr')!
  expect(within(row).getByRole('link', { name: 'Redaguoti' })).toBeTruthy()
  expect(within(row).getByRole('button', { name: 'Ištrinti' })).toBeTruthy()
  cleanup()

  open('/works', { '/api/works': [{ id: 1, name: workName, unit: 'm²', labourRate: 25, labourNorm: 0.1, lines: [] }] })
  const work = await screen.findByText(workName)
  expect(work.getAttribute('title')).toBe(workName)
  expect(within(work.closest('tr')!).getByRole('button', { name: 'Ištrinti' })).toBeTruthy()
})
