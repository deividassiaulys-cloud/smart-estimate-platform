import type { UserRole } from './auth'

export interface TeamMember {
  id: number
  firstName: string
  lastName: string
  email: string
  role: UserRole
}

export interface CreateViewerRequest {
  firstName: string
  lastName: string
  email: string
  password: string
}
