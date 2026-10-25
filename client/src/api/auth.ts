import type { RegisterRequest, RegisterResponse } from '../interfaces/auth'

/** Error returned by the API, carrying the HTTP status so the UI can pick a message. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/** Registers a visitor: creates their company and Manager account and returns a JWT. */
export async function register(request: RegisterRequest): Promise<RegisterResponse> {
  const response = await fetch('/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new ApiError(response.status, response.statusText)
  }
  return response.json()
}
