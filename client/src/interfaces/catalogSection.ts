/** One work of a catalog section with its coefficient (work quantity per 1 unit of the section). */
export interface CatalogSectionLine {
  id: number
  workId: number
  workName: string
  workUnit: string
  coefficient: number
}

/** A section template in the company's catalog (FR-030), e.g. "A++ siena". */
export interface CatalogSection {
  id: number
  name: string
  unit: string
  lines: CatalogSectionLine[]
}

export interface CatalogSectionRequest {
  name: string
  unit: string
  lines: { workId: number; coefficient: number }[]
}
