# Client Architecture

Layered, feature-based structure so the app stays scalable and testable.

## Layers (dependency direction: top → bottom)

```
App
  → features (auth, dashboard, branches, exams, …)
  → layouts
  → shared (components, hooks, types)
  → core (auth context, auth types)
  → config (env, routes)
  → infrastructure (http client)
```

- **Features** must not import from other features.
- **Core** has no UI and no API implementation; only auth state and types.
- **Shared** has no feature-specific logic.
- **Infrastructure** is the only place that talks to the backend (HTTP).

## Folder structure

```
src/
  config/           # App configuration
    env.ts          # Env vars (VITE_*)
    routes.ts       # Route path constants

  core/             # App-wide core (no UI, no HTTP)
    auth/
      auth.types.ts
      AuthProvider.tsx
    index.ts

  infrastructure/   # Outbound adapters
    http/
      client.ts     # Axios instance, interceptors
      index.ts

  shared/           # Reusable UI and utilities
    components/     # Button, Input, Spinner, …
    hooks/          # useAsync, …
    types/          # ApiResponse, PaginatedResponse, …
    index.ts

  features/         # Feature modules
    auth/           # Login
      api/
      hooks/
      pages/
      auth.types.ts
      index.ts
    dashboard/
      hooks/
      pages/
      index.ts
    branches/
      api/
      branches.types.ts
      index.ts
    exams/
      api/
      exams.types.ts
      index.ts

  layouts/          # Route layouts
    MainLayout.tsx
    ProtectedRoute.tsx

  App.tsx           # Route composition
  main.tsx
  index.css
```

## Adding a new feature

1. Create `src/features/<name>/` with:
   - `api/<name>.api.ts` – calls `apiClient` from `infrastructure/http`
   - `<name>.types.ts` – feature DTOs
   - `pages/` – feature pages
   - `hooks/` – feature-specific hooks
   - `index.ts` – public exports
2. Register the page in `App.tsx` and add the path to `config/routes.ts` if needed.
3. Do not import other features inside this feature; use shared/core/config/infrastructure only.

## Naming

- **API modules**: `something.api.ts`, export `somethingApi`.
- **Types**: `*.types.ts` or next to the module.
- **Pages**: `*Page.tsx` (e.g. `LoginPage`, `DashboardPage`).
- **Hooks**: `use*.ts` (e.g. `useLogin`, `useDashboardData`).
