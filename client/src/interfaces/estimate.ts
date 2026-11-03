/** Matches the server's EstimateStatus enum, which is sent as a number. */
export const EstimateStatus = {
  Draft: 0,
  Confirmed: 1,
} as const

export type EstimateStatus = (typeof EstimateStatus)[keyof typeof EstimateStatus]

export interface EstimateMaterialLine {
  id: number
  materialName: string
  materialUnit: string
  consumptionNorm: number
  wastePercent: number
  /** Calculated by the server, not rounded. */
  quantity: number
  price: number
  /** Calculated and rounded by the server. */
  cost: number
}

export interface EstimateWorkLine {
  id: number
  workName: string
  workUnit: string
  quantity: number
  labourNorm: number
  labourRate: number
  labourCost: number
  materialLines: EstimateMaterialLine[]
}

/** A whole estimate; every amount is calculated by the server (the browser never calculates). */
export interface Estimate {
  id: number
  projectId: number
  name: string
  status: EstimateStatus
  overheadPercent: number
  profitPercent: number
  vatPercent: number
  workLines: EstimateWorkLine[]
  directCosts: number
  overhead: number
  profit: number
  vat: number
  total: number
}

export interface EstimateSummary {
  id: number
  name: string
  status: EstimateStatus
  total: number
}

export interface Percentages {
  overheadPercent: number
  profitPercent: number
  vatPercent: number
}
