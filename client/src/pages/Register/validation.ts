import type { RegisterRequest } from '../../interfaces/auth'
import { emailError, passwordError } from '../../utils/validation'

export type RegisterErrors = Partial<Record<keyof RegisterRequest, string>>

/** Checks the form before submitting so the visitor gets immediate feedback. */
export function validateRegisterForm(form: RegisterRequest): RegisterErrors {
  const errors: RegisterErrors = {
    email: emailError(form.email),
    password: passwordError(form.password, form.email),
  }

  if (!form.companyName.trim()) errors.companyName = 'Įveskite įmonės pavadinimą.'
  if (!form.firstName.trim()) errors.firstName = 'Įveskite vardą.'
  if (!form.lastName.trim()) errors.lastName = 'Įveskite pavardę.'

  return Object.fromEntries(Object.entries(errors).filter(([, message]) => message)) as RegisterErrors
}
