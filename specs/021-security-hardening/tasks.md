# Tasks: Security Hardening

**Input**: spec.md, plan.md in `specs/021-security-hardening/`; findings and proposed tests from docs/security-review.md (sections 12 and 13)

`[US#]` refers to the user stories of this feature in `spec.md`; `[F#]` to the findings in docs/security-review.md section 12. Task IDs T001–T019 are kept from the review's section 13 so that the review, this file and the PRs use the same numbers. They do not continue the project-wide numbering (feature 020 ended at T356). Phases follow the PR order in plan.md, so the IDs are not in sequence.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. The named security test is written first and must fail before the fix (except T002, T004–T010 and T018–T019, which add tests or checks for behaviour that should already hold). This feature has one migration (T016), which the task explicitly requires (Constitution VIII).

## Phase 1: PR `021-ci-and-dependencies` — User Story 1 (P1) 🎯 first

**Goal**: every PR is built, tested and audited automatically; the current audit results are clean (F10, F11, F12).
**Independent Test**: spec US1.
**Who does what**: the developer creates `.github/workflows/ci.yml` by hand; the agent does not edit `.github/` (spec Q2, Article VIII).

- [X] T017 [US1] [F10] CI workflow `.github/workflows/ci.yml` (plan R1; actions pinned to major version tags). **Developer, by hand**: the workflow file with:
  - server job: `dotnet build`, `dotnet test`, `dotnet list package --vulnerable --include-transitive` (fail on any result);
  - client job: `npm ci`, `npm run lint`, `npm run build`, `npm run test`, `npm audit --omit=dev --audit-level=moderate`;
  - dummy `Jwt__Key` from the workflow env.

  **Agent** (everything else, no file in `.github/`): README.md, a short CI section (Constitution VI); make sure every command above passes locally with a dummy `Jwt__Key`; mark F10 in docs/security-review.md once the first run is green.

  Test: the workflow run on the PR is green; a deliberate failing test on a throwaway branch makes it red.
- [X] T018 [US1] [F11] `npm audit fix` (eslint 10.15.0, typescript-eslint 8.73.0). Test: `npm audit` reports 0 vulnerabilities; `npm run lint`, `npm run build`, `npm run test` pass.
- [X] T019 [US1] [F12] Remove `Microsoft.AspNetCore.Identity.EntityFrameworkCore` if `PasswordHasher<User>` still resolves from the shared framework; otherwise keep it and note why in plan.md. Test: `LoginAsyncTests.LoginAsync_ValidCredentials_ReturnsToken` and `RegisterAsyncTests.RegisterAsync_ValidData_CreatesCompanyAndManager` pass, plus a new `PasswordHashingTests.StoredHash_IsNotThePlainPassword`.

**Status notes (2026-11-23)**:
- T017: the developer wrote `.github/workflows/ci.yml` by hand (spec Q2): `backend` and `frontend` jobs on `pull_request` and on `push` to `main`, `permissions: contents: read`, actions pinned to major version tags (`checkout@v6`, `setup-dotnet@v5`, `setup-node@v6`), dummy `Jwt__Key`, every command listed in T017; the vulnerable-package step fails when `dotnet list package --vulnerable` reports any package. The agent did not edit `.github/`. README.md "Continuous Integration" section written by the agent (what runs, when, no real secrets, the same checks locally). First CI run on PR #27 failed in the frontend job: `ExportEstimate.test.tsx` built its fake response from a jsdom `Blob`, which Node 22 (CI) cannot read (Node 26 locally can); fixed in the test with a string body, no application change. The next run was green (backend and frontend), so F10 is marked FIXED in docs/security-review.md.
- T018: `npm audit fix` changed only `client/package-lock.json` (no `package.json` change, no breaking update): eslint 10.12.0 → 10.15.0, typescript-eslint 8.71.1 → 8.73.0, minimatch → 11.0.9. `npm audit` reports 0 vulnerabilities.
- T019: `PasswordHasher<User>` builds from the ASP.NET Core shared framework, so the `Microsoft.AspNetCore.Identity.EntityFrameworkCore` reference was removed from SmartEstimatePlatform.Api.csproj. `has-pending-model-changes` reports none. New test `PasswordHashingTests.StoredHash_IsNotThePlainPassword` (tests/server/AuthServiceTests) registers through the API and checks the stored hash with the application's own hasher.
- Checks: `dotnet build` 0 warnings, `dotnet test` 369 passed (368 + 1 new), `npm run lint` clean, `npm run build` passes, `npm run test` 123 passed.

## Phase 2: PR `021-auth-hardening` — User Story 2 (P1)

**Goal**: login does not reveal accounts; logout ends the session on the server (F5, F6).
**Independent Test**: spec US2.

- [X] T001 [US2] [F5] `AuthService.LoginAsync`: for an unknown email, verify the password against a fixed dummy hash (created once with the same `PasswordHasher<User>`) so both paths take the same time, then throw `InvalidCredentialsException` as today. This fixes only the timing difference; the 423 difference stays an accepted limitation (T002). Test: `LoginAsyncTests.LoginAsync_UnknownEmail_HashesLikeAKnownEmail` (the fake hasher records one verify call for each).
- [X] T002 [US2] [F5] Keep per-account lockout; document the account-existence and lock-out risk as an accepted limitation; rate limiting (T011) reduces abuse. The account-existence risk includes the 423 that only an existing account gets. Test: none new (documentation only, spec Q1); `LoginAsyncTests.LoginAsync_FiveFailuresWithin10Minutes_LocksAccount` keeps passing.
- [X] T003 [US2] [F6] Add `POST /api/auth/logout` (`[Authorize]`, 204): `AuthService.LogoutAsync` calls `User.InvalidateTokens()`; the client calls it before removing the token (plan R3). Test: `LogoutEndpointTests.TokenUsedAfterLogout_IsRejected` (server) and `Logout.test.tsx` "logging out calls the API and removes the token" (client).

**Status notes (2026-11-23)**:
- T001: `AuthService` verifies an unknown email's password against a dummy hash made once (a random password, so nothing ever matches), then throws `InvalidCredentialsException` as before. `LoginAsyncTests.LoginAsync_UnknownEmail_HashesLikeAKnownEmail` failed before the change (0 verifications for an unknown email) and passes now (1 and 1). `AuthServiceTestBase.WithServiceAsync` takes an optional hasher, and its `CountingPasswordHasher` wraps the real one.
- T002: no code. The account-existence (423) and lock-out risk is recorded as an accepted limitation in spec.md (Q1, FR-021-5) and docs/security-review.md (Accepted limitations, F5 PARTIAL); `LoginAsyncTests.LoginAsync_FiveFailuresWithin10Minutes_LocksAccount` passes unchanged.
- T003: `POST /api/auth/logout` (`[Authorize]`, 204) calls `AuthService.LogoutAsync`, which uses the existing `User.InvalidateTokens()`. The client's `NavBar` calls `logoutOnServer()` while the token is still stored and removes the token in `finally`, so logout also works offline. Tests: `LogoutEndpointTests.TokenUsedAfterLogout_IsRejected` and `LogoutEndpointTests.Logout_WithoutLogin_IsUnauthorized` (server, both failed before with 404), `Logout.test.tsx` "logging out calls the API and removes the token" (failed before) and "logging out still removes the token when the server cannot be reached". The two existing logout tests pass unchanged.
- Checks: `dotnet build` 0 warnings, `dotnet test` 372 passed (369 + 3), `npm run lint` clean, `npm run build` passes, `npm run test` 125 passed on Node 26 and Node 22.

## Phase 3: PR `021-authz-tests` — User Story 3 (P1)

**Goal**: every cell of the authorization matrix and every forged-token case is proven by a test (F2, F3).
**Independent Test**: spec US3.

Tests only; no production code change expected. Any failing test is a real bug and gets its own fix in this PR.

- [X] T004 [P] [US3] [F2] Anonymous matrix: one theory over every protected route in docs/security-review.md section 4 (method + URL, plan R4). Test: `AnonymousAccessTests.EveryProtectedEndpoint_Returns401`.
- [X] T005 [P] [US3] [F2] Catalog sections, Viewer and other company. Tests: `CatalogSectionAuthorizationTests.Viewer_IsForbiddenFromEveryCatalogSectionEndpoint` (list, get, create, update, delete) and `CatalogSectionIsolationTests.AnotherCompanysSection_CannotBeUpdated`.
- [X] T006 [P] [US3] [F2] Estimate sections. Tests: `EstimateSectionAuthorizationTests.Viewer_CannotChangeOrRemoveASection` and `EstimateSectionIsolationTests.AnotherCompanysEstimate_SectionsAreNotFound` (POST to the foreign estimate, PATCH, DELETE).
- [X] T007 [US3] [F2] `GET /api/auth/me` without a token (after T004, same file). Test: covered by `AnonymousAccessTests.EveryProtectedEndpoint_Returns401` (route listed explicitly).
- [X] T008 [P] [US3] [F2] SQL-looking search input. Test: `MaterialSearchTests.Search_SqlLikeText_IsTreatedAsData` (`' OR 1=1 --` returns no rows and the other company's data stays hidden); the same for works and catalog sections.
- [X] T009 [P] [US3] [F3] Forged tokens. Test: `TokenValidationTests.ForgedToken_IsRejected` theory: wrong signing key, `alg=none`, expired (via the test clock), wrong issuer, wrong audience, changed `company_id` claim → 401.
- [X] T010 [P] [US3] [F3] Start-up key check. Test: `JwtOptionsStartupTests.ShortKey_StopsTheApp` (factory with a 31-character key throws `OptionsValidationException`).

**Status notes (2026-11-23)**:
- Tests only; no production code changed. Every new test passed at once, so the matrix had untested cells but no access-control bug.
- T004, T007: `AnonymousAccessTests.EveryProtectedEndpoint_Returns401` covers all 48 protected method + route pairs, including `GET /api/auth/me` and `POST /api/auth/logout`. An extra check, `AnonymousAccessTests.TheListHoldsEveryProtectedEndpointOfTheApi`, compares the list with the API's real endpoints (`EndpointDataSource`), so a new endpoint without a row fails the tests (plan R4).
- T005: `CatalogSectionAuthorizationTests.Viewer_IsForbiddenFromEveryCatalogSectionEndpoint`, `CatalogSectionIsolationTests.AnotherCompanysSection_CannotBeUpdated`. New shared helpers `CatalogRequests.CatalogSectionRequest` and `CreateCatalogSectionAsync` in TestSupport.
- T006: `EstimateSectionAuthorizationTests.Viewer_CannotChangeOrRemoveASection`, `EstimateSectionIsolationTests.AnotherCompanysEstimate_SectionsAreNotFound` (POST to the foreign estimate, PATCH, DELETE).
- T008: `MaterialSearchTests.Search_SqlLikeText_IsTreatedAsData` is a theory over materials, works and catalog sections with three SQL-looking texts; a plain search is the control. The tests run on SQLite, not MySQL.
- T009: `TokenValidationTests.ForgedToken_IsRejected` (six forgeries) plus the control `TokenValidationTests.CorrectlyBuiltToken_IsAccepted`. "Expired" uses an expiry 10 minutes in the past (beyond the 5-minute clock skew) instead of a test clock, because JwtBearer reads the system clock. "Changed company_id" edits a real token's payload and keeps its signature.
- T010: `JwtOptionsStartupTests.ShortKey_StopsTheApp` (31 characters → `OptionsValidationException`) plus the boundary `JwtOptionsStartupTests.KeyOf32Characters_Starts`.
- docs/security-review.md: no MISSING cell left in section 4 (SC-021-3); F2 and F3 FIXED.
- Checks: `dotnet build` 0 warnings, `dotnet test` 437 passed (372 + 65), `npm run lint` clean, `npm run build` passes, `npm run test` 125 passed.

## Phase 4: PR `021-http-hardening` — User Story 4 (P2)

**Goal**: login and registration are rate limited; responses carry security headers; only configured hosts are accepted (F4, F7).
**Independent Test**: spec US4.

- [ ] T011 [US4] [F4] Add the built-in rate limiter (`AddRateLimiter`/`UseRateLimiter`, no new package) with a fixed-window policy per client IP on `login` and `register`, returning 429 with `ProblemDetails` (plan R5). Limits from configuration (default 10 requests per 60 seconds per client address); `ApiFactory` raises them. Document the forwarded-headers requirement in docs/architecture.md. Test: `RateLimitTests.Login_OverTheLimit_Returns429` and `RateLimitTests.Register_OverTheLimit_Returns429`; existing auth tests keep passing.
- [ ] T012 [US4] [F7] Add a small middleware in `Program.cs` that sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` on API responses (plan R6); document the CSP for the hosted client in docs/architecture.md. Test: `SecurityHeadersTests.EveryResponse_HasTheSecurityHeaders` (a 200, a 401 and a 404 response).
- [ ] T013 [US4] [F7] Outside Development, take `AllowedHosts` from the environment variable and refuse to start while it is `*`; `appsettings.json` keeps `*` for local runs, tests and CI (plan R7). Test: `HostFilteringTests.UnknownHost_IsRejectedOutsideDevelopment` and `HostFilteringTests.WildcardHosts_StopTheAppOutsideDevelopment`.

## Phase 5: PR `021-error-logging` — User Story 5 (P2)

**Goal**: security events are logged without secrets; over-long input is refused with 400 (F8, F9).
**Independent Test**: spec US5.

- [ ] T014 [US5] [F8] Log security events with `ILogger` in the services, without passwords or tokens: failed login (user id or email hash), lockout (user id), Viewer removal, password reset, estimate confirm, deletes (user id + entity id) (plan R8). Test: `SecurityLoggingTests.FailedLogin_IsLoggedWithoutThePassword` and `SecurityLoggingTests.RemoveViewer_IsLogged` (a test logger provider captures the entries).
- [ ] T015 [US5] [F8] Log 403 and cross-company 404 responses as warnings with user id and route, no body: authorization-failure logging through an `IAuthorizationMiddlewareResultHandler` and a log call in `DomainExceptionHandler` for `NotFoundException`. Test: `SecurityLoggingTests.ViewerCallingAManagerEndpoint_IsLogged` and `SecurityLoggingTests.AnotherCompanysId_IsLoggedAsNotFound`.
- [ ] T016 [US5] [F9] Add shared length rules (`PersonRules`: names 100, email 256, password 128, company name 200; search 200) on `RegisterRequest`, `CreateViewerRequest`, `LoginRequest`, `ResetPasswordRequest` and the search parameters (plan R9, Constitution X.10); migration for the `FirstName`, `LastName` and `Company.Name` columns after checking existing lengths. Paging stays out of scope (YAGNI, spec Edge Cases). Test: `RequestValidationTests.RegisterRequest_EmailOver256Characters_IsRejected`, `RequestValidationTests.RegisterRequest_NamesOverTheLimit_AreRejected`, `MaterialSearchTests.Search_OverTheLimit_Returns400`.

## Phase 6: Polish (after each PR)

Not new tasks: part of each PR's definition of done.

- Mark the PR's findings in docs/security-review.md section 12 as **FIXED** with the test names, and update the matrix in section 4 (no MISSING cell after PR `021-authz-tests`).
- Run the full check set and confirm that the CI run on the PR is green (after PR 1).

## Dependencies

- T017's workflow file is written by the developer (spec Q2). PR 1 (T017 → T018 → T019) is merged first, so PRs 2–5 run through CI.
- PRs 2–5 are independent of each other; recommended order: 2, 3, 4, 5 (plan.md).
- Inside PR 2: T001 → T002 → T003 (same service and tests).
- Inside PR 3: T004 → T007 (same test file); T005, T006, T008, T009, T010 in parallel.
- Inside PR 4: T011 → T012 → T013 (all in Program.cs).
- Inside PR 5: T014 → T015 (same logger setup) → T016.
- T002 refers to T011 (rate limiting) only in its reasoning; there is no build dependency.

## Parallel Execution Examples

- PR 3: T005 (catalog sections), T006 (estimate sections), T008 (search), T009 (tokens) and T010 (start-up key) touch different test files and can be written at the same time.
- PRs 2–5 can be prepared on their own branches in parallel once PR 1 is merged, but each is reviewed and merged on its own.

## Implementation Strategy

Turn on the safety net first (PR 1), so every later change is checked. Then fix what affects every user's account (PR 2), prove the access rules end to end (PR 3), harden the HTTP layer (PR 4), and finally make abuse visible and refuse over-long input (PR 5). Each PR is small, carries its own security tests, and updates docs/security-review.md when merged. Spec/plan/tasks commits stay separate from code commits (Article VI).
