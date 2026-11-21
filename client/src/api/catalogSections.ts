import type { CatalogSection, CatalogSectionRequest } from '../interfaces/catalogSection'
import { apiFetch } from './http'

/** Lists the company's catalog sections sorted by name, only those whose name contains `search` when given. */
export function getCatalogSections(search = ''): Promise<CatalogSection[]> {
  const query = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : ''
  return apiFetch(`/api/catalog-sections${query}`)
}

/** Returns one catalog section with its works, for the edit form. */
export function getCatalogSection(id: number): Promise<CatalogSection> {
  return apiFetch(`/api/catalog-sections/${id}`)
}

/** Adds a catalog section with its works. */
export function createCatalogSection(request: CatalogSectionRequest): Promise<CatalogSection> {
  return apiFetch('/api/catalog-sections', { method: 'POST', body: JSON.stringify(request) })
}

/** Changes a catalog section and replaces its works. */
export function updateCatalogSection(id: number, request: CatalogSectionRequest): Promise<CatalogSection> {
  return apiFetch(`/api/catalog-sections/${id}`, { method: 'PUT', body: JSON.stringify(request) })
}

/** Deletes a catalog section; estimates keep their own sections. */
export function deleteCatalogSection(id: number): Promise<void> {
  return apiFetch(`/api/catalog-sections/${id}`, { method: 'DELETE' })
}
