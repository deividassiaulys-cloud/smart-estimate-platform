import { EstimateStatus } from '../interfaces/estimate'

/** Lithuanian name of each estimate status, always shown as text, not only colour (FR-022). */
export const estimateStatusNames: Record<EstimateStatus, string> = {
  [EstimateStatus.Draft]: 'Juodraštis',
  [EstimateStatus.Confirmed]: 'Patvirtinta',
}
