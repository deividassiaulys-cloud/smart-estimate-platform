export interface Project {
  id: number
  name: string
  description: string | null
}

export interface ProjectRequest {
  name: string
  description: string | null
}
