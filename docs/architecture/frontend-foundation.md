# Frontend foundation decision record

## Milestone boundary

This milestone establishes the Nuxt application, Workly design tokens, global shell, responsive dashboard, typed mock data, and quality tooling. It deliberately excludes the API, authentication, database, Docker services, and CI workflow so those decisions can be reviewed in their own phases.

## Version baseline (verified 2026-09-29)

| Technology | Baseline | Compatibility decision |
| --- | --- | --- |
| Node.js | 24.21.0 locally | Supported by Nuxt 4.5 (`^24.11.0`) and Vitest 5 (`^24.0.0`) |
| Nuxt | 4.5.2 | Current stable 4.x patch; native `app/` structure |
| Vue | 3.5.43 | Current stable Vue 3 release |
| TypeScript | 5.9.3 | Compatibility baseline: TypeScript 7 is current, but Nuxt's ESLint parser stack fails against it; 5.9 is the latest supported line |
| Vitest | 5.0.2 | Current stable; supports Node 24 and Vite 8 |
| Playwright | 1.63.0 | Current stable; supports Node 20+ |
| .NET | SDK 10.0.401 / runtime 10.0.12 | Current .NET 10 LTS baseline; not installed locally yet |
| EF Core | 10.0.12 | Matches .NET 10 and is supported through 2028-11-10; deferred |
| PostgreSQL | 18.6 | Current stable major/patch; deferred |
| xUnit | 4.0.1 (`xunit.v3`) | Current stable package; deferred |
| Npgsql EF provider | 10.0.3 | Compatible major for EF Core 10; deferred |

Versions were checked against official project sites and package registries. Exact frontend versions are captured in the lockfile.

## Architecture decisions

### npm workspaces

The root delegates commands to `apps/web`. This makes the future `apps/api` boundary explicit without adding monorepo orchestration software before it is useful.

### Nuxt 4 structure

Application code lives in `apps/web/app`, following Nuxt 4 conventions. Pages compose domain components; mock data, types, state orchestration, and calculations remain independently testable.

### CSS design tokens

Workly tokens use CSS custom properties in one global stylesheet. This keeps the first milestone dependency-light and provides a stable semantic contract. A utility framework can be evaluated only if repeated patterns later justify it.

### Dashboard state contract

The dashboard reads typed fixture data through `useDashboard`. Query-controlled preview states (`loading`, `empty`, and `error`) make every major state reproducible for review and testing. The composable is the seam a later REST client will replace.

### Responsive shell

Desktop uses a persistent 256 px sidebar, tablet an 80 px icon rail, and mobile a modal navigation drawer. Cards reflow into fewer columns instead of shrinking the desktop layout.

### Accessibility

The shell uses semantic landmarks and labelled navigation. Controls have accessible names, focus rings are visible, statuses contain text as well as color, progress has ARIA values, and loading/error states announce themselves.
