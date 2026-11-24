# Testing Strategy

How Smart Estimate Platform is tested today. Everything below describes tests that exist in the repository. The numbers come from a full run on 2026-11-24 at `main` `a2b09d2`: **461 backend tests** (276 test methods, theories counted per data row) and **125 frontend tests** in 19 files, all passing.

| Layer | Tool | Where | Tests |
|---|---|---|---|
| Backend unit tests (no HTTP, no database) | xUnit | `tests/server` | 134 |
| Backend service tests on a real relational database | xUnit + in-memory SQLite | `tests/server/AuthServiceTests` (`LoginAsyncTests`, `RegisterAsyncTests`) | 16 |
| Backend API integration tests | xUnit + `WebApplicationFactory<Program>` + in-memory SQLite | `tests/server`, through `TestSupport/ApiFactory` | 311 |
| Frontend component and page tests | Vitest + React Testing Library + jsdom | `tests/client` | 125 |
| End-to-end browser tests | — | — | none (see Known limitations) |

## 1. Test pyramid

### 1.1 Unit tests (134)

Pure logic, run in milliseconds, with no web host and no database.

| Class | Tests | What it proves |
|---|---|---|
| `MoneyRoundingTests` | 7 | `MoneyRounding.Round` rounds money half away from zero to 2 decimals (`Round_HalvesGoAwayFromZero`: 0.005 → 0.01, 40.425 → 40.43, −0.005 → −0.01). |
| `EstimateCalculatorTests` | 17 | BR-015–BR-017 calculations: `WorkedExample_TotalIs1263_26`, `MidpointMaterialCost_IsRoundedAwayFromZero`, `QuantitiesAndNorms_AreNeverRounded`, `Total_IsTheSumOfTheRoundedParts`, section subtotals and section quantities × coefficients. |
| `EstimateCatalogRefreshTests` | 8 | Comparing estimate lines with the current catalog before a refresh (FR-024). |
| `RequestValidationTests`, `WorkRequestValidationTests`, `EstimateRequestValidationTests`, `ProjectRequestValidationTests` | 88 | The data annotations and `IValidatableObject` rules on the request records, checked with `Validator.TryValidateObject` exactly as `[ApiController]` checks a body: units, price/norm/quantity/percentage decimals and ranges, password rules, length limits. |
| `NumberPrecisionModelTests` | 14 | The EF Core model gives every price, norm, quantity and percentage its DECIMAL precision and scale. It checks the model because SQLite does not enforce DECIMAL scale; the MySQL migrations are generated from this model. |

**Worked examples used as oracles:**
- **1263.26 €**: the hand-calculated example of spec 007, a 35 m² wall with three materials and 10 % / 15 % / 21 %, including the 40.425 → 40.43 midpoint. It is checked in `EstimateCalculatorTests.WorkedExample_TotalIs1263_26`, and through the API in `EstimateLineEditingTests` and `EstimateRefreshEndpointTests`.
- **135 414.29 € ± 0.50 €**: `SampleEstimateAcceptanceTests.SampleEstimate_BuiltFromCatalogWorks_MatchesThePdfTotalWithin50Cents` builds the real 81-line sample estimate from `docs/reference/sample-estimates` through the API and compares its total with the source PDF.
- The earlier **394.91 €** example from the first requirements draft is **not** tested. Its inputs are not available (spec 007, Clarifications), so the 1263.26 € example replaced it.

**Why:** money is the core risk of the product. Rounding or ordering mistakes give a wrong offer to a client. Unit tests pin every rule and the edge cases (midpoints, no rounding of quantities and norms) cheaply, so they can be exhaustive.

### 1.2 Service tests on SQLite (16)

`AuthServiceTestBase` runs `AuthService` against its own in-memory SQLite database, with a controllable clock (`TestTimeProvider`) and an optionally replaced password hasher (`CountingPasswordHasher`).

- `LoginAsyncTests` (12): lockout after 5 failures in 10 minutes, unlock after 15 minutes, a streak that starts over, the same error for an unknown email and a wrong password, and `LoginAsync_UnknownEmail_HashesLikeAKnownEmail` (feature 021).
- `RegisterAsyncTests` (4): company and Manager created together, and duplicate emails refused, also in a different letter case.

**Why:** these rules depend on time and on the unique email index. A real relational database and a fake clock test them without waiting 15 minutes and without HTTP noise.

### 1.3 API integration tests (311)

`TestSupport/ApiFactory` (`WebApplicationFactory<Program>`) starts the **real API in-process**: real routing, model validation, JWT validation (`OnTokenValidated`), authorization attributes, global company query filters, `DomainExceptionHandler`, rate limiting and security headers. Only the database is swapped, for an in-memory SQLite connection (`TestSupport/SqliteTestDatabase`). Helpers create data through the API itself:
- `ApiFactory.RegisterCompanyAsync`, `CreateViewerClientAsync`, `LoginAsync`;
- `TestSupport/CatalogRequests`, `EstimateRequests`, `ProjectRequests`.

Examples: `MaterialEndpointsTests` (20), `EstimateEndpointsTests` (16), `EstimateLineEditingTests` (31), `EstimateRefreshEndpointTests` (18), `EstimateSectionEndpointsTests` (11), `EstimateExportTests` (9), `TeamEndpointsTests` (15), `ErrorResponseTests` (9).

**Why:** most risks of this system sit between the layers, for example a missing `[Authorize]`, a query that bypasses the company filter, a validation attribute that never runs, or an error mapped to the wrong status code. Only a test through the real HTTP pipeline catches them. The constitution also requires role and company rules to be covered by integration tests, not unit tests alone (Article IV).

### 1.4 Authorization and company-isolation matrix

Every protected endpoint is tested for the four callers in `docs/security-review.md` section 4: anonymous, Viewer, Manager of the same company, Manager of another company. No cell is left untested.

| Caller | Expected | Tests |
|---|---|---|
| Anonymous | 401 | `AnonymousAccessTests.EveryProtectedEndpoint_Returns401` (48 routes) and `AnonymousAccessTests.TheListHoldsEveryProtectedEndpointOfTheApi`, which compares the list with the API's real endpoints, so a new endpoint without a row fails |
| Viewer on Manager endpoints | 403, nothing changed | `MaterialAuthorizationTests`, `WorkAuthorizationTests`, `ProjectAuthorizationTests`, `EstimateAuthorizationTests`, `CatalogSectionAuthorizationTests`, `EstimateSectionAuthorizationTests`, `TeamEndpointsTests.Viewer_CannotUseTeamEndpoints` |
| Viewer on allowed endpoints | names only, no numbers | `EstimateNamesTests` (8), incl. `NamesResponses_CannotCarryNumbersOrStatus` |
| Manager of another company | 404 (not 403, so existence is not revealed), nothing changed | `CompanyIsolationTests/*` (16), `CatalogSectionIsolationTests`, `EstimateSectionIsolationTests`, `EstimateCopyIsolationTests`, plus cross-company references (`WorkEndpointsTests.Create_WithAnotherCompanysMaterial_IsRejectedAndSavesNothing`, `EstimateLineEditingTests.ReplaceMaterial_WithAnotherUnitOrAnotherCompanysMaterial_IsRefused`) |

**Why:** company isolation is the most important security property (one company must never see another's prices). A matrix makes a gap visible as an empty cell instead of a forgotten case.

### 1.5 Security tests (feature 021)

Feature 021 (PRs #27–#31) added 93 backend tests and 2 frontend tests, each named in `docs/security-review.md` section 12:

| Finding | Tests |
|---|---|
| F2 authorization gaps | `AnonymousAccessTests` (49), the catalog/estimate section authorization and isolation tests, `MaterialSearchTests.Search_SqlLikeText_IsTreatedAsData` (SQL-looking search text in all three catalogs) |
| F3 forged tokens | `TokenValidationTests.ForgedToken_IsRejected` (wrong key, `alg=none`, expired, wrong issuer, wrong audience, edited `company_id`) with the control `CorrectlyBuiltToken_IsAccepted`; `JwtOptionsStartupTests.ShortKey_StopsTheApp` |
| F4 brute force | `RateLimitTests.Login_OverTheLimit_Returns429`, `RateLimitTests.Register_OverTheLimit_Returns429` |
| F5 login timing | `LoginAsyncTests.LoginAsync_UnknownEmail_HashesLikeAKnownEmail` |
| F6 logout | `LogoutEndpointTests.TokenUsedAfterLogout_IsRejected`; client `Logout.test.tsx` "logging out calls the API and removes the token" |
| F7 HTTP hardening | `SecurityHeadersTests.EveryResponse_HasTheSecurityHeaders`, `HostFilteringTests.*` |
| F8 logging | `SecurityLoggingTests.*` (5), which capture log entries with `TestSupport/CapturingLoggerProvider` and assert that no password, token or request body is logged |
| F9 input limits | `RequestValidationTests.RegisterRequest_EmailOver256Characters_IsRejected`, `RegisterRequest_NamesOverTheLimit_AreRejected`, `PasswordFields_HaveAtMost128Characters`, `MaterialSearchTests.Search_OverTheLimit_Returns400` |
| F12 password hashing | `PasswordHashingTests.StoredHash_IsNotThePlainPassword` |

Older security tests also remain: `HstsTests`, `UnexpectedErrorTests.UnexpectedFailure_Returns500WithoutTechnicalDetails` (no stack trace leaks), `ErrorResponseTests`, `EstimateExportTests.FormulaLikeNames_AreExportedAsTextNotFormulas` (Excel formula injection).

### 1.6 Frontend tests (125)

Vitest with jsdom, React Testing Library and `@testing-library/user-event`. The tests render the real `App` (with `MemoryRouter`) or a page, and act as a user would: by role, label and visible text. `fetch` is replaced with `vi.stubGlobal`, so no server runs.

| Area | Files (tests) |
|---|---|
| Estimate editor | `EstimateEditorTests/*` (9 files, 59): line editing, refresh preview, sections, status, export download, add dialogs, replacement dialog, tables that fit |
| Messages and errors | `ErrorMessageTests/ErrorMessages.test.tsx` (21) |
| Navigation and layout | `NavigationTests/Navigation.test.tsx` (9), `Layout.test.tsx` (5) |
| Confirmation dialogs (NFR-011) | `ConfirmDialogTests/RemoveProject`, `RemoveViewer`, `RemoveWork` (10) |
| Catalog sections, precision, Viewer view, logout | `CatalogSections.test.tsx` (8), `Precision.test.tsx` (5), `ViewerEstimateView.test.tsx` (4), `Logout.test.tsx` (4) |

**Why:** the client must show only what the role allows, ask before destructive actions, send exactly the expected requests and show Lithuanian messages. These tests check behaviour and accessibility (roles, labels, focus, keyboard) without depending on a running backend.

## 2. Conventions

### 2.1 Naming

- **Backend:** test names read as `Subject_Condition_ExpectedResult`. When the condition is obvious it is folded into the subject, giving `Subject_ExpectedResult`.
  - Of the 276 test methods, 91 have three parts (e.g. `LoginAsync_FiveFailuresWithin10Minutes_LocksAccount`, `Search_OverTheLimit_Returns400`).
  - 184 have two parts (e.g. `Viewer_CannotChangeOrRemoveASection`, `ForgedToken_IsRejected`).
  - One class per subject, in a folder per feature (`MaterialServiceTests/`, `CompanyIsolationTests/`, `CrossCuttingTests/`).
- **Frontend:** `test('…')` names are plain sentences of behaviour, e.g. "confirming asks first; cancelling sends nothing", one file per screen or feature.

### 2.2 Arrange – Act – Assert

Each test has three phases separated by a blank line. Arrange creates the data (usually through the API helpers); Act is the one call under test; Assert checks the result and, for refused requests, that nothing changed:

```csharp
var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
var (estimateId, sectionId) = await EstimateWithSectionAsync(company);
var viewer = await _factory.CreateViewerClientAsync(company);

var change = await viewer.PatchAsJsonAsync($"/api/estimates/{estimateId}/sections/{sectionId}", ...);
var remove = await viewer.DeleteAsync($"/api/estimates/{estimateId}/sections/{sectionId}");

Assert.Equal((HttpStatusCode.Forbidden, HttpStatusCode.Forbidden), (change.StatusCode, remove.StatusCode));
Assert.Equal(10m, Assert.Single((await company.Client.GetEstimateAsync(estimateId)).Sections).Quantity);
```

(`EstimateSectionAuthorizationTests.Viewer_CannotChangeOrRemoveASection`.) One-line tests, such as the validation theories, are written as expression bodies.

### 2.3 Repeatability

- **Own data per test.** xUnit creates a new test class instance for every test, and each instance creates its own `ApiFactory`. That means a new in-process host with its own in-memory SQLite connection, so the database lives exactly as long as the test. `AuthServiceTestBase` does the same for the service tests.
- **No shared database, no order dependence.** No test relies on data from another test or on run order. Test classes run in parallel (xUnit's default) without interference.
- **No real secrets or services.** `ApiFactory` uses the constant dummy key `ApiFactory.JwtKey` ("integration-test-signing-key-…") and a placeholder connection string; CI sets a dummy `Jwt__Key`. No test needs MySQL, a network or an email server.
- **Controlled time.** Lockout rules use `TestTimeProvider`. The expired-token test uses an expiry 10 minutes in the past (beyond the 5-minute clock skew), because JwtBearer reads the system clock.
- **Settings for the test host.** `ApiFactory` lists the allowed test hosts (`localhost;smart-estimate.test`) and raises the login rate limit, so other tests are never throttled. `RateLimitTests` lowers it again for itself.
- **Frontend.** Each test stubs `fetch` and `localStorage` state in `beforeEach`/`afterEach` and calls `cleanup()`, so no state leaks between tests.

## 3. Manual checks and why

Some properties cannot be checked by the automated tests. They are done by hand and recorded in the feature's `tasks.md`.

| Check | Why it is manual | Where it is recorded |
|---|---|---|
| Visual layout at 360 px, 1280 px and 1920 px (no horizontal page scroll, NFR-008), touch targets ≥ 44 px | jsdom has no layout engine, so widths, wrapping and scrolling cannot be measured in Vitest | `specs/019-estimate-sections/tasks.md` T333, `specs/020-fit-tables/tasks.md` T344 and T355 (browser checks); `specs/016-price-norm-precision/tasks.md` T283 (checked by reading the CSS classes, with doubts noted) |
| The exported `.xlsx` opened in Excel or LibreOffice | `EstimateExportTests` read the workbook with ClosedXML (number cells, formats, no formulas, text cells), but how a spreadsheet program displays it can only be seen by opening it | `specs/010-estimate-export/quickstart.md` step 2 |
| A run against the real MySQL database (migrations, before/after totals) | The tests use SQLite with `EnsureCreated`, so migrations and MySQL-specific behaviour are never exercised automatically | `specs/016-price-norm-precision/quickstart.md` step 2 (its `tasks.md` T282 note still says this before/after comparison had not been run), `specs/019-estimate-sections/tasks.md` (before/after comparison done 2026-11-17), `specs/021-security-hardening/tasks.md` T016 (length check before `LimitPersonAndCompanyNames`, applied on 2026-11-23) |

## 4. Known limitations

- **SQLite is not MySQL.**
  - SQLite ignores `varchar(n)` lengths and DECIMAL scale. Length limits are tested at the request records (400 before any save), and precision through the model (`NumberPrecisionModelTests`), not through the database.
  - The `utf8mb4_0900_as_ci` collation is emulated on SQLite with an ordinal case-insensitive comparison (`SqliteTestDatabase`). Email uniqueness and name search ignore case as on MySQL, but accent and sorting rules are not identical.
  - `LIKE` and decimal handling come from a different provider, so the SQL-injection and search tests prove the EF Core parameterisation but run on SQLite.
- **Migrations are not tested.** The test databases are created with `Database.EnsureCreated()` from the model. A broken migration would only show up in the manual MySQL run; `dotnet ef migrations has-pending-model-changes` is run by hand when a feature adds one.
- **No end-to-end tests.** No browser test (e.g. Playwright) drives the real client against the real API. Because the frontend tests stub `fetch`, a mismatch between the client's TypeScript interfaces and the API's JSON would not be caught automatically.
- **No load, performance or coverage measurement.** There is no load test and no coverage tool. The rate-limit tests run in-process, where every request has the same (empty) client address.
- **CI does not check everything.** It audits runtime npm packages and all NuGet packages, but the full `npm audit` (dev tools) and the `--outdated` checks are run by hand (`docs/security-review.md` section 10).

## 5. CI and quality gates

`.github/workflows/ci.yml` (since PR #27; triggers and step order as of PR #35) runs on **every pull request that targets `main`** and on **every push to `main`**, with `permissions: contents: read` and a dummy `Jwt__Key`. Both jobs run on `ubuntu-latest`, and every step is named:

| Job | Steps |
|---|---|
| `backend` | `actions/setup-dotnet@v5` (10.0.x); Restore (`dotnet restore`); Build (`dotnet build`, Release, `--no-restore`); Test (`dotnet test`, Release, `--no-build`); Vulnerable packages: `dotnet list package --vulnerable --include-transitive`, which fails the job if any package is reported |
| `frontend` (in `client/`) | `actions/setup-node@v6` (Node 22, npm cache); Install dependencies (`npm ci`); Lint (`npm run lint`); Test (`npm run test`); Build (`npm run build`: TypeScript check + Vite build); Audit runtime dependencies (`npm audit --omit=dev --audit-level=moderate`) |

**Release packaging.** `.github/workflows/release.yml` (since PR #35) starts through `workflow_run` when CI completes on `main`, and runs only if CI succeeded. It checks out the exact commit CI tested (`head_sha`), runs `dotnet publish` of the API (Release) and `npm run build` of the client, and uploads the **api** and **client** artifacts. Packaging therefore never runs on a commit whose tests failed. Its first run, after PR #35, succeeded with both artifacts (api ≈ 5.7 MB, client ≈ 108 KB). It builds packages only; it does not deploy.

**Quality gates:**
- **Packages only from green builds.** The release workflow's `if` requires a successful CI run, so the artifacts always come from tested code.
- **Green CI before merging.** Both jobs (`backend`, `frontend`) must be green before a pull request is merged. The repository is public, and the GitHub ruleset `main-protection` (target `main`, active, no bypass actors) enforces this: it requires a pull request, requires the status checks `backend` and `frontend` to pass, blocks force pushes and blocks deleting `main` (checked through the GitHub API on 2026-11-24). The underlying rule is the constitution's: "A pull request MUST NOT be merged with failing automated tests" (Article III); the ruleset makes GitHub enforce it.
- **No warnings.** `dotnet build` must report 0 warnings; this is checked locally and noted in each feature's `tasks.md`.
- **Order of checks before proposing a commit (AGENTS.md):** `dotnet build`, `dotnet test`, `npm run lint`, `npm run build`, `npm run test`.
- **Node versions differ.** CI uses Node 22. A frontend test once passed on a newer local Node and failed in CI (PR #27, `ExportEstimate.test.tsx`), so a CI failure on a test that passes locally should first be tried under Node 22.

## 6. How to run

From the repository root:

```bash
# Backend: all 461 tests (needs only the .NET 10 SDK; no MySQL, no secrets)
cd server && dotnet test

# One class or one test
dotnet test --filter "FullyQualifiedName~AnonymousAccessTests"
dotnet test --filter "FullyQualifiedName~LoginAsync_UnknownEmail_HashesLikeAKnownEmail"

# Frontend: all 125 tests, lint and the type-checked build
cd client && npm ci
npm run test
npm run lint
npm run build

# One frontend test file
npx vitest run ../tests/client/LogoutTests

# The same dependency checks as CI
cd server && dotnet list SmartEstimatePlatform.slnx package --vulnerable --include-transitive
cd client && npm audit --omit=dev
```
