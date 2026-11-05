import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function token(role: 'Manager' | 'Viewer'): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: role })}.signature`
}

const estimate = {
  id: 9,
  projectId: 4,
  name: 'Pradinė sąmata',
  status: 0,
  overheadPercent: 0,
  profitPercent: 0,
  vatPercent: 21,
  workLines: [],
  directCosts: 0,
  overhead: 0,
  profit: 0,
  vat: 0,
  total: 0,
}

let fetchMock: ReturnType<typeof vi.fn>
let downloadedAs: string[]

function mockApi(exportResponse: () => Response) {
  fetchMock = vi.fn(async (path: string) => {
    if (path === '/api/estimates/9/export') return exportResponse()
    if (path === '/api/estimates/9/names') return Response.json({ name: 'Pradinė sąmata', works: [] })
    return Response.json(path === '/api/estimates/9' ? estimate : [])
  })
  vi.stubGlobal('fetch', fetchMock)
}

const xlsxFile = () =>
  new Response(new Blob(['xlsx']), {
    headers: { 'Content-Disposition': "attachment; filename=\"Pradine samata.xlsx\"; filename*=UTF-8''Pradin%C4%97%20s%C4%85mata.xlsx" },
  })

beforeEach(() => {
  downloadedAs = []
  // jsdom cannot save files: record what would be downloaded instead.
  vi.stubGlobal('URL', { ...URL, createObjectURL: () => 'blob:estimate', revokeObjectURL: () => {} })
  vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
    downloadedAs.push(this.download)
  })
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function exportCalls() {
  return fetchMock.mock.calls.filter(([path]) => path === '/api/estimates/9/export')
}

async function openExportDialog() {
  saveToken(token('Manager'))
  render(
    <MemoryRouter initialEntries={['/estimates/9']}>
      <App />
    </MemoryRouter>,
  )
  await userEvent.click(await screen.findByRole('button', { name: 'Eksportuoti į Excel' }))
  return screen.getByRole('alertdialog')
}

test('exporting asks first; cancelling requests nothing', async () => {
  mockApi(xlsxFile)
  const dialog = await openExportDialog()

  expect(within(dialog).getByText(/Eksportuoti „Pradinė sąmata“ į Excel\?/)).toBeTruthy()
  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))

  expect(exportCalls()).toHaveLength(0)
  expect(downloadedAs).toEqual([])
})

test("confirming downloads the file under the server's name", async () => {
  mockApi(xlsxFile)
  const dialog = await openExportDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Eksportuoti' }))

  expect(exportCalls()).toHaveLength(1)
  expect(downloadedAs).toEqual(['Pradinė sąmata.xlsx'])
  expect(screen.queryByRole('alertdialog')).toBeNull()
})

test('a failed export shows a message', async () => {
  mockApi(() => Response.json({ detail: 'error' }, { status: 500 }))
  const dialog = await openExportDialog()

  await userEvent.click(within(dialog).getByRole('button', { name: 'Eksportuoti' }))

  expect(await screen.findByText('Nepavyko eksportuoti sąmatos. Bandykite dar kartą.')).toBeTruthy()
  expect(downloadedAs).toEqual([])
})

test("the Viewer's Estimate View has no export button", async () => {
  mockApi(xlsxFile)
  saveToken(token('Viewer'))
  render(
    <MemoryRouter initialEntries={['/estimates/9/view']}>
      <App />
    </MemoryRouter>,
  )

  expect(await screen.findByRole('heading', { name: 'Pradinė sąmata' })).toBeTruthy()
  expect(screen.queryByRole('button', { name: 'Eksportuoti į Excel' })).toBeNull()
})
