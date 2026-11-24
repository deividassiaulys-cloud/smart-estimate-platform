# Tasks: Deployment and Production Readiness

**Input**: spec.md, plan.md in `specs/022-deployment/`; deployment settings from docs/architecture.md section 4 and docs/security-review.md sections 7–9

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering. Feature 020 ended at T356; feature 021 used the review's own IDs T001–T019. Phases follow the PR order in plan.md.

**Who does what** (spec Q9, Constitution Article VIII):
- **Agent**: only `server/`, `client/`, `tests/`, `docs/` and `specs/`. README.md is the one exception, and only with approval (plan Complexity Tracking).
- **Developer, by hand**: every file under `.github/` and `deploy/`, the server, the GitHub settings, the tag and the release. For these tasks the agent lists what the file must contain; it does not create or edit the file.
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

- [ ] T357 [US1] Health endpoints (plan R1): `Data/DatabaseHealthCheck.cs` (`CanConnectAsync`); `/health` (no checks, writes `status` and `version` from `APP_VERSION`, default `dev`); `/health/ready` (the `ready` check, default plain-text writer). Neither has `[Authorize]` or a rate-limit policy. Test: `HealthEndpointTests.Health_WithoutToken_ReturnsStatusAndVersion`, `HealthEndpointTests.Ready_WithDatabase_Returns200`, `HealthEndpointTests.Ready_WithBrokenDatabase_Returns503WithoutDetails` (a factory whose SQLite connection cannot open; the body is exactly `Unhealthy`), `HealthEndpointTests.Health_WithBrokenDatabase_StillReturns200`; `AnonymousAccessTests.TheListHoldsEveryProtectedEndpointOfTheApi` keeps passing unchanged.
- [ ] T358 [US1] Forwarded headers (plan R2): `ReverseProxy:TrustedNetwork` (CIDR), `XForwardedFor | XForwardedProto`, `ForwardLimit = 1`, only that network in `KnownIPNetworks`, `UseForwardedHeaders()` first in the pipeline. Outside Development, refuse to start without the key. `ApiFactory` sets a test network, and a test start-up filter sets `Connection.RemoteIpAddress`. Test: `ForwardedHeadersTests.TrustedProxy_RateLimitUsesTheForwardedAddress` (limit 3: 3 logins as `203.0.113.7`, then the 4th as `203.0.113.7` gets 429 and one as `203.0.113.8` does not), `ForwardedHeadersTests.UntrustedSource_ForwardedHeadersAreIgnored`, `ForwardedHeadersTests.ForwardedHttps_IsNotRedirectedAndGetsHsts`, `ForwardedHeadersTests.MissingTrustedNetwork_StopsTheAppOutsideDevelopment`; the `HostFilteringTests`, `HstsTests` and `RateLimitTests` keep passing.
- [ ] T359 [US1] Correlation id (plan R3): middleware next to the security-headers middleware sets `X-Correlation-ID` (the trace id) through `Response.OnStarting`. `CapturingLoggerProvider` learns to record scopes (`ISupportExternalScope`). Test: `CorrelationIdTests.EveryResponse_HasACorrelationId` (200, 401, 404), `CorrelationIdTests.ProblemDetailsTraceId_ContainsTheCorrelationId`, `CorrelationIdTests.LogEntriesOfARequest_CarryTheCorrelationId` (the 404 warning of `DomainExceptionHandler` has the response's id in its scope).
- [ ] T360 [US1] JSON console logs outside Development (plan R3): `AddJsonConsole` with `IncludeScopes` and UTC timestamps; Development keeps the simple console. Test: `ProductionLoggingTests.OutsideDevelopment_ConsoleIsJsonWithScopes` (resolves `ConsoleLoggerOptions.FormatterName == "json"` and `JsonConsoleFormatterOptions.IncludeScopes`), `ProductionLoggingTests.Requests_LogNoKeyOrConnectionString` (start, register, login, fail a login, and check that no captured entry contains the JWT key, the connection string, the password or the issued token); the `SecurityLoggingTests` keep passing.
- [ ] T361 [US1] docs/architecture.md section 4: rows for `ReverseProxy__TrustedNetwork` and `APP_VERSION`, the health endpoints, and the "Reverse proxy" note rewritten as done (feature 022, T358). Check: the documented setting names match `Program.cs` (`grep`); full check set green.

## Phase 2: PR `022-docker` — User Story 2 (P1)

**Goal**: the API and web images and the Compose stack run locally exactly as they will on the server (FR-022-5 to FR-022-7).
**Independent Test**: spec US2.
**Who does what**: the agent writes the Dockerfiles in `server/` and `client/`. The developer writes `deploy/docker-compose.yml`, `deploy/Caddyfile`, `deploy/.env.example` and the CI job by hand.

- [ ] T362 [P] [US2] `server/Dockerfile` (stages `build` and `final`; the `bundle` stage follows in T368) and `server/.dockerignore` (plan R4): `aspnet:10.0` final image, `USER $APP_UID`, port 8080, `ARG`/`ENV APP_VERSION`. **Agent.** Check:
  - `docker build --build-arg APP_VERSION=local -t smart-estimate-api:local server/` succeeds;
  - `docker run --rm --entrypoint id smart-estimate-api:local -u` is not `0`;
  - `docker run --rm --entrypoint ls smart-estimate-api:local -R /app` shows no `*.cs`, no `appsettings.Development.json` and no test assembly;
  - `docker history --no-trunc` shows no secret.
- [ ] T363 [P] [US2] `client/Dockerfile` (`node:22-alpine` build, then `caddy` with `dist` in `/srv`) and `client/.dockerignore` (plan R4). **Agent.** Check: `docker build -t smart-estimate-web:local client/` succeeds, and `docker run --rm --entrypoint ls smart-estimate-web:local /srv /srv/assets` lists `index.html` and hashed assets.
- [ ] T364 [US2] `deploy/docker-compose.yml` and `deploy/.env.example` (plan R5). **Developer, by hand.** The Compose file must have:
  - the services `mysql` (`mysql:8.4`, volume `mysql-data`, `mysqladmin ping` health check), `api` and `caddy`, with `migrate` following in T369;
  - the `internal` network with subnet `172.30.0.0/24`;
  - the `api` environment from plan R5 (`ASPNETCORE_ENVIRONMENT=Production`, `AllowedHosts=${SITE_ADDRESS}`, `ReverseProxy__TrustedNetwork=172.30.0.0/24`, connection string, `Jwt__Key`);
  - `api` waiting on `mysql` (`condition: service_healthy`);
  - only `caddy` publishing ports (80, 443, 443/udp), with volumes `caddy-data` and `caddy-config`;
  - `json-file` logs with `max-size: 10m` and `max-file: 3`;
  - `restart: unless-stopped`;
  - images `ghcr.io/<owner>/smart-estimate-{api,web}:${IMAGE_TAG}`.

  `.env.example`: every variable of plan R8 with placeholder values (`change-me`) and the `openssl rand -hex 32` hint. Check:
  - `docker compose -f deploy/docker-compose.yml --env-file deploy/.env.example config` passes;
  - `git check-ignore deploy/.env` confirms the real file is ignored;
  - `.env.example` contains no real value (reviewed in the PR diff).
- [ ] T365 [US2] `deploy/Caddyfile` (plan R5). **Developer, by hand.** It must have:
  - `{$SITE_ADDRESS}`;
  - `/api/*` and `/health*` sent to `api:8080`, with no header changes there;
  - client fallback to `index.html`;
  - client headers (CSP from docs/architecture.md section 4, `nosniff`, `no-referrer`, `X-Frame-Options: DENY`, HSTS);
  - `Cache-Control: no-cache` on `index.html` and an immutable cache on `/assets/*`;
  - `encode zstd gzip`.

  Check: `docker run --rm -e SITE_ADDRESS=localhost -v "$PWD/deploy/Caddyfile:/etc/caddy/Caddyfile:ro" smart-estimate-web:local caddy validate --config /etc/caddy/Caddyfile` passes.
- [ ] T366 [US2] Local stack check. **Developer**: first creates the local `deploy/.env` by hand from `deploy/.env.example` (`SITE_ADDRESS=localhost`, local-only values from `openssl rand -hex 32`). The agent does not create, edit or read this file (Article VIII). The developer runs the checks; the agent may then run the same `docker compose` and `curl` commands against the running stack and record the results. Images are tagged `IMAGE_TAG=local` from T362/T363. Check:
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

**Status notes**: (filled in during implementation)

## Phase 3: PR `022-db-migrations` — User Story 3 (P1)

**Goal**: every deployment backs up the database, then migrates it, and starts the new API only if the migration succeeded (FR-022-8).
**Independent Test**: spec US3.
**Who does what**: the agent adds the bundle stage to `server/Dockerfile`. The developer writes the `migrate` service and `deploy/deploy.sh` by hand.

- [ ] T368 [US3] `bundle` stage in `server/Dockerfile`: `dotnet tool install dotnet-ef --version 10.0.12`, `dotnet ef migrations bundle -c Release -o /out/efbundle`, copied into the final image as `/app/efbundle` (plan R4, R6). **Agent.** Check, against a throwaway `mysql:8.4` container on a Docker network, with the same environment as `api`:
  - the first run of `efbundle --connection …` exits 0;
  - `SELECT COUNT(*) FROM __EFMigrationsHistory` equals the number of `*.Designer.cs` files in `server/SmartEstimatePlatform.Api/Migrations`;
  - a second run prints that no migrations were applied and exits 0;
  - without `AllowedHosts`, the bundle fails. This confirms that it needs the API's environment (plan R6).
- [ ] T369 [US3] `migrate` service in `deploy/docker-compose.yml`: the API image, `command: ["./efbundle"]`, `profiles: [migrate]`, `depends_on: mysql (service_healthy)`, and exactly the `api` environment (a YAML anchor or `env_file`, so the two cannot drift). **Developer, by hand.** Check, on the local stack from T366:
  - `docker compose --profile migrate run --rm migrate` exits 0, after which registering through `https://localhost/api/auth/register` gives 201;
  - **data persists** (spec US2 scenario 4): after `docker compose down` (without `-v`) and `docker compose up -d`, logging in with the account just registered gives 200, and `docker volume ls` still lists `mysql-data`;
  - **application user, not root** (spec US3 scenario 5):
    - `docker compose config` shows that neither the `migrate` nor the `api` environment contains `MYSQL_ROOT_PASSWORD`;
    - their connection strings use `User=${MYSQL_USER}`, not `root`;
    - in MySQL, `SELECT user, host FROM mysql.user` lists the application user, and `SHOW GRANTS FOR '<MYSQL_USER>'@'%'` grants privileges only on `${MYSQL_DATABASE}.*`.
- [ ] T370 [US3] `deploy/deploy.sh` (plan R6). **Developer, by hand.** It runs:
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
- [ ] T371 [US3] Migration failure drill (SC-022-4). Locally:
  1. Build the API image from the commit before `LimitPersonAndCompanyNames` as `IMAGE_TAG=old` and deploy it on an empty volume.
  2. Insert a user whose `FirstName` has 101 characters.
  3. Build the current commit as `IMAGE_TAG=new` and run `PULL=0 IMAGE_TAG=new ./deploy.sh`.

  Check:
  - the script exits non-zero and prints the backup path;
  - `docker compose ps --format '{{.Service}} {{.Image}}'` still shows `api` on `:old`;
  - `/health` still reports `old`;
  - after the long name is removed, the same command succeeds.

  Record the commands and output in the status notes. No migration file is created.

**Status notes**: (filled in during implementation)

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
  - `.env.example` holds only placeholders;
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
