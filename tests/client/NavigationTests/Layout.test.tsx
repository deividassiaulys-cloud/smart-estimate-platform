import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
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
  overheadPercent: 12.5,
  profitPercent: 15,
  vatPercent: 21,
  workLines: [],
  directCosts: 0,
  overhead: 0,
  profit: 0,
  vat: 0,
  total: 0,
}

function renderAt(path: string, role: 'Manager' | 'Viewer', responses: Record<string, unknown>) {
  saveToken(token(role))
  vi.stubGlobal('fetch', vi.fn(async (url: string) => Response.json(responses[url] ?? [])))
  return render(
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

test('list cells carry the labels the cards show below 768 px', async () => {
  renderAt('/materials', 'Manager', { '/api/materials': [{ id: 1, name: 'Cementas', unit: 'kg', price: 0.25 }] })

  const price = await screen.findByText('0,25 €')

  expect(price.getAttribute('data-label')).toBe('Kaina')
  expect(screen.getByText('kg').getAttribute('data-label')).toBe('Mato vnt.')
})

test("a Viewer's Estimate View links back to the project it was opened from", async () => {
  renderAt('/projects/4', 'Viewer', {
    '/api/projects/4': { id: 4, name: 'Buto remontas', description: null },
    '/api/projects/4/estimate-names': [{ id: 9, name: 'Pradinė sąmata' }],
    '/api/estimates/9/names': { name: 'Pradinė sąmata', works: [] },
  })

  await userEvent.click(await screen.findByRole('link', { name: 'Atidaryti' }))

  const back = await screen.findByRole('link', { name: '← Projektas' })
  expect(back.getAttribute('href')).toBe('/projects/4')
})

test('an Estimate View opened directly links to the project list', async () => {
  renderAt('/estimates/9/view', 'Viewer', { '/api/estimates/9/names': { name: 'Pradinė sąmata', works: [] } })

  const back = await screen.findByRole('link', { name: '← Projektai' })
  expect(back.getAttribute('href')).toBe('/projects')
})

test('forms are prefilled with a decimal comma', async () => {
  renderAt('/materials/1/edit', 'Manager', {
    '/api/materials/1': { id: 1, name: 'Cementas', unit: 'kg', price: 4.5 },
    '/api/units': ['kg', 'm²'],
  })

  expect(((await screen.findByLabelText('Kaina, €')) as HTMLInputElement).value).toBe('4,50')
})

test('the editor shows percentages the Lithuanian way', async () => {
  renderAt('/estimates/9', 'Manager', { '/api/estimates/9': estimate })

  expect(await screen.findByText('Pridėtinės išlaidos (12,5 %)')).toBeTruthy()
  expect(((screen.getByLabelText('Pridėtinės išlaidos, %')) as HTMLInputElement).value).toBe('12,5')
})
