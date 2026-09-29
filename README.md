# Workly

Workly is an organization workspace for small teams to manage people, departments, projects, tasks, leave, and announcements.

This repository currently contains **Milestone 1: Frontend Foundation + Dashboard**. The dashboard uses typed mock data; authentication, APIs, database tables, and backend business modules are not part of this milestone.

## Repository shape

```text
workly/
├── apps/
│   ├── web/                 # Nuxt 4 + Vue 3 frontend
│       ├── app/
│       │   ├── assets/css/  # Design tokens and global styles
│       │   ├── components/  # Layout, UI, and dashboard components
│       │   ├── composables/ # Page-facing state orchestration
│       │   ├── data/        # Typed development fixtures
│       │   ├── layouts/
│       │   ├── pages/
│       │   ├── types/
│       │   └── utils/
│       ├── e2e/             # Playwright smoke tests
│       └── tests/           # Vitest unit tests
│   └── api/                 # ASP.NET Core .NET 10 (next milestone)
├── docs/
│   ├── architecture/        # Architecture decisions and version baseline
│   ├── database/            # ERD and database decisions (pending review)
│   └── api/                 # REST API contracts (future milestone)
├── docker/                  # Container configuration (future milestone)
├── .github/
│   └── workflows/           # CI/CD workflows (future milestone)
├── docker-compose.yml
└── package.json             # npm workspace commands
```

The reserved directories contain documentation placeholders only. Backend, database, container, and CI implementation still belong to later milestones.

## Prerequisites

- Node.js `^22.19.0` or `^24.11.0` (Node `24.21.0` was used here)
- npm 11+

.NET 10 and Docker are not required until the backend foundation milestone.

## Run locally

```bash
npm install
npm run dev
```

Open `http://localhost:3000/dashboard`. State previews are available at `?state=loading`, `?state=empty`, and `?state=error`.

## Quality commands

```bash
npm run lint
npm run typecheck
npm run test
npm run build
npm run test:e2e
```

Install Playwright Chromium once with `npx playwright install chromium` if needed.

## Current boundary

The next milestone is **Backend Foundation + Database Design**, but database creation waits until the ERD and its constraints have been reviewed and approved.
