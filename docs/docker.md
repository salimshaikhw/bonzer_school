# Running with Docker

## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage build of the API: .NET 8 SDK build, `aspnet:8.0` runtime, non-root user, port 8080, health check on `/health`. Installs fonts needed by QuestPDF (PDF) and NPOI (Excel) on Linux. |
| `docker-compose.yml` | PostgreSQL 18 + API. Optional profiles: `tools` (pgAdmin), `datadog` (Datadog Agent), `sonar` (SonarQube, see [sonarqube.md](sonarqube.md)). |
| `.env.example` | All settings. Copy to `.env` (git-ignored) and set the secrets. |
| `.dockerignore` | Keeps `bin/`, `obj/`, `.git`, secrets and docs out of the build context. |

## First run

```bash
cp .env.example .env              # set POSTGRES_PASSWORD, JWT_KEY, PLATFORM_ADMIN_PASSWORD
docker compose up -d --build
```

* API + Swagger: http://localhost:5102/swagger (`API_PORT`)
* PostgreSQL from the host: `localhost:5433` (`POSTGRES_PORT`; 5433 so it does not clash with a local install)
* With `ASPNETCORE_ENVIRONMENT=Development` and `SEED_DEMO_DATA=true`, the demo tenant is created:
  `groupadmin@bonzer.local / Admin@123`, `{role}@bonzer.local / Demo@123`, and the Super Admin from `.env`.

On startup the API creates and migrates `bonzer_catalog` and every `bonzer_school_<tenant>` database by itself;
there is no separate migration step.

## Everyday commands

```bash
docker compose logs -f api                 # follow API logs
docker compose up -d --build api           # rebuild after code changes
docker compose --profile tools up -d       # + pgAdmin on http://localhost:5050 (server: db, port 5432)
docker compose down                        # stop (data is kept)
docker compose down -v                     # stop and DELETE all data (databases, logs, keys)
docker build -t bonzer-school-api --build-arg VERSION=1.2.0 .   # image only
```

## Volumes

| Volume | Mounted at | Contents |
|---|---|---|
| `postgres-data` | `/var/lib/postgresql` | All databases |
| `api-logs` | `/app/Logs` | Rolling JSON log files (`bonzer-api-YYYYMMDD.json`) |
| `api-keys` | `/app/keys` | ASP.NET Core data-protection keys. Without them, password-reset and email-confirmation links break on every restart. |

## Configuration

Everything is set through environment variables in `docker-compose.yml`, filled from `.env`. ASP.NET Core
maps `Section__Key` to `Section:Key`, so any `appsettings.json` value can be overridden the same way, e.g.
`Serilog__MinimumLevel__Default=Debug`.

Secrets (`POSTGRES_PASSWORD`, `JWT_KEY`, `PLATFORM_ADMIN_PASSWORD`, `DD_API_KEY`, OAuth keys) only go in `.env`
or the hosting platform's secret store, never in `appsettings*.json` or the image.

### Production

* `ASPNETCORE_ENVIRONMENT=Production`, `SEED_DEMO_DATA=false`.
* Set `CORS_ORIGIN` to the SPA's URL.
* OAuth 2.0 server: set `OAUTH_SIGNING_CERTIFICATE` (base64 PFX), `OAUTH_SIGNING_CERTIFICATE_PASSWORD`,
  `OAUTH_ENCRYPTION_KEY` (32 random bytes, base64); otherwise the token endpoint is off outside Development.
* Put the container behind a TLS-terminating proxy (Azure App Service, nginx, Traefik) and set
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so client IPs and HTTPS are seen correctly.
* Use a managed PostgreSQL instead of the `db` service for real data and backups.

## Datadog

Either:

* **Agent** — set `DD_API_KEY`, `LOG_JSON_CONSOLE=true`, then `docker compose --profile datadog up -d`.
  The Agent collects only containers labelled `com.datadoghq.ad.logs` (the API) and tags them with
  `service`, `env` (`DD_ENV`) and `version` (`APP_VERSION`).
* **Direct** — set `DATADOG_DIRECT=true` and `DD_API_KEY`; the API sends logs over HTTPS itself (no Agent).

See [logging.md](logging.md) for the log format and suggested facets.
