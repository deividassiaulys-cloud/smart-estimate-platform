# Tasks: Deployment and Production Readiness

**Input**: spec.md, plan.md in `specs/022-deployment/`; deployment settings from docs/architecture.md section 4 and docs/security-review.md sections 7–9

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering. Feature 020 ended at T356; feature 021 used the review's own IDs T001–T019. Phases follow the PR order in plan.md.

**Who does what** (spec Q9, Constitution Article VIII):
- **Agent**: only `server/`, `client/`, `tests/`, `docs/` and `specs/`. README.md is the one exception, and only with approval (plan Complexity Tracking).
- **Developer, by hand**: every file under `.github/`, the server, the GitHub settings, the tag and the release. For these tasks the agent lists what the file must contain; it does not create or edit the file.
- **`deploy/` exception (approved 2026-11-24, plan Complexity Tracking)**: the agent writes `deploy/docker-compose.yml`, `deploy/Caddyfile`, `deploy/env.example` and `deploy/deploy.sh` (T364, T365, T369, T370), and the developer reviews each as a diff before commit. Where these tasks say "Developer, by hand", read "Agent (deploy/ exception)".
- **`.env` files** (Article VIII): the developer creates every `.env` by hand, the local `deploy/.env` as well as the server's `~/smart-estimate/.env`. The agent never creates, edits, reads or prints one. When the agent runs a local check, it only runs `docker compose` commands that use the existing file. Any value it needs comes from `docker compose config --services` or from the developer, never from `cat .env`.

**Rule for every task**:
- After each code task, `dotnet build` (0 warnings), `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass, and so must the CI run on the PR.
- A new test is written first and must fail before the change, except for tasks that only add checks.
- This feature has no migration (Constitution VIII).
- The plan's proposals P1–P6 must have a decision in the spec's Clarifications table before Phase 2 starts; no row may still read Pending. P5 and P6 change Phase 1 (T357, T358), so they are decided before T357.

## Phase 1: PR `022-health-and-config` — User Story 1 (P1) 🎯 first

**Goal**: the API has health endpoints, trusts forwarded headers only from the proxy network, and writes JSON logs with a correlation id (FR-022-1 to FR-022-4).
**Independent Test**: spec US1.
**Who does what**: agent only.

- [X] T357 [US1] Health endpoints (plan R1): `Data/DatabaseHealthCheck.cs` (`CanConnectAsync`); `/health` (no checks, writes `status` and `version` from `APP_VERSION`, default `dev`); `/health/ready` (the `ready` check, default plain-text writer). Neither has `[Authorize]` or a rate-limit policy. Test: `HealthEndpointTests.Health_WithoutToken_ReturnsStatusAndVersion`, `HealthEndpointTests.Ready_WithDatabase_Returns200`, `HealthEndpointTests.Ready_WithBrokenDatabase_Returns503WithoutDetails` (a factory whose SQLite connection cannot open; the body is exactly `Unhealthy`), `HealthEndpointTests.Health_WithBrokenDatabase_StillReturns200`; `AnonymousAccessTests.TheListHoldsEveryProtectedEndpointOfTheApi` keeps passing unchanged.
- [X] T358 [US1] Forwarded headers (plan R2): `ReverseProxy:TrustedNetwork` (CIDR), `XForwardedFor | XForwardedProto`, `ForwardLimit = 1`, only that network in `KnownIPNetworks`, `UseForwardedHeaders()` first in the pipeline. Outside Development, refuse to start without the key. `ApiFactory` sets a test network, and a test start-up filter sets `Connection.RemoteIpAddress`. Test: `ForwardedHeadersTests.TrustedProxy_RateLimitUsesTheForwardedAddress` (limit 3: 3 logins as `203.0.113.7`, then the 4th as `203.0.113.7` gets 429 and one as `203.0.113.8` does not), `ForwardedHeadersTests.UntrustedSource_ForwardedHeadersAreIgnored`, `ForwardedHeadersTests.ForwardedHttps_IsNotRedirectedAndGetsHsts`, `ForwardedHeadersTests.MissingTrustedNetwork_StopsTheAppOutsideDevelopment`; the `HostFilteringTests`, `HstsTests` and `RateLimitTests` keep passing.
- [X] T359 [US1] Correlation id (plan R3): middleware next to the security-headers middleware sets `X-Correlation-ID` (the trace id) through `Response.OnStarting`. `CapturingLoggerProvider` learns to record scopes (`ISupportExternalScope`). Test: `CorrelationIdTests.EveryResponse_HasACorrelationId` (200, 401, 404), `CorrelationIdTests.ProblemDetailsTraceId_ContainsTheCorrelationId`, `CorrelationIdTests.LogEntriesOfARequest_CarryTheCorrelationId` (the 404 warning of `DomainExceptionHandler` has the response's id in its scope).
- [X] T360 [US1] JSON console logs outside Development (plan R3): `AddJsonConsole` with `IncludeScopes` and UTC timestamps; Development keeps the simple console. Test: `ProductionLoggingTests.OutsideDevelopment_ConsoleIsJsonWithScopes` (resolves `ConsoleLoggerOptions.FormatterName == "json"` and `JsonConsoleFormatterOptions.IncludeScopes`), `ProductionLoggingTests.Requests_LogNoKeyOrConnectionString` (start, register, login, fail a login, and check that no captured entry contains the JWT key, the connection string, the password or the issued token); the `SecurityLoggingTests` keep passing.
- [X] T361 [US1] docs/architecture.md section 4: rows for `ReverseProxy__TrustedNetwork` and `APP_VERSION`, the health endpoints, and the "Reverse proxy" note rewritten as done (feature 022, T358). Check: the documented setting names match `Program.cs` (`grep`); full check set green.

**Status notes (2026-11-24)**:
- **T357**: `Data/DatabaseHealthCheck.cs` (`CanConnectAsync`, healthy or unhealthy, no reason reported) and `Controllers/HealthEndpoints.cs` (`AddHealthEndpoints`, `MapHealthEndpoints`, next to `AuthRateLimiting`).
  - `/health` runs no check and writes `{"status":"Healthy","version":…}`, taking the version from `APP_VERSION` (`dev` when empty).
  - `/health/ready` runs only the `ready`-tagged check with the default writer.
  - Tests: `HealthEndpointTests.Health_WithoutToken_ReturnsStatusAndVersion`, `Health_WithoutConfiguredVersion_ReportsDev`, `Ready_WithDatabase_Returns200`, `Ready_WithBrokenDatabase_Returns503WithoutDetails` (a read-only SQLite file that does not exist; body exactly `Unhealthy`) and `Health_WithBrokenDatabase_StillReturns200`. All 5 failed before (404).
  - `AnonymousAccessTests.TheListHoldsEveryProtectedEndpointOfTheApi` passes unchanged, because the health endpoints have no `IAuthorizeData`.
- **T358**: `Controllers/ReverseProxyTrust.cs` (`AddReverseProxyTrust`) reads `ReverseProxy:TrustedNetwork`.
  - In Development an empty value turns forwarded headers off. Otherwise the value must parse as a CIDR (`System.Net.IPNetwork.TryParse`), or start-up throws `InvalidOperationException`.
  - Options: `XForwardedFor | XForwardedProto`, `ForwardLimit = 1`, default `KnownProxies` and `KnownIPNetworks` cleared, only the configured network added.
  - `UseForwardedHeaders()` is the first middleware. `ApiFactory` trusts `10.99.0.0/24` (`ApiFactory.TrustedNetwork`), and the new `TestSupport/TestRemoteAddress` start-up filter sets the sender address from the `X-Test-Remote-Ip` header.
  - Tests: `ForwardedHeadersTests.TrustedProxy_RateLimitUsesTheForwardedAddress`, `ForwardedHttps_IsNotRedirectedAndGetsHsts`, and `MissingTrustedNetwork_StopsTheAppOutsideDevelopment` (empty and `not-a-network`). All failed before.
  - Two guard tests passed before and after: `UntrustedSource_ForwardedHeadersAreIgnored` and `MissingTrustedNetwork_IsAllowedInDevelopment`.
- **T359**: an inline middleware in `Program.cs`, after the security-headers middleware, sets `X-Correlation-ID` to `Activity.Current.TraceId` (falling back to `TraceIdentifier`) through `Response.OnStarting`.
  - `CapturingLoggerProvider` now implements `ISupportExternalScope`, and `LogEntry` has a `Scopes` dictionary; the existing logging tests pass unchanged.
  - Tests: `CorrelationIdTests.EveryResponse_HasACorrelationId` (200, 401, 404; 32 hex characters, a different id per request), `ProblemDetailsTraceId_ContainsTheCorrelationId` and `LogEntriesOfARequest_CarryTheCorrelationId` (the `DomainExceptionHandler` warning's `TraceId` scope equals the header). All 3 failed before.
- **T360**: outside Development `builder.Logging.AddJsonConsole` is used, with `IncludeScopes`, `UseUtcTimestamp` and the timestamp format `yyyy-MM-ddTHH:mm:ss.fffZ`.
  - Tests: `ProductionLoggingTests.OutsideDevelopment_ConsoleIsJsonWithScopes` failed before. Two check tests passed before and after: `InDevelopment_ConsoleIsNotJson` and `Requests_LogNoKeyOrConnectionString`. The latter registers, logs in, fails a login and gets a 404, then checks that no message, value or scope contains the JWT key, the connection string, either password or either token.
  - Manual check: the API was started from the build output in Production, with dummy values and an unreachable MySQL port. Results:
    - `/health` gave 200 `{"status":"Healthy","version":"localcheck"}`;
    - `/health/ready` gave 503 `Unhealthy`;
    - `/api/auth/me` gave 401;
    - all 8 log lines were JSON, the `TraceId` scope of the failed readiness check equalled its `X-Correlation-ID`, and no line contained the dummy password or key.
- **T361**: docs/architecture.md section 4 has rows for `ReverseProxy__TrustedNetwork` and `APP_VERSION`. The "Reverse proxy" note now says it is done, and new notes cover the health endpoints and the logs. The setting names match the code (`grep`).
- **Checks**: `dotnet build` 0 warnings; `dotnet test` 478 passed (461 + 17); `npm run lint` clean; `npm run build` passes; `npm run test` 125 passed.

## Phase 2: PR `022-docker` — User Story 2 (P1)

**Goal**: the API and web images and the Compose stack run locally exactly as they will on the server (FR-022-5 to FR-022-7).
**Independent Test**: spec US2.
**Who does what**: the agent writes the Dockerfiles in `server/` and `client/`. The developer writes `deploy/docker-compose.yml`, `deploy/Caddyfile`, `deploy/env.example` and the CI job by hand.

- [X] T362 [P] [US2] `server/Dockerfile` (stages `build` and `final`; the `bundle` stage follows in T368) and `server/.dockerignore` (plan R4): `aspnet:10.0` final image, `USER $APP_UID`, port 8080, `ARG`/`ENV APP_VERSION`. **Agent.** Check:
  - `docker build --build-arg APP_VERSION=local -t smart-estimate-api:local server/` succeeds;
  - `docker run --rm --entrypoint id smart-estimate-api:local -u` is not `0`;
  - `docker run --rm --entrypoint ls smart-estimate-api:local -R /app` shows no `*.cs`, no `appsettings.Development.json` and no test assembly;
  - `docker history --no-trunc` shows no secret.
- [X] T363 [P] [US2] `client/Dockerfile` (`node:22-alpine` build, then `caddy` with `dist` in `/srv`) and `client/.dockerignore` (plan R4). **Agent.** Check: `docker build -t smart-estimate-web:local client/` succeeds, and `docker run --rm --entrypoint ls smart-estimate-web:local /srv /srv/assets` lists `index.html` and hashed assets.
- [X] T364 [US2] `deploy/docker-compose.yml` and `deploy/env.example` (plan R5). **Developer, by hand.** The Compose file must have:
  - the services `mysql` (`mysql:8.4`, volume `mysql-data`, `mysqladmin ping` health check), `api` and `caddy`, with `migrate` following in T369;
  - the `internal` network with subnet `172.30.0.0/24`;
  - the `api` environment from plan R5 (`ASPNETCORE_ENVIRONMENT=Production`, `AllowedHosts=${SITE_ADDRESS}`, `ReverseProxy__TrustedNetwork=172.30.0.0/24`, connection string, `Jwt__Key`);
  - `api` waiting on `mysql` (`condition: service_healthy`);
  - only `caddy` publishing ports (80, 443, 443/udp), with volumes `caddy-data` and `caddy-config`;
  - `json-file` logs with `max-size: 10m` and `max-file: 3`;
  - `restart: unless-stopped`;
  - images `ghcr.io/<owner>/smart-estimate-{api,web}:${IMAGE_TAG}`.

  `env.example`: every variable of plan R8 with placeholder values (`change-me`) and the `openssl rand -hex 32` hint. Check:
  - `docker compose -f deploy/docker-compose.yml --env-file deploy/env.example config` passes;
  - `git check-ignore deploy/.env` confirms the real file is ignored;
  - `env.example` contains no real value (reviewed in the PR diff).
- [X] T365 [US2] `deploy/Caddyfile` (plan R5). **Developer, by hand.** It must have:
  - `{$SITE_ADDRESS}`;
  - `/api/*` and `/health*` sent to `api:8080`, with no header changes there;
  - client fallback to `index.html`;
  - client headers (CSP from docs/architecture.md section 4, `nosniff`, `no-referrer`, `X-Frame-Options: DENY`, HSTS);
  - `Cache-Control: no-cache` on `index.html` and an immutable cache on `/assets/*`;
  - `encode zstd gzip`.

  Check: `docker run --rm -e SITE_ADDRESS=localhost -v "$PWD/deploy/Caddyfile:/etc/caddy/Caddyfile:ro" smart-estimate-web:local caddy validate --config /etc/caddy/Caddyfile` passes.
- [X] T366 [US2] Local stack check. **Developer**: first creates the local `deploy/.env` by hand from `deploy/env.example` (`SITE_ADDRESS=localhost`, local-only values from `openssl rand -hex 32`). The agent does not create, edit or read this file (Article VIII). The developer runs the checks; the agent may then run the same `docker compose` and `curl` commands against the running stack and record the results. Images are tagged `IMAGE_TAG=local` from T362/T363. Check:
  - `docker compose up -d` starts `mysql`, `api` and `caddy`.
  - With `--cacert` set to Caddy's local root certificate (`/data/caddy/pki/authorities/local/root.crt`, copied out of the container):
    - `https://localhost/` returns `index.html` with the client CSP and `Cache-Control: no-cache`;
    - `https://localhost/some/client/route` returns `index.html` too;
    - `https://localhost/api/auth/me` returns 401 with the API's CSP (`default-src 'none'`) and an `X-Correlation-ID`;
    - `https://localhost/health` reports `"version":"local"`, and `/health/ready` is 200.
  - `http://localhost/` redirects to HTTPS.
  - `docker compose ps` and `ss -tlnp`/`lsof -i` show only 80 and 443 published.
  - `docker compose logs api` shows JSON lines.
- [ ] T367 [US2] CI image build. **Developer, by hand**: a job `docker` in `.github/workflows/ci.yml` builds `server/` and `client/` with `docker/setup-buildx-action` and `docker/build-push-action` (`push: false`); the developer adds `docker` to the required checks of the ruleset `main-protection`. Check:
  - the PR's CI run shows `backend`, `frontend` and `docker` green;
  - on a throwaway branch, a deliberately broken `server/Dockerfile` turns `docker` red;
  - the ruleset lists `docker` (GitHub API, as checked for feature 021).

**Status notes (2026-11-24)**:
- **T362**: `server/Dockerfile` has two stages, `build` (`sdk:10.0`; restore the API project first, then `dotnet publish -c Release --no-restore /p:UseAppHost=false`) and `final` (`aspnet:10.0`).
  - The final image sets `ARG`/`ENV APP_VERSION` (default `dev`), `ASPNETCORE_HTTP_PORTS=8080` and `USER $APP_UID`, with the entry point `dotnet SmartEstimatePlatform.Api.dll`.
  - The `bundle` stage follows in T368.
  - `server/.dockerignore` excludes `bin/`, `obj/`, `.env*`, `appsettings.Development.json`, `appsettings.*.local.json`, `*.user`, IDE folders, `.mcp.json` and `.DS_Store`.
  - Checks on Docker 29.7.2 (arm64):
    - `docker build --build-arg APP_VERSION=local -t smart-estimate-api:local server/` succeeds (394 MB);
    - `id -u` is `1654` (user `app`), not 0;
    - `/app` has no `*.cs`, `*.csproj`, `appsettings.Development.json`, test assembly or `.env`, and its `appsettings.json` is the tracked one, with no key or connection string;
    - `dotnet --list-sdks` lists nothing, so there is no SDK;
    - `docker history --no-trunc` contains no key, password, secret or connection string;
    - the image environment has only `APP_UID`, `ASPNETCORE_HTTP_PORTS`, `DOTNET_*`/`ASPNET_VERSION` (runtime 10.0.15) and `APP_VERSION=local`.
  - Run on its own in Production with dummy settings and no database: `/health` gave 200 `{"status":"Healthy","version":"local"}` with `X-Correlation-ID` and the API CSP, `/health/ready` gave 503 and `/api/auth/me` gave 401.
  - **Observation**: at start-up the container logs two Data Protection warnings: keys are stored in `/home/app/.aspnet/DataProtection-Keys`, which is not persisted, and no XML encryptor is configured. The API does not use Data Protection, because it has no cookies or antiforgery and uses JWT Bearer tokens, so a lost key ring has no effect. The warnings are expected; docs/deployment.md (T379) will say so. No code change, because this is not in the spec.
- **T363**: `client/Dockerfile` has two stages, `build` (`node:22-alpine`, the CI Node version; `npm ci`, then `npm run build`) and `final` (`caddy:2.11-alpine`, with `dist` copied to `/srv`).
  - `caddy:2.11-alpine` is the newest Caddy minor on Docker Hub on 2026-11-24 (updated 2026-11-13; no 2.12). It is pinned to the minor version, so patch releases still arrive (plan: Primary Dependencies, A1 of the analysis).
  - `client/.dockerignore` excludes `node_modules/`, `dist/`, `.vite/`, `*.local`, `.env*`, logs and IDE folders.
  - Check: `docker build -t smart-estimate-web:local client/` succeeds (88 MB), and `/srv` holds `index.html`, `favicon.svg`, `icons.svg` and `assets/` with the hashed `index-*.js` and `index-*.css`.
- **T364** (agent, `deploy/` exception approved 2026-11-24): `deploy/docker-compose.yml`.
  - Services `mysql`, `api` and `caddy`; `migrate` follows in T369.
  - `mysql` (`mysql:8.4`): volume `mysql-data`, and a health check `mysqladmin ping --host=127.0.0.1`, which goes over TCP and so passes only once the first-start initialisation is over. No password appears on the command line.
  - The API environment is one YAML anchor (`x-api-environment`), so T369's `migrate` can reuse it exactly. `api` waits for a healthy `mysql`.
  - Only `caddy` publishes ports (80, 443, 443/udp), with volumes `caddy-data` and `caddy-config` and the Caddyfile mounted read-only.
  - `internal` network with subnet `172.30.0.0/24`, the same value as `ReverseProxy__TrustedNetwork`.
  - Every service has `json-file` logs (10 MB × 3) and `restart: unless-stopped`.
  - Images `ghcr.io/deividassiaulys-cloud/smart-estimate-{api,web}:${IMAGE_TAG}`.
  - Every variable uses `${…:?message}`, so a missing value stops Compose with a clear message.
  - **Change from the task text**: the example file is `deploy/env.example`, not `deploy/.env.example`, because the root `.gitignore` pattern `.env.*` would ignore it. Editing the root `.gitignore` is outside the approved exception. All references in spec, plan and tasks were renamed.
  - Checks:
    - `IMAGE_TAG=check docker compose -f deploy/docker-compose.yml --env-file deploy/env.example config --quiet` passes;
    - `git check-ignore -v deploy/.env` gives `.gitignore:20:.env`, so the real file is ignored;
    - `deploy/env.example` is not ignored and holds only `change-me` placeholders.
- **T365** (agent, same exception): `deploy/Caddyfile`.
  - Site `{$SITE_ADDRESS}` with `encode zstd gzip` and a JSON access log on stdout.
  - `handle /api/*` and `handle /health*` go to `reverse_proxy api:8080` with no headers, so the API keeps its own CSP.
  - The client `handle` serves `root /srv` with `try_files {path} /index.html` and `file_server`, and sends: the CSP of docs/architecture.md section 4, `nosniff`, `no-referrer`, `X-Frame-Options: DENY` and HSTS, with the `Server` header removed.
  - `Cache-Control`: `public, max-age=31536000, immutable` for `/assets/*`, `no-cache` for everything else (index.html and client routes).
  - A commented-out `acme_ca` line points to Let's Encrypt's staging CA.
  - Checks: `caddy validate` gives "Valid configuration", and `caddy fmt` makes no changes.
  - The client was checked first for inline styles or scripts, which the CSP would block: none. Only the unused template file `src/assets/vite.svg` has a `<style>`.
- **T366** (local stack, agent-run). No `.env` file was created (Article VIII).
  - Throwaway local values came from the shell environment, under a separate Compose project `sep-localcheck`, using the images from T362/T363 re-tagged as `ghcr.io/deividassiaulys-cloud/smart-estimate-{api,web}:local`.
  - `mysql` reported healthy, then `api` and `caddy` started. Requests used `--cacert` with Caddy's local root certificate:
    - `https://localhost/`: HTTP/2 200 `index.html` with the client CSP, `Cache-Control: no-cache`, HSTS, `nosniff`, `DENY` and `no-referrer`;
    - `/projects/5`: `index.html`;
    - the hashed JS asset: `immutable` and `Content-Encoding: gzip`;
    - `/api/auth/me`: 401 with the API CSP (`default-src 'none'`) and `X-Correlation-ID`;
    - `/health`: `{"status":"Healthy","version":"local"}`;
    - `/health/ready`: 200;
    - `http://localhost/`: 308 to `https://localhost/`;
    - another host name: TLS refused (no certificate).
  - Ports: only Caddy publishes 80 and 443; `api` (8080) and `mysql` (3306) are internal only. The `mysqld *:3306` on the Mac is the developer's own MySQL, not a container.
  - Logs:
    - `docker compose logs api` shows 6 lines, all JSON;
    - Caddy's access log shows `Authorization: ["REDACTED"]` for a request sent with a test token, which does not appear in any log, and records the API's `X-Correlation-Id`;
    - no throwaway password or key appears in any container log.
  - The API sent no HSTS for `localhost`, because ASP.NET Core never sends HSTS to localhost (as `HstsTests` notes); Caddy's HSTS covers the client.
  - Stopped with `docker compose -p sep-localcheck down -v`, which removed the volumes.
  - Not checked here: data surviving `down`/`up` needs a schema, so it moves to T369 (analysis finding G1).
- **T367**: waiting for the developer (`.github/workflows/ci.yml` and the ruleset; the `deploy/` exception does not cover `.github/`).
- **Observation for T386/T380** (not part of this feature's tasks): the client's page title is still the Vite template's `client` (`client/index.html`). NFR-012 asks for a Lithuanian interface, so this should be noted in the requirements verification.

## Phase 3: PR `022-db-migrations` — User Story 3 (P1)

**Goal**: every deployment backs up the database, then migrates it, and starts the new API only if the migration succeeded (FR-022-8).
**Independent Test**: spec US3.
**Who does what**: the agent adds the bundle stage to `server/Dockerfile`. The developer writes the `migrate` service and `deploy/deploy.sh` by hand.

- [X] T368 [US3] `bundle` stage in `server/Dockerfile`: `dotnet tool install dotnet-ef --version 10.0.12`, `dotnet ef migrations bundle -c Release -o /out/efbundle`, copied into the final image as `/app/efbundle` (plan R4, R6). **Agent.** Check, against a throwaway `mysql:8.4` container on a Docker network, with the same environment as `api`:
  - the first run of `efbundle --connection …` exits 0;
  - `SELECT COUNT(*) FROM __EFMigrationsHistory` equals the number of `*.Designer.cs` files in `server/SmartEstimatePlatform.Api/Migrations`;
  - a second run prints that no migrations were applied and exits 0;
  - without `AllowedHosts`, the bundle fails. This confirms that it needs the API's environment (plan R6).
- [X] T369 [US3] `migrate` service in `deploy/docker-compose.yml`: the API image, `command: ["./efbundle"]`, `profiles: [migrate]`, `depends_on: mysql (service_healthy)`, and exactly the `api` environment (a YAML anchor or `env_file`, so the two cannot drift). **Developer, by hand.** Check, on the local stack from T366:
  - `docker compose --profile migrate run --rm migrate` exits 0, after which registering through `https://localhost/api/auth/register` gives 201;
  - **data persists** (spec US2 scenario 4): after `docker compose down` (without `-v`) and `docker compose up -d`, logging in with the account just registered gives 200, and `docker volume ls` still lists `mysql-data`;
  - **application user, not root** (spec US3 scenario 5):
    - `docker compose config` shows that neither the `migrate` nor the `api` environment contains `MYSQL_ROOT_PASSWORD`;
    - their connection strings use `User=${MYSQL_USER}`, not `root`;
    - in MySQL, `SELECT user, host FROM mysql.user` lists the application user, and `SHOW GRANTS FOR '<MYSQL_USER>'@'%'` grants privileges only on `${MYSQL_DATABASE}.*`.
- [X] T370 [US3] `deploy/deploy.sh` (plan R6). **Developer, by hand.** It runs:
  1. `set -euo pipefail`;
  2. `docker compose pull`, skipped with `PULL=0` for local runs;
  3. `mysqldump` (application user, `--single-transaction`) gzipped to `/var/backups/smart-estimate/<UTC>-<tag>.sql.gz`, keeping the last 7 (locally, a `BACKUP_DIR` override);
  4. `migrate`, which on failure prints `Migration failed; API not restarted; backup: <path>` and exits non-zero;
  5. `docker compose up -d api caddy`;
  6. a wait of up to 60 s for `/health/ready`, then a check that `/health` reports `IMAGE_TAG`;
  7. printing the previous tag, then writing the new one to `.deployed-tag`.

  Check, on the local stack:
  - `PULL=0 IMAGE_TAG=local ./deploy.sh` exits 0;
  - the dump exists, and `gunzip -c` shows `CREATE TABLE` for every table;
  - `.deployed-tag` is `local`;
  - restore drill: loading the dump into a scratch database gives the same row counts per table as the source.
- [X] T371 [US3] Migration failure drill (SC-022-4). Locally:
  1. Build the API image from the commit before `LimitPersonAndCompanyNames` as `IMAGE_TAG=old` and deploy it on an empty volume.
  2. Insert a user whose `FirstName` has 101 characters.
  3. Build the current commit as `IMAGE_TAG=new` and run `PULL=0 IMAGE_TAG=new ./deploy.sh`.

  Check:
  - the script exits non-zero and prints the backup path;
  - `docker compose ps --format '{{.Service}} {{.Image}}'` still shows `api` on `:old`;
  - `/health` still reports `old`;
  - after the long name is removed, the same command succeeds.

  Record the commands and output in the status notes. No migration file is created.

**Status notes (2026-11-24)**:
- **T368**: new `bundle` stage in `server/Dockerfile`, built `FROM build`.
  - It installs `dotnet tool install --global dotnet-ef --version 10.0.12`, then runs `dotnet ef migrations bundle --configuration Release --output /out/efbundle`. The final image gets `/app/efbundle` (44 MB, executable; image 452 MB).
  - **Deviation found while building**: the bundle build creates the `AppDbContext` once, and `Program.cs` requires a connection string. The step therefore runs with `ASPNETCORE_ENVIRONMENT=Development` and the placeholder `ConnectionStrings__DefaultConnection="Server=build-placeholder;Database=none"`. The placeholder is not a secret, nothing connects, and the variable exists only in that `RUN` step: `docker history` of the final image does not contain it. The first build failed with "Connection string 'DefaultConnection' is not configured".
  - `efbundle --help` reports "Entity Framework Core Migrations Bundle 10.0.12".
  - Check against a throwaway `mysql:8.4` on its own Docker network, with the API's environment:
    - the first run applied all 10 migrations and exited 0;
    - `__EFMigrationsHistory` has 10 rows, matching the 10 `*.Designer.cs` files, with 12 tables plus the history;
    - the second run gave "No migrations were applied. The database is already up to date." and exited 0;
    - without `AllowedHosts` it exited 1 ("AllowedHosts must list …", then "Unable to create a 'DbContext'"), which confirms plan R6: the bundle needs exactly the API's environment.
- **T369** (agent, `deploy/` exception): `migrate` service in `deploy/docker-compose.yml`.
  - It uses the API image, `profiles: [migrate]`, `entrypoint: ["/app/efbundle"]`, `environment: *api-environment` (the same YAML anchor as `api`, so the two cannot drift) and a dependency on a healthy `mysql`.
  - **Deviation**: `entrypoint` instead of the planned `command: ["./efbundle"]`, because the image's `ENTRYPOINT` is `dotnet SmartEstimatePlatform.Api.dll`, so a `command` would only be passed to it as arguments. Extra arguments still reach the bundle, for example a target migration (used in T371).
  - Plain `docker compose config --services` lists `mysql api caddy`; with `--profile migrate` it also lists `migrate`.
  - **Added**: `MYSQL_ROOT_HOST: localhost`. The image otherwise creates `root@%`, so any container on the network could try root. On a fresh volume only `root@localhost` exists; checked in T371.
  - Checks, on the local stack (Compose project `sep-localcheck`, throwaway values from the shell, no `.env` file):
    - after `deploy.sh`, registering through `https://localhost/api/auth/register` gave 201;
    - **persistence** (G1): after `docker compose down` without `-v`, the three volumes remained; after `up -d --wait`, logging in with the same account gave 200;
    - **application user** (G2): in `docker compose config`, neither `api` nor `migrate` has `MYSQL_ROOT_PASSWORD`, and both connection strings use `User=smart_estimate` and not `root`;
    - `SHOW GRANTS FOR 'smart_estimate'@'%'` shows `USAGE ON *.*` and `ALL PRIVILEGES ON smart_estimate.*` only.
- **T370** (agent, `deploy/` exception): `deploy/deploy.sh` (`set -euo pipefail`, executable).
  - Steps:
    1. optional pull (`PULL=0` skips it);
    2. `up -d --wait mysql`;
    3. `mysqldump --single-transaction --no-tablespaces` as the application user, with the password read inside the container from its own environment and not put on the host command line. The dump is written to `.partial`, then renamed and set to mode 600. Only the newest `KEEP_BACKUPS` (default 7) are kept;
    4. `docker compose --profile migrate run --rm migrate`; on failure it prints "Migration failed; API not restarted; backup: …", "The previous version (…) keeps running." and exits 1;
    5. `up -d api caddy`;
    6. a wait of up to 60 s for `/health/ready` = `Healthy` and `/health` reporting `IMAGE_TAG`;
    7. writing the tag to `DEPLOYED_TAG_FILE` (default `.deployed-tag`) and printing the previous one.
  - **Deviation**: the readiness wait asks the API from inside the network, through the Caddy container (`wget --header "Host: $SITE_ADDRESS" http://api:8080/health…`), not through `https://$SITE_ADDRESS`. It then does not depend on DNS or the certificate, and the public address is what the smoke test checks (T372).
  - **Added**: the `DEPLOYED_TAG_FILE` option, so a local run does not leave `.deployed-tag` in the repository.
  - `shellcheck` 0.11.0 (run from the `koalaman/shellcheck:stable` image) is clean, with one documented `SC2012` disable for `ls -t` on the script's own file names.
  - Checks:
    - the first run on an empty volume applied 10 migrations, exited 0 and wrote the tag file (`local`);
    - the second run gave "No migrations were applied", exit 0, and a dump with 13 `CREATE TABLE` statements and the registered user;
    - **restore drill**: the dump was loaded as root into a scratch database `restore_check`. Companies 1, Users 1, migrations 10 and tables 13 match the source, and the scratch database was dropped afterwards;
    - **retention** (G3): with 2 existing dumps and `KEEP_BACKUPS=2`, the next run removed the oldest and left 2.
- **T371** (migration failure drill, SC-022-4).
  - **Deviation from the task text** (equivalent, simpler): instead of building an image from the commit before `LimitPersonAndCompanyNames`, which has no `/health`, the drill used the current image built twice, as `:old` (`APP_VERSION=old`) and `:new`. On a fresh volume the bundle was given a target, `migrate 20261117083815_AddSections`, so 9 of 10 migrations were applied.
  - Steps:
    1. start `api`/`caddy` as `:old` and register a user (201);
    2. `UPDATE Users SET FirstName=REPEAT('x',101)`;
    3. run `IMAGE_TAG=new ./deploy.sh`.
  - Result:
    - the backup was taken, then `LimitPersonAndCompanyNames` failed with "Data too long for column 'FirstName' at row 1";
    - the script printed the backup path and "The previous version (old) keeps running." and exited **1**;
    - `docker compose ps` still showed `api …:old` and `caddy …:old`, `/health` still reported `"version":"old"`, and the tag file still said `old`.
  - **Confirmed edge case** (spec Edge Cases, MySQL DDL is not transactional): after the failure `Users.LastName` was already `varchar(100)`, while `FirstName` was still `longtext` and the history still had 9 rows. The migration had been half applied, which is why the dump comes first.
  - After `UPDATE Users SET FirstName='Ona'`, the same command applied the migration and exited 0. Afterwards:
    - 10 history rows;
    - `Users.FirstName`/`LastName` are `varchar(100)` and `Companies.Name` is `varchar(200)`;
    - `api`/`caddy` run `:new`, `/health` reports `new` and login gives 200;
    - no throwaway password or key appears in the drill output.
  - On a fresh volume, `mysql.user` has root only at `localhost` (T369 addition).
  - No migration file was created. The stack was removed with `down -v`, along with the `:old`/`:new` images.
- **Checks**: no application code changed in this PR (`server/Dockerfile` and `deploy/` only), so the test counts are unchanged (478 backend, 125 frontend); CI runs them on the PR.

## Phase 4: PR `022-deploy-pipeline` — User Story 4 (P1)

**Goal**: every green CI run on `main` is built, deployed over SSH and smoke-tested (FR-022-9 to FR-022-11).
**Independent Test**: spec US4.
**Who does what**: the agent writes the smoke test in `tests/smoke/`. The developer sets up the server and the GitHub Environment and writes `.github/workflows/deploy.yml` by hand.

- [ ] T372 [US4] `tests/smoke/smoke-test.sh` (plan R9): inputs `BASE_URL`, `EXPECTED_VERSION` (optional) and `SMOKE_CA_CERT` (optional); the 8 steps of spec US4 scenario 3; `ok <n> <name>` / `FAIL <n> <name>: …`; random passwords; `.invalid` emails; no token or password printed. **Agent.** Check against the local stack:
  - all 8 steps print `ok` and the script exits 0;
  - `EXPECTED_VERSION=wrong` gives `FAIL 1` and exit 1;
  - with the `api` container stopped, it fails at step 1;
  - the captured output contains no `eyJ` (JWT prefix) and none of the generated passwords;
  - `shellcheck tests/smoke/smoke-test.sh` reports nothing, if shellcheck is installed; otherwise this is noted in the status notes.
- [ ] T373 [US4] Server and GitHub Environment (plan R8, Security Risks). **Developer, by hand**, with no file in the repository:
  - an Ubuntu 24.04 VPS;
  - the user `deploy`, in the `docker` group, with an ed25519 deploy key;
  - `PasswordAuthentication no` and `PermitRootLogin no`;
  - ufw with only 22, 80 and 443;
  - `unattended-upgrades`;
  - Docker Engine and the Compose plugin from Docker's apt repository;
  - `~/smart-estimate/.env` (mode 0600, values from `openssl rand -hex 32`, `SITE_ADDRESS=<ip>.sslip.io`) and `/var/backups/smart-estimate/`;
  - the GitHub Environment `production`: secrets `SSH_HOST`, `SSH_USER`, `SSH_PRIVATE_KEY` and `SSH_KNOWN_HOSTS` (from `ssh-keyscan`, checked against the provider's console fingerprint), variable `SITE_ADDRESS`, and deployment branches limited to `main`.

  Check:
  - `ssh -i <deploy key> deploy@<host> docker compose version` works;
  - `ssh root@<host>` and password login are refused;
  - `sudo ufw status` lists only 22, 80 and 443;
  - `stat -c %a ~/smart-estimate/.env` is `600`;
  - the Environment page shows 4 secrets, 1 variable and the `main` rule.
- [ ] T374 [US4] `.github/workflows/deploy.yml` (plan R7). **Developer, by hand**:
  - `workflow_run` of `CI` on `main`, run only on `success`, with the concurrency group `production`;
  - job `images` (`packages: write`), which pushes both images tagged with `head_sha` and `APP_VERSION=head_sha`;
  - job `deploy` (`environment: production`, `contents: read`), which uses `known_hosts` from the secret, runs `scp` of the three `deploy/` files, then `ssh … IMAGE_TAG=<sha> ./deploy.sh`;
  - job `smoke`, which runs `tests/smoke/smoke-test.sh` with `BASE_URL=https://${{ vars.SITE_ADDRESS }}` and `EXPECTED_VERSION=<sha>`.

  There is no third-party SSH action and no `StrictHostKeyChecking=no`. Check: the PR diff is reviewed against plan R7, and `actionlint` passes if it is available; the real proof is T375.
- [ ] T375 [US4] First deployment. **Developer**:
  1. Merge PR 4. CI runs, then the deploy workflow.
  2. After the first push, set both GHCR packages to public and re-run the workflow if the pull failed.

  Check:
  - the deploy run is green, and the `smoke` job log shows `ok 1` to `ok 8`;
  - `curl -sI http://$SITE_ADDRESS` gives a 308 to HTTPS;
  - `openssl s_client -connect $SITE_ADDRESS:443 -servername $SITE_ADDRESS` shows a Let's Encrypt issuer (SC-022-3);
  - the `/health` version equals the merge commit's SHA.
- [ ] T376 [US4] Exposure and headers on production. Check:
  - from outside, `nc -zv <host> 3306` and `nc -zv <host> 8080` fail;
  - `curl -sI https://$SITE_ADDRESS/` shows the client CSP, HSTS, `nosniff`, `no-referrer` and `DENY`;
  - `curl -sI https://$SITE_ADDRESS/api/auth/me` shows 401 with the API's CSP and `X-Correlation-ID`;
  - on the server, `docker compose logs api --since 10m` shows the smoke test's requests as JSON lines with `TraceId`, without tokens or passwords (`grep -c eyJ` is `0`).
- [ ] T377 [US4] Secret check (SC-022-5):
  - `git ls-files | grep -E '(^|/)\.env$'` is empty;
  - `env.example` holds only placeholders;
  - the history scan of docs/security-review.md section 7 is repeated and finds nothing new;
  - in production, `docker run --rm --entrypoint env ghcr.io/<owner>/smart-estimate-api:<sha>` shows no secret, because secrets come only from the running container's environment;
  - the workflow logs show `***` wherever a secret is used.
- [ ] T378 [US4] Rollback drill. **Developer**, on the server: run `IMAGE_TAG=<previous sha> ./deploy.sh`, then `IMAGE_TAG=<current sha> ./deploy.sh`, at a moment with no schema change between the two. Check: after each run, `/health` reports the expected SHA, and the smoke test, run from the developer's computer with `EXPECTED_VERSION` set, passes. The steps go into docs/deployment.md (T379).

**Status notes**: (filled in during implementation)

## Phase 5: PR `022-docs-release` — User Story 5 (P2)

**Goal**: the deployment is documented, every requirement is accounted for, and v1.0.0 is tagged (FR-022-12).
**Independent Test**: spec US5.
**Who does what**: the agent writes the docs. README.md is edited only if the student approves the exception. The developer runs the production manual checks with the agent recording them, and creates the tag and the release.

- [ ] T379 [P] [US5] docs/deployment.md. **Agent.** It covers:
  - a hosting diagram;
  - server prerequisites and the first-time setup of T373;
  - every `.env` and GitHub Environment name, with how each is generated and no values;
  - the deployment flow (CI → images → `deploy.sh` → smoke);
  - the migration rule (additive only, or accepted downtime) and backups, with restore steps and the T370 drill;
  - rollback (T378);
  - logs and how to follow one `X-Correlation-ID` through Caddy and the API;
  - the health endpoints and the smoke test;
  - smoke-company cleanup SQL;
  - switching `SITE_ADDRESS` to a real domain, using Let's Encrypt's staging CA first;
  - troubleshooting: certificate, 502, 503 from `/health/ready`, failed migration.

  Check: every command in the guide was run in T366–T378 (cross-checked against the status notes), and the guide holds no real host, IP or secret.
- [ ] T380 [P] [US5] docs/requirements-verification.md. **Agent.** One table row for each of the 66 IDs (FR-001–FR-034, NFR-001–NFR-014, BR-001–BR-018), with status (Met / Partially met / Deferred), evidence (test class and method names, a manual check with its date and task, a production smoke step) and, if not Met, the reason. FR-025, FR-028 and FR-029 are Deferred, as in specs/deferred-features.md. Check:
  - `grep -oE '^\| (FR|NFR|BR)-[0-9]{3}' docs/requirements-verification.md | sort -u | wc -l` is 66;
  - the same list equals the IDs in docs/requirements.md (`diff`);
  - every test name cited exists (`grep` in `tests/`) (SC-022-6).
- [ ] T381 [US5] README.md **(exception; needs the student's approval first, plan Complexity Tracking)**: "Project Status" (v1.0.0, deployed); a "Deployment" section (what runs where, the link to docs/deployment.md, no address or secret); the docker job and the deploy workflow in "Continuous Integration"; docs/deployment.md, docs/requirements-verification.md and docs/testing-strategy.md in "Documentation". Check: the status and CI text match `ci.yml` and `deploy.yml`. If approval is refused, the agent gives the text in the PR description and the developer pastes it by hand.
- [ ] T382 [P] [US5] docs/architecture.md: technology rows for Docker Compose, Caddy and GHCR; a new section 5, "Hosting", with the container diagram, the network and ports, and the request path, including same-origin `/api`. Check: names and ports match `deploy/docker-compose.yml` and `deploy/Caddyfile`.
- [ ] T383 [P] [US5] docs/security-review.md:
  - section 7: the deployment column filled with the real places (server `.env`, GitHub Environment), and the T377 scan;
  - section 8: forwarded headers OK, with the T358 test names; CORS (same origin confirmed in production, T376); the hosted-client CSP served (T376);
  - section 9: JSON logs and the correlation id (T359, T360);
  - section 10: the base images and the new actions;
  - the "Reviewed" line updated.

  Check: every cited test name exists (`grep`).
- [ ] T384 [P] [US5] docs/testing-strategy.md:
  - new test counts and the T357–T360 tests in section 1.5;
  - a smoke-test section;
  - section 4: "No end-to-end tests" narrowed (the smoke test drives the real API and MySQL, but there is still no browser test), and "Migrations are not tested" updated (the bundle runs on every deployment; drill T371);
  - section 5: the `docker` job and `deploy.yml`.

  Check: the counts match the output of `dotnet test` and `npm run test` on the PR.
- [ ] T385 [P] [US5] docs/releases/v1.0.0.md, the release notes. **Agent.** They list:
  - what is included (features 001–022, by user-facing capability);
  - how to reach and run it (link to docs/deployment.md);
  - known limitations (docs/security-review.md Accepted limitations, docs/testing-strategy.md section 4);
  - deferred features (FR-025, FR-028, FR-029).

  Check: every feature folder in `specs/` is mentioned, and nothing deferred is listed as included.
- [ ] T386 [US5] Production manual checks, recorded in docs/requirements-verification.md with date and result. **Developer**, with the agent recording:
  - NFR-008: 360 px, 1280 px and 1920 px on the live site;
  - NFR-009: an estimate with 200 lines loads in under 3 s and recalculates in under 1 s, measured in the browser's network and performance tools;
  - NFR-013: the two latest versions of Chrome, Edge, Firefox and Safari, as far as available; any browser that could not be tested is written down as such;
  - NFR-001: HTTPS only and a valid certificate (T375).

  Check: each row has a date and a measured value, or "not tested" with the reason.
- [ ] T387 [US5] Release v1.0.0. **Developer, by hand**, after PR 5 is merged and its deployment's smoke test is green:
  - `git tag -a v1.0.0 <deployed merge commit> -m "v1.0.0"`, then `git push origin v1.0.0`;
  - `gh release create v1.0.0 --notes-file docs/releases/v1.0.0.md`.

  Check: `git rev-parse 'v1.0.0^{commit}'` equals the `version` from `https://$SITE_ADDRESS/health`, and the Release page shows the notes.

**Status notes**: (filled in during implementation)

## Phase 6: Polish (after each PR)

Not new tasks: part of each PR's definition of done.

- Fill in the phase's status notes: commands run, results, test counts, deviations from the plan.
- Confirm the CI run on the PR is green. From PR 4 on, also confirm that the deployment after the merge is green with all 8 smoke steps.
- Never commit `deploy/.env`, a dump, a key or a real host address.

## Dependencies

- P5 and P6 need a decision before T357; P1–P4 before Phase 2 (spec Clarifications table). T381 needs the README exception approved.
- PR 1 → PR 2 → PR 3 → PR 4 → PR 5, strictly in this order:
  - the API needs T357 (health) and T358 (trusted network) before it can run behind Caddy in T366;
  - T368 adds to T362's Dockerfile;
  - T369 adds to T364's Compose file;
  - T372 is tried against the stack of T366/T369;
  - T374 uses T370's `deploy.sh` and T372's script;
  - PR 5 documents what T375–T378 proved.
- Inside PR 1: T357 → T358 → T359 → T360, all in `Program.cs` and `ApiFactory`; then T361.
- Inside PR 2: T362 and T363 in parallel → T364 → T365 → T366 → T367.
- Inside PR 3: T368 → T369 → T370 → T371.
- Inside PR 4: T372 and T373 in parallel → T374 → T375 → T376, T377 and T378.
- Inside PR 5: T379, T380 and T382–T385 in parallel; T381 after approval; T386 before T380 is final; T387 last, after the merge.

## Parallel Execution Examples

- PR 2: the agent writes both Dockerfiles (T362, T363) while the developer drafts the Compose file and Caddyfile (T364, T365) from plan R5.
- PR 4: the agent writes and tries the smoke test locally (T372) while the developer sets up the VPS and the GitHub Environment (T373).
- PR 5: the agent writes docs/deployment.md, the verification table, the architecture, security and testing updates and the release notes in parallel (T379, T380, T382–T385). The developer does the browser checks of T386 at the same time.

## Implementation Strategy

Make the API ready for a proxy first (PR 1), because everything later runs behind Caddy. Then package it (PR 2), make schema changes safe (PR 3), and only then automate deployment, with a smoke test that proves each one end to end (PR 4). Finish by writing down how it works and how every requirement was verified, and tag the exact commit that runs in production as v1.0.0 (PR 5). Each PR is small and carries its own tests or checks. Developer-only files under `.github/` and `deploy/` are reviewed in the same PR as the agent's work. Spec/plan/tasks commits stay separate from code commits (Article VI).
