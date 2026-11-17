/** Matches the server's MaterialKind enum, which is sent as a number (FR-030). */
export const MaterialKind = {
  Material: 0,
  Machinery: 1,
} as const

export type MaterialKind = (typeof MaterialKind)[keyof typeof MaterialKind]

/** The Lithuanian name of each kind, defined once for the form, the list and the refresh dialog. */
export const materialKindLabels: Record<MaterialKind, string> = {
  [MaterialKind.Material]: 'Medžiaga',
  [MaterialKind.Machinery]: 'Mechanizmas',
}

export interface Material {
  id: number
  name: string
  unit: string
  kind: MaterialKind
  price: number
}

export interface MaterialRequest {
  name: string
  unit: string
  kind: MaterialKind
  price: number
}
