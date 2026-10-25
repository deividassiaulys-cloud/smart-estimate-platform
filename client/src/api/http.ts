import { getToken, logout, sessionExpiredEvent } from '../utils/auth'

/** Error returned by the API, carrying the HTTP status and the problem details body. */
export class ApiError extends Error {
  readonly status: number
  readonly body: Record<string, unknown>

  constructor(status: number, body: Record<string, unknown>) {
    super(typeof body.detail === 'string' ? body.detail : `Request failed with status ${status}`)
    this.status = status
    this.body = body
  }
}

/**
 * Sends a JSON request to the API, adding the stored JWT when there is one.
 * A 401 for a request that carried a token means the session has expired: the token is
 * discarded and the app is told to show Login.
 */
export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken()
  const headers = new Headers(init.headers)
  headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(path, { ...init, headers })

  if (!response.ok) {
    if (response.status === 401 && token) {
      logout()
      window.dispatchEvent(new Event(sessionExpiredEvent))
    }
    const body = await response.json().catch(() => ({}))
    throw new ApiError(response.status, body)
  }
  return response.json()
}
