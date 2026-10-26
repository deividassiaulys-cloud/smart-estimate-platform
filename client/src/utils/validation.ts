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
