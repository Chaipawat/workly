export type MemberRole = 'employee' | 'manager' | 'hr' | 'admin' | 'owner'
export interface Member { id: string, name: string, email: string, jobTitle: string, role: MemberRole, status: 'active' | 'removed', departmentId: string | null, departmentName: string | null }
export interface Department { id: string, name: string, managerId: string | null, managerName: string | null, memberCount: number }
export interface Project { id: string, name: string, description: string | null, status: 'active' | 'on-hold' | 'completed' | 'archived', startDate: string | null, dueDate: string | null, completedTasks: number, totalTasks: number }
export interface WorkItem { id: string, projectId: string, projectName: string, title: string, description: string | null, status: 'todo' | 'in-progress' | 'done', priority: 'low' | 'medium' | 'high', assigneeId: string | null, assigneeName: string | null, dueDate: string | null, position: number }
export interface LeaveItem { id: string, memberId: string, memberName: string, type: 'annual' | 'sick' | 'personal' | 'other', startDate: string, endDate: string, reason: string, status: 'pending' | 'approved' | 'rejected' | 'cancelled', decisionNote: string | null, createdAt: string }
export interface AnnouncementItem { id: string, title: string, body: string, pinned: boolean, authorName: string, createdAt: string }
