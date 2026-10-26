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

/** Returns the logged-in user's name, company and role. */
export function getCurrentUser(): Promise<CurrentUser> {
  return apiFetch('/api/auth/me')
}
