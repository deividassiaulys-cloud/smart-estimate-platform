import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

// Feature 020: "+ Pridėti darbą" / "+ Pridėti skyrių" open dialogs with search, quantity and "create new".

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function managerToken(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Manager' })}.signature`
}

const estimate = {
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status: 0,
  overheadPercent: 0,
  profitPercent: 0,
  vatPercent: 0,
  workLines: [],
  sections: [{ id: 5, name: 'A++ siena', unit: 'm²', quantity: 10, subtotal: 0 }],
  unsectionedSubtotal: 0,
  directCosts: 0,
  overhead: 0,
  profit: 0,
  vat: 0,
  total: 0,
}

const works = [
  { id: 1, name: 'Tašelis 28x45 mm', unit: 'm²', labourRate: 25, labourNorm: 0.04, lines: [] },
  { id: 2, name: 'Pelių barjeras', unit: 'm', labourRate: 25, labourNorm: 0.11, lines: [] },
]
const catalogSections = [{ id: 7, name: 'Stogas', unit: 'm²', lines: [{ id: 1, workId: 1, workName: 'Tašelis 28x45 mm', workUnit: 'm²', coefficient: 1 }] }]

let fetchMock: ReturnType<typeof vi.fn>
let addAnswer: () => Response

beforeEach(() => {
  saveToken(managerToken())
  addAnswer = () => Response.json(estimate)
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method === 'POST' && path === '/api/works')
      return Response.json({ id: 99, lines: [], ...JSON.parse(String(init.body)) }, { status: 201 })
    if (init?.method === 'POST' && path === '/api/catalog-sections') {
      const body = JSON.parse(String(init.body))
      return Response.json({ id: 77, name: body.name, unit: body.unit, lines: [] }, { status: 201 })
    }
    if (init?.method && init.method !== 'GET') return addAnswer()
    const responses: Record<string, unknown> = {
      '/api/estimates/9': estimate,
      '/api/works': works,
      '/api/catalog-sections': catalogSections,
      '/api/units': ['m²', 'm'],
    }
    return Response.json(responses[path] ?? [])
  })
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function posts() {
  return fetchMock.mock.calls
    .filter(([, init]) => (init as RequestInit | undefined)?.method === 'POST')
    .map(([path, init]) => ({ path, body: JSON.parse(String((init as RequestInit).body)) }))
}

async function openAddWork() {
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await screen.findByRole('heading', { name: 'Pradinė sąmata' })
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti darbą' }))
  return screen.getByRole('dialog', { name: 'Pridėti darbą' })
}

test('the work dialog searches, ignoring case, with the focus in the search field', async () => {
  const dialog = await openAddWork()
  const search = within(dialog).getByLabelText('Paieška')

  expect(document.activeElement).toBe(search)
  await userEvent.type(search, 'TAŠ')
  expect(within(dialog).getAllByRole('listitem').map((item) => item.querySelector('span')?.textContent)).toEqual([
    'Tašelis 28x45 mm',
  ])
  await userEvent.clear(search)
  await userEvent.type(search, 'nėra tokio')
  expect(within(dialog).getByText('Darbų nerasta.')).toBeTruthy()
})

test('choosing a work moves to its quantity; an invalid quantity is refused and nothing is sent', async () => {
  const dialog = await openAddWork()

  await userEvent.click(within(dialog).getByRole('button', { name: /Pelių barjeras/ }))
  const quantity = within(dialog).getByLabelText('Kiekis, m')
  expect(document.activeElement).toBe(quantity)
  await userEvent.type(quantity, '0')
  await userEvent.click(within(dialog).getByRole('button', { name: 'Pridėti' }))

  expect(within(dialog).getByText('Kiekis turi būti didesnis už 0, ne daugiau kaip 3 skaitmenys po kablelio.')).toBeTruthy()
  expect(posts()).toEqual([])
})

test('adding sends the same request as before and closes the dialog; a refusal keeps it open with the message', async () => {
  addAnswer = () => Response.json({ status: 400, detail: 'Server text XYZ' }, { status: 400 })
  const dialog = await openAddWork()
  await userEvent.click(within(dialog).getByRole('button', { name: /Tašelis/ }))
  await userEvent.type(within(dialog).getByLabelText('Kiekis, m²'), '12,5')
  await userEvent.click(within(dialog).getByRole('button', { name: 'Pridėti' }))

  expect(await within(dialog).findByRole('alert')).toBeTruthy()
  expect(screen.getByRole('dialog', { name: 'Pridėti darbą' })).toBeTruthy()

  addAnswer = () => Response.json(estimate)
  await userEvent.click(within(dialog).getByRole('button', { name: 'Pridėti' }))
  await vi.waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
  expect(posts().map((call) => call.body)).toEqual([
    { workId: 1, quantity: 12.5, sectionId: null },
    { workId: 1, quantity: 12.5, sectionId: null },
  ])
})

test('Escape and Atšaukti close the dialog without adding', async () => {
  await openAddWork()
  await userEvent.keyboard('{Escape}')
  expect(screen.queryByRole('dialog')).toBeNull()

  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti darbą' }))
  await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Atšaukti' }))
  expect(screen.queryByRole('dialog')).toBeNull()
  expect(posts()).toEqual([])
})

test('"+ Kurti naują darbą" opens the work form with the search text as its name and comes back with it chosen', async () => {
  const dialog = await openAddWork()
  await userEvent.type(within(dialog).getByLabelText('Paieška'), 'Vėjo izoliacija')
  await userEvent.click(within(dialog).getByRole('button', { name: '+ Kurti naują darbą' }))

  const form = await screen.findByRole('dialog', { name: 'Naujas darbas' })
  expect((within(form).getByLabelText('Pavadinimas') as HTMLInputElement).value).toBe('Vėjo izoliacija')
  await userEvent.selectOptions(await within(form).findByLabelText('Mato vienetas'), 'm²')
  await userEvent.type(within(form).getByLabelText('Darbo įkainis, €/val.'), '25')
  await userEvent.type(within(form).getByLabelText('Darbo norma, val. vienetui'), '0,05')
  await userEvent.click(within(form).getByRole('button', { name: 'Išsaugoti' }))

  const back = await screen.findByRole('dialog', { name: 'Pridėti darbą' })
  expect(posts()[0]).toEqual({
    path: '/api/works',
    body: { name: 'Vėjo izoliacija', unit: 'm²', labourRate: 25, labourNorm: 0.05, lines: [] },
  })
  expect(within(back).getByRole('button', { name: /Vėjo izoliacija/ }).getAttribute('aria-pressed')).toBe('true')
  expect(within(back).getByLabelText('Kiekis, m²')).toBeTruthy()
})

test('cancelling the new work returns to the add dialog with nothing created', async () => {
  const dialog = await openAddWork()
  await userEvent.click(within(dialog).getByRole('button', { name: '+ Kurti naują darbą' }))
  const form = await screen.findByRole('dialog', { name: 'Naujas darbas' })

  await userEvent.click(within(form).getByRole('button', { name: 'Atšaukti' }))

  expect(await screen.findByRole('dialog', { name: 'Pridėti darbą' })).toBeTruthy()
  expect(posts()).toEqual([])
})

test('the section dialog adds a catalog section and can create a new one', async () => {
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await screen.findByRole('heading', { name: 'Pradinė sąmata' })
  await userEvent.click(screen.getByRole('button', { name: '+ Pridėti skyrių' }))
  const dialog = screen.getByRole('dialog', { name: 'Pridėti skyrių' })
  expect(within(dialog).queryByLabelText('Skyrius')).toBeNull()

  await userEvent.type(within(dialog).getByLabelText('Paieška'), 'Grindys')
  expect(within(dialog).getByText('Skyrių nerasta.')).toBeTruthy()
  await userEvent.click(within(dialog).getByRole('button', { name: '+ Kurti naują skyrių' }))
  const form = await screen.findByRole('dialog', { name: 'Naujas skyrius' })
  expect((within(form).getByLabelText('Pavadinimas') as HTMLInputElement).value).toBe('Grindys')
  await userEvent.selectOptions(await within(form).findByLabelText('Mato vienetas'), 'm²')
  await userEvent.click(within(form).getByRole('button', { name: '+ Pridėti darbą' }))
  await userEvent.selectOptions(within(form).getByLabelText('Darbas (1 eilutė)'), '1')
  await userEvent.click(within(form).getByRole('button', { name: 'Išsaugoti' }))

  const back = await screen.findByRole('dialog', { name: 'Pridėti skyrių' })
  await userEvent.type(within(back).getByLabelText('Kiekis, m²'), '20')
  await userEvent.click(within(back).getByRole('button', { name: 'Pridėti' }))

  expect(posts()).toEqual([
    { path: '/api/catalog-sections', body: { name: 'Grindys', unit: 'm²', lines: [{ workId: 1, coefficient: 1 }] } },
    { path: '/api/estimates/9/sections', body: { catalogSectionId: 77, quantity: 20 } },
  ])
})
