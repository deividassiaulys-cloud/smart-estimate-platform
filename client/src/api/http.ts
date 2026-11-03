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
 * Sends a request to the API, adding the stored JWT when there is one, and throws an ApiError for a failure.
 * A 401 for a request that carried a token means the session has expired: the token is
 * discarded and the app is told to show Login.
 */
async function send(path: string, init: RequestInit): Promise<Response> {
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
  return response
}

/** Sends a JSON request to the API and returns the JSON answer (nothing for 204). */
export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await send(path, init)
  if (response.status === 204) return undefined as T
  return response.json()
}

/** A downloaded file and the name the server gave it. */
export interface DownloadedFile {
  blob: Blob
  fileName: string
}

/** Downloads a file from the API, taking its name from the Content-Disposition header. */
export async function apiDownload(path: string, fallbackName: string): Promise<DownloadedFile> {
  const response = await send(path, {})
  return { blob: await response.blob(), fileName: fileNameOf(response.headers.get('Content-Disposition')) ?? fallbackName }
}

// Prefers the RFC 5987 filename* (keeps Lithuanian letters), then the plain filename.
function fileNameOf(disposition: string | null): string | null {
  if (!disposition) return null
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)
  if (encoded) return decodeURIComponent(encoded[1])
  const plain = /filename="?([^";]+)"?/i.exec(disposition)
  return plain ? plain[1] : null
}
