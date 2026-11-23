# Plan: Security Hardening

**Branch**: `021-security-hardening` (specification); five implementation PRs | **Date**: 2026-11-23 | **Spec**: [spec.md](spec.md)

## Summary

Close the open findings F2–F12 of docs/security-review.md in five small PRs, each with its own branch, the security tests named in the review, and a green CI run:

| Order | PR / branch | Findings | Tasks |
|---|---|---|---|
| 1 | `021-ci-and-dependencies` | F10, F11, F12 | T017–T019 |
| 2 | `021-auth-hardening` | F5, F6 | T001–T003 |
| 3 | `021-authz-tests` | F2, F3 | T004–T010 |
| 4 | `021-http-hardening` | F4, F7 | T011–T013 |
| 5 | `021-error-logging` | F8, F9 | T014–T016 |

The CI PR goes first, so the other four are checked automatically. PRs 2–5 do not depend on each other; they may be merged in any order after PR 1. The order above is the recommended one: authentication first (highest user impact), then tests, which may find real bugs.

## Technical Context

**Language/Version**: C# / .NET 10 (server), TypeScript / React 19 (client: logout call only), GitHub Actions YAML (CI)

**Primary Dependencies**: none added. Rate limiting (`Microsoft.AspNetCore.RateLimiting`), host filtering, `ILogger` and the authorization result handler are part of the ASP.NET Core shared framework. One package was removed (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`, T019: `PasswordHasher<User>` comes from the shared framework). The CI workflow uses `actions/checkout`, `actions/setup-dotnet` and `actions/setup-node`, pinned to major version tags. These are CI tools, not application dependencies.

**Storage**: one migration in T016 (maximum lengths for `Users.FirstName`, `Users.LastName`, `Companies.Name`). The task explicitly requires it (Constitution VIII).

**Testing**:
- xUnit integration tests through `ApiFactory` (in-memory SQLite, real token validation, routing and company filters), as required for role and company rules (Constitution IV).
- Log assertions use a test `ILoggerProvider` registered in the factory; no new package.
- Rate-limit tests use a factory with a low limit; the default `ApiFactory` raises the limits so the existing auth tests are unaffected.
- Forged tokens are built in the test with `JsonWebTokenHandler` and the test key or another key; expiry uses the test clock.
- Client: Vitest for the logout call.

**Project Type**: Web application

**Constraints**: no behaviour change apart from FR-021-4, FR-021-6, FR-021-9, FR-021-10 and FR-021-12; no secrets in CI (the workflow uses a dummy `Jwt__Key`); every existing test keeps passing (SC-021-9).

## Design Decisions

- **R1 CI** (T017): one workflow, `ci.yml`, written by the developer by hand (spec Q2; the agent does not edit `.github/`), on every `pull_request` and on `push` to `main`, with `permissions: contents: read`. Jobs: `backend` (`dotnet build` and `dotnet test` in Release, which restore implicitly; `dotnet list package --vulnerable --include-transitive`, failing when any package is reported) and `frontend` in `client/` (`npm ci`, `lint`, `build`, `test`, `npm audit --omit=dev --audit-level=moderate`). Actions: `checkout@v6`, `setup-dotnet@v5` (10.0.x), `setup-node@v6` (Node 22, npm cache). `Jwt__Key` is a dummy value in the workflow env; the tests already use SQLite, so no database service is needed.
- **R3 Logout** (T003): `POST /api/auth/logout`, `[Authorize]`, returns 204. `AuthService.LogoutAsync` loads the current user and calls the existing `User.InvalidateTokens()`. The existing `OnTokenValidated` check then rejects every older token. The client calls it, then removes the token whatever the result.
- **R4 Anonymous matrix** (T004, T007): one xUnit theory with `MemberData` listing every protected method and route from the review's section 4, so a new endpoint without a row is visible in review.
- **R5 Rate limiting** (T011): `AddRateLimiter` with a fixed-window policy partitioned by `RemoteIpAddress`, applied with `[EnableRateLimiting("auth")]` on `Register` and `Login` only. `RejectionStatusCode = 429`, with a `ProblemDetails` body written in `OnRejected`. Limits come from configuration (`RateLimiting:Auth:PermitLimit`, `WindowSeconds`), defaulting to 10 requests per 60 seconds per client address for login and register together.
- **R6 Headers** (T012): a small inline middleware in `Program.cs` that sets the four headers on every response before the next middleware runs, so 401, 403 and 404 responses carry them too. No package such as NWebsec.
- **R7 Hosts** (T013): `appsettings.json` keeps `"AllowedHosts": "*"` (used locally, in tests and in CI). Every environment other than Development must set the list through the environment variable `AllowedHosts` (semicolon-separated host names, e.g. `estimates.example.lt`); the app refuses to start outside Development while it is `*`. `appsettings.Development.json` is not used for this, because it is in `.gitignore`.
- **R8 Logging** (T014, T015): `ILogger<T>` in `AuthService`, `TeamService`, `EstimateService` and the delete methods; message templates with ids only. 403s are logged by an `IAuthorizationMiddlewareResultHandler` that wraps the default one; cross-company 404s by `DomainExceptionHandler` for `NotFoundException`. An email is logged as a SHA-256 hash prefix, never in clear text.
- **R9 Length rules** (T016): `PersonRules` (names 100, email 256, password 128, company name 200) next to `PasswordRules`, applied with `[StringLength]` on the request records (X.10). Search length: `[StringLength(200)]` on the `search` query parameter. The migration narrows the three `longtext` columns after a check that no existing value is longer.

## Security Risks (Constitution IV)

- **Rate limiting by IP behind a proxy**: if the API runs behind a reverse proxy, every client has the proxy's address, and one attacker could block logins for everyone. Mitigation: the deployment must configure forwarded headers before T011 goes live; documented in docs/architecture.md.
- **Logout invalidates all devices**: accepted (spec Edge Cases).
- **Logging personal data**: only ids and email hashes are logged (GDPR); no request bodies.
- **CI secrets**: none needed; a dummy key only. Actions pinned to major version tags from their official publishers limit the supply-chain risk.
- **Remaining 423 difference**: the fifth failed login gives 423 only for an existing account, so an attacker can still find accounts that way; accepted (spec Q1), reduced by rate limiting. T001 only removes the timing difference.
- **Remaining login risk**: per-account lockout lets anyone lock out a known email for 15 minutes; accepted (spec Q1), reduced by rate limiting.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| I / II.5 tests | Every task names its security test; role and company rules are integration tests | Pass |
| II.1 / III business logic | Login, logout and logging live in services; controllers stay one call (X.2) | Pass |
| II.6 simplicity | Built-in rate limiter, inline header middleware, logout reuses `TokenVersion` | Pass |
| III dependencies | No application dependency added; one removed (T019) | Pass |
| III no features beyond spec | Paging and per-device sessions excluded | Pass |
| III passwords | Never logged; logging tests assert it | Pass |
| IV security | Risks documented above before implementation | Pass |
| VI | Spec/plan/tasks committed separately from code; README.md has a "Continuous Integration" section (T017) | Pass |
| VII.2 | No clarification marker remains (Q1, Q2 answered) | Pass |
| VIII paths | The developer writes `.github/workflows/ci.yml` by hand (spec Q2). The agent's README.md CI section is outside the allowed folders; the student approved it as an exception (Article VI requires README updates; Complexity Tracking) | Pass with approved exception |
| VIII migrations | One migration, explicitly required by T016 | Pass |
| X.9 / X.10 | No CancellationToken added; length rules declared once on the request records | Pass |

**Post-design re-check**: Pass, with one approved exception (Complexity Tracking).

## Project Structure

```text
.github/workflows/ci.yml                         # T017, written by the developer by hand (spec Q2)
README.md                                        # T017: CI section
client/package.json, client/package-lock.json    # T018
client/src/utils/auth.ts, client/src/api/…       # T003: call POST /api/auth/logout
server/SmartEstimatePlatform.Api/
├── SmartEstimatePlatform.Api.csproj             # T019
├── Program.cs                                   # T011 rate limiter, T012 headers, T013 hosts, T015 result handler
├── appsettings.json                             # T011 limits, T013 hosts
├── Controllers/AuthController.cs                # T003 logout, T011 [EnableRateLimiting]
├── Controllers/DomainExceptionHandler.cs        # T015 404 logging
├── Services/AuthService.cs, IAuthService.cs     # T001 dummy hash, T003 LogoutAsync, T014 logging
├── Services/TeamService.cs, EstimateService.cs, MaterialService.cs, WorkService.cs,
│   CatalogSectionService.cs, ProjectService.cs  # T014 logging
├── Services/PersonRules.cs                      # T016
├── Models/DTOs/Auth/*Request.cs, Team/*Request.cs  # T016
└── Migrations/                                  # T016
tests/server/
├── AuthServiceTests/, ErrorResponseTests/       # T001
├── AuthServiceTests/LogoutEndpointTests.cs      # T003
├── CrossCuttingTests/AnonymousAccessTests.cs, TokenValidationTests.cs, JwtOptionsStartupTests.cs,
│   RateLimitTests.cs, SecurityHeadersTests.cs, HostFilteringTests.cs, SecurityLoggingTests.cs  # T004, T009–T015
├── CatalogSectionTests/, EstimateSectionTests/  # T005, T006
├── MaterialServiceTests/MaterialSearchTests.cs  # T008, T016
├── RequestValidationTests/                      # T016
└── TestSupport/ApiFactory.cs                    # raised rate limits, test logger provider
tests/client/LogoutTests/Logout.test.tsx         # T003
docs/security-review.md, docs/architecture.md    # status updates, CSP and proxy notes
```

## Implementation Order

1. **PR 1 `021-ci-and-dependencies`**: T017 (developer writes ci.yml) → T018 → T019.
2. **PR 2 `021-auth-hardening`**: T001 → T002 → T003.
3. **PR 3 `021-authz-tests`**: T004–T010 (tests only; any failing test gets its fix in the same PR).
4. **PR 4 `021-http-hardening`**: T011 → T012 → T013.
5. **PR 5 `021-error-logging`**: T014 → T015 → T016.

After each PR: mark its findings in docs/security-review.md section 12 as fixed, with the test names.

## Complexity Tracking

| Violation | Why needed | Simpler alternative rejected because |
|---|---|---|
| README.md (repository root) edited by the agent, outside the folders allowed by Article VIII | Article VI requires README.md to be updated when setup or run instructions change; T017 adds CI, which changes how changes are checked | The student approved this one exception on 2026-11-23 (Article VII.3). The other option, the developer writing the section by hand as for ci.yml, was rejected as unnecessary for documentation text |

The CI workflow itself is not an exception: the developer writes it by hand (spec Q2).
