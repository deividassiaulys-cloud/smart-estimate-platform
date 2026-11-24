# Feature 22: Deployment and Production Readiness

**Feature Branch**: `022-deployment` (specification); implemented in five PRs: `022-health-and-config`, `022-docker`, `022-db-migrations`, `022-deploy-pipeline`, `022-docs-release`
**Created**: 2026-11-24
**Status**: Draft
**Input**: Independent Work 8 (Deployment and Production Readiness). Deploy the platform to one Linux VPS (Ubuntu; provider not chosen yet) with Docker Compose: `api` (ASP.NET Core 10), `mysql` (named volume) and `caddy` (HTTPS through Let's Encrypt; serves the built client and proxies `/api` to the API on the same origin, so no CORS is needed). The public address is one variable, `SITE_ADDRESS` (an `<ip>.sslip.io` address or a real domain, set later), and the same value goes into `AllowedHosts`. Secrets live only in the GitHub Environment `production` and in the server's `.env` file, never in Git. Migrations run as an EF Core migrations bundle before the new API starts, and the deployment stops if they fail. Health endpoints `/health` (liveness) and `/health/ready` (database check). Forwarded headers trust only the Docker network proxy. Structured console logs carry a correlation id and no secrets. After a green CI on `main`, a deploy job connects over SSH, and a smoke test script then checks the site. Release v1.0.0, docs/deployment.md, and a final requirements verification table in docs/requirements-verification.md.

## Specification
The platform runs today only on a developer's computer. CI tests every change (feature 021) and the release workflow builds packages, but nothing runs them where users can reach them. Feature 021 also left several settings for deployment: `AllowedHosts`, the hosted client's CSP and forwarded headers behind a proxy (docs/architecture.md section 4).

This feature puts the platform on one Linux server with three containers behind one HTTPS address:
- Caddy serves the React build and sends `/api` and `/health` to the API, so the browser sees one origin.
- MySQL keeps its data in a named volume.
- The API runs as a non-root container, and its schema is brought up to date by a migrations bundle before it starts.

Every green CI run on `main` deploys automatically, and a smoke test then proves the live site works end to end: register, login, create, read back, isolation and anonymous access. The API answers health checks and writes structured logs that can be followed by one correlation id. It trusts forwarded headers only from the proxy, so the rate limit of feature 021 sees the real client address.

The feature ends with release v1.0.0, a deployment guide, and a table that shows how each requirement in docs/requirements.md is verified. Users see no change in behaviour: the application is the same, but now it is reachable over HTTPS.

## User Scenarios & Testing

### User Story 1: The API reports its health and is safe behind a proxy (Priority: P1) — PR `022-health-and-config`
As the person running the system, I want health endpoints, correct client addresses behind the proxy and structured logs with a correlation id, so that the deployment can tell when the API is ready, the rate limit works per real client, and one failed request can be followed through the logs.

**Why this priority**: the deployment (US3, US4) waits on `/health/ready`, and the rate limit of feature 021 is unsafe behind a proxy until forwarded headers are set (docs/security-review.md section 8; plan of feature 021, risk "Rate limiting by IP behind a proxy").

**Independent Test**: run the API tests. `/health` answers 200 without a token, `/health/ready` answers 200 with a working database and 503 without one, and every response carries an `X-Correlation-ID` header. A forwarded client address is used only when the request comes from the trusted network.

**Acceptance Scenarios**:
1. Given a running API, when `GET /health` is called without a token, then it returns 200 with the status and the deployed version, and it does not touch the database.
2. Given a reachable database, when `GET /health/ready` is called, then it returns 200. Given an unreachable database, it returns 503 without any exception text, connection string or host name.
3. Given a request from the trusted proxy network with `X-Forwarded-For: 203.0.113.7` and `X-Forwarded-Proto: https`, then the API treats the client address as `203.0.113.7` and the scheme as HTTPS: the rate limit partitions by it, and no HTTPS redirect is issued.
4. Given the same headers from any other address, then they are ignored.
5. Given an environment other than Development without the trusted network setting, then the API does not start (as with `AllowedHosts`, feature 021).
6. Given any response, then it carries `X-Correlation-ID`, and every log entry written while handling that request carries the same id.
7. Given an environment other than Development, then logs are written as one JSON object per line to the console, with no password, token, connection string or request body.

### User Story 2: The whole platform starts from containers (Priority: P1) — PR `022-docker`
As the developer, I want an API image, a web image (Caddy with the built client) and a Compose file that starts `api`, `mysql` and `caddy`, so that the same artifacts run on my computer and on the server.

**Why this priority**: every later PR deploys these images.

**Independent Test**: on the developer's computer, `docker compose up` with a local `.env` (`SITE_ADDRESS=localhost`) serves the client at `https://localhost`. `/api/auth/me` without a token answers 401 through the same origin, with the API's own security headers. `/health/ready` is 200, and only Caddy publishes ports.

**Acceptance Scenarios**:
1. Given the API image, then it runs as a non-root user, contains no SDK, source code, test project or secret, and listens on port 8080 inside the Docker network only.
2. Given the web image, then it serves `client/dist` with a fallback to `index.html` for client routes. It sends the hosted-client CSP and headers from docs/architecture.md section 4 and HSTS, and it does not overwrite the API's own security headers.
3. Given `SITE_ADDRESS`, then Caddy obtains a Let's Encrypt certificate for it, and HTTP is redirected to HTTPS. The API's `AllowedHosts` is set from the same variable.
4. Given the Compose file, then MySQL keeps its data in a named volume across `docker compose down` and `up`. MySQL and the API publish no host port, and only Caddy publishes 80 and 443.
5. Given a pull request, then CI builds both images, without pushing them, so a broken Dockerfile is caught before `main`.

### User Story 3: Migrations run before the new API, or the deployment stops (Priority: P1) — PR `022-db-migrations`
As the person running the system, I want the database schema brought up to date by a migrations bundle before the new API starts, and the deployment to stop if that fails, so that a new API never runs against an old schema.

**Why this priority**: the tests use SQLite with `EnsureCreated` and never run migrations (docs/testing-strategy.md section 4). The first production run is the first time migrations are applied automatically.

**Independent Test**: against an empty MySQL container, the migrate step creates the full schema and a second run changes nothing. With a migration that fails, the deploy script stops with a non-zero exit code before the new API container starts, and the previous API keeps running.

**Acceptance Scenarios**:
1. Given an empty database, when the migrate step runs, then every migration in `server/SmartEstimatePlatform.Api/Migrations` is applied and `__EFMigrationsHistory` lists them all.
2. Given an up-to-date database, when the migrate step runs again, then nothing changes and it exits with 0.
3. Given a migration that fails, when the deploy script runs, then it stops with a non-zero exit code, does not start the new API, and prints where the pre-migration backup is.
4. Given any deployment, then a database dump is taken before migrations run. The last 7 dumps are kept on the server, outside the Docker volume.
5. Given the migrate step, then it uses the application's database user, not the MySQL root user, and the connection string comes from the server's `.env` file.

### User Story 4: Every green `main` is deployed and checked automatically (Priority: P1) — PR `022-deploy-pipeline`
As the developer, I want every commit on `main` that passes CI to be built into images, deployed over SSH and checked by a smoke test, so that the live site always runs tested code and a broken deployment is visible at once.

**Why this priority**: it is the core of Independent Work 8, and the smoke test is the only automated end-to-end check of the real client, API and MySQL together (docs/testing-strategy.md section 4: "No end-to-end tests", "Migrations are not tested").

**Independent Test**: merge a PR. CI passes, the deploy workflow pushes images tagged with the commit SHA, deploys them, and the smoke test passes against `https://$SITE_ADDRESS`. The smoke test exits non-zero when any check fails, for example with a wrong expected version. A non-zero exit code fails the `smoke` job and with it the deploy workflow.

**Acceptance Scenarios**:
1. Given a CI run on `main` that failed, then no deployment starts. Given one that succeeded, then exactly that commit (`head_sha`) is built, pushed and deployed, and two deployments never run at the same time.
2. Given the deploy job, then it uses the GitHub Environment `production`. It reads only the SSH host, user, private key and known-hosts entry from it, and it never writes the server's `.env` file.
3. Given the deployment, when the smoke test runs, then it checks, in order:
   - `/health` is 200 and reports the deployed commit;
   - `/health/ready` is 200;
   - the client's start page is served;
   - a throwaway company A registers and logs in;
   - A creates a material (201);
   - A reads it back by id with a new request, with the same name, unit and price;
   - a throwaway company B gets 404 for A's material;
   - a request without a token gets 401.

   Any failed check fails the workflow, and the output names the step.
4. Given the smoke test, then it prints no token or password. Its throwaway passwords are random per run, and its emails use the reserved `.invalid` domain.
5. Given a failed smoke test, then the workflow is red and the previous image tag is printed, so the developer can roll back by hand (docs/deployment.md). There is no automatic rollback.

### User Story 5: The release is documented and every requirement is accounted for (Priority: P2) — PR `022-docs-release`
As the student and the instructor, I want a deployment guide, a requirements verification table and a tagged v1.0.0 release, so that anyone can set the system up again, see how each requirement was checked, and find the exact code that runs in production.

**Why this priority**: it closes the feature and the project. It depends on US1–US4 being live.

**Independent Test**: follow docs/deployment.md on a new server and reach a green smoke test. Every FR, NFR and BR in docs/requirements.md has a row in docs/requirements-verification.md. Tag `v1.0.0` points at the commit that runs in production.

**Acceptance Scenarios**:
1. Given docs/deployment.md, then it covers:
   - server prerequisites and first-time setup;
   - every `.env` variable (names and how to generate them, no values) and the GitHub Environment settings;
   - how a deployment runs, migrations and backups, rollback and restore;
   - logs and the correlation id, the health endpoints and the smoke test;
   - changing `SITE_ADDRESS` from sslip.io to a real domain.
2. Given docs/requirements-verification.md, then each of FR-001–FR-034, NFR-001–NFR-014 and BR-001–BR-018 has one row with:
   - a status: Met, Partially met or Deferred;
   - evidence: test names, a manual check with its date, or a production smoke step;
   - for anything not fully met, the reason. The deferred items match specs/deferred-features.md.
3. Given the release, then tag `v1.0.0` is on the deployed commit of `main`, and the GitHub Release notes list what is included, the known limitations and the deferred features.
4. Given README.md, docs/architecture.md, docs/security-review.md and docs/testing-strategy.md, then they describe the deployed setup: project status, the deployment link, the hosting diagram, deployment secrets, the forwarded-headers fix and the smoke test.

### Edge Cases
- **The sslip.io address changes when the server's IP changes**, and a later switch to a real domain changes `SITE_ADDRESS`. Caddy obtains a new certificate and `AllowedHosts` follows the same variable, so only `.env` and the GitHub Environment variable change. A token issued under the old address simply stops being sent, because `localStorage` is per origin.
- **Let's Encrypt rate limits**: repeated failed certificate attempts during setup can block issuance for hours. docs/deployment.md tells the developer to try Let's Encrypt's staging CA first.
- **A failed migration on MySQL is not rolled back automatically**: MySQL commits each DDL statement on its own, so a migration that fails half-way can leave the schema partly changed. That is why the dump is taken before migrating (US3), and docs/deployment.md describes the restore.
- **The old API keeps running while migrations run**: a migration must not break the running version (only additive changes), or a short downtime is accepted. Every migration so far is additive or narrows unused length; this is recorded as a rule in docs/deployment.md.
- **Smoke-test companies stay in the production database**: there is no endpoint to delete a company. Two companies per deployment, with `.invalid` emails and no personal data, are accepted. docs/deployment.md gives a SQL statement to remove them.
- **Rate limit during the smoke test**: it sends 2 registrations and 2 logins, well under 10 per 60 s. Two deployments in the same minute cannot happen, because of the concurrency group.
- **Health endpoints are public**: they reveal only `Healthy`/`Unhealthy` and the commit SHA of a public repository. They are not rate limited and need no token.
- **Downtime**: recreating the API container gives a few seconds of 502 from Caddy. This is accepted for one small server (no blue-green deployment, Constitution II.6).
- **The deploy user is in the `docker` group, which is equivalent to root on the server**: accepted for one VPS. SSH is key-only, root login is disabled, and the firewall opens only 22, 80 and 443.

## Requirements

### Functional Requirements
- **FR-022-1**: `GET /health` returns 200 with `status` and `version` (the commit SHA from the image, `dev` locally) without a token and without touching the database. `GET /health/ready` returns 200 when the database answers and 503 otherwise, with no technical details.
- **FR-022-2**: Outside Development, the API accepts `X-Forwarded-For` and `X-Forwarded-Proto` only from the network named in configuration (the Docker network of the Compose file), with one proxy hop, and refuses to start without that setting.
- **FR-022-3**: Every response carries `X-Correlation-ID`, which equals the request's trace id. Every log entry written while handling the request carries it as a scope. Outside Development the console log is JSON, one entry per line.
- **FR-022-4**: Logs contain no password, token, connection string or request body. The existing security logging of feature 021 is unchanged.
- **FR-022-5**: A non-root API image without SDK or secrets, and a web image (Caddy with the built client). Compose services `api`, `mysql` (named volume, MySQL 8.4) and `caddy`, with only Caddy publishing ports. `SITE_ADDRESS` drives both the certificate and `AllowedHosts`.
- **FR-022-6**: The web server sends the hosted-client CSP and headers from docs/architecture.md section 4 and HSTS on client responses, leaves the API's own headers unchanged, and sends `Cache-Control: no-cache` for `index.html`.
- **FR-022-7**: CI builds both images on every pull request without pushing them.
- **FR-022-8**: The API image contains an EF Core migrations bundle. Each deployment takes a database dump, runs the bundle, and starts the new API only if the bundle exits with 0. Otherwise it stops with a non-zero exit code.
- **FR-022-9**: After a successful CI run on `main`, a deploy workflow builds and pushes both images, tagged with the commit SHA, to GitHub Container Registry. It deploys them over SSH with the GitHub Environment `production`, one deployment at a time.
- **FR-022-10**: A smoke test script checks the deployed site as listed in US4 scenario 3, fails on the first failed check and prints no secret.
- **FR-022-11**: Secrets exist only in the GitHub Environment `production` (SSH access) and in the server's `.env` (database passwords, `Jwt__Key`). Neither is ever committed. `deploy/env.example` lists the variable names with placeholder values.
- **FR-022-12**: docs/deployment.md, docs/requirements-verification.md, release notes for v1.0.0, tag `v1.0.0`, and the documentation updates listed in US5 scenario 4.

### Key Entities
No new entity and no migration. The deployment adds runtime settings only:
- `SITE_ADDRESS`;
- the trusted proxy network;
- `APP_VERSION`;
- the database user and password;
- `Jwt__Key`.

## Success Criteria

### Measurable Outcomes
- **SC-022-1**: 100% of successful CI runs on `main` after PR `022-deploy-pipeline` start a deployment; 0 failed CI runs do.
- **SC-022-2**: Every deployment ends with all 8 smoke-test checks (US4 scenario 3) green, or the workflow is red and names the failed check.
- **SC-022-3**: `https://$SITE_ADDRESS` has a valid Let's Encrypt certificate, and `http://` redirects to `https://` (NFR-001).
- **SC-022-4**: With a deliberately failing migration, the deploy script exits non-zero and the running API container is unchanged, as shown by its image tag.
- **SC-022-5**: `git log -p` and a secret scan of the repository find no real secret after the feature, as in docs/security-review.md section 7.
- **SC-022-6**: 100% of the FR, NFR and BR in docs/requirements.md (34 + 14 + 18 = 66) have a row in docs/requirements-verification.md; every row that is not "Met" gives a reason.
- **SC-022-7**: Every existing test keeps passing (461 backend, 125 frontend), and the new health, forwarded-header and correlation tests pass.

## Assumptions
- One VPS with at least 1 vCPU, 2 GB RAM and 20 GB disk, running Ubuntu 24.04 LTS, with a public IPv4 address. The provider is chosen later and does not affect the design.
- The production database starts empty. Local data is not migrated.
- The repository is public (feature 021), so the GHCR images can be public and the server pulls them without credentials.
- Let's Encrypt issues certificates for `<ip>.sslip.io` names; a real domain can replace it at any time.
- No paging, scaling, second server, managed database or monitoring service (YAGNI). The health endpoints and logs are enough for one server.
- Backup here means only the pre-migration dump that protects a deployment. Backup management as a product feature stays out of scope (docs/requirements.md section 5).

## Clarifications

### Session 2026-11-24
Decisions given in the feature input:
- Q1: Where does it run? → A: One Linux VPS (Ubuntu, provider not fixed) with Docker Compose: `api`, `mysql` (named volume) and `caddy` (Let's Encrypt; serves the client and proxies `/api` on the same origin, no CORS).
- Q2: How is the public address configured? → A: One variable, `SITE_ADDRESS` (`<ip>.sslip.io` or a real domain, set later), also used for `AllowedHosts`.
- Q3: Where are secrets kept? → A: Only in the GitHub Environment `production` and in the server's `.env` file, never in Git.
- Q4: How do migrations run? → A: As an EF Core migrations bundle before the new API starts; the deployment stops if it fails.
- Q5: Which health endpoints? → A: `/health` (liveness) and `/health/ready` (database check).
- Q6: Which proxy is trusted? → A: Forwarded headers are trusted only from the Docker network proxy.
- Q7: What is logged? → A: Structured console logs with a correlation id and no secrets.
- Q8: How is it deployed? → A: An automated deploy job after a green CI on `main` (SSH), followed by the smoke test script of US4.
- Q9: Who writes which files? → A: The developer creates everything under `.github/` and `deploy/` by hand (Constitution Article VIII). The agent writes only in `server/`, `client/`, `tests/`, `docs/` and `specs/`.
  - **Amended 2026-11-24, during PR `022-docker`**: the student approved an exception for `deploy/` (Article VII.3; plan.md Complexity Tracking). The agent writes `deploy/docker-compose.yml`, `deploy/Caddyfile`, `deploy/env.example` and `deploy/deploy.sh`.
  - The student reviews each one as a diff before it is committed.
  - Everything under `.github/`, every real `.env` file, the server, the GitHub settings and the tag stay with the developer.

Proposed by the agent and decided by the student on 2026-11-24 (Constitution V; details and rejected alternatives in plan.md "Agent proposals"). Phase 2 of tasks.md must not start while any row is Pending. Each decision is recorded here with its date. A rejected proposal is replaced by the student's choice, and spec, plan and tasks are updated before Phase 2.

| ID | Proposal | Affects | Decision |
|---|---|---|---|
| P1 | Images built in GitHub Actions and pushed to GHCR, not built on the server | T364, T374, T375 | Accepted (2026-11-24) |
| P2 | No automatic rollback; the developer runs `IMAGE_TAG=<previous> ./deploy.sh` | T370, T378 | Accepted (2026-11-24) |
| P3 | One pre-migration dump per deployment, the last 7 kept on the server | T370 | Accepted (2026-11-24) |
| P4 | Smoke-test companies stay in the production database (cleanup SQL documented) | T372, T379 | Accepted (2026-11-24) |
| P5 | Outside Development the API refuses to start without `ReverseProxy:TrustedNetwork` | T358 | Accepted (2026-11-24) |
| P6 | `/health` reports the deployed commit SHA | T357, T370, T372 | Accepted (2026-11-24) |
