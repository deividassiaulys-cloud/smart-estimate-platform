import type { RegisterRequest } from '../../interfaces/auth'

/** Must match AuthService.MinPasswordLength on the server, which is the authoritative check. */
export const minPasswordLength = 8

export type RegisterErrors = Partial<Record<keyof RegisterRequest, string>>

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/** Checks the form before submitting so the visitor gets immediate feedback. */
export function validateRegisterForm(form: RegisterRequest): RegisterErrors {
  const errors: RegisterErrors = {}

  if (!form.companyName.trim()) errors.companyName = 'Įveskite įmonės pavadinimą.'
  if (!form.firstName.trim()) errors.firstName = 'Įveskite vardą.'
  if (!form.lastName.trim()) errors.lastName = 'Įveskite pavardę.'

  if (!form.email.trim()) errors.email = 'Įveskite el. paštą.'
  else if (!emailPattern.test(form.email)) errors.email = 'Neteisingas el. pašto formatas.'

  if (form.password.length < minPasswordLength) {
    errors.password = `Slaptažodis turi būti bent ${minPasswordLength} simbolių.`
  } else if (form.password.toLowerCase() === form.email.trim().toLowerCase()) {
    errors.password = 'Slaptažodis negali sutapti su el. paštu.'
  }

  return errors
}
