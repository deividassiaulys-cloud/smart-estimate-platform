import type { Work, WorkRequest } from '../interfaces/work'
import { apiFetch } from './http'

/** Lists the company's works sorted by name, only those whose name contains `search` when given. */
export function getWorks(search = ''): Promise<Work[]> {
  const query = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : ''
  return apiFetch(`/api/works${query}`)
}

/** Returns one work with its composition, for the edit form. */
export function getWork(id: number): Promise<Work> {
  return apiFetch(`/api/works/${id}`)
}

/** Adds a work with its material lines to the company's catalog. */
export function createWork(request: WorkRequest): Promise<Work> {
  return apiFetch('/api/works', { method: 'POST', body: JSON.stringify(request) })
}

/** Changes a work and replaces its whole composition. */
export function updateWork(id: number, request: WorkRequest): Promise<Work> {
  return apiFetch(`/api/works/${id}`, { method: 'PUT', body: JSON.stringify(request) })
}

/** Deletes a work. */
export function deleteWork(id: number): Promise<void> {
  return apiFetch(`/api/works/${id}`, { method: 'DELETE' })
}
