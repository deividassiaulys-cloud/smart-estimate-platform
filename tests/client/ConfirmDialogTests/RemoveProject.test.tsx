import { cleanup, render, screen, within } from '@testing-library/react'
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

const project = { id: 4, name: 'Buto remontas', description: 'Klientas: J. Jonaitis' }

let fetchMock: ReturnType<typeof vi.fn>

function openProject(role: 'Manager' | 'Viewer') {
  saveToken(token(role))
  fetchMock = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method === 'DELETE') return new Response(null, { status: 204 })
    return Response.json(path === '/api/projects' ? [] : project)
  })
  vi.stubGlobal('fetch', fetchMock)
  render(
    <MemoryRouter initialEntries={['/projects/4']}>
      <App />
    </MemoryRouter>,
  )
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

function deleteCalls() {
  return fetchMock.mock.calls.filter(([, init]) => (init as RequestInit | undefined)?.method === 'DELETE')
}

test('cancelling the confirmation keeps the project', async () => {
  openProject('Manager')
  await userEvent.click(await screen.findByRole('button', { name: 'Ištrinti' }))
  const dialog = screen.getByRole('alertdialog')

  await userEvent.click(within(dialog).getByRole('button', { name: 'Atšaukti' }))

  expect(screen.queryByRole('alertdialog')).toBeNull()
  expect(deleteCalls()).toHaveLength(0)
  expect(screen.getByRole('heading', { name: 'Buto remontas' })).toBeTruthy()
})

test('confirming deletes the project and returns to the list', async () => {
  openProject('Manager')
  await userEvent.click(await screen.findByRole('button', { name: 'Ištrinti' }))

  await userEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Ištrinti' }))

  expect(deleteCalls().map(([path]) => path)).toEqual(['/api/projects/4'])
  expect(await screen.findByText('Projektas ištrintas.')).toBeTruthy()
})

test('a Viewer sees the project but no actions to change it', async () => {
  openProject('Viewer')

  expect(await screen.findByRole('heading', { name: 'Buto remontas' })).toBeTruthy()
  expect(screen.getByText('Klientas: J. Jonaitis')).toBeTruthy()
  expect(screen.queryByText('Redaguoti')).toBeNull()
  expect(screen.queryByRole('button', { name: 'Ištrinti' })).toBeNull()

  await userEvent.click(screen.getByText('← Projektai'))
  expect(await screen.findByText('Projektų dar nėra.')).toBeTruthy()
  expect(screen.queryByText('+ Naujas projektas')).toBeNull()
})
