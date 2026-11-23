import type {
  CurrentUser,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
} from '../interfaces/auth'
import { apiFetch } from './http'

/** Registers a visitor: creates their company and Manager account and returns a JWT. */
export function register(request: RegisterRequest): Promise<RegisterResponse> {
  return apiFetch('/api/auth/register', { method: 'POST', body: JSON.stringify(request) })
}

/** Logs a user in and returns a JWT. */
export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiFetch('/api/auth/login', { method: 'POST', body: JSON.stringify(request) })
}

/** Ends the session on the server: every token issued to the user so far stops working. */
export function logoutOnServer(): Promise<void> {
  return apiFetch('/api/auth/logout', { method: 'POST' })
}

/** Returns the logged-in user's name, company and role. */
export function getCurrentUser(): Promise<CurrentUser> {
  return apiFetch('/api/auth/me')
}
