import type { Project, ProjectRequest } from '../interfaces/project'
import { apiFetch } from './http'

/** Lists the company's projects sorted by name. */
export function getProjects(): Promise<Project[]> {
  return apiFetch('/api/projects')
}

/** Returns one project, for Project Detail and the edit form. */
export function getProject(id: number): Promise<Project> {
  return apiFetch(`/api/projects/${id}`)
}

/** Creates a project. */
export function createProject(request: ProjectRequest): Promise<Project> {
  return apiFetch('/api/projects', { method: 'POST', body: JSON.stringify(request) })
}

/** Changes a project's name and description. */
export function updateProject(id: number, request: ProjectRequest): Promise<Project> {
  return apiFetch(`/api/projects/${id}`, { method: 'PUT', body: JSON.stringify(request) })
}

/** Deletes a project permanently. */
export function deleteProject(id: number): Promise<void> {
  return apiFetch(`/api/projects/${id}`, { method: 'DELETE' })
}
