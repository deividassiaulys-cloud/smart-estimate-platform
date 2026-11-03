import type { Estimate, EstimateSummary, Percentages } from '../interfaces/estimate'
import { apiFetch } from './http'

/** Lists a project's estimates by name, with status and total. */
export function getProjectEstimates(projectId: number): Promise<EstimateSummary[]> {
  return apiFetch(`/api/projects/${projectId}/estimates`)
}

/** Creates a Draft estimate in a project. */
export function createEstimate(projectId: number, name: string): Promise<Estimate> {
  return apiFetch(`/api/projects/${projectId}/estimates`, { method: 'POST', body: JSON.stringify({ name }) })
}

/** Returns one estimate with every amount calculated. */
export function getEstimate(id: number): Promise<Estimate> {
  return apiFetch(`/api/estimates/${id}`)
}

/** Adds a catalog work with a quantity; returns the recalculated estimate. */
export function addWorkLine(id: number, workId: number, quantity: number): Promise<Estimate> {
  return apiFetch(`/api/estimates/${id}/lines`, { method: 'POST', body: JSON.stringify({ workId, quantity }) })
}

/** Changes a work line's quantity; returns the recalculated estimate. */
export function changeQuantity(id: number, lineId: number, quantity: number): Promise<Estimate> {
  return apiFetch(`/api/estimates/${id}/lines/${lineId}`, { method: 'PATCH', body: JSON.stringify({ quantity }) })
}

/** Removes a work line with its material lines; returns the recalculated estimate. */
export function removeWorkLine(id: number, lineId: number): Promise<Estimate> {
  return apiFetch(`/api/estimates/${id}/lines/${lineId}`, { method: 'DELETE' })
}

/** Changes the overhead, profit and VAT percentages; returns the recalculated estimate. */
export function changePercentages(id: number, percentages: Percentages): Promise<Estimate> {
  return apiFetch(`/api/estimates/${id}/percentages`, { method: 'PATCH', body: JSON.stringify(percentages) })
}

/** Confirms a Draft estimate; returns it, now read-only. */
export function confirmEstimate(id: number): Promise<Estimate> {
  return apiFetch(`/api/estimates/${id}/confirm`, { method: 'PATCH' })
}

/** Returns a Confirmed estimate to Draft; returns it, editable again. */
export function returnEstimateToDraft(id: number): Promise<Estimate> {
  return apiFetch(`/api/estimates/${id}/draft`, { method: 'PATCH' })
}
