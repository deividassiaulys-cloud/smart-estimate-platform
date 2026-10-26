import type { Material, MaterialRequest } from '../interfaces/material'
import { apiFetch } from './http'

/** Lists the company's materials sorted by name, only those whose name contains `search` when given. */
export function getMaterials(search = ''): Promise<Material[]> {
  const query = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : ''
  return apiFetch(`/api/materials${query}`)
}

/** Returns one material, for the edit form. */
export function getMaterial(id: number): Promise<Material> {
  return apiFetch(`/api/materials/${id}`)
}

/** Adds a material to the company's catalog. */
export function createMaterial(request: MaterialRequest): Promise<Material> {
  return apiFetch('/api/materials', { method: 'POST', body: JSON.stringify(request) })
}

/** Changes a material's name, unit and price. */
export function updateMaterial(id: number, request: MaterialRequest): Promise<Material> {
  return apiFetch(`/api/materials/${id}`, { method: 'PUT', body: JSON.stringify(request) })
}

/** Deletes a material. */
export function deleteMaterial(id: number): Promise<void> {
  return apiFetch(`/api/materials/${id}`, { method: 'DELETE' })
}

/** Lists the fixed measurement units. */
export function getUnits(): Promise<string[]> {
  return apiFetch('/api/units')
}
