import type { CreateOrganizationInput, Organization } from '~/types/workspace'

export const useWorkspace = () => {
  const organizations = useState<Organization[]>('workspace:organizations', () => [])
  const currentOrganizationId = useState<string | null>('workspace:current-id', () => null)
  const initialized = useState<boolean>('workspace:initialized', () => false)
  const { apiFetch, user } = useAuth()

  const currentOrganization = computed(() =>
    organizations.value.find(organization => organization.id === currentOrganizationId.value) ?? null)

  const selectOrganization = (organizationId: string) => {
    if (!organizations.value.some(organization => organization.id === organizationId)) return
    currentOrganizationId.value = organizationId
    if (import.meta.client) localStorage.setItem('workly:organization-id', organizationId)
  }

  const createOrganization = async (input: CreateOrganizationInput) => {
    const organization = await apiFetch<Organization>('/organizations', { method: 'POST', body: input })
    organizations.value = [...organizations.value, organization]
    selectOrganization(organization.id)
    return organization
  }

  const updateOrganization = async (organizationId: string, name: string, slug: string) => {
    const organization = await apiFetch<Organization>(`/organizations/${organizationId}`, {
      method: 'PATCH',
      body: { name, slug },
    })
    organizations.value = organizations.value.map(item => item.id === organization.id ? organization : item)
    return organization
  }

  const initialize = async () => {
    if (initialized.value) return currentOrganization.value
    organizations.value = await apiFetch<Organization[]>('/organizations')
    if (organizations.value.length === 0 && user.value) {
      const suffix = crypto.randomUUID().slice(0, 8)
      const base = user.value.displayName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'workspace'
      await createOrganization({ name: `${user.value.displayName}'s Workspace`, slug: `${base.slice(0, 60)}-${suffix}` })
    }
    const saved = import.meta.client ? localStorage.getItem('workly:organization-id') : null
    const next = organizations.value.find(organization => organization.id === saved) ?? organizations.value[0]
    if (next) selectOrganization(next.id)
    initialized.value = true
    return currentOrganization.value
  }

  const reset = () => {
    organizations.value = []
    currentOrganizationId.value = null
    initialized.value = false
  }

  return {
    organizations: readonly(organizations),
    currentOrganizationId: readonly(currentOrganizationId),
    currentOrganization,
    initialized: readonly(initialized),
    initialize,
    createOrganization,
    updateOrganization,
    selectOrganization,
    reset,
  }
}
