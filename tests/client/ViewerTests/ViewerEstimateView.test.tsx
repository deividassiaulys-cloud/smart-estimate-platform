import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from '../../../client/src/App'
import { saveToken } from '../../../client/src/utils/auth'

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// An unsigned token is enough here: the client only reads the role from it, the API is mocked.
function viewerToken(): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({ [roleClaim]: 'Viewer' })}.signature`
}

const responses: Record<string, unknown> = {
  '/api/projects/4': { id: 4, name: 'Buto remontas', description: 'Klientas: J. Jonaitis' },
  '/api/projects/4/estimate-names': [
    { id: 9, name: 'Pradinė sąmata' },
    { id: 10, name: 'Virtuvė' },
  ],
  '/api/estimates/9/names': {
    name: 'Pradinė sąmata',
    works: [{ workName: 'Siena A++', materialNames: ['Gipso plokštė', 'Varžtai', 'Plėvelė'] }],
  },
}

let fetchMock: ReturnType<typeof vi.fn>

beforeEach(() => {
  saveToken(viewerToken())
  fetchMock = vi.fn(async (path: string) => Response.json(responses[path] ?? []))
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function requestedPaths() {
  return fetchMock.mock.calls.map(([path]) => path as string)
}

test("a Viewer's Project Detail lists the estimates by name only", async () => {
  render(
    <MemoryRouter initialEntries={['/projects/4']}>
      <App />
    </MemoryRouter>,
  )

  expect(await screen.findByText('Pradinė sąmata')).toBeTruthy()
  expect(screen.getByText('Virtuvė')).toBeTruthy()
  expect(screen.getAllByRole('link', { name: 'Atidaryti' })).toHaveLength(2)
  expect(screen.queryByText(/Juodraštis|Patvirtinta|€/)).toBeNull()
  expect(screen.queryByText('+ Nauja sąmata')).toBeNull()
  // The Viewer never even asks for the Manager's estimate data.
  expect(requestedPaths()).not.toContain('/api/projects/4/estimates')
})

test('the Estimate View shows the work and material names and nothing commercial', async () => {
  render(
    <MemoryRouter initialEntries={['/estimates/9/view']}>
      <App />
    </MemoryRouter>,
  )

  expect(await screen.findByRole('heading', { name: 'Pradinė sąmata' })).toBeTruthy()
  expect(screen.getByRole('heading', { name: 'Siena A++' })).toBeTruthy()
  const page = within(screen.getByRole('main'))
  expect(page.getAllByRole('listitem').map((item) => item.textContent)).toEqual(['Gipso plokštė', 'Varžtai', 'Plėvelė'])

  const text = document.body.textContent ?? ''
  for (const forbidden of ['€', '35', '77', '918,75', '420,00', 'Juodraštis', 'Patvirtinta']) {
    expect(text).not.toContain(forbidden)
  }
  // The only estimate data requested is the names; the shared menu (012) also asks for the company name.
  expect(requestedPaths().filter((path) => path.startsWith('/api/estimates'))).toEqual(['/api/estimates/9/names'])
})

test('a Viewer can go from Project Detail to the Estimate View', async () => {
  render(
    <MemoryRouter initialEntries={['/projects/4']}>
      <App />
    </MemoryRouter>,
  )

  await userEvent.click((await screen.findAllByRole('link', { name: 'Atidaryti' }))[0])

  expect(await screen.findByRole('heading', { name: 'Siena A++' })).toBeTruthy()
})
