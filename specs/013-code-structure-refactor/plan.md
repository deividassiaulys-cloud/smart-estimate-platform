# Implementation Plan: Code Structure Refactor (Article X)

**Branch**: `013-code-structure-refactor` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/013-code-structure-refactor/spec.md`

## Summary

Restructure the backend and frontend of features 001–004 to Constitution Article X (v1.2.0) without changing what users see. On the backend, this means service interfaces, DTO records mapped in one place, validation declared on the requests, thin controllers, one exception handler, entities with private setters and no cancellation tokens. On the frontend, it means Tailwind only and one component per page section. Characterization tests are written first and stay green through every step. Decisions and their reasons are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript 6 + React 19 (frontend)

**Primary Dependencies**: ASP.NET Core 10, EF Core 10 (Pomelo MySQL; SQLite in tests), React Router 7, Vite 8. New: `tailwindcss` 4.3.3 and `@tailwindcss/vite` 4.3.3 (dev). Both are required by Article X.7 and instructor remark 9, and are the official way to use Tailwind with Vite.

**Storage**: MySQL; schema unchanged, no new migration

**Testing**: xUnit + WebApplicationFactory/SQLite (`dotnet test`), Vitest + Testing Library (`npm run test`), plus `npm run lint`, `npm run build` and `dotnet ef migrations has-pending-model-changes`

**Target Platform**: Web API + SPA

**Project Type**: Web application (server/ + client/)

**Performance Goals**: Unchanged; no new queries

**Constraints**: No behaviour change except the two differences allowed by FR-014; every step builds and keeps all checks green

**Scale/Scope**: 4 controllers, 4 services, 3 entities, 10 DTOs → 9, 8 CSS files → 1, 13 React components (+6 extracted). Steps that cannot stay green alone are done as one unit: T133 + T135 and T142–T145

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | Lockout, duplicate and company rules stay in services. Validation rules are **defined** once in the Application layer (`PasswordRules`, `MaterialRules`) and only **applied** declaratively by the request records (X.10) | Pass |
| II.2 doc comments | New public interfaces, records, mapper and handler get purpose doc comments | Pass |
| II.4 / X.1 DRY | Identical auth responses merged. One exception handler, one mapper and one validation definition per rule. Lockout reset now has one mutator | Pass |
| III dependencies | `tailwindcss`, `@tailwindcss/vite` justified above | Pass |
| III no features beyond spec | No new endpoints or fields | Pass |
| IV security | Authentication, authorization and company-isolation integration tests are unchanged and must stay green | Pass |
| VI version control | Constitution/spec commits separate from code commits; Conventional Commits | Pass |
| VIII autonomy | No migration files touched; every change reviewed as a diff | Pass |
| X.2–X.8 | Covered by FR-001…FR-012. X.2: `UnitsController` only returns the fixed unit list, which is not logic, so it gets no service (spec Clarifications). X.4: DTOs reference `UserRole` (an enum) and `MeasurementUnit` (a fixed list). Neither is an EF entity type; both live in Models/Entities by owner decision and per the AGENTS.md project map | Pass |
| X.9 | Project's own tokens removed. `IExceptionHandler.TryHandleAsync` and the `SaveChangesAsync` override keep theirs because the framework signature requires them (not "added") | Pass |
| X.10 | Request-only checks move to request records. Checks needing stored data stay in services and reuse the shared definition | Pass |

**Post-design re-check (after Phase 1)**: Pass, no violations, Complexity Tracking empty.

## Project Structure

### Documentation (this feature)

```text
specs/013-code-structure-refactor/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/api-changes.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
server/SmartEstimatePlatform.Api/
├── Controllers/
│   ├── ApiControllerBase.cs          # new: [ApiController] + common response metadata
│   ├── DomainExceptionHandler.cs     # replaces NotFoundExceptionHandler
│   └── Auth|Materials|Team|UnitsController.cs
├── Models/
│   ├── DTOs/Auth/        AuthResponse, LoginRequest, RegisterRequest, CurrentUserResponse
│   ├── DTOs/Team/        CreateViewerRequest, ResetPasswordRequest, TeamMemberResponse
│   ├── DTOs/Materials/   MaterialRequest, MaterialResponse
│   └── Entities/         Company, User, Material (private setters), UserRole, MeasurementUnit
├── Services/
│   ├── IAuthService, ITokenService, ITeamService, IMaterialService (+ implementations)
│   ├── ICurrentUser / HttpCurrentUser   # renamed from ICurrentCompany, adds UserId
│   ├── PasswordRules, MaterialRules     # one definition per rule
│   ├── Mapping/DtoMapper.cs
│   └── ServiceCollectionExtensions.cs   # AddApplicationServices()
└── Program.cs

tests/server/  ErrorResponseTests/ (new), RequestValidationTests/ (new), existing folders
client/src/
├── index.css                     # Tailwind entry only
├── components/ConfirmDialog.tsx
└── pages/Dashboard/ DashboardPage, DashboardHeader, UserSummary, QuickActions
    pages/Materials/ MaterialsPage, MaterialSearch, MaterialsTable, MaterialFormPage
    pages/Team/      TeamPage, TeamTable, ViewerFormPage, ResetPasswordPage
```

**Structure Decision**: Keep the existing web-application layout. No new projects or folders beyond `Models/DTOs/<Feature>/`, `Services/Mapping/` and two new test folders.

## Implementation Order

Each step is its own commit. After each step: `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test`. After steps that touch entities, also `dotnet ef migrations has-pending-model-changes`.

0. **Characterization tests**: status, title, detail and extensions for 401/404/409/423; status and "nothing saved" for each 400 case.
1. **Service interfaces** + `AddApplicationServices()`; consumers use the interfaces.
2. **DTOs** (2a) move into feature folders → (2b) sealed records with `init` properties, with tests switched to `with` expressions → (2c) merge `AuthResponse`, move the `From()` methods into `DtoMapper`, remove entity references.
3. **Validation on requests (X.10)**: `PasswordRules`/`MaterialRules` become the definitions; requests implement `IValidatableObject`; services drop the request-only checks; `InvalidMaterialException` removed; service password tests moved to `RequestValidationTests`.
4. **Thin controllers**: services return DTOs; `ICurrentUser`; `DomainExceptionHandler`; `ApiControllerBase`; no try/catch in controllers.
5. **Remove CancellationToken (X.9)** from the project's own controllers and services.
6. **Entities with private setters**: constructors and mutators; DbContext sets `CompanyId` through the change tracker; test setup adjusted.
7. **Component extraction**: Dashboard, Materials and Team sections.
8. **Tailwind**: delete `App.css` → install and configure → migrate one stylesheet per commit, deleting it; check each page by hand.
9. **Wrap-up**: full check run, statuses in `docs/review/instructor-feedback.md`, AGENTS.md conventions synced with X.9/X.10.

## Complexity Tracking

No violations.
