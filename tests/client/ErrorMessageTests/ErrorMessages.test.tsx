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

// What the forms need to load before they can be submitted.
const fixtures: Record<string, unknown> = {
  '/api/auth/me': { firstName: 'Jonas', lastName: 'Jonaitis', email: 'jonas@example.com', role: 0, companyName: 'UAB Statyba' },
  '/api/units': ['kg', 'm2', 'vnt'],
  '/api/materials': [],
  '/api/team': [{ id: 2, firstName: 'Petras', lastName: 'Petraitis', email: 'petras@example.com', role: 1 }],
}

// The server's own text, which must never reach the user.
const serverFailure = () => Response.json({ title: 'Internal Server Error', detail: 'Internal failure XYZ' }, { status: 500 })

type Case = {
  name: string
  path: string
  role?: 'Manager' | 'Viewer'
  // Fill and submit the form; without it the page's first load fails.
  submit?: (user: ReturnType<typeof userEvent.setup>) => Promise<void>
}

const password = 'Slaptazodis123!'

const cases: Case[] = [
  { name: 'Dashboard', path: '/dashboard', role: 'Manager' },
  { name: 'Projects', path: '/projects', role: 'Manager' },
  { name: 'Project Detail (Manager)', path: '/projects/4', role: 'Manager' },
  { name: 'Project Detail (Viewer)', path: '/projects/4', role: 'Viewer' },
  { name: 'project edit form', path: '/projects/4/edit', role: 'Manager' },
  { name: 'Estimate Editor', path: '/estimates/9', role: 'Manager' },
  { name: 'Estimate View', path: '/estimates/9/view', role: 'Viewer' },
  { name: 'Materials', path: '/materials', role: 'Manager' },
  { name: 'material edit form', path: '/materials/1/edit', role: 'Manager' },
  { name: 'Works', path: '/works', role: 'Manager' },
  { name: 'work edit form', path: '/works/1/edit', role: 'Manager' },
  { name: 'Team', path: '/team', role: 'Manager' },
  { name: 'reset password form', path: '/team/2/password', role: 'Manager' },
  {
    name: 'new project form',
    path: '/projects/new',
    role: 'Manager',
    submit: async (user) => {
      await user.type(await screen.findByLabelText(/Pavadinimas/), 'Buto remontas')
      await user.click(screen.getByRole('button', { name: /Išsaugoti|Sukurti/ }))
    },
  },
  {
    name: 'new estimate form',
    path: '/projects/4/estimates/new',
    role: 'Manager',
    submit: async (user) => {
      await user.type(await screen.findByLabelText(/Pavadinimas/), 'Pradinė sąmata')
      await user.click(screen.getByRole('button', { name: /Išsaugoti|Sukurti/ }))
    },
  },
  {
    name: 'new material form',
    path: '/materials/new',
    role: 'Manager',
    submit: async (user) => {
      await user.type(await screen.findByLabelText('Pavadinimas'), 'Cementas')
      await user.selectOptions(await screen.findByLabelText('Mato vienetas'), 'kg')
      await user.type(screen.getByLabelText('Kaina, €'), '0,25')
      await user.click(screen.getByRole('button', { name: /Išsaugoti|Sukurti/ }))
    },
  },
  {
    name: 'new work form',
    path: '/works/new',
    role: 'Manager',
    submit: async (user) => {
      await user.type(await screen.findByLabelText(/Pavadinimas/), 'Tinkavimas')
      await user.selectOptions(await screen.findByLabelText(/Mato vienetas/), 'm2')
      await user.type(screen.getByLabelText(/Darbo kaina|įkainis/i), '10')
      await user.type(screen.getByLabelText(/norma/i), '0,5')
      await user.click(screen.getByRole('button', { name: /Išsaugoti|Sukurti/ }))
    },
  },
  {
    name: 'new viewer form',
    path: '/team/new',
    role: 'Manager',
    submit: async (user) => {
      await user.type(await screen.findByLabelText('Vardas'), 'Petras')
      await user.type(screen.getByLabelText('Pavardė'), 'Petraitis')
      await user.type(screen.getByLabelText('El. paštas'), 'petras@example.com')
      await user.type(screen.getByLabelText('Slaptažodis'), password)
      await user.click(screen.getByRole('button', { name: /Išsaugoti|Sukurti/ }))
    },
  },
  {
    name: 'reset password submit',
    path: '/team/2/password',
    role: 'Manager',
    submit: async (user) => {
      await user.type(await screen.findByLabelText('Naujas slaptažodis'), password)
      await user.click(screen.getByRole('button', { name: /Išsaugoti|Pakeisti/ }))
    },
  },
  {
    name: 'Login',
    path: '/login',
    submit: async (user) => {
      await user.type(await screen.findByLabelText('El. paštas'), 'jonas@example.com')
      await user.type(screen.getByLabelText('Slaptažodis'), password)
      await user.click(screen.getByRole('button', { name: /Prisijungti/ }))
    },
  },
  {
    name: 'Register',
    path: '/register',
    submit: async (user) => {
      await user.type(await screen.findByLabelText('Įmonės pavadinimas'), 'UAB Statyba')
      await user.type(screen.getByLabelText('Vardas'), 'Jonas')
      await user.type(screen.getByLabelText('Pavardė'), 'Jonaitis')
      await user.type(screen.getByLabelText('El. paštas'), 'jonas@example.com')
      await user.type(screen.getByLabelText('Slaptažodis'), password)
      await user.click(screen.getByRole('button', { name: /Registruoti/ }))
    },
  },
]

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
})

test.each(cases)('$name shows a Lithuanian message, never the server text', async ({ path, role, submit }) => {
  if (role) saveToken(token(role))
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string, init?: RequestInit) => {
      const method = init?.method ?? 'GET'
      // Load cases: every request fails. Submit cases: the page loads, then the submit fails.
      if (!submit || method !== 'GET') return serverFailure()
      return Response.json(fixtures[url] ?? {})
    }),
  )
  const user = userEvent.setup()
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )

  if (submit) await submit(user)

  expect(await screen.findByText(/nepavyko/i)).toBeTruthy()
  expect(document.body.textContent).not.toContain('XYZ')
  expect(document.body.textContent).not.toContain('Request failed')
  if (role) expect(screen.getByRole('button', { name: 'Atsijungti' })).toBeTruthy()
})
