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
