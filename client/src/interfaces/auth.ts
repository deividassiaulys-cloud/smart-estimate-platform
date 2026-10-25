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
  role: number
}
