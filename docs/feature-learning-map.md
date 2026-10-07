# Workly feature learning map

Use this file as the implementation and troubleshooting route for the whole product. A feature is complete only when its API authorization, database invariants, UI states, audit event, and tests are all present.

## 1. Identity and workspace foundation

Status: implemented.

- Register, login, refresh rotation, replay protection, logout, and current user
- First workspace created atomically with owner membership
- Organization list, creation, remembered selection, and tenant-state cleanup on logout
- Tenant-filtered real dashboard with role-based project visibility
- Loading, empty, error, and rapid-organization-switch behavior
- Workspace name/slug, personal display name, and password settings
- Global people/project/task search and workspace notification panel

Practice incidents: stolen/replayed refresh token, missing membership, cross-tenant query, stale response race, locked build output.

## 2. People and departments

Status: implemented for the V1 workspace workflow.

- Add an existing registered user, list members, and edit job title, department, role, and status
- Department create/rename, manager assignment, member counts, and guarded deletion
- Owner protections against removal or demotion
- Authorization matrix: owner/admin manage roles; HR manages people data
- Audit events for member and department changes

Practice incidents: two admins edit the same member, a department with members is deleted, a manager belongs to another tenant, the last privileged member is removed.

## 3. Invitations

Planned extension (email delivery is not configured yet):

- Create, list, resend, revoke, expire, and accept invitations
- Store only token hashes; bind acceptance to the authenticated email
- Prevent duplicate active invitations and duplicate memberships
- Delivery adapter with a development outbox before connecting a real email provider

Practice incidents: expired link, replayed link, mail provider timeout, resend racing with accept, mixed-case email duplication.

## 4. Projects and project membership

Status: implemented for the V1 workspace workflow.

- Project list/create/edit, dates, description, and lifecycle status including archive
- Project visibility rules enforced by role and membership
- Task assignment automatically creates active project membership
- Progress derived from tasks rather than stored as a competing value

Practice incidents: HR sees a restricted project, a removed project member remains assigned, archive races with a task update, due date precedes start date.

## 5. Tasks and comments

Status: implemented for the V1 list workflow.

- Create/edit/delete, status transitions, priority, assignee, due date, and position
- Project task filters from the Projects page
- Require assignees to be active workspace/project members

Planned extension: task comments, drag-and-drop Kanban, transactional reorder, and optimistic concurrency.

Practice incidents: two users reorder the same column, an assignee is removed mid-edit, duplicate submits, a comment is posted after task deletion.

## 6. Leave workflow

Status: implemented.

- Request, list, filter, cancel, approve, and reject
- Prevent overlapping pending/approved requests
- Enforce owner/admin/HR and department-manager decision rules
- Prevent self-approval and record decision actor/time/note

Practice incidents: overlapping dates, two approvers decide simultaneously, manager changes department, timezone changes today's leave list.

## 7. Announcements

Status: implemented.

- List/detail/create/edit/delete and pin/unpin
- Owner/admin/HR authoring; organization-wide audience in V1
- Stable pinned-first pagination and audit events

Practice incidents: identical timestamps reorder pages, author is removed, oversized rich text, unsafe HTML.

## 8. Activity and operations

Status: implemented for dashboard activity; operational hardening remains.

- Typed event names and metadata contracts
- Filter/paginate the activity feed
- Correlation id, structured logs, metrics, health/readiness, and safe error responses
- Background cleanup for expired refresh tokens and invitations

Practice incidents: malformed legacy metadata, missing actor, log volume spike, database degraded while liveness remains healthy.

## Definition of done for every feature

1. Write request/response contracts and authorization decision first.
2. Express durable invariants in PostgreSQL when possible and workflow rules in the service.
3. Set the verified organization context before every business query.
4. Project only response fields; never return EF entities directly.
5. Record an activity event in the same transaction as the state change.
6. Add unit/contract tests, integration tests for database behavior, and UI loading/empty/error tests.
7. Add one failure drill explaining how to reproduce, observe, isolate, fix, and prevent a realistic incident.
