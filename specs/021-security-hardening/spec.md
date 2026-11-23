# Feature 21: Security Hardening

**Feature Branch**: `021-security-hardening` (specification); implemented in five PRs: `021-ci-and-dependencies`, `021-auth-hardening`, `021-authz-tests`, `021-http-hardening`, `021-error-logging`
**Created**: 2026-11-23
**Status**: Draft
**Input**: Fix the open findings F2–F12 of docs/security-review.md (review of 2026-11-23, `main` at `a06120d`), following its section 13 proposal: CI and dependency audits, login and logout hardening, a complete authorization test matrix, rate limiting and security headers, security event logging and input length limits. F1 (EF warning 10622) is already fixed. Each change comes with the security test named in the review.

## Specification
The security review found that the main controls are in place: every endpoint checks the role and the company, there is no raw SQL, no secret in the repository and no vulnerable runtime package. What is missing is mostly around them. Nothing runs the tests or the audits automatically. Login reveals whether an account exists and has no rate limit. Logout leaves the token valid. The authorization tests skip some endpoints. The API sends no security headers. Security events are not logged, and some text fields have no length limit. This feature closes those gaps in five small, independent PRs. The CI PR goes first, so every later PR is checked automatically. Users see no change except a 429 message after too many login or registration attempts and a server-side logout.

## User Scenarios & Testing

### User Story 1: Every change is checked automatically (Priority: P1) — PR `021-ci-and-dependencies`
As the developer, I want every pull request to build, run all tests and audit the dependencies automatically, so that a failing test or a known vulnerable package cannot be merged unnoticed.

**Why this priority**: it comes first so that the four later PRs run through it (F10). It also clears the current audit results (F11) and an unneeded package (F12).

**Independent Test**: open a PR. The workflow runs and passes. A deliberately failing test on a throwaway branch turns it red.

**Acceptance Scenarios**:
1. Given a pull request to `main`, when it is opened or updated, then the workflow builds the server and client, runs `dotnet test`, `npm run lint`, `npm run build` and `npm run test`, and fails if any of them fails.
2. Given a dependency with a known vulnerability (NuGet, any severity; npm runtime, moderate or above), then the workflow fails.
3. Given the current dependencies, then `npm audit` reports 0 vulnerabilities.
4. Given the server project, then it references no package that the code does not need, and password hashing still works.

### User Story 2: Login timing does not reveal accounts and logout ends access (Priority: P1) — PR `021-auth-hardening`
As a user, I want login to take the same time whether or not an email has an account, and logout to end my session on the server, so that an attacker cannot find accounts by timing or reuse my old token.

**Why this priority**: F5 and F6 are authentication weaknesses (OWASP A07) that affect every account.

**Independent Test**: log in with an unknown email and with a known email and a wrong password. Both give 401 and both verify one password hash. Log out, then call the API with the old token: 401.

**Acceptance Scenarios**:
1. Given an unknown email, when someone logs in, then a password hash is verified exactly as for a known email, so the response takes the same time, and the response is the same 401 as for a wrong password.
2. Given a logged-in user, when they log out, then the server invalidates their tokens, and any copy of the old token is rejected with 401.
3. Given the per-account lockout, then it stays as it is: the fifth failure gives 423 only for an existing account. This account-existence and lock-out risk is recorded as an accepted limitation.

### User Story 3: Every endpoint's access rules are proven by a test (Priority: P1) — PR `021-authz-tests`
As the developer, I want an automated test for every cell of the authorization matrix and for forged tokens, so that a missing `[Authorize]` or a broken company filter is caught at once (Constitution IV).

**Why this priority**: the controls exist, but F2 and F3 leave some of them untested, and Article IV requires role and company rules to be covered by integration tests.

**Independent Test**: run the new tests. Every protected endpoint answers 401 without a token, 403 for a Viewer where the review's matrix says so, and 404 for another company. Every forged token is rejected.

**Acceptance Scenarios**:
1. Given no token, when any protected endpoint in the review's section 4 is called, then it returns 401.
2. Given a Viewer, when any catalog-section endpoint, or a section change or removal in an estimate, is called, then it returns 403 and nothing changes.
3. Given another company's catalog section or estimate, when it is updated, or a section is added, changed or removed, then the response is 404 and nothing changes.
4. Given a token with a wrong signature, no signature (`alg=none`), an expired lifetime, a wrong issuer or audience, or a changed `company_id`, then the request is rejected with 401.
5. Given a `Jwt:Key` shorter than 32 characters, then the application does not start.
6. Given a search text such as `' OR 1=1 --`, then it is treated as plain text and returns no other company's data.

### User Story 4: The API resists guessing and browser attacks (Priority: P2) — PR `021-http-hardening`
As a user, I want login and registration to be rate limited and API responses to carry security headers, so that password guessing is slowed down and browsers block framing, MIME sniffing and injected scripts.

**Why this priority**: F4 and F7 are medium risks. The lockout already limits guessing per account, and React already escapes output.

**Independent Test**: send login requests above the limit from one address: 429. Any API response carries the security headers. A request with an unknown host is rejected outside Development.

**Acceptance Scenarios**:
1. Given more than 10 login or registration requests from one client address within 60 seconds (the default, configurable), then the next ones get 429 with a `ProblemDetails` body until the window ends.
2. Given any API response (200, 401, 404), then it carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`.
3. Given an environment other than Development, when a request names a host outside the list in the `AllowedHosts` environment variable, then it is rejected (400); the app does not start there while the list is `*`.

### User Story 5: Security events are logged and over-long input is refused (Priority: P2) — PR `021-error-logging`
As the person running the system, I want failed logins, lockouts, denied and cross-company requests and destructive actions to be logged without secrets, and over-long input to be refused with a clear message, so that attacks can be noticed and never end as a server error.

**Why this priority**: F8 (no security logging) and F9 (missing length limits). Neither is exploitable on its own, but both hide abuse.

**Independent Test**: trigger each event and read the captured log entries: no password or token in them. Send an over-long email, name or search text: 400 with a field message, not 500.

**Acceptance Scenarios**:
1. Given a failed login or a lockout, then a warning is logged with the user id or an email hash, never the password.
2. Given a Viewer removal, a password reset, an estimate confirmation or a delete, then an information entry is logged with the user id and the entity id.
3. Given a 403, or a 404 for another company's ID, then a warning is logged with the user id and the route, without the request body.
4. Given an email over 256 characters, a first or last name over 100, a company name over 200, a password over 128 or a search text over 200 characters, then the request is refused with 400 and a field message.

### Edge Cases
- Rate limiting must not break the existing auth tests, which log in many times from one address: the limits are configuration values that the test factory can raise.
- A user who logs out on one device is logged out on every device, because `TokenVersion` is per user. This is accepted (a simpler design than per-device tokens, Constitution II.6).
- A user removed or with a reset password is already rejected on the next request; logout reuses the same mechanism.
- The CSP covers API responses only. The hosted client's CSP depends on how it is deployed; docs/architecture.md records the required policy.
- Paging stays out of scope until a list grows large (YAGNI). The request body limit stays at Kestrel's default.
- Existing data longer than a new column limit: the migration in T016 must check the current maximum lengths before narrowing `longtext` columns.

## Requirements

### Functional Requirements
- **FR-021-1** (F10): A CI workflow runs on every pull request to `main`. It builds the server and client, runs all backend and frontend tests, lint and build, and fails on any failure or on a known vulnerable dependency.
- **FR-021-2** (F11): The client's dev dependencies are updated so that `npm audit` reports 0 vulnerabilities.
- **FR-021-3** (F12): The server references no package it does not need; password hashing keeps working.
- **FR-021-4** (F5): Login verifies a password hash for an unknown email too, so an unknown email and a wrong password take the same time and give the same 401.
- **FR-021-5** (F5): Lockout stays per account; the account-existence risk (423 only for existing accounts) and the lock-out risk are documented as an accepted limitation.
- **FR-021-6** (F6): `POST /api/auth/logout` invalidates the caller's tokens; the client calls it before discarding the token.
- **FR-021-7** (F2): Every cell of the authorization matrix in docs/security-review.md section 4 is covered by an integration test, including search input that looks like SQL.
- **FR-021-8** (F3): Forged, unsigned, expired, wrong-issuer, wrong-audience and changed-company tokens are proven to be rejected, and a short `Jwt:Key` is proven to stop the application.
- **FR-021-9** (F4): Login and registration are rate limited per client address, by default 10 requests per 60 seconds (configurable), and answer 429 with `ProblemDetails` above the limit.
- **FR-021-10** (F7): Every API response carries the four security headers; outside Development, only the hosts listed in the `AllowedHosts` environment variable are accepted.
- **FR-021-11** (F8): Security events (failed login, lockout, 403, cross-company 404, Viewer removal, password reset, estimate confirmation, deletes) are logged without passwords, tokens or request bodies.
- **FR-021-12** (F9): Personal names, email, company name, password and search text have maximum lengths, declared once on the request records (Constitution X.10).

### Key Entities
- **User**: no new field. `TokenVersion` is now also increased by logout.
- **User / Company**: `FirstName`, `LastName` and `Company.Name` get maximum lengths (migration in T016).

## Success Criteria

### Measurable Outcomes
- **SC-021-1**: 100% of pull requests to `main` after PR `021-ci-and-dependencies` show a CI run; a failing test makes it fail.
- **SC-021-2**: on every CI run, `dotnet list package --vulnerable --include-transitive` and `npm audit --omit=dev` (runtime dependencies) report 0 vulnerabilities; a full `npm audit` (including dev tools) reported 0 after T018.
- **SC-021-3**: 0 cells of the review's authorization matrix are marked MISSING.
- **SC-021-4**: An unknown email and a wrong password both give 401 and both verify exactly one password hash per attempt.
- **SC-021-5**: A token used after logout is rejected (401) in 100% of the tests.
- **SC-021-6**: Login requests above the limit get 429; every tested API response has the 4 security headers.
- **SC-021-7**: Each of the 8 security event types produces a log entry; 0 entries contain a password or token.
- **SC-021-8**: 0 inputs from FR-021-12 can reach the database over their limit (each over-long field gives 400, not 500).
- **SC-021-9**: Every existing test keeps passing; the only changed expectations are the logout test in the client and tests that need a raised rate limit.

## Assumptions
- The review in docs/security-review.md is the source of the findings and of the proposed tests. Its task IDs T001–T019 are kept so that the review and this feature match.
- The built-in ASP.NET Core rate limiter and logging are used: no new NuGet or npm package (AGENTS.md).
- The project is hosted on GitHub; GitHub Actions is available on the free plan.
- Branch protection cannot be enforced on the free private plan, so "CI must pass" is a working rule, as is "no merge with failing tests" today (Constitution III).
- Lockout stays per account (spec Q1).

## Clarifications

### Session 2026-11-23
- Q1: Should the lockout move to per account + IP once rate limiting exists? → A: No. Keep per-account lockout; document the account-existence and lock-out risk as an accepted limitation; rate limiting (T011) reduces abuse.
- Q2: T017 needs `.github/workflows/ci.yml`, but Constitution Article VIII allows an AI agent to change files only in server/, client/, tests/, docs/ and specs/. Who writes the workflow? → A: ci.yml is created by the developer by hand; the agent does not edit .github/; T017's agent work is limited to everything else.
