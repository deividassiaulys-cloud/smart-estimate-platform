import { UserRole } from '../interfaces/auth'

/** Lithuanian name of each role, as shown to users. */
export const roleNames: Record<UserRole, string> = {
  [UserRole.Manager]: 'Vadovas',
  [UserRole.Viewer]: 'Peržiūrėtojas',
}
