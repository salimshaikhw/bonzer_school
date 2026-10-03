# BonzerSchool – React Client

React (Vite + TypeScript) front-end for the BonzerSchool API.

## Setup

```bash
npm install
```

## Run (development)

1. Start the BonzerSchool API (e.g. from the solution root: run the API project on HTTPS, typically port 7001 or 5001).
2. In this folder:

```bash
npm run dev
```

The app runs at **http://localhost:3000**.  
API requests go to `/api` and are proxied to the backend (see `vite.config.ts`).  
To point at a different API URL, set `VITE_API_BASE_URL` in `.env` (e.g. `VITE_API_BASE_URL=https://localhost:7001/api`).

## Build

```bash
npm run build
```

Output is in `dist/`. Preview with `npm run preview`.

## Features

- **Login** – `POST /api/Auth/login`, JWT stored in `localStorage`, sent as `Authorization: Bearer …`
- **Dashboard** – after login, calls `GET /api/Branch` and `GET /api/Exam` to show branches and exams
- **Protected routes** – unauthenticated users are redirected to `/login`
- **401 handling** – on 401 response, tokens are cleared and user is sent to login

## Project layout (layered architecture)

See **ARCHITECTURE.md** for the full layering and dependency rules.

- **config/** – env, route constants
- **core/** – auth context and types (no UI, no HTTP)
- **infrastructure/** – HTTP client (Axios, interceptors)
- **shared/** – reusable components (Button, Input, Spinner), hooks (useAsync), types
- **features/** – auth, dashboard, branches, exams (each with api/, hooks/, pages/, types)
- **layouts/** – ProtectedRoute, MainLayout
- **App.tsx** – route composition

Add new features under `src/features/<name>/` with `api/`, `hooks/`, `pages/`, and an `index.ts` that exports the public surface.
