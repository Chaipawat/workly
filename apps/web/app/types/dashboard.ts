export type TaskStatus = 'todo' | 'in-progress' | 'review' | 'done'
export type LeaveType = 'annual' | 'sick'
export type DashboardViewState = 'default' | 'loading' | 'empty' | 'error'

export interface DashboardMetric {
  label: string
  value: number
  trend: string
  icon: 'users' | 'departments' | 'projects' | 'leave'
  tone: 'blue' | 'indigo' | 'emerald' | 'amber'
}

export interface DashboardProject {
  id: string
  name: string
  description: string
  completedTasks: number
  totalTasks: number
  dueLabel: string
  memberInitials: string[]
  accent: 'blue' | 'indigo' | 'emerald'
}

export interface LeaveTodayItem {
  id: string
  name: string
  initials: string
  role: string
  type: LeaveType
  returnLabel: string
}

export interface UpcomingItem {
  id: string
  day: string
  title: string
  detail: string
  kind: 'task' | 'leave'
}

export interface ActivityItem {
  id: string
  actor: string
  initials: string
  action: string
  target: string
  time: string
  tone: 'blue' | 'indigo' | 'emerald' | 'amber'
}

export interface TeamMember {
  id: string
  name: string
  initials: string
  role: string
  status: 'online' | 'away' | 'offline'
}

export interface DashboardData {
  organizationName: string
  currentUserName: string
  metrics: DashboardMetric[]
  projects: DashboardProject[]
  onLeaveToday: LeaveTodayItem[]
  upcoming: UpcomingItem[]
  taskStatus: Record<TaskStatus, number>
  team: TeamMember[]
  activities: ActivityItem[]
}
