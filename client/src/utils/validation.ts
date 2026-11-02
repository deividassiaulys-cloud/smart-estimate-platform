/** Must match PasswordRules.MinLength on the server, which is the authoritative check. */
export const minPasswordLength = 8

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/** Returns an error message for a missing or malformed email, or undefined when it looks valid. */
export function emailError(email: string): string | undefined {
  if (!email.trim()) return 'Įveskite el. paštą.'
  if (!emailPattern.test(email.trim())) return 'Neteisingas el. pašto formatas.'
  return undefined
}

/** Mirrors the server's password rules for immediate feedback: at least 8 characters, not the email. */
export function passwordError(password: string, email: string): string | undefined {
  if (password.length < minPasswordLength) return `Slaptažodis turi būti bent ${minPasswordLength} simbolių.`
  if (password.toLowerCase() === email.trim().toLowerCase()) return 'Slaptažodis negali sutapti su el. paštu.'
  return undefined
}

/**
 * Matches a number that is zero or more with at most `places` decimal places; a comma is accepted as the
 * decimal separator. Mirrors the server's checks for immediate feedback.
 */
export function decimalPattern(places: number): RegExp {
  return new RegExp(`^\\d+([.,]\\d{1,${places}})?$`)
}

/** Converts a number typed with a comma or a dot as the decimal separator. */
export function parseDecimal(text: string): number {
  return Number(text.trim().replace(',', '.'))
}
