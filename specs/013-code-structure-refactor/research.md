# Research: Code Structure Refactor (Article X)

## R1 — Records for DTOs and options

- **Decision**: `sealed record` with `{ get; init; }` properties (not positional) for every DTO and for `JwtOptions`.
- **Rationale**: Verified on 2026-11-02 in a scratch .NET 10 project: an init-property record binds through `AddOptions<T>().Bind().Validate().ValidateOnStart()`, keeps default values (`ExpiresMinutes = 60`), and supports the object initializer used in `AuthServiceTestBase`. Data annotations stay on the properties as they are today.
- **Alternatives considered**: Positional records fail at runtime (`MissingMethodException: No parameterless constructor defined`) because the options factory needs a parameterless constructor. They would also break 21 object initializers in tests and need `[property: Required]`.

## R2 — Merging identical DTOs

- **Decision**: `RegisterResponse` + `LoginResponse` → `AuthResponse` (Token, UserId, CompanyId, Role). No other DTOs are identical.
- **Rationale**: Same fields and same JSON; the client types do not change. The only visible difference is the schema name in the API description (FR-014a).

## R3 — Where validation lives (X.10)

- **Decision**: Rules are *defined* once in static Application-layer classes and *applied* by the request records through `IValidatableObject`:
  - `PasswordRules` (exists): `MinLength`, plus one method that returns the error for a password, checking "not equal to the email" when an email is given.
  - `MaterialRules` (new, beside PasswordRules): `NameMaxLength` (moved from `Material`), `MaxPrice` (moved from `MaterialService`), and one method `Validate(unit, price)` returning the errors for unit (via `MeasurementUnit.IsValid`) and price (≥ 0, at most 2 decimals, ≤ MaxPrice). The name is checked by `[Required]` and `[StringLength(MaterialRules.NameMaxLength)]` on the request.
  - `RegisterRequest`, `CreateViewerRequest`: password checked with their own Email. `ResetPasswordRequest`: length only. `TeamService.ResetPasswordAsync` keeps the "not equal to the Viewer's stored email" check through `PasswordRules` (stored data), still raising `WeakPasswordException` → 400.
  - `MaterialRequest`: `[Required]` and `[StringLength(MaterialRules.NameMaxLength)]` stay; unit and price are checked in `Validate`. Trimming the name is normalisation, not validation, so the service keeps doing it.
- **Rationale**: Satisfies X.10 and Article II together: the rule (business knowledge) stays in the Application layer, while the request only declares that it applies. Error texts stay the same English texts as today.
- **Consequence**: Invalid requests are rejected by `[ApiController]` model validation with a `ValidationProblemDetails` 400 (per-field `errors`, no `detail`). The client checks only `status === 400` (RegisterPage, ViewerFormPage, MaterialFormPage), so the UI is unchanged. `InvalidMaterialException` is no longer needed and is deleted.
- **Alternatives considered**: (a) Custom attributes per rule (`[StrongPassword]`). Rejected: the email comparison needs two properties, so it would need a class-level attribute anyway. (b) A custom `InvalidModelStateResponseFactory` to keep the old single-message body. Rejected under YAGNI, because no client reads the body.

## R4 — One exception handler

- **Decision**: `DomainExceptionHandler : IExceptionHandler` replaces `NotFoundExceptionHandler` and maps:

  | Exception | Status | Detail |
  |---|---|---|
  | `NotFoundException` | 404, title "Not found" | exception message (as today) |
  | `WeakPasswordException` | 400 | exception message |
  | `DuplicateEmailException` | 409 | "An account with this email already exists." |
  | `DuplicateMaterialException` | 409 | "A material with this name and unit already exists." |
  | `InvalidCredentialsException` | 401 | "Invalid email or password." |
  | `AccountLockedException` | 423 | exception message + `minutesRemaining` |
  | `UnauthorizedAccessException` | 401 | none (the unreachable "user vanished" branch of `/me`) |

- **Rationale**: Removes all 5 try/catch blocks. The 409 texts are fixed strings, unlike the exception messages, which contain the email or name. Titles and types come from the same ProblemDetails defaults the controllers use today. Step 0 tests pin them, and any difference is fixed in the handler, not in the tests.
- **Alternatives considered**: An exception filter or base controller `try` wrapper. Rejected because `IExceptionHandler` is already used and covers every endpoint.

## R5 — Thin controllers and the current user

- **Decision**: Services return DTOs through `DtoMapper`. `ICurrentCompany` is renamed `ICurrentUser` with `UserId` and `CompanyId` (both from the validated token), so `/api/auth/me` becomes one call: `authService.GetCurrentUserAsync()`. `RegisterAsync` creates the token itself, as `LoginAsync` already does.
- **Rationale**: Removes claim parsing and DTO construction from `AuthController` (X.2).
- **Alternatives considered**: Passing `ClaimsPrincipal` into the service. Rejected because it ties services to HTTP.

## R6 — Common response metadata

- **Decision**: `ApiControllerBase : ControllerBase` carries `[ApiController]` and class-level `[ProducesResponseType<ValidationProblemDetails>(400)]`. `MaterialsController` and `TeamController` declare 401/403 once at class level. Actions keep only their specific codes (201/204/404/409/423).
- **Rationale**: About 26 → ~14 attributes. Affects only the API description.
- **Alternatives considered**: `DefaultApiConventions`. Rejected because it matches on action names (Get/Post/Put/Delete), and most of our action names (Register, Login, Me, CreateViewer, RemoveViewer, ResetPassword, List) do not match.

## R7 — Removing CancellationToken (X.9)

- **Decision**: Remove the parameter from every controller action and service method (and the interfaces) and call EF without it. Keep it in `IExceptionHandler.TryHandleAsync` and the `AppDbContext.SaveChangesAsync` override (framework signatures).
- **Rationale**: No specification requires cancellation (X.9).

## R8 — Entities with private setters (Decision A1)

- **Decision**: Each entity gets a private parameterless constructor for EF Core and a public constructor for required state, for example `User(firstName, lastName, email, role, companyId)` or `User(..., Company company)` for registration. Simple mutators without rules:
  - `User`: `SetPasswordHash`, `SetLoginState(failedCount, firstFailedAt, lockoutEnd)`, `ResetLoginFailures()`, `InvalidateTokens()`
  - `Material`: `Update(name, unit, price)`

  `ICompanyScoped.CompanyId` becomes get-only, and `AppDbContext` assigns it through `entry.Property(e => e.CompanyId).CurrentValue`.
- **Rationale**: EF Core maps private setters and constructors without schema changes. `has-pending-model-changes` proves it. The lockout calculation stays in `AuthService`, which computes the values and calls `SetLoginState`. The duplicated lockout reset in `TeamService` becomes `ResetLoginFailures()`.
- **Test impact**: `CompanyScopingTests` builds its Viewer through the constructor. The forged-token test loads the real user and sets `CompanyId` through `db.Entry(...)` without saving.

## R9 — Tailwind

- **Decision**: `tailwindcss@4.3.3` + `@tailwindcss/vite@4.3.3` (peer: Vite ^8, matches). The plugin goes in `vite.config.ts`. `index.css` becomes `@import "tailwindcss";` plus a `@theme` block with the existing colour tokens. Dark mode uses `dark:` utilities, as today's `prefers-color-scheme` does.
- **Rationale**: The official Tailwind v4 + Vite setup; no PostCSS config file needed. Keeping `index.css` as the entry means no new CSS file (X.7).
- **Risks**: Preflight resets base styles (headings, buttons, lists), so each page is checked by hand after its migration. `App.css` is not imported anywhere and is deleted first.

## R10 — Component extraction

- **Decision**: New files:
  - `pages/Dashboard/`: `DashboardHeader`, `UserSummary`, `QuickActions` (including the action list and role filter)
  - `pages/Materials/`: `MaterialSearch`, `MaterialsTable`
  - `pages/Team/`: `TeamTable`

  This is done **before** the Tailwind migration, so classes are rewritten in smaller files.
- **Rationale**: These are the distinct page sections (X.7). The form pages have no repeated section, so nothing more is extracted there (YAGNI).
