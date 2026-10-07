import { dashboardMock } from '~/data/dashboard.mock'
import type { DashboardData, DashboardViewState } from '~/types/dashboard'

const validStates: DashboardViewState[] = ['default', 'loading', 'empty', 'error']

export const useDashboard = () => {
  const route = useRoute()
  const { apiFetch } = useAuth()
  const { currentOrganization, initialize } = useWorkspace()
  const data = useState<DashboardData>('dashboard:data', () => dashboardMock)
  const requestState = ref<DashboardViewState>('loading')
  const requestVersion = ref(0)

  const load = async () => {
    const version = ++requestVersion.value
    requestState.value = 'loading'
    try {
      await initialize()
      if (!currentOrganization.value) {
        requestState.value = 'empty'
        return
      }
      const today = new Date().toLocaleDateString('en-CA')
      const response = await apiFetch<DashboardData>(
        `/organizations/${currentOrganization.value.id}/dashboard`, { query: { date: today } },
      )
      if (version !== requestVersion.value) return
      data.value = response
      requestState.value = 'default'
    }
    catch {
      if (version === requestVersion.value) requestState.value = 'error'
    }
  }

  onMounted(load)
  watch(() => currentOrganization.value?.id, (next, previous) => {
    if (previous && next !== previous) load()
  })

  const state = computed<DashboardViewState>(() => {
    const requested = Array.isArray(route.query.state) ? route.query.state[0] : route.query.state
    return validStates.includes(requested as DashboardViewState) ? requested as DashboardViewState : requestState.value
  })

  return { data, state, reload: load }
}
