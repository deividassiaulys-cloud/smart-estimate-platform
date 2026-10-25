/** Matches the server's UserRole enum, which is sent as a number. */
export const UserRole = {
  Manager: 0,
  Viewer: 1,
} as const

export type UserRole = (typeof UserRole)[keyof typeof UserRole]

export interface RegisterRequest {
  companyName: string
  firstName: string
  lastName: string
  email: string
  password: string
}

export interface RegisterResponse {
  token: string
  userId: number
  companyId: number
  role: UserRole
}

export interface LoginRequest {
  email: string
  password: string
}

export type LoginResponse = RegisterResponse

export interface CurrentUser {
  firstName: string
  lastName: string
  companyName: string
  role: UserRole
}
