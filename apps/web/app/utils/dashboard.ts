import type { DashboardProject, TaskStatus } from '~/types/dashboard'

export const getProjectProgress = (project: Pick<DashboardProject, 'completedTasks' | 'totalTasks'>): number => {
  if (project.totalTasks <= 0) return 0
  return Math.min(100, Math.max(0, Math.round((project.completedTasks / project.totalTasks) * 100)))
}

export const getTaskTotal = (status: Record<TaskStatus, number>): number =>
  Object.values(status).reduce((total, count) => total + count, 0)
