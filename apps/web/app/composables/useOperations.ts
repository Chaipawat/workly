export const useOperations = () => {
  const { apiFetch } = useAuth()
  const workspace = useWorkspace()

  const request = async <T>(path: string, options: Parameters<typeof apiFetch<T>>[1] = {}) => {
    await workspace.initialize()
    const organization = workspace.currentOrganization.value
    if (!organization) throw new Error('No active workspace.')
    return apiFetch<T>(`/organizations/${organization.id}/${path}`, options)
  }

  return { request, currentOrganization: workspace.currentOrganization }
}
