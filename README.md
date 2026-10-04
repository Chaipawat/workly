# Workly

Workly is an organization workspace for small teams to manage people, departments, projects, tasks, leave, and announcements.

This repository contains the frontend dashboard and the backend/database foundation.
The dashboard still uses typed mock data; the .NET API now has PostgreSQL wiring,
health checks, the reviewed V1 schema, and an initial EF Core migration. Authentication
and business endpoints remain later milestones.

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

API resource contracts and CI/CD remain reserved for later milestones.

## Prerequisites

- Node.js `^22.19.0` or `^24.11.0` (Node `24.21.0` was used here)
- npm 11+
- .NET SDK 10
- Docker Desktop (for local PostgreSQL)


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
dotnet test apps/api/Workly.sln
```

Install Playwright Chromium once with `npx playwright install chromium` if needed.

## Backend setup

```bash
docker compose up -d postgres
dotnet tool restore
dotnet ef database update --project apps/api/src/Workly.Infrastructure
dotnet run --project apps/api/src/Workly.Api
```

The next backend milestone is authentication and versioned business API endpoints.
