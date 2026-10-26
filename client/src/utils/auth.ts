const tokenKey = 'token'

/** Fired when the API rejects the stored token, so the app can send the user to Login. */
export const sessionExpiredEvent = 'session-expired'

/** Stores the JWT of the logged-in user. */
export function saveToken(token: string): void {
  localStorage.setItem(tokenKey, token)
}

/** Returns the stored JWT, or null when nobody is logged in. */
export function getToken(): string | null {
  return localStorage.getItem(tokenKey)
}

/** Logs the user out on this device by discarding the stored JWT. */
export function logout(): void {
  localStorage.removeItem(tokenKey)
}

// The server puts the role in the standard .NET role claim.
const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

/** Returns the role name ("Manager" or "Viewer") in the stored JWT, or null when there is none. */
export function getRoleName(): string | null {
  const token = getToken()
  if (!token) return null
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const claims = JSON.parse(atob(payload)) as Record<string, unknown>
    return typeof claims[roleClaim] === 'string' ? (claims[roleClaim] as string) : null
  } catch {
    return null
  }
}
