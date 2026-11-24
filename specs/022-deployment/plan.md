# Plan: Deployment and Production Readiness

**Branch**: `022-deployment` (specification); five implementation PRs | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

## Summary

Deploy the platform to one Ubuntu VPS with Docker Compose (`api`, `mysql`, `caddy`) behind one HTTPS address, `SITE_ADDRESS`, in five PRs, each with its own branch, the tests or checks named in tasks.md, and a green CI run:

| Order | PR / branch | User story | Tasks | Who writes it |
|---|---|---|---|---|
| 1 | `022-health-and-config` | US1 | T357–T361 | agent (server, tests) |
| 2 | `022-docker` | US2 | T362–T367 | agent: Dockerfiles; developer: `deploy/`, CI job |
| 3 | `022-db-migrations` | US3 | T368–T371 | agent: bundle in the API image; developer: `deploy/` |
| 4 | `022-deploy-pipeline` | US4 | T372–T378 | agent: smoke test; developer: server, GitHub, workflow |
| 5 | `022-docs-release` | US5 | T379–T387 | agent: docs; developer: tag and release |

The PRs are sequential: each one uses the one before it. The API needs PR 1's health check and forwarded headers before it can run behind Caddy, PR 3 adds to PR 2's image and Compose file, PR 4 deploys what PRs 2 and 3 build, and PR 5 documents the live system.

## Technical Context

**Language/Version**: C# / .NET 10 (server), TypeScript / React 19 (client: no code change), Dockerfile, Compose YAML, Caddyfile, Bash (smoke test, deploy script), GitHub Actions YAML

**Primary Dependencies**: no application package is added. The tools and images used are listed below with their one-line justification (AGENTS.md):
- **Health checks** (`AddHealthChecks`, `MapHealthChecks`), forwarded headers (`UseForwardedHeaders`) and the JSON console formatter (`AddJsonConsole`) are part of the ASP.NET Core shared framework.
- **The database health check** is a small `IHealthCheck` that calls `Database.CanConnectAsync()`. The package `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` would do the same, but it is not needed for one call.
- **`dotnet-ef` 10.0.12** (the EF Core version of the project) is installed in the Docker build stage only, to build the migrations bundle. It is a build tool, not a runtime dependency, and the version is pinned.
- **Container images**, pinned by version tag:
  - `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0` (official .NET images; `aspnet` has a non-root `app` user);
  - `node:22-alpine`, the same Node major version as CI;
  - `caddy:2.x`, pinned to an exact minor version when T363 is done: automatic Let's Encrypt with no extra tool;
  - `mysql:8.4`, the current LTS; it supports the `utf8mb4_0900_as_ci` collation the model uses.
- **GitHub Actions**: `actions/checkout@v6` (already used), `docker/login-action`, `docker/setup-buildx-action` and `docker/build-push-action`, from Docker's official publisher, pinned to major version tags as in feature 021. SSH and SCP use the runner's own `ssh`/`scp`, not a third-party SSH action, so no extra party sees the key.

**Storage**: MySQL 8.4 in the named volume `mysql-data`. No new migration: the bundle applies the existing ones.

**Testing**:
- xUnit integration tests through `ApiFactory`: health, forwarded headers, correlation id and logging format (Constitution IV: the proxy trust is a security rule).
- Checks of containers, migrations and the pipeline are scripted or manual, and each is named in tasks.md.
- `tests/smoke/smoke-test.sh` is the end-to-end check, run locally against Compose and in the pipeline against production.

**Project Type**: Web application with a deployment

**Constraints**:
- No change in behaviour for users.
- No secret in Git, in an image or in a workflow file (FR-022-11).
- Every existing test keeps passing (SC-022-7).
- The developer writes everything under `.github/` and `deploy/` by hand (spec Q9, Article VIII).

## Design Decisions

Decisions R1–R9 follow the feature input (spec Clarifications Q1–Q9). P1–P6 were the agent's proposals; the student accepted all six on 2026-11-24 (Constitution V; spec Clarifications).

- **R1 Health** (T357): `builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])`, with `Data/DatabaseHealthCheck.cs` calling `AppDbContext.Database.CanConnectAsync()`.
  - `app.MapHealthChecks("/health", new() { Predicate = _ => false, ResponseWriter = … })` writes `{"status":"Healthy","version":"<APP_VERSION>"}`, so it does not touch the database.
  - `app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") })` uses the default plain-text writer (`Healthy`/`Unhealthy`), so no exception text leaks.
  - Neither endpoint has `[Authorize]` or a rate-limit policy, so `AnonymousAccessTests.TheListHoldsEveryProtectedEndpointOfTheApi` (which lists only endpoints with `IAuthorizeData`) is unaffected.
  - `APP_VERSION` is read from configuration (the image sets it from a build argument), with `dev` as the default.
- **R2 Forwarded headers** (T358): the configuration key `ReverseProxy:TrustedNetwork` holds a CIDR, for example `172.30.0.0/24`, the fixed subnet of the Compose network.
  - When it is set, `ForwardedHeadersOptions` uses `XForwardedFor | XForwardedProto` and `ForwardLimit = 1`, clears the default loopback entries, and adds only that network to `KnownIPNetworks`.
  - `app.UseForwardedHeaders()` is the first middleware, so the HSTS, HTTPS redirection, rate limiter and logging all see the real client and scheme.
  - Outside Development the app refuses to start without the key, the same rule as `AllowedHosts` (T013 of feature 021) and for the same reason: behind the proxy every client would otherwise share the proxy's address in the rate limiter (docs/security-review.md section 8).
  - `ApiFactory` sets a test network.
  - Caddy replaces any `X-Forwarded-For` a client sends, because Caddy trusts no upstream proxy by default, so a client cannot spoof the address through Caddy.
- **R3 Correlation id and logs** (T359, T360): the correlation id is the request's W3C trace id (`Activity.Current.TraceId`, falling back to `HttpContext.TraceIdentifier`).
  - A small middleware next to the security-headers middleware sets `X-Correlation-ID` through `Response.OnStarting`.
  - Outside Development, `builder.Logging.AddJsonConsole(o => { o.IncludeScopes = true; o.UseUtcTimestamp = true; })` replaces the default console logger. The hosting layer's request scope (`TraceId`, `SpanId`, `RequestId`, `RequestPath`) is then on every entry, so all entries of one request share the id.
  - The `ProblemDetails` `traceId` already contains the same trace id, so a 4xx/5xx body can be matched to the logs.
  - No incoming correlation header is accepted, because it would be untrusted input written into logs (YAGNI).
  - No secrets: EF Core sensitive-data logging stays off (the default), `Microsoft.AspNetCore` stays at `Warning`, and the existing feature 021 logging writes ids only.
  - Caddy's access log redacts the `Authorization` header by default (`log_credentials` off) and records `X-Correlation-ID` among the response headers.
- **R4 Images** (T362, T363):
  - `server/Dockerfile`, build context `server/`, multi-stage:
    - `build` (sdk): restore, then `dotnet publish -c Release`;
    - `bundle` (sdk): `dotnet tool install dotnet-ef --version 10.0.12`, then `dotnet ef migrations bundle -c Release -o /out/efbundle`;
    - `final` (aspnet): copies the published app and the bundle, sets `ENV APP_VERSION=$APP_VERSION`, `USER $APP_UID` and port 8080.
  - `client/Dockerfile`, build context `client/`:
    - `build` (node:22-alpine): `npm ci`, then `npm run build`;
    - `final` (caddy): copies `dist` to `/srv`.
  - `.dockerignore` files keep `bin/`, `obj/`, `node_modules/`, `dist/`, `.env*`, `appsettings.Development.json` and IDE folders out of the build contexts.
  - The Caddyfile is not baked into the image: Compose mounts `deploy/Caddyfile`, which the developer owns (Article VIII).
- **R5 Compose and Caddy** (T364, T365; developer):
  - `deploy/docker-compose.yml` has the services `mysql`, `migrate`, `api` and `caddy`:
    - **`mysql`**: `mysql:8.4`, volume `mysql-data`, health check `mysqladmin ping`; it gets `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD` and `MYSQL_ROOT_PASSWORD` from `.env`.
    - **`migrate`**: the API image with `command: ["./efbundle"]`, `profiles: [migrate]`, and the same environment as `api`.
    - **`api`**: the API image, with the environment `ASPNETCORE_ENVIRONMENT=Production`, `AllowedHosts=${SITE_ADDRESS}`, `ReverseProxy__TrustedNetwork=172.30.0.0/24`, `ConnectionStrings__DefaultConnection=Server=mysql;Database=…;User=…;Password=${MYSQL_PASSWORD}` and `Jwt__Key=${JWT_KEY}`.
    - **`caddy`**: the web image; ports 80, 443 and 443/udp; volumes `caddy-data` and `caddy-config`; `deploy/Caddyfile` mounted read-only; `SITE_ADDRESS` from `.env`.
  - One network, `internal`, with the fixed subnet `172.30.0.0/24`.
  - The `json-file` log driver with `max-size: 10m` and `max-file: 3` on every service, so logs cannot fill a small disk.
  - `restart: unless-stopped` on every service except `migrate`.
  - Images are `ghcr.io/<owner>/smart-estimate-api:${IMAGE_TAG}` and `ghcr.io/<owner>/smart-estimate-web:${IMAGE_TAG}`.
  - `deploy/Caddyfile`, one site block `{$SITE_ADDRESS}`:
    - `handle /api/*` and `handle /health*` go to `reverse_proxy api:8080`, with no `header` directive, so the API's own CSP stays.
    - The fallback `handle` serves the client: `root * /srv`, `try_files {path} /index.html`, `file_server`, and the client headers of docs/architecture.md section 4, plus `Strict-Transport-Security: max-age=31536000` and `X-Frame-Options: DENY`. `index.html` gets `Cache-Control: no-cache`, and `/assets/*` gets a long immutable cache, because Vite hashes those file names.
    - `encode zstd gzip`.
- **R6 Migrations** (T368–T370): the bundle runs as `docker compose run --rm migrate` before `docker compose up -d api caddy`.
  - Building the bundle's `AppDbContext` runs `Program.cs` up to `Build()`, so the bundle needs the same environment as the API: the `AllowedHosts`, trusted-network and connection-string checks run there too. The Compose file therefore gives `migrate` exactly the environment of `api` (T369 checks it).
  - `deploy/deploy.sh` (developer) uses `set -euo pipefail` and runs these steps in order:
    1. `docker compose pull`;
    2. `mysqldump` through `docker compose exec mysql`, written to `/var/backups/smart-estimate/<UTC timestamp>-<tag>.sql.gz`, keeping the last 7;
    3. `migrate`, which stops the script on failure and prints the dump path;
    4. `up -d api caddy`;
    5. a wait of up to 60 s for `https://$SITE_ADDRESS/health/ready`, and a check that `/health` reports `IMAGE_TAG`;
    6. writing the deployed tag to `.deployed-tag`, after printing the previous one.
  - The old API keeps running until step 4 (spec Edge Cases).
- **R7 Pipeline** (T374; developer): `.github/workflows/deploy.yml` runs on `workflow_run` of `CI` on `main` with `types: [completed]`.
  - It runs only when the conclusion is `success`, with `concurrency: { group: production, cancel-in-progress: false }`.
  - Job `images` (`permissions: contents: read, packages: write`): checks out `head_sha`, then builds and pushes both images tagged `head_sha`, with `APP_VERSION=head_sha`.
  - Job `deploy` (`needs: images`, `environment: production`, `permissions: contents: read`):
    - writes `SSH_PRIVATE_KEY` to a 0600 file and `SSH_KNOWN_HOSTS` to `known_hosts`, with no `StrictHostKeyChecking=no`;
    - `scp` copies `deploy/docker-compose.yml`, `deploy/Caddyfile` and `deploy/deploy.sh` to `~/smart-estimate/`;
    - `ssh` runs `IMAGE_TAG=<sha> ./deploy.sh`.
  - Job `smoke` (`needs: deploy`): runs `tests/smoke/smoke-test.sh` with `BASE_URL=https://${{ vars.SITE_ADDRESS }}` and `EXPECTED_VERSION=<sha>`.
  - `release.yml` stays as it is. Its artifacts are still useful for downloads, and merging the two workflows can come later.
- **R8 GitHub Environment `production`** (T373; developer):
  - **Secrets**: `SSH_HOST`, `SSH_USER`, `SSH_PRIVATE_KEY` (an ed25519 key used only for deployment) and `SSH_KNOWN_HOSTS`.
  - **Variable**: `SITE_ADDRESS`.
  - **Deployment branch rule**: `main` only.
  - The server's `.env` (`SITE_ADDRESS`, `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD`, `MYSQL_ROOT_PASSWORD`, `JWT_KEY`) is written once by hand on the server with mode 0600 and is never sent by the workflow. Passwords and the key are generated with `openssl rand -hex 32`: hex only, so a `;` cannot break the connection string.
- **R9 Smoke test** (T372): `tests/smoke/smoke-test.sh` in Bash with `curl` and `jq` (both on `ubuntu-latest` and on Ubuntu).
  - Inputs: `BASE_URL` and `EXPECTED_VERSION` (optional locally), plus `SMOKE_CA_CERT` (optional; used locally with Caddy's internal CA instead of `-k`).
  - It runs `set -euo pipefail`, and each step prints `ok <n> <name>` or `FAIL <n> <name>: expected …, got …`, with no token or password.
  - Its 8 steps are those of spec US4 scenario 3.
  - Emails look like `smoke-<UTC timestamp>-a@smoke.invalid`, and passwords come from `openssl rand`.
  - Total requests: 2 registrations and 2 logins (under the limit of 10 per 60 s).

### Agent proposals (accepted by the student on 2026-11-24)

- **P1 Images built in CI and pushed to GHCR**, not built on the server.
  - Why: the server needs no SDK, Node or build memory; the image is built from exactly the commit CI tested; and rolling back means running the previous tag.
  - Rejected alternative: `git pull && docker compose build` on the VPS (simpler, but slow and memory-hungry on 2 GB, and not reproducible).
- **P2 No automatic rollback.** The workflow goes red and prints the previous tag, and the developer runs `IMAGE_TAG=<previous> ./deploy.sh`. Migrations are forward-only, so a rollback after a schema change needs the dump (docs/deployment.md). Automatic rollback with schema changes is complex and rarely safe (II.6).
- **P3 One pre-migration dump per deployment, the last 7 kept**, in `/var/backups/smart-estimate/` on the server. Off-server copies are not part of this feature, and docs/deployment.md tells the operator to download a copy before risky changes.
- **P4 Smoke-test companies stay in production**: 2 per deployment, `.invalid` emails, no personal data. docs/deployment.md gives the cleanup SQL.
- **P5 The trusted network is required outside Development** (R2).
- **P6 `/health` reports the commit SHA**, so the smoke test proves the new version answers and not the old one. The SHA of a public repository is not a secret.

## Security Risks (Constitution IV)

- **Proxy trust**:
  - Risk: trusting forwarded headers from everywhere would let anyone pick their own IP for the rate limiter.
  - Mitigation: only `172.30.0.0/24` is trusted, with one hop; Caddy overwrites the client's header; and a test proves headers from an untrusted address are ignored (T358).
- **Secrets in transit and at rest**:
  - The workflow never sends application secrets.
  - The SSH key is a dedicated deploy key, stored as an Environment secret that only `main` can use.
  - `.env` is mode 0600 and owned by the deploy user.
  - `docker inspect` shows container environment variables to anyone in the `docker` group, which is only the deploy user.
- **Root-equivalent deploy user** (docker group):
  - Accepted for one VPS (spec Edge Cases).
  - Mitigation: SSH is key-only, `PermitRootLogin no`, ufw opens only 22, 80 and 443, and unattended security upgrades are on.
- **Exposed services**: MySQL and the API publish no host port; only Caddy is reachable. T366 checks this with `ss -tlnp` and an external port scan of 3306 and 8080.
- **Public health endpoints**: they reveal only the status and the commit SHA (P6), with no stack trace or host name; T357 tests this.
- **Supply chain**: base images are pinned by version tag from official publishers; actions are pinned to major versions from official publishers; there is no third-party SSH action; and the images are rebuilt on every deployment, so base image security updates come in.
- **Data loss on a failed migration**: MySQL DDL is not transactional, so a dump is taken before each migration (R6), and the restore is documented and tried once (T370).
- **Logs**: JSON logs on the server keep 30 MB per container. They hold user ids, email hashes and paths only (feature 021), never tokens or passwords; T360 tests this.
- **The token in `localStorage`** stays an accepted limitation (docs/security-review.md). The hosted-client CSP from docs/architecture.md section 4 is now actually served (T365) and checked by T366.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| I / II.5 tests | Each task names its test or check; health, proxy trust and correlation id are integration tests | Pass |
| II.1 / III business logic | No business rule changes; health check and middleware are infrastructure in `Program.cs` and `Data/` | Pass |
| II.6 simplicity | One server, Compose, Caddy's automatic HTTPS, no orchestrator, no automatic rollback, no blue-green | Pass |
| III dependencies | No application package added; build tool and images justified above | Pass |
| III secrets | Only in the GitHub Environment and the server's `.env`; `deploy/env.example` holds placeholders; SC-022-5 | Pass |
| III no merge with failing tests | Deployment only after a green CI on `main`; ruleset `main-protection` unchanged | Pass |
| III no features beyond spec | No paging, monitoring service, second server or product backup feature | Pass |
| IV security | Risks above documented before implementation | Pass |
| V proposals | P1–P6 were proposals; the student accepted all six on 2026-11-24 (spec Clarifications table) | Pass |
| VI | Spec/plan/tasks committed separately; README.md gets a Deployment section (T381) | Pass with exception (Complexity Tracking) |
| VII.2 | No clarification marker; Q1–Q9 answered, P1–P6 explicit proposals | Pass |
| VIII paths | `.github/` is written by the developer by hand. The agent writes in `server/`, `client/`, `tests/`, `docs/` and `specs/`, plus two approved exceptions: README.md (pending approval) and the `deploy/` files (approved 2026-11-24). Both are recorded in Complexity Tracking | Pass with exceptions |
| VIII migrations | No new migration; the bundle only applies the existing ones | Pass |
| X.2 / X.3 | No controller logic; `DatabaseHealthCheck` is infrastructure, not an application service, so it needs no `IXxxService` | Pass |
| X.9 | `CheckHealthAsync` receives the framework's cancellation token by its interface signature; no new `CancellationToken` in services or controllers | Pass |

**Post-design re-check**: Pass, with one exception (README.md) to be approved.

## Project Structure

```text
server/
├── Dockerfile                                   # T362 (agent): build, bundle, final stages
├── .dockerignore                                # T362 (agent)
└── SmartEstimatePlatform.Api/
    ├── Program.cs                               # T357 health, T358 forwarded headers, T359 correlation id, T360 JSON logs
    ├── appsettings.json                         # T358: ReverseProxy:TrustedNetwork absent (Development)
    └── Data/DatabaseHealthCheck.cs              # T357
client/
├── Dockerfile                                   # T363 (agent): build, then Caddy with /srv
└── .dockerignore                                # T363 (agent)
tests/
├── server/CrossCuttingTests/HealthEndpointTests.cs, ForwardedHeadersTests.cs,
│   CorrelationIdTests.cs, ProductionLoggingTests.cs  # T357–T360
├── server/TestSupport/ApiFactory.cs             # T358: trusted test network
└── smoke/smoke-test.sh                          # T372 (agent)
deploy/                                          # developer, by hand (Article VIII)
├── docker-compose.yml                           # T364, T369
├── Caddyfile                                    # T365
├── env.example                                 # T364: names and placeholders only
└── deploy.sh                                    # T370
.github/workflows/
├── ci.yml                                       # T367 (developer): image build job
└── deploy.yml                                   # T374 (developer)
docs/
├── deployment.md                                # T379
├── requirements-verification.md                 # T380
├── releases/v1.0.0.md                           # T385
├── architecture.md, security-review.md, testing-strategy.md  # T382–T384
README.md                                        # T381 (exception)
```

## Implementation Order

1. **PR 1 `022-health-and-config`**: T357 → T358 → T359 → T360 → T361.
2. **PR 2 `022-docker`**: T362, T363 (agent) → T364, T365 (developer) → T366 (local check) → T367 (developer, CI job).
3. **PR 3 `022-db-migrations`**: T368 (agent) → T369, T370 (developer) → T371 (failure drill).
4. **PR 4 `022-deploy-pipeline`**: T372 (agent; tried locally against PR 2's stack) → T373 (developer: server and GitHub Environment) → T374 (developer: workflow) → T375 (first deployment) → T376–T378 (checks).
5. **PR 5 `022-docs-release`**: T379–T385 (agent) → T386 (production manual checks) → T387 (developer: tag and release).

## Complexity Tracking

| Violation | Why needed | Simpler alternative rejected because |
|---|---|---|
| README.md (repository root) edited by the agent, outside the folders allowed by Article VIII (T381) | Article VI requires README.md to be updated when setup or run instructions change. This feature adds the deployment and makes the "Project Status" line ("Requirements and design phase") wrong | Needs the student's approval for this feature (Article VII.3), as given for feature 021. If it is refused, the developer writes the section by hand from the text the agent proposes in T381 |

| `deploy/docker-compose.yml`, `deploy/Caddyfile`, `deploy/env.example`, `deploy/deploy.sh` written by the agent, outside the folders allowed by Article VIII (T364, T365, T369, T370) | The deployment files must match the API's settings exactly: the trusted network, `AllowedHosts`, health paths, headers and the bundle's environment. The agent wrote and tested that side, and the student asked the agent to write them (2026-11-24) | The student approved this exception on 2026-11-24 (Article VII.3), replacing the original split of spec Q9. The original split, with the developer writing every `deploy/` file by hand from plan R5/R6, was the alternative. The exception does not cover `.github/`, any real `.env` file, the server or the GitHub settings, which stay with the developer. Every file is reviewed as a diff before commit (Article VIII) |

The files under `.github/` are not an exception: the developer writes them by hand (spec Q9).
