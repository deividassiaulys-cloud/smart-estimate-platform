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

const estimate = {
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status: 0,
  overheadPercent: 10,
  profitPercent: 15,
  vatPercent: 21,
  workLines: [],
  directCosts: 825.31,
  overhead: 82.53,
  profit: 136.18,
  vat: 219.24,
  total: 1263.26,
}

// The worked example after the catalog changed the labour rate to 16 €/h and the board to 5.00 € (spec 015).
const preview = {
  changes: [
    { workName: 'Siena A++', materialName: null, field: 0, oldValue: 15, newValue: 16 },
    { workName: 'Siena A++', materialName: 'Gipso plokštė', field: 2, oldValue: 4.5, newValue: 5 },
    { workName: 'Siena A++', materialName: 'Gipso plokštė', field: 4, oldValue: 10, newValue: 12.5 },
  ],
  notFound: [{ workName: 'Siena A++', materialName: 'Plėvelė' }],
  currentTotal: 1263.26,
  newTotal: 1365.05,
}

let fetchMock: ReturnType<typeof vi.fn>
let previewAnswer: () => Response

beforeEach(() => {
  saveToken(managerToken())
  previewAnswer = () => Response.json(preview)
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (path === '/api/estimates/9/refresh') {
      return init?.method === 'POST' ? Response.json({ ...estimate, total: 1365.05 }) : previewAnswer()
    }
    return Response.json(path === '/api/estimates/9' ? estimate : [])
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

async function openPreview() {
  await openEditor()
  await userEvent.click(screen.getByRole('button', { name: 'Atnaujinti kainas iš katalogo' }))
  return screen.findByRole('alertdialog')
}

function posts() {
  return fetchMock.mock.calls.filter(([, init]) => (init as RequestInit | undefined)?.method === 'POST')
}

const normalize = (text: string | null) => (text ?? '').replace(/\s+/g, ' ').trim()

test('the dialog lists the changes, the lines not found and both totals in Lithuanian', async () => {
  const dialog = await openPreview()

  const items = within(dialog).getAllByRole('listitem').map((item) => normalize(item.textContent))
  expect(items).toEqual([
    'Siena A++ — Darbo įkainis: 15,00 € → 16,00 €',
    'Siena A++ / Gipso plokštė — Kaina: 4,50 € → 5,00 €',
    'Siena A++ / Gipso plokštė — Nuostoliai: 10 % → 12,5 %',
    'Siena A++ / Plėvelė',
  ])
  expect(within(dialog).getByText('Kataloge nerasta, liks nepakeista:')).toBeTruthy()
  expect(normalize(within(dialog).getByText(/Iš viso su PVM/).textContent)).toBe('Iš viso su PVM: 1 263,26 € → 1 365,05 €')
})

test('cancelling sends no update', async () => {
  const dialog = await openPreview()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))

  expect(screen.queryByRole('alertdialog')).toBeNull()
  expect(posts()).toEqual([])
})

test('confirming updates the estimate and shows the returned total', async () => {
  const dialog = await openPreview()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Atnaujinti' }))

  expect(posts().map(([path]) => path)).toEqual(['/api/estimates/9/refresh'])
  expect(await screen.findByText('1 365,05 €')).toBeTruthy()
  expect(screen.queryByRole('alertdialog')).toBeNull()
})

test('an estimate that already matches the catalog shows a message and no dialog', async () => {
  previewAnswer = () => Response.json({ changes: [], notFound: [], currentTotal: 1263.26, newTotal: 1263.26 })
  await openEditor()

  await userEvent.click(screen.getByRole('button', { name: 'Atnaujinti kainas iš katalogo' }))

  expect(await screen.findByText('Sąmatos kainos ir normos jau sutampa su katalogu.')).toBeTruthy()
  expect(screen.queryByRole('alertdialog')).toBeNull()
})

test('a Confirmed estimate has no refresh button', async () => {
  fetchMock.mockImplementation(async (path: string) =>
    Response.json(path === '/api/estimates/9' ? { ...estimate, status: 1 } : []),
  )
  await openEditor()

  expect(screen.queryByRole('button', { name: 'Atnaujinti kainas iš katalogo' })).toBeNull()
})

test('a preview refused because the estimate was confirmed elsewhere shows the status message', async () => {
  previewAnswer = () => Response.json({ status: 409, detail: 'Server text XYZ' }, { status: 409 })
  await openEditor()

  await userEvent.click(screen.getByRole('button', { name: 'Atnaujinti kainas iš katalogo' }))

  expect(await screen.findByText('Sąmatos būsena jau pasikeitė, todėl pakeitimas neišsaugotas.')).toBeTruthy()
  expect(document.body.textContent).not.toContain('XYZ')
})

test('Tab moves from Atšaukti to the list of changes and then to Atnaujinti', async () => {
  const dialog = await openPreview()
  const cancel = within(dialog).getByRole('button', { name: 'Atšaukti' })
  expect(document.activeElement).toBe(cancel)

  await userEvent.tab()
  expect(document.activeElement?.contains(within(dialog).getAllByRole('listitem')[0])).toBe(true)

  await userEvent.tab()
  expect(document.activeElement).toBe(within(dialog).getByRole('button', { name: 'Atnaujinti' }))
})
