import type { CreateViewerRequest, TeamMember } from '../interfaces/team'
import { apiFetch } from './http'

/** Lists everyone in the Manager's company. */
export function getTeam(): Promise<TeamMember[]> {
  return apiFetch('/api/team')
}

/** Creates a Viewer account in the Manager's company. */
export function createViewer(request: CreateViewerRequest): Promise<TeamMember> {
  return apiFetch('/api/team/viewers', { method: 'POST', body: JSON.stringify(request) })
}

/** Removes a Viewer; their access ends immediately. */
export function removeViewer(id: number): Promise<void> {
  return apiFetch(`/api/team/viewers/${id}`, { method: 'DELETE' })
}

/** Sets a new password for a Viewer and logs out their open sessions. */
export function resetViewerPassword(id: number, newPassword: string): Promise<void> {
  return apiFetch(`/api/team/viewers/${id}/password`, {
    method: 'PATCH',
    body: JSON.stringify({ newPassword }),
  })
}
