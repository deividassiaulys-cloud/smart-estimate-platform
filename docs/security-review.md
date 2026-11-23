# Security Review

Project: Smart Estimate Platform
Scope: ASP.NET Core API (`server/SmartEstimatePlatform.Api`, .NET 10), React/Vite client (`client/`), MySQL.
Method: asset identification, threat modelling, code review against the OWASP Top 10 and OWASP ASVS, automated tests for negative cases.
Reviewed: 2026-11-23, against `main` at `a06120d` (after PR #26). Every statement below was checked against the code; file and test names are the evidence.

Status legend: **OK** = control exists and is tested; **PARTIAL** = control exists, test or detail missing; **GAP** = missing, fix planned; **FIXED** = was a finding, now fixed and tested.

Test names are written as `Class.Method`. Server tests are in `tests/server`, client tests in `tests/client`.

---

## 1. Protected assets

| Asset | Why compromise matters |
|---|---|
| User accounts (email, role, company) | An attacker could act as a Manager, read or change the company's prices and estimates, or remove Viewers. |
| Password hashes | Offline cracking would expose passwords that users may reuse elsewhere. |
| JWT signing key (`Jwt:Key`) | Anyone holding it can forge a token for any user and any company. |
| JWT access tokens | A stolen token gives the attacker the user's access until it expires (60 min) or is revoked. |
| Database connection string | Direct access to all companies' data. |
| Company data: materials, works, catalog sections, prices, labour norms | Business-critical pricing. A competitor reading it or an attacker changing it causes financial loss. |
| Projects and estimates (confirmed estimates are locked) | Altering a confirmed estimate changes the amount offered to a client. |
| Company isolation (tenant boundary) | One company must never read or change another company's data. |
| Personal data (first name, last name, email) | Privacy obligations (GDPR) and phishing risk if leaked. |
| Exported Excel files (`GET /api/estimates/{id}/export`) | Contain full estimates and could carry formula injection into the user's spreadsheet. |
| Source repository | A leaked secret or a malicious change compromises the whole system. There is no CI configuration in the repository (see F10). |

## 2. Threat model

| # | Workflow | Threat | Attacker | Impact | Control (as implemented) | Verification |
|---|---|---|---|---|---|---|
| T1 | Any resource by ID | Manager of company A changes the ID in a request to read or edit company B's material, work, catalog section, project, estimate or Viewer | Authenticated user | Cross-company data leak or tampering | Global query filter on every `ICompanyScoped` entity (`AppDbContext.ApplyCompanyFilter`), with `CompanyId` taken from the token; child entities are filtered through their parent; a foreign ID is not found, so the API returns 404 | `*IsolationTests` in `CompanyIsolationTests/`, `CompanyScopingTests.Queries_NeverSeeAnotherCompanysRecords` (see the matrix in section 4 for gaps) |
| T2 | Composition across catalogs | Company A sends a `MaterialId`, `WorkId` or `CatalogSectionId` of company B inside a work, catalog section or estimate | Authenticated user | Link to foreign data, price leak | Services load the referenced IDs through the filtered `DbSet`s; a foreign material or work is rejected with 400 (`UnknownMaterialException`, `UnknownWorkException`), a foreign catalog section with 404. `WorkMaterialLine` is filtered through its work (EF warning 10622 fixed in `5018d4c`) | `WorkEndpointsTests.Create_WithAnotherCompanysMaterial_IsRejectedAndSavesNothing`, `WorkEndpointsTests.Update_WithAnotherCompanysMaterial_IsRejectedAndKeepsTheComposition`, `CatalogSectionEndpointsTests.AnotherCompanysWork_IsRejectedAndSavesNothing`, `EstimateEndpointsTests.AddWork_UnknownOrAnotherCompanysWork_IsRejected`, `EstimateLineEditingTests.ReplaceMaterial_WithAnotherUnitOrAnotherCompanysMaterial_IsRefused`, `EstimateSectionEndpointsTests.ViewerIsForbidden_AndAnotherCompanyGetsNotFound`, `CompanyScopingTests.WorkMaterialLines_AreScopedThroughTheirWork` |
| T3 | Write operations | Viewer calls a Manager endpoint directly | Authenticated Viewer | Unauthorized change | `[Authorize(Roles = "Manager")]` on the Materials, Works, CatalogSections, Estimates and Team controllers, and on the Projects write actions | `*AuthorizationTests`, `TeamEndpointsTests.Viewer_CannotUseTeamEndpoints` (gaps in section 4) |
| T4 | Viewer data | Viewer reads prices through the API although the UI shows names only | Authenticated Viewer | Price disclosure | Viewer cannot reach Materials, Works, CatalogSections or Estimates endpoints (403). Viewer-only data comes from `EstimateNamesController`, whose responses have no number fields | `EstimateNamesTests.NamesResponses_CannotCarryNumbersOrStatus`, `EstimateNamesTests.Viewer_IsStillForbiddenFromTheManagersEstimateData` |
| T5 | Login | Password guessing / credential stuffing | Anonymous | Account takeover | `PasswordHasher<User>` (PBKDF2); lockout after 5 failures within 10 minutes for 15 minutes (`AuthService`); same message for unknown email and wrong password, and since feature 021 the same cost: an unknown email is verified against a dummy hash. **No rate limiting**; lockout (423 only for existing accounts) reveals that an account exists and lets anyone lock a known user out (accepted limitation) | `LoginAsyncTests.*`, `LoginAsyncTests.LoginAsync_UnknownEmail_HashesLikeAKnownEmail`, `ErrorResponseTests.Login_WrongPassword_Returns401WithGenericMessage`, `ErrorResponseTests.Login_FifthFailure_Returns423WithMinutesRemaining`; rate limit: none (F4, F5) |
| T6 | Registration | Weak passwords, duplicate emails | Anonymous | Weak accounts | `PasswordRules` (min 8, not equal to the email) applied by `IValidatableObject` on the request records; unique index on `Users.Email` with a case-insensitive collation; `UniqueSave` reports concurrent duplicates | `RequestValidationTests.RegisterRequest_*`, `RegisterAsyncTests.RegisterAsync_DuplicateEmail*`, `ErrorResponseTests.Register_ExistingEmail_Returns409` |
| T7 | Token use | Stolen or old token used after logout, Viewer removal or password reset | Token thief | Continued access | 60-minute lifetime; `OnTokenValidated` checks on every request that the user exists, has the token's company and the same `TokenVersion`; `TokenVersion` increases on password reset and, since feature 021, on logout (`POST /api/auth/logout`) | `TeamEndpointsTests.RemoveViewer_CannotLogInAndTheirTokenIsRejected`, `TeamEndpointsTests.ResetPassword_RejectsTokensIssuedBeforeIt`, `CompanyScopingTests.Token_WhoseCompanyDoesNotMatchTheUser_IsRejected`, `LogoutEndpointTests.TokenUsedAfterLogout_IsRejected` |
| T8 | Token forgery | Forged, unsigned (`alg=none`), expired or foreign-issuer token | Anonymous | Full impersonation | `TokenValidationParameters` with issuer, audience and signing key (signature, lifetime, issuer and audience are validated by default; unsigned tokens are refused); key at least 32 characters, checked at start-up (`ValidateOnStart` in `Program.cs`) | MISSING: no test sends a tampered, unsigned, expired or wrong-issuer token, and none starts the app with a short key (F3) |
| T9 | Invalid input | Negative quantity or price, percentage above 100, huge strings, invalid unit | Authenticated user | Wrong totals, DB errors, DoS | Request records with data annotations and `IValidatableObject` using `NumberRules`, `MaterialRules`, `WorkRules`, `EstimateRules`, `CatalogSectionRules`, `MeasurementUnit`. **Personal names, company name, email and password have no length limit** | `RequestValidationTests/*` (section 5); length limits: none (F9) |
| T10 | Search/list | SQL injection through search text | Authenticated user | Data disclosure | EF Core LINQ only; search uses `EF.Functions.Like` with a parameter built by `SearchPattern.Contains`, which escapes `%`, `_` and `\`; no raw SQL anywhere | Code search (section 6); `MaterialSearchTests.Search_PercentSign_MatchesLiterally` |
| T11 | Excel export | Cell starting with `=`, `+`, `-`, `@` executes as a formula | Authenticated Manager | Code execution in the victim's Excel | Every text cell goes through `EstimateExportService.Text`, which stores it as text with a quote prefix; amounts are number cells without formulas | `EstimateExportTests.FormulaLikeNames_AreExportedAsTextNotFormulas`, `EstimateExportTests.Export_WritesNumberCellsWithFormatsAndNoFormulas` |
| T12 | Secrets in Git | Password, JWT key or connection string committed | Anyone with repo access | Full compromise | Connection string and `Jwt:Key` come from user secrets (`UserSecretsId` in the csproj) or environment variables; `appsettings.Development.json`, `.env*` and `server/.mcp.json` are ignored | History search (section 7): no real secret found |
| T13 | Dependencies | Known vulnerable or unused package | Supply chain | Remote exploitation | Manual audit (section 10); no automated audit | `dotnet list package --vulnerable`, `npm audit` (F10, F11, F12) |
| T14 | Browser | XSS steals the token from `localStorage` | Attacker controlling injected script | Account takeover | React escaping; no `dangerouslySetInnerHTML`, `eval` or `innerHTML` in `client/src`. **No Content-Security-Policy** | Code search (section 6); header test: none (F7) |
| T15 | Browser | Foreign website calls the API | Malicious site | Request abuse | No CORS policy is registered, so browsers refuse cross-origin reads; the token is sent in the `Authorization` header, never automatically, so CSRF does not apply. The client reaches the API through the Vite proxy (same origin) | No test; correct by default as long as the deployed client is served from the same origin |
| T16 | Error handling | Stack trace or database error shown to the user | Anyone | Information disclosure | `UseExceptionHandler` + `AddProblemDetails`; `DomainExceptionHandler` maps domain errors to fixed messages; other exceptions return a generic 500 `ProblemDetails` | `UnexpectedErrorTests.UnexpectedFailure_Returns500WithoutTechnicalDetails`, `ErrorResponseTests.*` |
| T17 | Delete | Deleting a material or work still in use; accidental destructive actions | Authenticated Manager | Broken data | FK `Restrict` and a 409 from the service (`MaterialInUseException`, `WorkInUseException`); shared `ConfirmDialog` on every delete in the client | `MaterialDeleteInUseTests.Delete_MaterialUsedInWorks_IsRejectedAndListsTheWorks`, `CatalogSectionEndpointsTests.WorkUsedInASection_CannotBeDeletedUntilTheSectionIsGone`, `tests/client/ConfirmDialogTests` |
| T18 | Confirmed estimate | Changing a Confirmed estimate through the API | Authenticated Manager | Offered amount changes silently | Services refuse every change to a non-Draft estimate (`EstimateNotDraftException` → 409) | `EstimateDraftGuardTests`, `EstimateLineEditingTests.ConfirmedEstimate_RefusesTheChange` |

## 3. Authentication review

| Requirement | Finding | Status |
|---|---|---|
| Passwords never stored in plaintext | `AuthService.CreateAccountAsync` and `TeamService` store only `passwordHasher.HashPassword(...)` in `User.PasswordHash`; `PasswordHasher<User>` is registered in `ServiceCollectionExtensions`. Old hashes are upgraded on login (`SuccessRehashNeeded`) | OK |
| Established library, no custom crypto | `PasswordHasher<User>` from ASP.NET Core Identity; tokens issued with `JsonWebTokenHandler` (HMAC-SHA256) and validated by `Microsoft.AspNetCore.Authentication.JwtBearer` | OK |
| Failures do not expose information | Unknown email and wrong password both give 401 "Invalid email or password." (`LoginAsyncTests.LoginAsync_UnknownEmailAndWrongPassword_GetTheSameError`, `ErrorResponseTests.Login_WrongPassword_Returns401WithGenericMessage`). But: (a) the 5th failure on an existing account returns 423 with `minutesRemaining`, which never happens for an unknown email, so an attacker can tell that an account exists (accepted limitation, spec 021 Q1); (b) an unknown email used to return without hashing, so the response was measurably faster. (b) is fixed in feature 021: an unknown email is verified against a dummy hash (`LoginAsyncTests.LoginAsync_UnknownEmail_HashesLikeAKnownEmail`) | PARTIAL (F5: timing fixed, 423 accepted) |
| Brute-force protection | Account lockout: 5 failures within 10 minutes lock the account for 15 minutes (`AuthService.MaxFailedLogins`, `FailedLoginWindow`, `LockoutDuration`; `LoginAsyncTests.LoginAsync_FiveFailuresWithin10Minutes_LocksAccount` and the following tests). No IP-based rate limiting on login or register. Anyone who knows an email can keep that account locked | PARTIAL (F4, F5) |
| Logout | Since feature 021, `POST /api/auth/logout` (`[Authorize]`, 204) increases `TokenVersion`, so every token issued before it, on every device, is rejected. The client calls it while the token is still stored, then removes the token even when the server cannot be reached (`NavBar.handleLogout`) | OK (`LogoutEndpointTests.TokenUsedAfterLogout_IsRejected`, `LogoutEndpointTests.Logout_WithoutLogin_IsUnauthorized`, `Logout.test.tsx` "logging out calls the API and removes the token", "logging out still removes the token when the server cannot be reached") |
| Token validation | Issuer, audience and signing key are set in `Program.cs`; lifetime and signature are validated by default; `MapInboundClaims = false`. On every request `OnTokenValidated` calls `AuthService.IsTokenUserValidAsync` (user exists, same company, same `TokenVersion`). `Jwt:Key` must be at least 32 characters, checked at start-up. No test for tampered, unsigned, expired or wrong-issuer tokens, or for the start-up key check | PARTIAL (F3) |
| Token storage in the browser | Token kept in `localStorage` (`client/src/utils/auth.ts`), exposed to XSS. The role is read from the token only to show or hide actions; the server enforces the rules | Accepted limitation, see the end of the document |
| Registration email enumeration | Register and Create Viewer return 409 "An account with this email already exists." | Accepted limitation |

## 4. Authorization review

Rules: every protected backend operation enforces the role (Manager/Viewer) and the company on the server. Hidden UI buttons are not a control.

How it is enforced:
- Role: `[Authorize]` / `[Authorize(Roles = nameof(UserRole.Manager))]` on the controller class (and on the Projects write actions). No endpoint is `[AllowAnonymous]`; only `register` and `login` have no `[Authorize]`.
- Company: `AppDbContext` query filters by `CurrentCompanyId` from the token, and `AssignNewRecordsToCurrentCompany` sets `CompanyId` on every new record, whatever the client sent (`CompanyScopingTests.NewRecords_AreAssignedToTheCurrentCompany_WhateverCompanyIsSet`). Only `AuthService` uses `IgnoreQueryFilters` (`CompanyScopingTests.OnlyAuthService_BypassesTheCompanyScoping`).
- Another company's resource is reported as 404, never 403, so its existence is not revealed.

Endpoint matrix (from `server/SmartEstimatePlatform.Api/Controllers`). Cells show the response; "list: empty" means the other company's records are not listed. In the Test column, **MISSING** names the cells no test covers.

| Endpoint | Anonymous | Viewer | Manager, same company | Manager, other company | Test |
|---|---|---|---|---|---|
| `POST /api/auth/register` | allowed (creates a new company) | allowed | allowed | n/a | `RegisterAsyncTests.RegisterAsync_ValidData_CreatesCompanyAndManager`, `ErrorResponseTests.Register_ExistingEmail_Returns409`, `ErrorResponseTests.Register_WeakPassword_Returns400AndCreatesNothing` |
| `POST /api/auth/login` | allowed | allowed | allowed | n/a | `LoginAsyncTests.*`, `ErrorResponseTests.Login_WrongPassword_Returns401WithGenericMessage`, `ErrorResponseTests.Login_FifthFailure_Returns423WithMinutesRemaining` |
| `GET /api/auth/me` | 401 | own user | own user | n/a (always the caller) | `CompanyScopingTests.Me_ReturnsOnlyTheCallersOwnCompany`, `CompanyScopingTests.Token_WhoseCompanyDoesNotMatchTheUser_IsRejected`; anonymous: **MISSING** |
| `POST /api/auth/logout` | 401 | allowed (own tokens) | allowed (own tokens) | n/a (always the caller) | `LogoutEndpointTests.TokenUsedAfterLogout_IsRejected`, `LogoutEndpointTests.Logout_WithoutLogin_IsUnauthorized`, `Logout.test.tsx` "logging out calls the API and removes the token" |
| `GET /api/units` | 401 | allowed | allowed | n/a (fixed list) | `MaterialAuthorizationTests.Viewer_CanReadTheUnits`, `MaterialAuthorizationTests.AnonymousRequest_IsUnauthorized` |
| `GET /api/materials`, `GET /api/materials/{id}` | 401 | 403 | allowed | list: empty; get: 404 | `MaterialAuthorizationTests.Viewer_IsForbiddenFromEveryMaterialEndpoint`, `MaterialAuthorizationTests.AnonymousRequest_IsUnauthorized` (list only), `MaterialIsolationTests.AnotherCompanysMaterial_IsNotListedAndCannotBeReadChangedOrDeleted`; anonymous get: **MISSING** |
| `POST`, `PUT /{id}`, `DELETE /{id}` `/api/materials` | 401 | 403 | allowed | PUT/DELETE: 404 | `MaterialAuthorizationTests.Viewer_IsForbiddenFromEveryMaterialEndpoint`, `MaterialIsolationTests.AnotherCompanysMaterial_IsNotListedAndCannotBeReadChangedOrDeleted`; anonymous: **MISSING** |
| `GET /api/works`, `GET /api/works/{id}` | 401 | 403 | allowed | list: empty; get: 404 | `WorkAuthorizationTests.Viewer_IsForbiddenFromEveryWorkEndpoint`, `WorkAuthorizationTests.AnonymousRequest_IsUnauthorized` (list only), `WorkIsolationTests.AnotherCompanysWork_IsNotListedAndCannotBeReadChangedOrDeleted` |
| `POST`, `PUT /{id}`, `DELETE /{id}` `/api/works` | 401 | 403 | allowed | PUT/DELETE: 404; foreign material in lines: 400 | `WorkAuthorizationTests.Viewer_IsForbiddenFromEveryWorkEndpoint`, `WorkIsolationTests.AnotherCompanysWork_IsNotListedAndCannotBeReadChangedOrDeleted`, `WorkEndpointsTests.Create_WithAnotherCompanysMaterial_IsRejectedAndSavesNothing`, `WorkEndpointsTests.Update_WithAnotherCompanysMaterial_IsRejectedAndKeepsTheComposition`; anonymous: **MISSING** |
| `GET /api/catalog-sections`, `GET /{id}` | 401 | 403 | allowed | list: empty; get: 404 | `CatalogSectionEndpointsTests.AnotherCompanysSection_IsNotFound_AndAViewerIsForbidden` (Viewer: list only); Viewer get: **MISSING**; anonymous: **MISSING** |
| `POST`, `PUT /{id}`, `DELETE /{id}` `/api/catalog-sections` | 401 | 403 | allowed | PUT/DELETE: 404; foreign work in lines: 400 | `CatalogSectionEndpointsTests.AnotherCompanysSection_IsNotFound_AndAViewerIsForbidden` (other company: DELETE only), `CatalogSectionEndpointsTests.AnotherCompanysWork_IsRejectedAndSavesNothing`; Viewer POST/PUT/DELETE: **MISSING**; other company PUT: **MISSING**; anonymous: **MISSING** |
| `GET /api/projects`, `GET /api/projects/{id}` | 401 | allowed (name, description; no prices) | allowed | list: empty; get: 404 | `ProjectAuthorizationTests.Viewer_CanListAndOpenProjects`, `ProjectAuthorizationTests.AnonymousRequest_IsUnauthorized`, `ProjectIsolationTests.AnotherCompanysProject_IsNotListedAndCannotBeReadChangedOrDeleted` |
| `POST`, `PUT /{id}`, `DELETE /{id}` `/api/projects` | 401 | 403 | allowed | PUT/DELETE: 404 | `ProjectAuthorizationTests.Viewer_IsForbiddenToCreateChangeOrDelete`, `ProjectIsolationTests.AnotherCompanysProject_IsNotListedAndCannotBeReadChangedOrDeleted`; anonymous: **MISSING** |
| `GET`, `POST /api/projects/{projectId}/estimates`; `GET /api/estimates/{id}` | 401 | 403 | allowed | 404 | `EstimateAuthorizationTests.Viewer_IsForbiddenFromEveryEstimateEndpoint`, `EstimateAuthorizationTests.AnonymousRequest_IsUnauthorized` (GET estimate only), `EstimateIsolationTests.AnotherCompanysEstimate_IsNotFoundForEveryEndpoint` |
| `POST /estimates/{id}/lines`, `PATCH`/`DELETE /lines/{lineId}`, `PATCH /percentages`, `PATCH /confirm`, `PATCH /draft` | 401 | 403 | allowed (Draft only, else 409) | 404; foreign work: 400 | `EstimateAuthorizationTests.Viewer_IsForbiddenFromEveryEstimateEndpoint`, `EstimateIsolationTests.AnotherCompanysEstimate_IsNotFoundForEveryEndpoint`, `EstimateEndpointsTests.AddWork_UnknownOrAnotherCompanysWork_IsRejected`, `EstimateDraftGuardTests`; anonymous: **MISSING** |
| `PATCH /lines/{lineId}/labour-rate`, `PATCH /lines/{lineId}/materials/{materialLineId}/price`, `PATCH .../material`, `DELETE .../materials/{materialLineId}` | 401 | 403 | allowed (Draft only) | 404; foreign material: refused | `EstimateLineEditingTests.Viewer_IsForbidden`, `EstimateLineEditingTests.AnotherCompanysEstimate_IsNotFound`, `EstimateLineEditingTests.LinesOfAnotherEstimateOrWorkLine_AreNotFound`, `EstimateLineEditingTests.ReplaceMaterial_WithAnotherUnitOrAnotherCompanysMaterial_IsRefused`; anonymous: **MISSING** |
| `POST /estimates/{id}/sections`, `PATCH`/`DELETE /sections/{sectionId}` | 401 | 403 | allowed (Draft only) | 404; foreign catalog section: 404 | `EstimateSectionEndpointsTests.ViewerIsForbidden_AndAnotherCompanyGetsNotFound` (Viewer: POST only; other company: DELETE and foreign catalog section), `EstimateSectionEndpointsTests.AddWork_ToASectionOrToNone_AndAnotherEstimatesSectionIsNotFound`; Viewer PATCH/DELETE: **MISSING**; other company POST to the foreign estimate and PATCH: **MISSING**; anonymous: **MISSING** |
| `GET`/`POST /estimates/{id}/refresh` | 401 | 403 | allowed (Draft only) | 404 | `EstimateRefreshEndpointTests.Viewer_IsForbidden`, `EstimateRefreshEndpointTests.AnotherCompanysEstimate_IsNotFound`; anonymous: **MISSING** |
| `GET /estimates/{id}/export` | 401 | 403 | allowed | 404 | `EstimateExportTests.Viewer_CannotExport`, `EstimateIsolationTests.AnotherCompanysEstimate_IsNotFoundForEveryEndpoint`; anonymous: **MISSING** |
| `GET /api/projects/{projectId}/estimate-names`, `GET /api/estimates/{id}/names` | 401 | allowed (names only) | allowed | 404 | `EstimateNamesTests.Viewer_ListsEveryEstimateByNameWhateverItsStatus`, `EstimateNamesTests.Viewer_OpensAnEstimateAndGetsOnlyNamesInOrder`, `EstimateNamesTests.Manager_CanUseTheNamesEndpointsToo`, `EstimateNamesTests.AnonymousRequests_AreUnauthorized`, `EstimateIsolationTests.AnotherCompanysEstimate_IsNotFoundForEveryEndpoint` |
| `GET /api/team`, `POST /api/team/viewers` | 401 | 403 | allowed | list: own company only | `TeamEndpointsTests.Viewer_CannotUseTeamEndpoints`, `TeamEndpointsTests.TeamEndpoints_WithoutLogin_AreUnauthorized` (list only), `TeamIsolationTests.AnotherCompanysViewer_IsNotListedAndCannotBeRemovedOrReset`; anonymous POST: **MISSING** |
| `DELETE /api/team/viewers/{id}`, `PATCH /api/team/viewers/{id}/password` | 401 | 403 | allowed for Viewers; a Manager: 404 | 404 | `TeamEndpointsTests.Viewer_CannotUseTeamEndpoints`, `TeamEndpointsTests.Manager_CannotBeRemovedOrHavePasswordReset`, `TeamIsolationTests.AnotherCompanysViewer_IsNotListedAndCannotBeRemovedOrReset`; anonymous: **MISSING** |

Result: the role and company checks exist on every endpoint, applied at class level, so a missing cell is a missing test, not a missing control. The gaps are listed in F2. EF warning 10622 (`Material` filtered, `WorkMaterialLine` not) was fixed in `5018d4c`: `WorkMaterialLine` is now filtered through `Work.CompanyId`, and the API builds with 0 warnings (F1, FIXED).

## 5. Input validation

Validation is declared on the request records (data annotations and `IValidatableObject`) and runs through `[ApiController]` on `ApiControllerBase`, which returns 400 before any service runs.

| Input | Rule (as implemented) | Where | Status |
|---|---|---|---|
| Route IDs | `{id:int}` route constraints; a missing or foreign ID → `NotFoundException` → 404. Zero or negative IDs are not rejected separately; they are simply not found | controllers (routes), services, `DomainExceptionHandler` | OK (`CompanyScopingTests.NotFoundException_IsReturnedAs404`, `ErrorResponseTests.MissingMaterial_Returns404`) |
| Catalog and project names | `[Required]` (rejects empty and whitespace-only) and `[StringLength(200)]` from `MaterialRules`, `WorkRules`, `CatalogSectionRules`, `ProjectRules`, `EstimateRules`; trimmed in the services; columns `varchar(200)` | request records | OK (`RequestValidationTests.MaterialRequest_EmptyName_IsRejected`, `ProjectRequestValidationTests.Name_HasAtMost200Characters`, `EstimateRequestValidationTests.EstimateNameOver200Characters_IsRejected`) |
| Project description | `[StringLength(2000)]`, optional | `ProjectRequest` | OK (`ProjectRequestValidationTests.Description_HasAtMost2000Characters`) |
| First name, last name, company name | `[Required]` only, no length limit; columns are `longtext` | `RegisterRequest`, `CreateViewerRequest` | GAP (F9) |
| Units | Must be exactly one of `vnt., m, m², m³, kg, t, l, val., kompl.` (`MeasurementUnit.All`, `MeasurementUnit.IsValid`) | `MaterialRules`, `WorkRules`, `CatalogSectionRules` | OK (`WorkRequestValidationTests.Unit_OutsideTheFixedList_IsRejected`, `RequestValidationTests.MaterialRequest_ChecksUnitAndPrice`) |
| Prices, labour rates | ≥ 0, at most 4 decimals, fits DECIMAL(18,4) (`NumberRules.UnitPriceError`) | `MaterialRequest`, `WorkRequest`, `ChangePriceRequest`, `ChangeLabourRateRequest` | OK (`WorkRequestValidationTests.LabourRate_MustBe0OrMoreWithAtMost4Decimals`, `EstimateRequestValidationTests.EstimatePriceAndLabourRate_MustBe0OrMoreWithAtMost4Decimals`) |
| Norms, coefficients | Labour norm ≥ 0; consumption norm and section coefficient > 0; at most 6 decimals, DECIMAL(18,6) (`NumberRules.NormError`) | `WorkRequest`, `WorkMaterialLineRequest`, `CatalogSectionLineRequest` | OK (`WorkRequestValidationTests.ConsumptionNorm_MustBeGreaterThan0WithAtMost6Decimals`, `WorkRequestValidationTests.LabourNorm_MayBe0WithAtMost6Decimals`; coefficient through the API: `CatalogSectionEndpointsTests.InvalidCoefficient_IsRejected`) |
| Quantities | > 0, at most 3 decimals, DECIMAL(18,3) (`NumberRules.PositiveQuantityError`) | `AddWorkLineRequest`, `ChangeQuantityRequest`, `AddSectionRequest`, `ChangeSectionQuantityRequest` | OK (`EstimateRequestValidationTests.Quantity_MustBeGreaterThan0WithAtMost3Decimals`) |
| Waste, overhead, profit, VAT | 0–100 inclusive, at most 2 decimals, DECIMAL(5,2) (`NumberRules.PercentageError`) | `WorkMaterialLineRequest`, `ChangePercentagesRequest` | OK (`WorkRequestValidationTests.WastePercent_MustBeFrom0To100WithAtMost2Decimals`, `EstimateRequestValidationTests.EachPercentage_MustBeFrom0To100WithAtMost2Decimals`) |
| Email | `[Required]`, `[EmailAddress]`; unique, case-insensitive (`utf8mb4_0900_as_ci`). The column is `varchar(256)` but the request has no `[StringLength]`, so a longer email reaches MySQL and would fail as a 500 instead of a 400. `LoginRequest.Email` has `[Required]` only | `RegisterRequest`, `CreateViewerRequest`, `LoginRequest` | PARTIAL (F9) |
| Password | At least 8 characters and not equal to the email, ignoring case (`PasswordRules.Validate`), declared through `IValidatableObject`; `ResetPasswordRequest` checks the length, `TeamService` checks the stored email with `PasswordRules.EnsureNotEmail`. No maximum length | `RegisterRequest`, `CreateViewerRequest`, `ResetPasswordRequest` | OK, maximum missing (F9) (`RequestValidationTests.RegisterRequest_PasswordShorterThan8Characters_IsRejected`, `RequestValidationTests.RegisterRequest_PasswordEqualToEmail_IsRejected`, `TeamEndpointsTests.CreateViewer_WeakPassword_IsRejected`, `TeamEndpointsTests.ResetPassword_WeakPassword_IsRejectedAndOldPasswordStillWorks`) |
| Search text (materials, works, catalog sections) | `?search=` used only as a LIKE parameter, with `%`, `_`, `\` escaped (`SearchPattern.Contains`); no length limit | services | PARTIAL (F9) (`MaterialSearchTests.Search_PercentSign_MatchesLiterally`) |
| Request body size and paging | Kestrel's default request body limit (about 30 MB) applies; nothing is configured. List endpoints return every record of the company; there is no paging | host configuration | GAP (F9) |

## 6. Injection review

Searches run on 2026-11-23 over the tracked files in `server/`, `tests/` and `client/src`:

| Search | Result |
|---|---|
| `FromSql`, `FromSqlRaw`, `ExecuteSql`, `ExecuteSqlRaw`, `SqlQuery`, `DbCommand`, `CommandText` | none |
| String-built SQL (`$"SELECT`, `$"INSERT`, `$"UPDATE`, `$"DELETE`) | none |
| `migrationBuilder.Sql` in migrations | none |
| `Process.Start` (shell commands) | none |
| File paths from user input | none. The export file name is built from the estimate name with control and invalid file-name characters replaced (`EstimateExportService`, line 183) and sent only as the download name |
| `dangerouslySetInnerHTML`, `eval(`, `new Function`, `innerHTML`, `document.write` in `client/src` | none |

- Data access is EF Core with LINQ only; values are parameterized by the provider.
- Search uses `EF.Functions.Like(name, pattern, SearchPattern.Escape)` in `MaterialService`, `WorkService` and `CatalogSectionService`. The user value is a parameter, never concatenated into SQL, and LIKE wildcards are escaped. OK.
- Excel export: every text cell is written as text with a quote prefix (`EstimateExportService.Text`, NFR-007). OK (`EstimateExportTests.FormulaLikeNames_AreExportedAsTextNotFormulas`).
- No test sends an SQL-looking value such as `' OR 1=1 --`; the code search above is the evidence. A test is proposed in F2's PR.

## 7. Secrets management

| Secret | Local | CI | Deployment |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | .NET user secrets (`UserSecretsId` in `SmartEstimatePlatform.Api.csproj`); the app refuses to start without it (`Program.cs`) | no CI exists; the tests replace it with in-memory SQLite (`ApiFactory`) | environment variable `ConnectionStrings__DefaultConnection` |
| `Jwt:Key` | .NET user secrets; at least 32 characters, checked at start-up | no CI exists; the tests use the constant `ApiFactory.JwtKey` ("integration-test-signing-key-…"), which is not a real secret | environment variable `Jwt__Key` |

Checks run on 2026-11-23:
- Tracked config: `appsettings.json` holds only logging, `AllowedHosts` and the JWT issuer, audience and lifetime; no key or connection string. `appsettings.Development.json` exists locally but is ignored (`.gitignore` line 22) and has never been committed. `Properties/launchSettings.json` holds only URLs and `ASPNETCORE_ENVIRONMENT`.
- `server/.mcp.json`: ignored (`.gitignore` line 38) and never committed; it holds only a local Rider MCP URL (`http://127.0.0.1:…`), no credentials.
- `.gitignore` covers `.env`, `.env.*`, `*.user`, `appsettings.Development.json`, `appsettings.*.local.json`, `*.db`, `server/.mcp.json`. `secrets.json` is not listed, but user secrets are stored outside the repository (`~/.microsoft/usersecrets/`), so it does not need to be.
- History (175 commits, all branches): `git log -p -S"Password="` finds only the documented dummy `Server=localhost;Database=unused;User=unused;Password=unused` in spec quickstarts; `git log -p -G'Jwt__Key|Jwt:Key|"Key":'` finds only the test key and the start-up check; `git log -p -S"Jwt"` and `-S"Key"` find code and docs only. A pattern scan for GitHub tokens, API keys, AWS keys and private keys found nothing. No secret file was ever added and deleted.

Result: no real secret in tracked files or history. OK.

## 8. Secure communication

From `server/SmartEstimatePlatform.Api/Program.cs`:

| Item | Finding | Status |
|---|---|---|
| HTTPS | `UseHttpsRedirection()` always; `UseHsts()` outside Development. With the `http` launch profile, the development warning "Failed to determine the https port for redirect" is expected and harmless: there is no HTTPS port, so nothing is redirected. In production the host must listen on HTTPS | OK (`HstsTests.OutsideDevelopment_ResponseHasStrictTransportSecurity`, `HstsTests.InDevelopment_ResponseHasNoStrictTransportSecurity`) |
| CORS | No `AddCors`/`UseCors`, so the browser's same-origin policy refuses cross-origin reads. The client reaches the API through the Vite dev proxy (`client/vite.config.ts`, `/api` → `http://localhost:5084`), so no CORS is needed. If the client is ever hosted on a different origin, a named policy with only that origin must be added | OK by design, no test |
| Allowed hosts | `"AllowedHosts": "*"` in `appsettings.json`: host filtering is off | GAP, Low (F7) |
| Cookies | Not used: the token is sent in the `Authorization: Bearer` header, so cookie flags and CSRF do not apply | OK by design |
| Security headers | None of `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` / `frame-ancestors`, `Referrer-Policy`, `Content-Security-Policy` is set by the API or by `client/index.html`. CSP matters because the token is in `localStorage` | GAP (F7) |
| Rate limiting | No `AddRateLimiter`/`UseRateLimiter`; login and register are unlimited | GAP (F4) |

## 9. Logging and error handling

The application code has no `ILogger` usage (`git grep ILogger server` finds nothing). Only framework logging runs, with `Microsoft.AspNetCore` at `Warning` (`appsettings.json`).

| Event | Logged today | Client response | Status |
|---|---|---|---|
| Failed login | No | 401 "Invalid email or password." | GAP (F8) |
| Account locked | No | 423 with `minutesRemaining` | GAP (F8) |
| Access denied (403) | No (framework authorization messages are below `Warning`) | 403, empty body | GAP (F8) |
| Cross-company access (404) | No (`DomainExceptionHandler` handles it, and .NET 10 does not log exceptions handled by an `IExceptionHandler`) | 404 `ProblemDetails` "Not found" | GAP (F8) |
| Important actions (confirm estimate, remove Viewer, reset password, delete material/work/section/project) | No | normal response | GAP (F8) |
| Unhandled exception | Yes: the exception-handler middleware logs it at `Error` with the stack trace | generic 500 `ProblemDetails`, no type, message or stack trace | OK (`UnexpectedErrorTests.UnexpectedFailure_Returns500WithoutTechnicalDetails`) |

Error handling:
- `UseExceptionHandler()` + `AddProblemDetails()` + `DomainExceptionHandler` (`Controllers/DomainExceptionHandler.cs`) map domain exceptions to 400/401/404/409/423. Duplicate errors use fixed texts so they do not echo the email or name (`ErrorResponseTests.*`).
- In Development, ASP.NET Core adds the developer exception page automatically, so stack traces are shown on a developer's machine only.
- Multi-step writes: every service operation ends with one `SaveChangesAsync` (or `UniqueSave.SaveAsync`), which EF Core runs in one transaction, so a failure leaves no half-written state. No explicit transactions are needed today.

## 10. Dependency review

Run on 2026-11-23:

```
dotnet list server/SmartEstimatePlatform.slnx package --vulnerable --include-transitive
dotnet list server/SmartEstimatePlatform.slnx package --outdated
cd client && npm audit && npm audit --omit=dev && npm outdated
```

Results:
- `dotnet list package --vulnerable --include-transitive`: **no vulnerable packages** in `SmartEstimatePlatform.Api` or `SmartEstimatePlatform.Api.Tests`.
- `dotnet list package --outdated`: every Microsoft package is on 10.0.12, the latest .NET 10 patch; 11.0.0 is the next major (.NET 11) and needs a framework upgrade. Test tools: `Microsoft.NET.Test.Sdk` 17.14.1 (latest 18.12.0), `xunit.runner.visualstudio` 3.1.4 (latest 4.0.1). `ClosedXML` 0.105.1 is current.
- `npm audit`: **9 moderate**, 0 high, 0 critical, all in dev tooling: `minimatch` 7.0.0–11.0.0 (GHSA-g3hv-75wg-m249, denial of service through glob patterns), reached through `eslint` 10.12.0 and `typescript-eslint` 8.71.1 (`@typescript-eslint/*`, `@eslint/config-array`). `npm audit fix` resolves them within the current ranges (eslint 10.15.0, typescript-eslint 8.73.0).
- After T018 (2026-11-23): `npm audit` reports **0 vulnerabilities** (F11 fixed).
- `npm audit --omit=dev`: **0 vulnerabilities** in the shipped dependencies (`react`, `react-dom`, `react-router-dom`).
- `npm outdated`: patch/minor updates within the ranges for `react`/`react-dom` 19.3.1, `react-router-dom` 7.18.6, `vite` 8.3.8, `vitest` 5.1.1, `@vitejs/plugin-react` 6.3.0, `tailwindcss`/`@tailwindcss/vite` 4.3.5 (pinned), `@testing-library/user-event` 14.6.13; majors available for `typescript` 7, `jsdom` 31, `@types/node` 26.

| Package | Purpose | Used? | Decision |
|---|---|---|---|
| Microting.EntityFrameworkCore.MySql 10.0.12 | MySQL provider | yes (`Program.cs`) | keep |
| Microsoft.EntityFrameworkCore.Design 10.0.12 | migrations tooling (private assets) | yes (tooling) | keep |
| Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12 | JWT validation | yes (`Program.cs`) | keep |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.12 | only `PasswordHasher<User>` is used (`AuthService`, `TeamService`, `ServiceCollectionExtensions`); no `IdentityDbContext`, no Identity stores | partly | **removed** 2026-11-23: `PasswordHasher<TUser>` comes from the ASP.NET Core shared framework (F12, FIXED) |
| ClosedXML 0.105.1 | Excel export | yes (`EstimateExportService`) | keep |
| Swashbuckle.AspNetCore / OpenAPI | Swagger | **not referenced**: removed in feature 018 (`a992b13`) | nothing to do |
| xUnit, Microsoft.AspNetCore.Mvc.Testing, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.NET.Test.Sdk | tests | yes | keep |
| react, react-dom, react-router-dom | frontend runtime | yes | keep |
| vite, @vitejs/plugin-react, typescript, tailwindcss, @tailwindcss/vite, vitest, jsdom, @testing-library/react, @testing-library/user-event, eslint, @eslint/js, typescript-eslint, eslint-plugin-react-hooks, eslint-plugin-react-refresh, globals, @types/react, @types/react-dom, @types/node | build, lint, tests | yes (imports, `eslint.config.js`, `vite.config.ts`, `tsconfig.node.json` types) | keep; eslint and typescript-eslint updated by T018 (F11, FIXED) |

There is no CI, so none of these audits runs automatically (F10).

## 11. OWASP Top 10 and ASVS review

| Category | Risk | Application area | Existing protection | Remaining limitation | Verification |
|---|---|---|---|---|---|
| A01 Broken Access Control (ASVS V4) | Horizontal and vertical privilege escalation; cross-company access | All controllers, `AppDbContext` | Class-level role attributes; global company query filters incl. child entities; `CompanyId` forced on new records; 404 for foreign resources; F1 (EF 10622) fixed | Test matrix has gaps (F2) | `*AuthorizationTests`, `*IsolationTests`, `CompanyScopingTests` |
| A02 Security Misconfiguration (V14) | Missing headers, open host filter, verbose errors | `Program.cs`, `appsettings.json` | Secrets outside the repo; HSTS outside Development; HTTPS redirection; no CORS (same origin); generic 500 | No security headers or CSP; `AllowedHosts: "*"` (F7) | `HstsTests.*`, `UnexpectedErrorTests.*` |
| A03 Software Supply Chain Failures (V15) | Vulnerable or unneeded packages | NuGet, npm | `package-lock.json`; pinned NuGet versions; no runtime vulnerabilities today | No CI and no automated audit (F10); F11 and F12 fixed (0 npm advisories; unneeded Identity package removed) | Section 10 commands |
| A04 Cryptographic Failures (V11) | Weak hashing, weak JWT key, secrets in repo | Auth, configuration | `PasswordHasher` (PBKDF2); HMAC-SHA256 tokens; key ≥ 32 characters checked at start-up; no secrets in history | No test of the key check or of forged tokens (F3) | Section 7 history search |
| A05 Injection (V5) | SQL, formula (Excel) and XSS injection | Data access, search, export, React | EF Core parameters; escaped LIKE patterns; export cells written as text; React escaping; no raw HTML sinks | No SQL-injection input test (proposed in F2) | Section 6 searches; `MaterialSearchTests.Search_PercentSign_MatchesLiterally`; `EstimateExportTests.FormulaLikeNames_AreExportedAsTextNotFormulas` |
| A06 Insecure Design (V1) | Business rule bypass (edit confirmed estimate, delete used material/work) | Services | Draft-only guard (409); FK `Restrict` + 409; rules in services | — | `EstimateDraftGuardTests`, `EstimateLineEditingTests.ConfirmedEstimate_RefusesTheChange`, `MaterialDeleteInUseTests.*`, `CatalogSectionEndpointsTests.WorkUsedInASection_CannotBeDeletedUntilTheSectionIsGone` |
| A07 Authentication Failures (V2, V3) | Credential stuffing, account enumeration, token reuse | Auth | Hashing; lockout; generic 401; `TokenVersion` and user check on every request | No rate limiting (F4); lockout reveals accounts and can lock out a known user (F5, accepted; timing difference fixed); no MFA; token in `localStorage` | `LoginAsyncTests.*`, `TeamEndpointsTests.ResetPassword_RejectsTokensIssuedBeforeIt` |
| A08 Software or Data Integrity Failures (V13) | Tampered tokens, unreviewed changes | JWT, repository | Signature validation; PR workflow (feature branches, PRs to `main`) | No CI checks on PRs (F10); branch protection not available on the free private plan; no tampered-token test (F3) | PR history |
| A09 Security Logging and Alerting Failures (V16) | Attacks not detected | Logging | Framework logs unhandled exceptions | No security event logging at all (F8); no alerting | — |
| A10 Mishandling of Exceptional Conditions (V16) | Stack trace leak, half-written state, DB errors as 500 | Global error handling, database writes | `UseExceptionHandler` + `ProblemDetails`; `DomainExceptionHandler`; one `SaveChangesAsync` per operation (atomic); `UniqueSave` turns unique-index races into 409 | Over-long email or names reach the database and fail as 500 (F9) | `UnexpectedErrorTests.UnexpectedFailure_Returns500WithoutTechnicalDetails`, `ErrorResponseTests.*` |

## 12. Findings and fixes

All fixes follow: issue/specification → branch → implementation → security test → PR → CI → merge → branch deletion.

| ID | Finding | Severity | Spec/task | Branch | Status |
|---|---|---|---|---|---|
| F1 | EF warning 10622: `Material` had a global query filter and `WorkMaterialLine` did not, so a line could be loaded without its company filter. Fixed in `5018d4c`: `WorkMaterialLine` is filtered through `Work.CompanyId`; build shows 0 warnings | High | — | `fix/work-line-query-filter` (PR #12, merged) | **FIXED** (`CompanyScopingTests.WorkMaterialLines_AreScopedThroughTheirWork`, `WorkEndpointsTests.Update_WithAnotherCompanysMaterial_IsRejectedAndKeepsTheComposition`) |
| F2 | Authorization test matrix incomplete: anonymous is tested on one endpoint per controller only; catalog sections lack Viewer get/create/update/delete, other-company PUT and anonymous tests; estimate sections lack Viewer PATCH/DELETE and other-company POST/PATCH; `GET /api/auth/me` anonymous untested; no SQL-injection input test | Medium | 021 T004–T008 | `021-authz-tests` | open |
| F3 | No tests for forged tokens (bad signature, `alg=none`, expired, wrong issuer/audience) or for the start-up check of a short `Jwt:Key` | Medium | 021 T009–T010 | `021-authz-tests` | open |
| F4 | No rate limiting on `POST /api/auth/login` and `POST /api/auth/register` | Medium | 021 T011 | `021-http-hardening` | open |
| F5 | Login reveals that an account exists (423 only for existing accounts; unknown email skips hashing and answers faster), and anyone knowing an email can keep that account locked | Medium | 021 T001–T002 | `021-auth-hardening` | **PARTIAL**: timing difference fixed (`LoginAsyncTests.LoginAsync_UnknownEmail_HashesLikeAKnownEmail`); 423 difference and lock-out risk accepted (spec 021 Q1, Accepted limitations), reduced by rate limiting (F4) |
| F6 | Logout is client-only; a token copied before logout stays valid up to 60 minutes | Medium | 021 T003 | `021-auth-hardening` | **FIXED** (`LogoutEndpointTests.TokenUsedAfterLogout_IsRejected`, `Logout.test.tsx` "logging out calls the API and removes the token") |
| F7 | No security headers (`X-Content-Type-Options`, `X-Frame-Options`/`frame-ancestors`, `Referrer-Policy`, `Content-Security-Policy`); `AllowedHosts: "*"` | Medium | 021 T012–T013 | `021-http-hardening` | open |
| F8 | No security event logging: failed logins, lockouts, 403, cross-company 404s and destructive actions are not logged | Medium | 021 T014–T015 | `021-error-logging` | open |
| F9 | Missing length limits: first name, last name and company name (`longtext`), email (column 256, request unlimited → DB error → 500), password and login fields, search text; no paging on list endpoints | Low | 021 T016 | `021-error-logging` | open |
| F10 | No CI pipeline: tests and dependency audits run only by hand; PRs are merged without automated checks | Medium | 021 T017 | `021-ci-and-dependencies` | **FIXED** (`.github/workflows/ci.yml`; first green run on PR #27, 2026-11-23: backend and frontend jobs pass) |
| F11 | `npm audit`: 9 moderate advisories in dev tooling (`minimatch` GHSA-g3hv-75wg-m249 via eslint / typescript-eslint); 0 in runtime dependencies. Fixed 2026-11-23 by `npm audit fix` (lockfile only: eslint 10.15.0, typescript-eslint 8.73.0, minimatch 11.0.9) | Low | 021 T018 | `021-ci-and-dependencies` | **FIXED** (`npm audit`: 0 vulnerabilities; lint, build and 123 client tests pass) |
| F12 | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` was referenced only for `PasswordHasher<User>`, which the shared framework already provides. Removed 2026-11-23; the API builds with 0 warnings | Low | 021 T019 | `021-ci-and-dependencies` | **FIXED** (`PasswordHashingTests.StoredHash_IsNotThePlainPassword`, `LoginAsyncTests.LoginAsync_ValidCredentials_ReturnsToken`, `RegisterAsyncTests.RegisterAsync_ValidData_CreatesCompanyAndManager`) |

Not findings (checked and OK): raw SQL and string-built queries (none), XSS sinks (none), secrets in the repository or history (none), Excel formula injection (neutralised and tested), unused Swashbuckle (already removed in feature 018), generic 500 responses (tested), HSTS (tested).

## Accepted limitations

- JWT is kept in `localStorage`; mitigated by React escaping and no raw HTML rendering. A Content Security Policy is planned (F7). A cookie-based flow is out of scope.
- No MFA, email verification or password reset by email (out of scope).
- Registration and Create Viewer reveal whether an email is already used (409).
- Keep per-account lockout; document the account-existence and lock-out risk as an accepted limitation; rate limiting (T011) reduces abuse.
- The fifth failed login returns 423 only for an existing account, so account existence can still be found that way (T001 removes only the timing difference).
- No intrusion alerting; logs only (once F8 is done).
- In Development, the developer exception page shows stack traces on the developer's machine.

## 13. Proposal: `specs/021-security-hardening/tasks.md`

Not created yet; this is the proposed content. Each PR is small and independent, and each task names the security test that proves it. Every PR runs `dotnet test` and `npm run test` before it is opened.

### PR 1 — `021-auth-hardening`: company boundary and authentication state (F5, F6)

Tenant isolation itself has no open finding (F1 fixed); this PR keeps it guarded and fixes the login and logout weaknesses.

- [X] T001 [F5] `AuthService.LoginAsync`: for an unknown email, verify the password against a fixed dummy hash so both paths take the same time. This fixes only the timing difference; the 423 difference stays an accepted limitation. Test: `LoginAsyncTests.LoginAsync_UnknownEmail_HashesLikeAKnownEmail` (the fake hasher records one verify call for each).
- [X] T002 [F5] Keep per-account lockout; document the account-existence and lock-out risk as an accepted limitation; rate limiting (T011) reduces abuse.
- [X] T003 [F6] Add `POST /api/auth/logout` (`[Authorize]`): `AuthService.LogoutAsync` calls `User.InvalidateTokens()`; the client calls it before removing the token. Test: `LogoutEndpointTests.TokenUsedAfterLogout_IsRejected` (server) and `Logout.test.tsx` "logging out calls the API and removes the token" (client).

### PR 2 — `021-authz-tests`: complete the authorization matrix (F2, F3)

Tests only; no production code change expected. Any failing test is a real bug and gets its own fix in this PR.

- [ ] T004 [F2] Anonymous matrix: one theory over every protected route in section 4 (method + URL). Test: `AnonymousAccessTests.EveryProtectedEndpoint_Returns401`.
- [ ] T005 [F2] Catalog sections, Viewer and other company. Tests: `CatalogSectionAuthorizationTests.Viewer_IsForbiddenFromEveryCatalogSectionEndpoint` (list, get, create, update, delete) and `CatalogSectionIsolationTests.AnotherCompanysSection_CannotBeUpdated`.
- [ ] T006 [F2] Estimate sections. Tests: `EstimateSectionAuthorizationTests.Viewer_CannotChangeOrRemoveASection` and `EstimateSectionIsolationTests.AnotherCompanysEstimate_SectionsAreNotFound` (POST to the foreign estimate, PATCH, DELETE).
- [ ] T007 [F2] `GET /api/auth/me` without a token. Test: covered by `AnonymousAccessTests.EveryProtectedEndpoint_Returns401` (route listed explicitly).
- [ ] T008 [F2] SQL-looking search input. Test: `MaterialSearchTests.Search_SqlLikeText_IsTreatedAsData` (`' OR 1=1 --` returns no rows and the other company's data stays hidden); same for works and catalog sections.
- [ ] T009 [F3] Forged tokens. Test: `TokenValidationTests.ForgedToken_IsRejected` theory: wrong signing key, `alg=none`, expired (via the test clock), wrong issuer, wrong audience, changed `company_id` claim → 401.
- [ ] T010 [F3] Start-up key check. Test: `JwtOptionsStartupTests.ShortKey_StopsTheApp` (factory with a 31-character key throws `OptionsValidationException`).

### PR 3 — `021-http-hardening`: rate limiting, headers, hosts (F4, F7)

- [ ] T011 [F4] Add the built-in rate limiter (`AddRateLimiter`/`UseRateLimiter`, no new package) with a fixed-window policy per client IP on `login` and `register` (default 10 requests per 60 seconds, configurable), returning 429 with `ProblemDetails`. Test: `RateLimitTests.Login_OverTheLimit_Returns429` and `RateLimitTests.Register_OverTheLimit_Returns429`; existing auth tests keep passing (limits configurable for the test factory).
- [ ] T012 [F7] Add a small middleware in `Program.cs` that sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` on API responses; document the CSP for the hosted client in `docs/architecture.md`. Test: `SecurityHeadersTests.EveryResponse_HasTheSecurityHeaders` (a 200, a 401 and a 404 response).
- [ ] T013 [F7] Outside Development, take `AllowedHosts` from the environment variable and refuse to start while it is `*`; `appsettings.json` keeps `*` for local runs, tests and CI. Test: `HostFilteringTests.UnknownHost_IsRejectedOutsideDevelopment` and `HostFilteringTests.WildcardHosts_StopTheAppOutsideDevelopment`.

### PR 4 — `021-error-logging`: security logging and input limits (F8, F9)

- [ ] T014 [F8] Log security events with `ILogger` in the services, without passwords or tokens: failed login (user id or email hash), lockout (user id), Viewer removal, password reset, estimate confirm, deletes (user id + entity id). Test: `SecurityLoggingTests.FailedLogin_IsLoggedWithoutThePassword` and `SecurityLoggingTests.RemoveViewer_IsLogged` (a test logger provider captures the entries).
- [ ] T015 [F8] Log 403 and cross-company 404 responses as warnings with user id and route, no body: authorization-failure logging through an `IAuthorizationMiddlewareResultHandler` and a log call in `DomainExceptionHandler` for `NotFoundException`. Test: `SecurityLoggingTests.ViewerCallingAManagerEndpoint_IsLogged` and `SecurityLoggingTests.AnotherCompanysId_IsLoggedAsNotFound`.
- [ ] T016 [F9] Add shared length rules (e.g. a `PersonRules` with names 100, email 256, password 128, company name 200, search 200) on `RegisterRequest`, `CreateViewerRequest`, `LoginRequest`, `ResetPasswordRequest` and the search parameters; migration for the `FirstName`, `LastName` and `Company.Name` columns. Test: `RequestValidationTests.RegisterRequest_EmailOver256Characters_IsRejected`, `RequestValidationTests.RegisterRequest_NamesOverTheLimit_AreRejected`, `MaterialSearchTests.Search_OverTheLimit_Returns400`. Paging stays out of scope until a list grows large (YAGNI); record that decision in the spec.

### PR 5 — `021-ci-and-dependencies`: CI and packages (F10, F11, F12)

- [X] T017 [F10] The developer adds `.github/workflows/ci.yml` by hand (the agent does not edit `.github/`; actions pinned to major version tags): `dotnet build`, `dotnet test`, `dotnet list package --vulnerable --include-transitive` (fail on any result), `npm ci`, `npm run lint`, `npm run build`, `npm run test`, `npm audit --omit=dev --audit-level=moderate`; dummy `Jwt__Key` from the workflow env. Test: the workflow run on the PR is green; a deliberate failing test on a throwaway branch makes it red.
- [X] T018 [F11] `npm audit fix` (eslint 10.15.0, typescript-eslint 8.73.0). Test: `npm audit` reports 0 vulnerabilities; `npm run lint`, `npm run build`, `npm run test` pass.
- [X] T019 [F12] Remove `Microsoft.AspNetCore.Identity.EntityFrameworkCore` if `PasswordHasher<User>` still resolves from the shared framework; otherwise keep it and note why in the plan. Test: `LoginAsyncTests.LoginAsync_ValidCredentials_ReturnsToken` and `RegisterAsyncTests.RegisterAsync_ValidData_CreatesCompanyAndManager` pass, plus a new `PasswordHashingTests.StoredHash_IsNotThePlainPassword`.
