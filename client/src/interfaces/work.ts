export interface WorkLine {
  id: number
  materialId: number
  materialName: string
  materialUnit: string
  consumptionNorm: number
  wastePercent: number
}

export interface Work {
  id: number
  name: string
  unit: string
  /** Labour rate in euros per hour. */
  labourRate: number
  /** Labour hours per unit of work. */
  labourNorm: number
  lines: WorkLine[]
}

export interface WorkLineRequest {
  materialId: number
  consumptionNorm: number
  wastePercent: number
}

export interface WorkRequest {
  name: string
  unit: string
  labourRate: number
  labourNorm: number
  lines: WorkLineRequest[]
}
