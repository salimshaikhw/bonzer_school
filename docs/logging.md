# Logging, error handling and Datadog

## What gets logged

Every log event is structured JSON with these attributes:

| Attribute | Example | Source |
|---|---|---|
| `timestamp`, `message`, `message_template`, `logger` | | Serilog |
| `status` | `info`, `warning`, `error` | DatadogEnricher (Datadog's level attribute) |
| `service`, `env`, `version`, `host` | `bonzer-school-api`, `production`, `1.0.0+316bccf`, … | LoggingExtensions |
| `CorrelationId` | `630bd2a9fdcea11a…` | CorrelationIdMiddleware (also the `X-Correlation-ID` response header) |
| `TenantId`, `Tenant`, `UserId` | school group and user of the request | RequestLogContextMiddleware |
| `trace_id`, `span_id` | W3C trace context | ASP.NET Core activity |
| `ErrorCode`, `ErrorReference` | `UniqueViolation`, `2e05591c-…` | GlobalExceptionHandler |
| `error.kind`, `error.message`, `error.stack` | exception type, message, stack | DatadogEnricher (feeds Datadog Error Tracking) |
| template properties | `StudentId`, `Id`, `EntityType`, … | the log call itself |

Per request there is:

* one summary line — `HTTP GET /api/FeeHead responded 200 in 12.3 ms` (Information; 4xx/5xx and slow requests are Warning; `/health` and Swagger are not logged);
* for a failed request, one entry from `GlobalExceptionHandler` — `DELETE /api/FeeHead/… failed with 404 ItemNotFound: FeeHead not found … (reference …)`. Client errors (4xx) are Warning without a stack trace; server errors (5xx) are Error with the exception.

The `ReferenceId` and `CorrelationId` in the error response are the `ErrorReference` and `CorrelationId` attributes of the log entries, so a user's screenshot is enough to find the logs.

## Rules for writing logs

* Use message templates, never string interpolation: `_logger.LogInformation("Promoted student {StudentId} to class {ClassId}", studentId, classId)`. Datadog can then filter and group by `StudentId`.
* Use PascalCase property names that say what the value is (`StudentId`, not `Id` when there is more than one id).
* **Log or throw, not both.** Throw a `BaseServiceException` with the right `ExceptionCodes` (`Validation`, `ItemNotFound`, `UnAuthorized`, `Restriction`, `Operation`); the global handler logs it once with the request context. Only log in a `catch` that handles the error and carries on.
* Levels: `Debug` for read tracing ("Fetching …"), `Information` for business events (created, approved, issued), `Warning` for something unusual that was handled, `Error` for failures that need attention.
* Never log passwords, tokens, secrets, or personal data in bulk (email addresses, phone numbers). Log ids and counts instead.

## Error responses

All exceptions end in `API/ExceptionHandling/GlobalExceptionHandler`, which uses `ExceptionClassifier` to choose the status:

| Cause | Status | ErrorCode |
|---|---|---|
| `ExceptionCodes.Validation`, FluentValidation `ValidationException` | 400 | `Validation` |
| `ExceptionCodes.ItemNotFound` | 404 | `ItemNotFound` |
| `ExceptionCodes.UnAuthorized` / `Restriction` | 403 | `UnAuthorized` / `Restriction` |
| `ExceptionCodes.Operation` | 409 | `Operation` |
| PostgreSQL unique violation (23505) | 409 | `UniqueViolation` |
| PostgreSQL foreign key violation (23503) | 409 on delete, 400 on insert | `ForeignKeyViolation` |
| PostgreSQL not-null / length / check violation | 400 | `Validation` |
| `RAISE EXCEPTION` in our SQL functions (TC workflow) | 400 | `BusinessRule` |
| Concurrency conflict, deadlock, serialization failure | 409 | `Concurrency` |
| Database unreachable / too many connections | 503 | `DatabaseUnavailable` |
| Client disconnected | 499 (no body) | `RequestCancelled` |
| Anything else | 500 | `Unexpected` |

Wrapped exceptions are unwrapped: a "not found" inside a repository's `Database` exception is still a 404.

The body is `ExceptionSummary`: `UserMessage`, `ValidationMessage` (4xx only), `SupportMessages`, `ReferenceId`, `TimeStamp` (UTC), `StatusCode`, `ErrorCode`, `CorrelationId`, and `Errors` (field errors, validation only). Internal details (exception chain) are only included in Development.

## Configuration

`appsettings.json`:

```jsonc
"Serilog": { "MinimumLevel": { "Default": "Information", "Override": { "Microsoft": "Warning", ... } } },
"AppLogging": {
  "ServiceName": "bonzer-school-api",
  "JsonConsole": null,              // null = JSON everywhere except Development
  "FilePath": "Logs/bonzer-api-.json",
  "RetainedFileCount": 14,
  "SlowRequestThresholdMs": 3000
},
"Datadog": { "Enabled": false, "Site": "datadoghq.com", "MinimumLevel": "Information", "Tags": [] }
```

Levels can be changed per category without a code change, e.g. `Serilog__MinimumLevel__Override__Infrastructure.Services.Fees=Debug`.

`Microsoft.EntityFrameworkCore.Database.Command`, `.Update` and `.Query` are set to `Fatal`: EF would otherwise log its own error for every failed query or save, duplicating the global handler's entry (and turning an expected 409 into an error). Lower them temporarily to see the failing SQL when debugging.

## Sending logs to Datadog

Pick **one** of these:

### Option A — direct HTTP (simplest, works on Azure App Service)

Set in Azure configuration (or User Secrets locally):

```
Datadog__Enabled = true
Datadog__ApiKey  = <Datadog API key>
Datadog__Site    = datadoghq.com        # or datadoghq.eu, us5.datadoghq.com, ...
```

Logs are batched and sent by `Serilog.Sinks.Datadog.Logs` with `service`, `env` and `version` tags.

### Option B — Datadog Agent

Leave `Datadog:Enabled` false. The console (stdout) and `Logs/bonzer-api-*.json` are already JSON in Datadog's format; point the Agent (or the Azure App Service Datadog extension) at either one.

### Traces and log correlation (optional, recommended)

Install the Datadog .NET tracer (Azure: "Datadog APM" site extension; containers: `dd-trace-dotnet`) and set:

```
DD_SERVICE=bonzer-school-api
DD_ENV=production
DD_VERSION=<release>
DD_LOGS_INJECTION=true
```

The tracer then adds `dd.trace_id` / `dd.span_id` to every log event, linking logs to APM traces.

### Suggested Datadog facets and monitors

* Facets: `@Tenant`, `@UserId`, `@CorrelationId`, `@ErrorCode`, `@ErrorReference`, `@StatusCode`, `@RequestPath`, `@Elapsed`.
* Monitors: `status:error service:bonzer-school-api` count > N in 5 min; `@ErrorCode:DatabaseUnavailable`; p95 of `@Elapsed` on the request summary.
