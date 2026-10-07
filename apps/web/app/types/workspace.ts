export interface Organization {
  id: string
  name: string
  slug: string
  role: 'employee' | 'manager' | 'hr' | 'admin' | 'owner'
}

export interface CreateOrganizationInput {
  name: string
  slug: string
}
