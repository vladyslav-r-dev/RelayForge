# RelayForge

RelayForge is a webhook delivery service built with .NET 10 and PostgreSQL. It persists events and delivery attempts, sends events to active receivers, and retries transient failures across application restarts.

This is a portfolio MVP for demonstrating backend engineering: domain rules, application use cases, relational persistence, background processing, HTTP failure handling, and automated tests. It is designed for **one API/worker instance** and a small, operator-managed set of trusted receivers. It is not a production-ready public webhook platform.

## Features

- Create, activate, deactivate, and retrieve webhook endpoints.
- Create events and their deliveries in one database transaction.
- Persist `Pending`, `Delivered`, and `Failed` states, attempt counts, and retry schedules.
- Deliver JSON over HTTPS with a 10-second timeout and a three-attempt budget.
- Retry HTTP 429, HTTP 5xx, network failures, and timeouts with exponential backoff.
- Require an API key and an exact receiver URL allowlist; never follow redirects.
- Provide structured logs, error responses, development OpenAPI/Swagger UI, and a database readiness endpoint.
- Run in Docker Compose and validate changes through xUnit and GitHub Actions.

## Technology and architecture

.NET 10, C#, ASP.NET Core Minimal API, EF Core 10, Npgsql/PostgreSQL 17, `BackgroundService`, `IHttpClientFactory`, xUnit, Docker Compose, and GitHub Actions.

| Layer | Responsibility |
| --- | --- |
| `src/RelayForge.Domain` | Entities and delivery state transitions; no infrastructure dependencies. |
| `src/RelayForge.Application` | Use cases, repository/unit-of-work contracts, DTOs, and trusted receiver policy. |
| `src/RelayForge.Infrastructure` | EF Core mappings, migrations, PostgreSQL repositories, and unit of work. |
| `src/RelayForge.Api` | HTTP routes, middleware, composition root, hosted worker, and scoped delivery processor. |
| `tests/RelayForge.Domain.Tests` | Existing test project, extended with processor, security, and PostgreSQL integration tests. |

The worker creates a DI scope per batch and processes up to ten due deliveries sequentially. An error saving data aborts the batch; the next polling cycle gets a fresh DbContext. HTTP code is kept in the API host; application and domain layers do not depend on it.

## Delivery and retry semantics

1. Creating an event snapshots the active endpoints and saves the event plus its deliveries atomically. An event with no active endpoints is still saved, with zero deliveries. Activating an endpoint later does not replay old events.
2. Every five seconds, the worker selects the oldest `Pending` deliveries whose `NextAttemptAt` is null or due.
3. Before sending, it checks that the event and endpoint exist, the endpoint is active, and its URL is still allowed. A failed check makes the delivery `Failed` without consuming an HTTP attempt.
4. The worker increments and **commits the attempt count and recovery schedule before HTTP**. If that commit fails or its outcome is unknown, it does not send.
5. A 2xx response marks the delivery `Delivered`. HTTP 429/5xx, network errors, and timeouts are retryable; other responses, including redirects and other 4xx responses, are terminal failures. Response bodies are not buffered or processed by the delivery code.
6. Retries normally wait at least 5 seconds after the first failure and 10 seconds after the second. Positive `Retry-After` values on retryable responses can extend the delay, capped at 24 hours. Polling and sequential processing can add latency.
7. After three reserved attempts, a failed delivery becomes `Failed`. A restart that finds an exhausted `Pending` delivery closes it without a fourth request. Terminal states clear `NextAttemptAt`.

On shutdown during HTTP, the committed reservation remains pending. After restart it is retried if budget remains. The recovery schedule saved before the third attempt is 20 seconds; this only delays recovery/finalization, not an additional attempt.

**There is no exactly-once guarantee.** The receiver may accept a request before RelayForge crashes or fails to persist the response, leading to a duplicate on retry. Conversely, a crash after reserving an attempt but before HTTP consumes that attempt without sending. Three reservations do not guarantee three completed network requests. `Failed` means RelayForge has stopped trying; it does not prove the receiver never accepted the event. Receivers should deduplicate by `eventId`, scoped to the receiving endpoint as appropriate.

## Docker quick start

Prerequisite: Docker with the Compose plugin. A local .NET SDK is not needed for this path.

```bash
cp .env.example .env
```

Edit `.env`: replace both secret placeholders with independently generated random values (for example, `openssl rand -hex 32` twice). Use hex values to avoid connection-string escaping issues. Set `RELAYFORGE_RECEIVER_URL` to an **exact HTTPS URL you control and trust**. The `.example` URL is only a placeholder, not a hosted demo. Keep `.env` out of version control.

```bash
docker compose up --build -d
curl http://localhost:8080/health
docker compose logs -f api
```

The API listens on loopback port 8080 and PostgreSQL on loopback port 5433. Override `API_PORT` / `POSTGRES_PORT` if those ports are occupied. Compose waits for PostgreSQL readiness and enables database migrations before starting the worker. The runtime container runs as a non-root user. `/health` returns 200 when the database is reachable, otherwise 503; it does not report worker progress.

```bash
docker compose down
```

The named `postgres_data` volume survives container recreation. Do not use `down --volumes` for a database containing data you need. An existing volume retains its database password: changing `.env` does not rotate PostgreSQL credentials. Reuse the existing credentials or rotate them through PostgreSQL administration. Back up an existing database before applying migrations; no migration history was rewritten by this audit.

For additional receivers, configure `Delivery__AllowedReceiverUrls__1`, `Delivery__AllowedReceiverUrls__2`, etc. on the API service. An empty allowlist denies all receiver creation and sending. Configuration is read at startup; restart the API after changing it.

## API

All routes except `/health` require `X-Api-Key: <your-api-key>`. Keys are not forwarded to receivers. Missing/invalid keys return 401, invalid input returns 400, and missing individual resources return 404. Unknown internal errors return a generic 500 response.

| Method | Path | Result |
| --- | --- | --- |
| POST | `/endpoints` | Create a trusted receiver; 201 with its ID. |
| GET | `/endpoints/{id}` | Retrieve endpoint details. |
| PATCH | `/endpoints/{id}/activate` | Activate endpoint; 204. |
| PATCH | `/endpoints/{id}/deactivate` | Deactivate endpoint; 204. |
| POST | `/events` | Save an event and deliveries; 201 with its ID. |
| GET | `/events/{id}` | Retrieve event details. |
| GET | `/events/{id}/deliveries` | List deliveries, including status and attempt count. |
| GET | `/health` | Public database readiness check. |

The deliveries endpoint preserves the existing contract: it returns an empty array for an event with no deliveries or an unknown event ID. Status values are numeric: `Pending = 0`, `Delivered = 1`, `Failed = 2`.

Example requests (replace the key and receiver URL locally):

```bash
curl -X POST http://localhost:8080/endpoints \
  -H 'X-Api-Key: <your-api-key>' -H 'Content-Type: application/json' \
  -d '{"url":"https://receiver.example/webhook"}'

curl -X POST http://localhost:8080/events \
  -H 'X-Api-Key: <your-api-key>' -H 'Content-Type: application/json' \
  -d '{"eventType":"order.paid","data":"{\"orderId\":123}"}'
```

The outgoing body preserves the existing contract: `data` is a **string**, not an embedded JSON object. RelayForge does not require that string to contain valid JSON.

```json
{
  "eventId": "<event-guid>",
  "eventType": "order.paid",
  "data": "{\"orderId\":123}"
}
```

Endpoint deactivation prevents creation of new deliveries. Queued deliveries observed after deactivation are marked failed; an already-running request cannot be recalled. Activation does not retry terminal failures.

## Local development and tests

Install the .NET 10 SDK. Configure `ConnectionStrings__RelayForge`, `Security__ApiKey`, and indexed `Delivery__AllowedReceiverUrls__0` environment variables or their equivalents in .NET user secrets. Never put real credentials into `appsettings.json` or committed HTTP examples. Set `Database__ApplyMigrations=true` for an explicitly managed local migration run; it defaults to false outside Compose.

```bash
dotnet restore RelayForge.sln
dotnet build RelayForge.sln --configuration Release --no-restore
dotnet test RelayForge.sln --configuration Release --no-build
dotnet run --project src/RelayForge.Api --launch-profile http
```

The development launch profile uses port 5204. `/openapi/v1.json` and `/swagger` are available only in Development and also require the API key. For browser Swagger UI, use a local request-header tool to supply `X-Api-Key` on its UI, schema, and API requests. Production Compose does not expose Swagger. See `src/RelayForge.Api/RelayForge.Api.http` for request examples.

PostgreSQL integration tests are explicitly skipped unless `RELAYFORGE_TEST_POSTGRES` contains a PostgreSQL connection string for a **test server** with `CREATEDB` permission. For example, in PowerShell:

```powershell
$env:RELAYFORGE_TEST_POSTGRES = 'Host=localhost;Port=5433;Database=postgres;Username=relayforge;Password=<local-password>'
dotnet test RelayForge.sln --configuration Release
```

The integration test creates a uniquely named database, applies the real migrations, checks due-delivery selection and restart recovery, and drops only that database afterward. CI supplies a disposable PostgreSQL service and runs these checks too. Unit tests use controlled HTTP handlers to cover success, retryable/terminal responses, timeout, cancellation, and persistence failures. See [the audit report](docs/audit.md) for checks actually performed.

## Security and scope limits

- The allowlist compares the entire URL with ordinal equality, including path and query. HTTPS is mandatory; userinfo and fragments are rejected. Wildcards, prefix matching, redirects, cookies, and system HTTP proxies are not enabled.
- This is an operator trust boundary, not a general-purpose SSRF firewall. Only allow receivers whose DNS, TLS certificates, infrastructure, and handling of payloads you trust. An administrator can still allow an internal address or a host whose DNS later changes. Use network egress restrictions for a public deployment; never let API clients edit the allowlist.
- Outbound TLS certificate validation remains enabled. HTTP URLs and untrusted/self-signed certificates are not silently accepted.
- Default HTTP-client URL logging is disabled; processor logs use IDs/status codes and omit payloads, keys, receiver URLs, and raw network exception messages. Do not enable sensitive EF logging or request-body/header logging in a deployed environment.
- The shared API key grants access to all data. There is no per-tenant authorization, key lifecycle management, receiver authentication/signatures, or rate limiting. Use TLS termination and access restrictions before exposing the API outside localhost; Compose itself provides only local HTTP.
- Run exactly one worker. There are no distributed claims/leases, so multiple instances can send the same delivery concurrently. Processing is sequential, with no ordering guarantee across receivers or retries.
- There is no event-ingestion idempotency key, replay/dead-letter UI, retention policy, pagination, or operational alerting. Data and queue size can grow without bound. A failing database can cause repeated batch aborts until it recovers.
- Automatic migrations in Compose simplify the demo; production deployments need a separate migration/backup policy and least-privilege database accounts. The Compose database account owns the database.

## Demo and screenshots

No public demo is deployed. Suggested future assets: an authenticated Swagger screenshot, an event's delivery status before/after a retry, and a short receiver demo. Add real captures under `docs/` when available; no fabricated demo URL or screenshot is included.
