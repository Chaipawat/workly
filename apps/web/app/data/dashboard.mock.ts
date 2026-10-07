import type { DashboardData } from '~/types/dashboard'

export const dashboardMock: DashboardData = {
  organizationName: 'Acme Studio',
  currentUserName: 'Ryu',
  metrics: [
    { label: 'Members', value: 24, trend: '2 pending invites', icon: 'users', tone: 'blue' },
    { label: 'Departments', value: 5, trend: 'Across the organization', icon: 'departments', tone: 'indigo' },
    { label: 'Active projects', value: 8, trend: '3 due this month', icon: 'projects', tone: 'emerald' },
    { label: 'On leave today', value: 2, trend: 'Both return tomorrow', icon: 'leave', tone: 'amber' },
  ],
  projects: [
    { id: 'website-redesign', name: 'Website Redesign', description: 'Marketing site refresh', completedTasks: 13, totalTasks: 18, dueLabel: 'Due Oct 8', memberInitials: ['SC', 'AM', 'JL'], accent: 'blue' },
    { id: 'mobile-app', name: 'Mobile App', description: 'iOS and Android MVP', completedTasks: 10, totalTasks: 21, dueLabel: 'Due Oct 21', memberInitials: ['RT', 'NK', 'SC'], accent: 'indigo' },
    { id: 'internal-portal', name: 'Internal Portal', description: 'Operations workspace', completedTasks: 21, totalTasks: 25, dueLabel: 'Due Oct 2', memberInitials: ['AM', 'RT'], accent: 'emerald' },
  ],
  onLeaveToday: [
    { id: 'sarah-chen', name: 'Sarah Chen', initials: 'SC', role: 'Product Designer', type: 'annual', returnLabel: 'Returns tomorrow' },
    { id: 'omar-hassan', name: 'Omar Hassan', initials: 'OH', role: 'QA Engineer', type: 'sick', returnLabel: 'Returns tomorrow' },
  ],
  upcoming: [
    { id: 'auth-api', day: 'Today', title: 'API authentication', detail: 'Task due · Mobile App', kind: 'task' },
    { id: 'sarah-away', day: 'Tomorrow', title: 'Sarah Chen away', detail: 'Approved leave', kind: 'leave' },
    { id: 'homepage-copy', day: 'Oct 2', title: 'Homepage copy review', detail: 'Task due · Website Redesign', kind: 'task' },
  ],
  taskStatus: { todo: 18, 'in-progress': 12, done: 31 },
  team: [
    { id: 'alex', name: 'Alex Morgan', initials: 'AM', role: 'Engineering Lead', status: 'online' },
    { id: 'sarah', name: 'Sarah Chen', initials: 'SC', role: 'Product Designer', status: 'away' },
    { id: 'noah', name: 'Noah Kim', initials: 'NK', role: 'Frontend Engineer', status: 'online' },
    { id: 'jane', name: 'Jane Lee', initials: 'JL', role: 'Marketing Lead', status: 'offline' },
  ],
  activities: [
    { id: 'activity-1', actor: 'Alex', initials: 'AM', action: 'moved', target: 'Authentication API to In Progress', time: '12 minutes ago', tone: 'blue' },
    { id: 'activity-2', actor: 'Sarah', initials: 'SC', action: 'requested', target: 'Annual Leave', time: '48 minutes ago', tone: 'amber' },
    { id: 'activity-3', actor: 'Ryu', initials: 'RT', action: 'created', target: 'Website Redesign', time: '2 hours ago', tone: 'indigo' },
    { id: 'activity-4', actor: 'People team', initials: 'PT', action: 'posted', target: 'Office Holiday Notice', time: 'Yesterday', tone: 'emerald' },
  ],
}
