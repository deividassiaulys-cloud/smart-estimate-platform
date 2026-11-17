import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 019 US2/US3: estimate sections in the editor — add, group with subtotals, change the quantity after a
// warning, remove with confirmation, add a work to a section.

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function managerToken(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const workLine = (id: number, sectionId: number | null, workName: string, quantity: number, labourCost: number) => ({
  id,
  sectionId,
  workName,
  workUnit: 'm²',
  quantity,
  labourNorm: 0.1,
  labourRate: 25,
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
  vatPercent: 0,
  workLines: [workLine(21, 5, 'Tašelis 28x45 mm', 121.7, 304.25), workLine(22, 5, 'Pelių barjeras', 48.68, 121.7), workLine(23, null, 'Šiukšlių išvežimas', 1, 50)],
  sections: [{ id: 5, name: 'A++ siena', unit: 'm²', quantity: 121.7, subtotal: 425.95 }],
  unsectionedSubtotal: 50,
  directCosts: 475.95,
  overhead: 0,
  profit: 0,
  vat: 0,
  total: 475.95,
}

const catalogSections = [{ id: 7, name: 'A++ siena', unit: 'm²', lines: [] }]
const works = [{ id: 1, name: 'Tašelis 28x45 mm', unit: 'm²', labourRate: 25, labourNorm: 0.1, lines: [] }]

let fetchMock: ReturnType<typeof vi.fn>
let shown: typeof estimate

beforeEach(() => {
  saveToken(managerToken())
  shown = estimate
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method && init.method !== 'GET') return Response.json(shown)
    if (path === '/api/estimates/9') return Response.json(shown)
    if (path === '/api/catalog-sections') return Response.json(catalogSections)
    if (path === '/api/works') return Response.json(works)
    return Response.json([])
  })
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

async function openEditor() {
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await screen.findByRole('heading', { name: 'Pradinė sąmata' })
}

// Money is formatted with a non-breaking space before "€".
const normalize = (text: string | null) => (text ?? '').replace(/\s+/g, ' ')

function changes() {
  return fetchMock.mock.calls
    .filter(([, init]) => (init as RequestInit | undefined)?.method && (init as RequestInit).method !== 'GET')
    .map(([path, init]) => ({ path, method: (init as RequestInit).method, body: (init as RequestInit).body ?? null }))
}

test('adding a catalog section sends it with the section quantity', async () => {
  await openEditor()

  // Spec 020: the section is chosen in the "Pridėti skyrių" dialog; the catalog sections load after the estimate.
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti skyrių' }))
  const dialog = screen.getByRole('dialog', { name: 'Pridėti skyrių' })
  await userEvent.click(await within(dialog).findByRole('button', { name: /A\+\+ siena/ }))
  await userEvent.type(within(dialog).getByLabelText('Kiekis, m²'), '121,7')
  await userEvent.click(within(dialog).getByRole('button', { name: 'Pridėti' }))

  expect(changes()).toEqual([
    { path: '/api/estimates/9/sections', method: 'POST', body: JSON.stringify({ catalogSectionId: 7, quantity: 121.7 }) },
  ])
})

test('lines are grouped under their section with subtotals, the lines without a section last', async () => {
  await openEditor()

  const headings = screen.getAllByRole('rowheader').map((heading) => heading.textContent)
  expect(headings).toEqual(['A++ siena', 'Be skyriaus'])
  expect(normalize(screen.getByText('Skyriaus suma: A++ siena').closest('tr')!.textContent)).toContain('425,95 €')
  expect(normalize(screen.getByText('Skyriaus suma: Be skyriaus').closest('tr')!.textContent)).toContain('50,00 €')
  const order = Array.from(document.querySelectorAll('tbody')).map((body) => body.textContent ?? '')
  const wallIndex = order.findIndex((text) => text.startsWith('A++ siena'))
  const otherIndex = order.findIndex((text) => text.includes('Šiukšlių išvežimas'))
  expect(wallIndex).toBeLessThan(otherIndex)
})

test('changing the section quantity warns first; cancelling sends nothing, confirming sends it', async () => {
  await openEditor()
  const field = () => screen.getByLabelText('Skyriaus kiekis: A++ siena') as HTMLInputElement

  await userEvent.clear(field())
  await userEvent.type(field(), '130{Enter}')
  const dialog = await screen.findByRole('alertdialog')
  expect(within(dialog).getByText(/ranka pakeisti kiekiai bus perrašyti/)).toBeTruthy()
  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))
  expect(changes()).toEqual([])
  expect(field().value).toBe('121,7')

  await userEvent.clear(field())
  await userEvent.type(field(), '130{Enter}')
  await userEvent.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Perskaičiuoti' }))
  expect(changes()).toEqual([
    { path: '/api/estimates/9/sections/5', method: 'PATCH', body: JSON.stringify({ quantity: 130 }) },
  ])
})

test('removing a section asks for confirmation first', async () => {
  await openEditor()

  await userEvent.click(screen.getByRole('button', { name: 'Pašalinti skyrių A++ siena' }))
  const dialog = screen.getByRole('alertdialog')
  expect(within(dialog).getByText(/visi jo darbai su medžiagomis bus pašalinti/)).toBeTruthy()
  await userEvent.click(within(dialog).getByRole('button', { name: 'Pašalinti' }))

  expect(changes()).toEqual([{ path: '/api/estimates/9/sections/5', method: 'DELETE', body: null }])
})

test('a single work can be added to a section', async () => {
  await openEditor()

  // Spec 020: the work, its quantity and its section are chosen in the "Pridėti darbą" dialog.
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti darbą' }))
  const dialog = screen.getByRole('dialog', { name: 'Pridėti darbą' })
  await userEvent.click(await within(dialog).findByRole('button', { name: /Tašelis 28x45 mm/ }))
  await userEvent.selectOptions(within(dialog).getByLabelText('Skyrius'), '5')
  await userEvent.type(within(dialog).getByLabelText('Kiekis, m²'), '2')
  await userEvent.click(within(dialog).getByRole('button', { name: 'Pridėti' }))

  expect(changes()).toEqual([
    { path: '/api/estimates/9/lines', method: 'POST', body: JSON.stringify({ workId: 1, quantity: 2, sectionId: 5 }) },
  ])
})

test('an estimate without sections shows no section headings or section choice', async () => {
  shown = { ...estimate, workLines: [workLine(23, null, 'Šiukšlių išvežimas', 1, 50)], sections: [], directCosts: 50, total: 50 }
  await openEditor()

  expect(screen.queryAllByRole('rowheader')).toHaveLength(0)
  expect(screen.queryByText(/Skyriaus suma/)).toBeNull()
  // The "Pridėti darbą" dialog offers no section choice either.
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti darbą' }))
  expect(within(screen.getByRole('dialog')).queryByLabelText('Skyrius')).toBeNull()
})

test('a Confirmed estimate has no section controls', async () => {
  shown = { ...estimate, status: 1 }
  await openEditor()

  expect(screen.queryByRole('button', { name: '+ Pridėti skyrių' })).toBeNull()
  expect(screen.queryByRole('button', { name: '+ Pridėti darbą' })).toBeNull()
  expect(screen.queryByRole('button', { name: 'Pašalinti skyrių A++ siena' })).toBeNull()
  expect((screen.getByLabelText('Skyriaus kiekis: A++ siena') as HTMLInputElement).disabled).toBe(true)
})

test('after the section quantity changes, each work line shows its recalculated quantity without a reload', async () => {
  // Regression: the work quantity field kept its first text until the page was reloaded.
  await openEditor()
  shown = {
    ...estimate,
    workLines: [workLine(21, 5, 'Tašelis 28x45 mm', 130, 325), workLine(22, 5, 'Pelių barjeras', 52, 130), estimate.workLines[2]],
    sections: [{ ...estimate.sections[0], quantity: 130, subtotal: 455 }],
  }
  const field = screen.getByLabelText('Skyriaus kiekis: A++ siena') as HTMLInputElement

  await userEvent.clear(field)
  await userEvent.type(field, '130{Enter}')
  await userEvent.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Perskaičiuoti' }))

  await vi.waitFor(() =>
    expect((screen.getByLabelText('Kiekis: Pelių barjeras') as HTMLInputElement).value).toBe('52'),
  )
  expect((screen.getByLabelText('Kiekis: Tašelis 28x45 mm') as HTMLInputElement).value).toBe('130')
})
