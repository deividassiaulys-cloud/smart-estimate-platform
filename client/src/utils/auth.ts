const tokenKey = 'token'

/** Stores the JWT of the logged-in user. */
export function saveToken(token: string): void {
  localStorage.setItem(tokenKey, token)
}
