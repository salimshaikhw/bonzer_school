# Bonzer School ERP — solution structure

Clean architecture, four projects. Dependencies point inwards only:

```
API  ──►  Infrastructure  ──►  Application  ──►  Domain
 └──────────────────────────►  Application
```

| Project | What belongs here | Must not contain |
|---|---|---|
| **Domain** | Entities, enums, mapping annotations. No framework code beyond EF attributes. | Services, DTOs, HTTP |
| **Application** | Contracts (`I…Service`, `I…Repository`), DTOs / view models, validators, exception types, constants (roles). | EF Core queries, HTTP, Serilog |
| **Infrastructure** | Implementations: EF Core contexts, migrations, repositories, services, tenancy, security (JWT, SSO, OAuth server), email. | Controllers, middleware |
| **API** | ASP.NET Core host: controllers, middleware, global exception handling, logging setup, `Program.cs`. | Business rules, SQL |

## Feature folders

The same 11 feature folders are used in every layer, and namespaces follow them:

`Academics, Administration, Attendances, Auth, Certificates, Communication, Exams, Fees, Organization, Personnel, Students`

(plus `Tenancy` for platform-level tenant management). A new feature gets one folder in each of
`Domain/Entities`, `Application/Abstractions/{Repositories,Services}`, `Infrastructure/{Services,Persistence/Repositories}`
and `API/Controllers`.

## Folder map

```
API/
  Controllers/{Feature}/      HTTP endpoints, thin: validate input, call a service, return a result
  ExceptionHandling/          GlobalExceptionHandler + ExceptionClassifier (exception -> status, message, log level)
  Logging/                    Serilog setup, Datadog options and enricher, request log context
  Middlewares/                CorrelationId, TenantResolution, RequestLogContext
  Extensions/                 DI registration of services and repositories
  Models/, Services/          API-only helpers (upload forms, PDF receipts, Excel export)
  appsettings*.json           Non-secret configuration. Secrets live in User Secrets / Azure configuration.

Application/
  Abstractions/               Interfaces: Repositories/{Feature}, Services/{Feature}, Messaging, Tenancy
  Common/                     Constants (Roles, RoleGroups), ExceptionCodes, helpers (cache, fire-and-forget)
  Exceptions/                 HCBException, BaseServiceException, RepositoryException, ExceptionSummary (error body)
  Validation/                 FluentValidation validators
  ViewModels/                 Request/response DTOs (JSON field names are a contract with the client)

Infrastructure/
  Persistence/
    Catalog/                  CatalogDbContext: shared DB (organizations = school groups = tenants, Identity logins, refresh tokens, OpenIddict)
    Postgres/                 BonzerSchoolDbContext: one database per tenant (school group)
    Migrations/Catalog|Tenant EF Core migrations (Legacy/ is excluded from the build)
    Conventions/, Interceptors/  snake_case naming, annotation conventions, audit stamping
    Repositories/{Feature}/, UnitOfWork/, Seed/
  Services/{Feature}/         Business logic
  Security/                   Token (JWT + refresh), External (Google/Microsoft ID tokens), OAuth (OpenIddict options)
  Tenancy/                    Tenant context, store, per-tenant connection strings
  Messaging/                  Email
  Mapping/                    AutoMapper profiles
  Extensions/                 Authentication and multi-tenancy registration

Domain/
  Entities/{Feature}/         Entities with data annotations (no IEntityTypeConfiguration classes)
  Common/, DataAnnotations/, Enums/

docs/                         Architecture, logging, Docker, API reference (docs/api)
Dockerfile, docker-compose.yml, .env.example   Container build and local stack (see docs/docker.md)
tools/sonar/                  SonarQube scan scripts and analysis settings (see docs/sonarqube.md)
Scripts/                      Legacy database-first SQL, superseded by migrations; kept for reference
```

## Request pipeline (API/Program.cs)

```
CorrelationIdMiddleware        assigns X-Correlation-ID, adds it to every log line
Serilog request logging        one summary line per request
UseExceptionHandler            GlobalExceptionHandler: logs once, returns ExceptionSummary
Swagger, HTTPS redirect, CORS
Authentication                 ApiBearer: own JWT or OAuth 2.0 (OpenIddict) token
TenantResolutionMiddleware     picks the tenant database from the tenant_id claim
RequestLogContextMiddleware    adds TenantId / Tenant / UserId to every log line
Authorization -> Controllers
```

See [logging.md](logging.md) for logging, error handling and Datadog, [docker.md](docker.md) for running with Docker, and [sonarqube.md](sonarqube.md) for code quality.
