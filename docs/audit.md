# RelayForge technical audit

Date: 2026-10-19. Scope: the existing .NET solution, tests, migrations, configuration, and local container workflow. No commit, push, paid deployment, or modification of the pre-existing PostgreSQL container was performed.

## Findings and fixes

| Priority | Finding | Resolution |
| --- | --- | --- |
| Critical | A crash after `RegisterAttempt()` could leave a pending delivery that exceeded the attempt budget after restart. | Enforce a domain maximum of three reservations; save the count and recovery schedule before HTTP; finalize exhausted pending deliveries without sending again. |
| Critical | Arbitrary endpoint URLs and automatic redirects allowed outbound requests outside an operator-controlled destination set. | Exact HTTPS URL allowlist, checked during endpoint creation and immediately before sending; redirects disabled. Empty allowlist denies sending. |
| High | Missing event/endpoint records were skipped indefinitely. | Mark these deliveries failed without consuming a network attempt. Also fail deliveries observed with inactive or no-longer-allowed receivers. |
| High | HTTP and persistence handling were intertwined. | Separate a scoped processor from the polling worker; keep outcome persistence outside HTTP exception handling; abort on database errors and discard the batch scope. |
| High | API key validation occurred only on requests; comparison was ordinary string equality and protection covered only selected path prefixes. | Require configuration at startup; compare fixed-size key hashes in constant time; reject multiple header values; protect all routes except readiness. |
| High | Compose contained a literal development database password and did not run the API. | Require environment configuration; add a multi-stage non-root API image, startup migrations, PostgreSQL readiness dependency, persistent volume, and loopback bindings. |
| Medium | Missing endpoints produced a generic exception and HTTP 500. | Use the existing not-found exception mapping to return 404. |
| Medium | Raw outbound exception logging and default HttpClient logging could expose receiver details. | Log IDs, status, and error types; disable named-client loggers and avoid raw network exception messages. Suppress routine SQL command logs. |
| Medium | Mutable entity properties bypassed domain transitions; nullable DTO properties raised compiler warnings. | Make delivery status, endpoint URL, and event payload/type setters private; initialize DTO strings and enforce database field lengths in constructors. |
| Medium | Some classes had no namespace; interface/response/enum paths were inconsistent. | Move `IUnitOfWork` to Interfaces, responses to Responses, and the enum to Domain/Enums; normalize handwritten namespaces and formatting. |
| Medium | Floating API package versions and differing transitive EF versions reduced reproducibility. | Pin already-resolved API versions and explicitly align EF Core Relational with EF Core Design at 10.0.13. No new testing framework or mocking package was added. |
| Medium | Only eight domain tests existed; no CI, Dockerfile, or project README existed. | Extend the existing test project, add PostgreSQL-backed CI, and document the real behavior and limits. |

## Design decisions

- Preserve the existing four production projects and Clean Architecture dependencies. `DeliveryProcessor` is a concrete scoped API-host service, introduced to make delivery behavior independently testable; it does not add another abstraction layer or queue framework.
- Keep the existing REST paths, DTO field names, numeric statuses, and outgoing string-valued `data` field. Rejecting untrusted/non-HTTPS endpoint URLs is an intentional security restriction. Development Swagger now also requires the API key.
- Preserve all migration files and the EF snapshot. No schema migration was needed for these changes. The current model was checked against the snapshot using the real provider.
- Choose bounded durable reservations, rather than claiming exactly-once delivery. A reservation committed before HTTP can be consumed by a crash. A successful remote request whose result was not committed can be sent again. After the third reservation, recovery finalizes the delivery as failed even if the last remote outcome is unknown.
- Keep one sequential worker and batches of ten. No distributed locking, leases, parallel sending, new status values, or automatic replay was added.
- Retry 429/5xx/network errors/timeouts. Other responses terminate delivery. Respect `Retry-After` when it extends exponential delay, with a documented 24-hour cap.
- Fail closed on receiver policy and missing configuration. The allowlist is an operator trust decision, not a universal DNS/IP safety validator. HTTPS validation remains enabled.
- Enable automatic migrations explicitly in Compose only. Local/direct execution defaults to migrations disabled. Existing-volume password changes require PostgreSQL administration, not just editing environment variables.

## Changed files

Behavior and configuration:

- `src/RelayForge.Api/BackgroundServices/DeliveryWorker.cs`
- `src/RelayForge.Api/BackgroundServices/DeliveryProcessor.cs` (new)
- `src/RelayForge.Api/Middleware/ApiKeyMiddleware.cs`
- `src/RelayForge.Api/Exceptions/GlobalExceptionHandler.cs`
- `src/RelayForge.Api/Program.cs`
- `src/RelayForge.Api/RelayForge.Api.csproj`
- `src/RelayForge.Api/RelayForge.Api.http`
- `src/RelayForge.Api/appsettings.json` and `appsettings.Development.json`
- `src/RelayForge.Application/Security/TrustedReceivers.cs` (new)
- `src/RelayForge.Application/UseCases/CreateEndpointUseCase.cs`, `GetEndpointUseCase.cs`, and `CreateWebhookEventUseCase.cs`
- `src/RelayForge.Application/DTOs/CreateEndpointRequest.cs` and `CreateWebhookEventRequest.cs`
- `src/RelayForge.Domain/Entities/Delivery.cs`, `Endpoint.cs`, and `WebhookEvent.cs`
- `src/RelayForge.Infrastructure/RelayForge.Infrastructure.csproj`

Organization and style:

- `Application/UseCases/IUnitOfWork.cs` moved to `Application/Interfaces/IUnitOfWork.cs` with a namespace.
- `Application/Response/{Delivery,Endpoint,WebhookEvent}Response.cs` moved to `Application/Responses/` with a namespace; endpoint response URL initialized.
- `Domain/Entities/Enums/DeliveryStatus.cs` moved to `Domain/Enums/DeliveryStatus.cs`; enum values unchanged.
- Other application use cases and repository interfaces updated for namespaces/formatting.
- Infrastructure repositories, `UnitOfWork.cs`, `RelayForgeDbContext.cs`, and all three mapping configurations normalized; persistence mappings unchanged.
- `.editorconfig` and `.gitattributes` added.

Tests and delivery assets:

- Existing `DeliveryTests.cs` expanded; `EndpointTests.cs` and `WebhookEventTests.cs` formatted.
- `DeliveryProcessorTests.cs`, `SecurityTests.cs`, and `PostgresTests.cs` added to the existing test project.
- Test project references the API so its real processor and transitive infrastructure can be exercised without another test project or mocking library.
- `Dockerfile`, `.dockerignore`, `.env.example`, `.github/workflows/ci.yml`, `README.md`, and this report added.
- `compose.yaml` and `.gitignore` updated.

## Verification performed

| Check | Observed result |
| --- | --- |
| Initial `dotnet test RelayForge.sln` | 8 passed. |
| Initial full build | Succeeded with 4 nullable-property warnings. |
| Final `dotnet build RelayForge.sln -c Release` | Succeeded with 0 warnings and 0 errors. |
| Final `dotnet test RelayForge.sln -c Release --no-build` with test PostgreSQL configured | 28 passed, 0 failed, 0 skipped. TRX generated locally under the ignored test results directory. |
| `dotnet format RelayForge.sln --verify-no-changes --no-restore` | Passed. |
| `git diff --check` | No whitespace errors. |
| `dotnet list RelayForge.sln package --vulnerable --include-transitive` | No known vulnerable packages reported by the configured NuGet sources at audit time. This is not a guarantee against undisclosed vulnerabilities. |
| `docker compose -p relayforge-audit config --quiet` | Passed with disposable environment values. |
| Compose build and startup | API image built, PostgreSQL became healthy, migrations applied, API started. |
| Final Docker image rebuild | Passed. |
| Final image startup against the audit volume | Passed; re-running migrations against an already migrated database succeeded. |
| Startup without an API key | Failed immediately with the intended configuration error. |
| HTTP smoke against container | Readiness 200, missing key 401, disallowed receiver 400, missing endpoint 404, event creation/read and empty delivery list, endpoint creation/activation/deactivation, invalid event 400. |

The PostgreSQL test creates a random test-only database and exercises the real migration chain, model/snapshot agreement, event creation with zero active endpoints, event/delivery persistence, due-versus-future/terminal selection, recovery after reopening the context, successful delivery persistence, visibility of the committed reservation before sending, and atomic rollback on a foreign-key failure. It removes only its own database afterward.

The isolated audit containers and their test volume were removed after validation. The pre-existing PostgreSQL container and its data were left intact. Built local audit images remain available in Docker's image cache.

HTTP unit tests use controlled handlers for 2xx/3xx/4xx/429/5xx responses, network failures, timeout, cancellation, reservation/outcome save failures, `Retry-After`, missing dependencies, inactive receivers, allowlist removal, and exhausted attempt recovery. The domain tests cover attempt limits, exponential delays, terminal-state guards, and scheduling cleanup. Security tests cover exact matching, denied creation before saving, missing/multiple API key headers, protection of future routes, and public readiness.

## Verification limits and remaining risks

- GitHub Actions is configured but was not executed on GitHub: no commit or push was made. Its restore/build/test commands were executed locally, and PostgreSQL integration tests actually ran.
- No live external HTTPS receiver or browser Swagger session was used. HTTP response/error behavior was exercised through controlled handlers; the running container's inbound API was smoke-tested.
- Restart recovery was tested through persisted reservations and fresh contexts, not OS-level forced process termination at every instruction boundary. Database outcome-save errors were injected at the unit-of-work boundary; the integration test additionally exercised a real PostgreSQL transaction failure.
- No load test, multi-instance test, production deployment, penetration test, or DNS rebinding simulation was performed. One worker is an explicit requirement.
- Allowlisted hosts must remain trusted. DNS changes, a mistaken internal destination, or a compromised receiver are outside this simple policy; production needs egress restrictions and deployment-specific controls.
- A shared API key provides no tenant isolation. No rate limits, webhook signatures, replay UI, retention, pagination, or operational alerts were added. The database readiness route cannot establish that deliveries are progressing.
- A receiver can accept a request whose final database state is unknown. Duplicates and exhausted attempts without confirmed delivery remain possible; receiver-side deduplication is required.
- The original development password was removed from Compose, but Git history was not rewritten. Rotate any credential previously reused from committed development configuration. User secrets and existing local environment files were not copied into the repository.
- Existing PostgreSQL data was not reset or migrated during the audit. Container checks used a separate Compose project and disposable test credentials. Publication should present the project as a scoped portfolio MVP, not as production-ready infrastructure.
