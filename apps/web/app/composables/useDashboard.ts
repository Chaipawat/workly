import { dashboardMock } from '~/data/dashboard.mock'
import type { DashboardViewState } from '~/types/dashboard'

const validStates: DashboardViewState[] = ['default', 'loading', 'empty', 'error']

export const useDashboard = () => {
  const route = useRoute()
  const state = computed<DashboardViewState>(() => {
    const requested = Array.isArray(route.query.state) ? route.query.state[0] : route.query.state
    return validStates.includes(requested as DashboardViewState) ? requested as DashboardViewState : 'default'
  })

  return { data: ref(dashboardMock), state }
}
