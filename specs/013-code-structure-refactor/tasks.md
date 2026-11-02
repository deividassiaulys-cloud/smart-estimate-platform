# Tasks: Code Structure Refactor (Article X)

**Input**: Design documents from `specs/013-code-structure-refactor/` (spec.md, plan.md, research.md, data-model.md, contracts/api-changes.md, quickstart.md)

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering (last used: T120).

**Rule for every task**: behaviour stays the same (spec FR-014). Tasks marked 🔗 cannot stay green on their own and are done and committed as one unit with the tasks named in them. After each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` (in client/) must all pass. Tasks marked 🗄 also need `dotnet ef migrations has-pending-model-changes --project server/SmartEstimatePlatform.Api` to report no changes. Existing test assertions may not change, except where a task says so explicitly.

## Phase 1: Setup

- [X] T121 Run the full check set on the untouched branch and record the baseline (the exact number of backend tests `dotnet test` reports, including every theory case, all green; 2 frontend test files green; lint and build clean; no pending model changes) in specs/013-code-structure-refactor/quickstart.md; save the Development `/openapi/v1.json` to specs/013-code-structure-refactor/baseline-openapi.json for later comparison

## Phase 2: Foundational (blocks both stories)

- [X] T122 Add characterization tests that pin the current error responses through the real API (ApiFactory) in tests/server/ErrorResponseTests/ErrorResponseTests.cs:
  - 401 login with a wrong password: status, title, detail "Invalid email or password."
  - 423 after 5 failed logins: status, detail, `minutesRemaining` extension present and > 0
  - 409 register with an existing email, and 409 create Viewer with an existing email: detail "An account with this email already exists."
  - 409 duplicate material: detail "A material with this name and unit already exists."
  - 404 missing material and missing Viewer: title "Not found", detail "Material not found." / "Viewer not found."
  - 400 for each invalid input, asserting only the status and that nothing was saved (Viewer, reset and material cases are already pinned this way in TeamEndpointsTests and MaterialEndpointsTests; register cases added here): register password "Abc1234" (7 chars), register password equal to the email, create Viewer with a short password, reset password "short", material price −0.01, price 12.345, unit "pcs", blank name "   "

## Phase 3: User Story 1 — Backend follows Article X (Priority: P1) 🎯 MVP

**Goal**: Backend of features 001–004 complies with Article X.2–X.10.
**Independent Test**: All backend tests, including T122, pass; `has-pending-model-changes` reports no changes; the SC-004 grep checks from quickstart.md hold.

### Service interfaces (X.3)

- [X] T123 [P] [US1] Add IAuthService, ITokenService, ITeamService, IMaterialService (the public methods of each service, with doc comments) next to their implementations in server/SmartEstimatePlatform.Api/Services/, and make each service implement its interface
- [X] T124 [US1] Add `AddApplicationServices(this IServiceCollection)` in server/SmartEstimatePlatform.Api/Services/ServiceCollectionExtensions.cs registering ICurrentCompany, IPasswordHasher<User>, the four interfaces and TimeProvider.System, and replace those registrations in server/SmartEstimatePlatform.Api/Program.cs with one call
- [X] T125 [US1] Switch every consumer to the interfaces: AuthController, TeamController, MaterialsController, TeamService (IAuthService), AuthService (ITokenService), `OnTokenValidated` in Program.cs (IAuthService), and `GetRequiredService<TokenService>` in tests/server/CompanyIsolationTests/CompanyScopingTests.cs (ITokenService)

### DTOs (X.4, X.5, X.1)

- [X] T126 [US1] Move the DTOs into feature folders and namespaces: Models/DTOs/Auth (RegisterRequest, RegisterResponse, LoginRequest, LoginResponse, CurrentUserResponse), Models/DTOs/Team (CreateViewerRequest, ResetPasswordRequest, TeamMemberResponse), Models/DTOs/Materials (MaterialRequest, MaterialResponse) under server/SmartEstimatePlatform.Api/Models/DTOs; update usings in server and tests/server
- [X] T127 [US1] Convert all DTOs to `sealed record` with `{ get; init; }` properties, keeping every property, default, data annotation and doc comment (not positional records, see research R1); in tests/server replace in-place changes such as `request.Password = ...` and `duplicate.Email = ...` with `with` expressions (setup change only)
- [X] T128 [US1] Convert server/SmartEstimatePlatform.Api/Services/JwtOptions.cs to a `sealed record` with init properties (`Key`, `Issuer`, `Audience`, `ExpiresMinutes = 60`, `SectionName = "Jwt"`), keeping the binding and `Key.Length >= 32` start-up validation in Program.cs
- [X] T129 [US1] Merge RegisterResponse and LoginResponse into one `AuthResponse` record (Token, UserId, CompanyId, Role) in server/SmartEstimatePlatform.Api/Models/DTOs/Auth/AuthResponse.cs; delete the two old files and update AuthService, AuthController and tests/server/TestSupport
- [X] T130 [US1] Add the single mapper `DtoMapper` (with purpose doc comments) (static extension methods `ToResponse(this Material)`, `ToResponse(this User)` → TeamMemberResponse, `ToCurrentUserResponse(this User)`, `ToAuthResponse(this User, string token)`) in server/SmartEstimatePlatform.Api/Services/Mapping/DtoMapper.cs; delete `MaterialResponse.From` and `TeamMemberResponse.From`, and replace every mapping in controllers and AuthService with the mapper
- [X] T131 [US1] Remove entity-class references from DTOs: add `MaterialRules` (with purpose doc comments) in server/SmartEstimatePlatform.Api/Services/MaterialRules.cs with `NameMaxLength = 200` (moved from Material) and `MaxPrice = 9_999_999_999_999_999.99m` (moved from MaterialService); point MaterialRequest, Material, AppDbContext and MaterialService at it. DTOs may still reference `UserRole` and `MeasurementUnit` (owner decision)

### Validation on request records (X.10)

- [X] T132 [US1] Make PasswordRules the single definition in server/SmartEstimatePlatform.Api/Services/PasswordRules.cs: add `string? Validate(string password, string? email)` returning "Password must be at least 8 characters long." or "Password must not be the same as the email." (email compared trimmed and case-insensitively, only when email is given); keep `EnsureStrong` built on it for the stored-email check
- [X] T133 [US1] 🔗 (one unit with T135; write T135's new tests first) Implement `IValidatableObject` with PasswordRules.Validate on RegisterRequest (Password, Email) and CreateViewerRequest (Password, Email), and on ResetPasswordRequest (NewPassword, no email); remove the PasswordRules.EnsureStrong call from AuthService.CreateAccountAsync; keep it in TeamService.ResetPasswordAsync only for the "not equal to the Viewer's stored email" check
- [X] T134 [US1] Add `MaterialRules.Validate(string unit, decimal price)` returning the existing texts ("Unit must be one of the fixed measurement units.", "Price cannot be negative.", "Price can have at most 2 decimal places.", "Price is too large."); implement `IValidatableObject` on MaterialRequest with it, keeping `[Required]` and `[StringLength(MaterialRules.NameMaxLength)]` on Name; reduce MaterialService.Apply to trimming the name and setting the values; delete server/SmartEstimatePlatform.Api/Services/InvalidMaterialException.cs and its catch in MaterialsController
- [X] T135 [US1] 🔗 (one unit with T133) Move the service-level weak-password tests (`RegisterAsync_PasswordShorterThan8Characters_IsRejectedAndCreatesNothing`, `RegisterAsync_PasswordEqualToEmail_IsRejectedAndCreatesNothing` with both cases) from tests/server/AuthServiceTests/RegisterAsyncTests.cs to tests/server/RequestValidationTests/RequestValidationTests.cs as `Validator.TryValidateObject(..., validateAllProperties: true)` tests with the same inputs; add the same style of tests for CreateViewerRequest, ResetPasswordRequest and MaterialRequest (−0.01, 12.345, "pcs" rejected; 0 and 12.34 accepted). This is the only allowed assertion change (spec SC-001)

### Thin controllers and one error handler (X.2, X.8)

- [X] T136 [US1] Rename ICurrentCompany/HttpCurrentCompany to ICurrentUser/HttpCurrentUser in server/SmartEstimatePlatform.Api/Services and add `int? UserId` (from the token's `sub` claim) next to `CompanyId`; update AppDbContext, AddApplicationServices, TeamService and tests/server/TestSupport (FixedCurrentCompany → FixedCurrentUser)
- [X] T137 [US1] Make services return DTOs through DtoMapper: MaterialService (MaterialResponse / list), TeamService (TeamMemberResponse / list), AuthService.RegisterAsync → AuthResponse (creating the token itself, like LoginAsync), and new `AuthService.GetCurrentUserAsync()` → CurrentUserResponse, which uses ICurrentUser.UserId and throws UnauthorizedAccessException when the user is not found; update the interfaces and the tests that read service results
- [X] T138 [US1] Replace server/SmartEstimatePlatform.Api/Controllers/NotFoundExceptionHandler.cs with DomainExceptionHandler.cs (with purpose doc comments) mapping exactly as research R4: NotFoundException → 404, title "Not found", detail = message; WeakPasswordException → 400 with detail = message; DuplicateEmailException → 409 "An account with this email already exists."; DuplicateMaterialException → 409 "A material with this name and unit already exists."; InvalidCredentialsException → 401 "Invalid email or password."; AccountLockedException → 423 with detail = message and `minutesRemaining`; UnauthorizedAccessException → 401 with title "Unauthorized" and no detail (as today's `Unauthorized()`). Register it in Program.cs; update the handler test in tests/server/CompanyIsolationTests/CompanyScopingTests.cs to the new class name
- [X] T139 [US1] Add server/SmartEstimatePlatform.Api/Controllers/ApiControllerBase.cs (`[ApiController]`, class-level `[ProducesResponseType<ValidationProblemDetails>(400)]`); make all four controllers inherit it; declare 401/403 once at class level on MaterialsController and TeamController; keep only action-specific codes (201/204/404/409/423) on actions
- [X] T140 [US1] Reduce every action in AuthController, TeamController and MaterialsController to one service call returning its result (201 only via `StatusCode(201, ...)`, as today; not `CreatedAtAction`, which would add a Location header; 204 via `NoContent()`); no try/catch, mapping or claim parsing remains. T122 must stay green unchanged

### Cancellation tokens (X.9)

- [X] T141 [US1] Remove `CancellationToken` parameters and arguments from all controller actions, service methods and service interfaces in server/SmartEstimatePlatform.Api; keep them only in `DomainExceptionHandler.TryHandleAsync` and the `AppDbContext.SaveChangesAsync` override (framework signatures); update test call sites in tests/server if they pass a token

### Entities with private setters (X.6, Decision A1) 🗄

- [X] T142 [US1] 🗄 🔗 (one unit with T143–T145) Give Company, Material and User private setters, a private parameterless constructor for EF Core and public constructors (with purpose doc comments) as data-model.md describes: `Company(name)`, `Material(name, unit, price)` + `Update(name, unit, price)`, `User(firstName, lastName, email, role, companyId)` and `User(firstName, lastName, email, role, company)` + `SetPasswordHash`, `SetLoginState(failedCount, firstFailedAt, lockoutEnd)`, `ResetLoginFailures()`, `InvalidateTokens()`; mutators contain no business rules — in server/SmartEstimatePlatform.Api/Models/Entities
- [X] T143 [US1] 🗄 🔗 Make `ICompanyScoped.CompanyId` get-only and assign it in `AppDbContext.AssignNewRecordsToCurrentCompany` through `entry.Property(e => e.CompanyId).CurrentValue` in server/SmartEstimatePlatform.Api/Data/AppDbContext.cs and Models/Entities/ICompanyScoped.cs
- [X] T144 [US1] 🗄 🔗 Update AuthService (RecordFailedLogin computes the values and calls SetLoginState; ClearFailedLogins → ResetLoginFailures; SetPasswordHash on register and rehash), TeamService (ResetPasswordAsync uses SetPasswordHash, InvalidateTokens, ResetLoginFailures) and MaterialService (constructor / Update) to use the constructors and mutators
- [X] T145 [US1] 🔗 Adjust test setup in tests/server/CompanyIsolationTests/CompanyScopingTests.cs: build the Viewer with the User constructor; for the forged token, load the real Alpha Manager and set CompanyId to Beta's through `db.Entry(user).Property(u => u.CompanyId).CurrentValue` without saving; assertions unchanged

**Checkpoint**: US1 is complete when T122–T145 are done, all backend tests pass, there are no pending model changes, and the quickstart SC-004 greps hold.

## Phase 4: User Story 2 — Frontend follows Article X (Priority: P2)

**Goal**: The client uses Tailwind utility classes only, with one component per page section.
**Independent Test**: `npm run lint`, `npm run build` and `npm run test` pass; `find client/src -name "*.css"` returns only client/src/index.css; the manual page walk in quickstart.md matches the current version.

### Component extraction (X.7) — before Tailwind (research R10)

- [ ] T146 [P] [US2] Extract DashboardHeader (company name + logout button), UserSummary (greeting and name · company · role line) and QuickActions (the `quickActions` list, role filter, links and disabled "Netrukus" buttons) from client/src/pages/Dashboard/DashboardPage.tsx into DashboardHeader.tsx, UserSummary.tsx and QuickActions.tsx in client/src/pages/Dashboard/
- [ ] T147 [P] [US2] Extract MaterialSearch (search input with its delay) and MaterialsTable (table rows with edit and delete actions) from client/src/pages/Materials/MaterialsPage.tsx into MaterialSearch.tsx and MaterialsTable.tsx in client/src/pages/Materials/; ConfirmDialog stays in the page
- [ ] T148 [P] [US2] Extract TeamTable from client/src/pages/Team/TeamPage.tsx into client/src/pages/Team/TeamTable.tsx; ConfirmDialog stays in the page; tests/client/ConfirmDialogTests/RemoveViewer.test.tsx must pass unchanged

### Tailwind (X.7)

- [ ] T149 [US2] Delete the unused client/src/App.css (imported nowhere)
- [ ] T150 [US2] Install `tailwindcss@4.3.3` and `@tailwindcss/vite@4.3.3` as dev dependencies in client/package.json; add the `tailwindcss()` plugin in client/vite.config.ts; replace client/src/index.css with `@import "tailwindcss";` plus a `@theme` block holding the existing colour and font tokens; move its base styles (body, #root, headings, dark mode) to utilities in the components
- [ ] T151 [P] [US2] Convert client/src/components/ConfirmDialog.tsx to Tailwind utilities and delete client/src/components/ConfirmDialog.css
- [ ] T152 [P] [US2] Convert client/src/pages/Landing/LandingPage.tsx to Tailwind utilities and delete client/src/pages/Landing/Landing.css
- [ ] T153 [P] [US2] Convert DashboardPage, DashboardHeader, UserSummary and QuickActions to Tailwind utilities and delete client/src/pages/Dashboard/Dashboard.css
- [ ] T154 [P] [US2] Convert TeamPage and TeamTable to Tailwind utilities and delete client/src/pages/Team/Team.css
- [ ] T155 [P] [US2] Convert MaterialsPage, MaterialSearch and MaterialsTable to Tailwind utilities and delete client/src/pages/Materials/Materials.css
- [ ] T156 [US2] Convert LoginPage, RegisterPage, ViewerFormPage, ResetPasswordPage and MaterialFormPage to Tailwind utilities and delete client/src/styles/auth.css (and the empty styles/ folder)
- [ ] T157 [US2] Walk through every page in client/src/pages and client/src/components as in quickstart.md (light and dark mode, phone width) and fix any visual regression with utilities only

**Checkpoint**: US2 is complete when only client/src/index.css remains and all frontend checks pass.

## Phase 5: Polish & Cross-Cutting

- [ ] T158 Compare the current `/openapi/v1.json` with specs/013-code-structure-refactor/baseline-openapi.json; the only differences allowed are those in contracts/api-changes.md (AuthResponse schema, 400 ValidationProblemDetails, response metadata); then delete the baseline file
- [ ] T159 Sync the "Code structure conventions" in AGENTS.md with Constitution X.9 (no CancellationToken unless a spec requires it) and X.10 (declarative request validation, one definition per rule)
- [ ] T160 Mark remarks 1–12 as done in docs/review/instructor-feedback.md, and run the full check set plus the quickstart.md structure greps one last time

## Dependencies

- T121 → T122 → US1 and US2. US2 does not depend on US1, so the two can run in parallel after T122.
- **US1 order**: interfaces (T123–T125) → DTOs (T126–T131) → validation (T132–T135) → thin controllers (T136–T140) → tokens (T141) → entities (T142–T145).
  - T129 needs T126/T127.
  - T130 needs T129.
  - T137 needs T130 and T136.
  - T140 needs T137–T139.
- **US2 order**: extraction (T146–T148) → T149 → T150 → conversions (T151–T156) → T157.
- **Polish (T158–T160)**: after both stories.

## Parallel Execution Examples

- **US1**: T123 interfaces for the four services can be written in parallel (one file each). T128 (JwtOptions) is independent of T126/T127.
- **US2**:
  - T146, T147 and T148 touch different pages.
  - After T150, T151–T155 touch different files and stylesheets. T156 waits because it shares auth.css across five pages.

## Implementation Strategy

1. **MVP = US1** (P1). The backend structure is what features 005+ will copy, so finish it first and get it reviewed.
2. One commit per task (or per small group of tasks within one heading), Conventional Commits, e.g. `refactor(api): add service interfaces`. This spec/plan/tasks commit stays separate from the code commits (Article VI).
3. If a characterization test (T122) fails after a step, fix the production code, not the test.
