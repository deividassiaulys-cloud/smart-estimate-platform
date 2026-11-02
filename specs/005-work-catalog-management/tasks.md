# Tasks: Work Catalog Management

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/works-api.md, quickstart.md in `specs/005-work-catalog-management/`

`[US#]` refers to the user stories of this feature in `spec.md`. The IDs T038, T043 and T045–T057 are kept from the earlier task list. New tasks continue the project-wide numbering from T161.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. Tasks marked 🔗 are done and committed as one unit with the tasks they name.

## Phase 1: Setup (shared helpers, no behaviour change)

- [X] T161 [P] Add `NumberRules` in server/SmartEstimatePlatform.Api/Services/NumberRules.cs (with purpose doc comments): `MaxMoney = 9_999_999_999_999_999.99m` (DECIMAL(18,2)), `MaxQuantity = 999_999_999_999_999.999m` (DECIMAL(18,3)), `HasAtMostDecimals(decimal value, int places)`. Make `MaterialRules` use `MaxMoney` and `HasAtMostDecimals(price, 2)` instead of its own `MaxPrice` and rounding check. Same texts and behaviour; existing material tests stay unchanged
- [X] T162 [P] Add `SearchPattern.Contains(string search)` (with purpose doc comments) in server/SmartEstimatePlatform.Api/Services/SearchPattern.cs, returning `%…%` with `\`, `%` and `_` escaped for LIKE with escape character `\`. Make MaterialService.SearchAsync use it; MaterialSearchTests stay unchanged
- [X] T163 Add `UniqueSave.SaveAsync(AppDbContext db, Func<Task<bool>> duplicateExists, Func<Exception> duplicateError)` (with purpose doc comments) in server/SmartEstimatePlatform.Api/Services/UniqueSave.cs. It checks for a duplicate first, saves, and on `DbUpdateException` clears the change tracker, checks again, and throws `duplicateError()` or rethrows. Replace the copies in AuthService.CreateAccountAsync and MaterialService.SaveAsync; the duplicate email and duplicate material tests stay unchanged

## Phase 2: Foundational (data and validation, blocks both stories)

- [X] T045 Add the `Work` entity (`ICompanyScoped`; Id, Name, Unit, LabourRate, LabourNorm, CompanyId, Company, Lines) and the `WorkMaterialLine` entity (Id, WorkId, MaterialId, Material, ConsumptionNorm, WastePercent), with private setters, a private EF constructor and public constructors `Work(name, unit, labourRate, labourNorm)` with `Update(...)` and `ReplaceLines(IEnumerable<WorkMaterialLine>)`, and `WorkMaterialLine(materialId, consumptionNorm, wastePercent)` (doc comments; mutators hold no rules), in server/SmartEstimatePlatform.Api/Models/Entities
- [X] T165 Add `WorkRules` in server/SmartEstimatePlatform.Api/Services/WorkRules.cs (doc comments):
  - `NameMaxLength = 200`
  - `Validate(unit, labourRate, labourNorm)`:
    - unit in `MeasurementUnit.All`: "Unit must be one of the fixed measurement units."
    - rate ≥ 0, at most 2 decimals, ≤ `NumberRules.MaxMoney`: "Labour rate cannot be negative." / "Labour rate can have at most 2 decimal places." / "Labour rate is too large."
    - norm ≥ 0, at most 3 decimals, ≤ `NumberRules.MaxQuantity`: "Labour norm cannot be negative." / "Labour norm can have at most 3 decimal places." / "Labour norm is too large."
  - `ValidateLine(consumptionNorm, wastePercent)`:
    - norm > 0, at most 3 decimals, ≤ MaxQuantity: "Consumption norm must be greater than 0." / "…at most 3 decimal places." / "…is too large."
    - waste 0–100 inclusive, at most 2 decimals: "Waste percentage must be between 0 and 100." / "Waste percentage can have at most 2 decimal places."
- [X] T164 Configure Works in server/SmartEstimatePlatform.Api/Data/AppDbContext.cs:
  - `DbSet<Work> Works`, `DbSet<WorkMaterialLine> WorkMaterialLines`
  - Name: max `WorkRules.NameMaxLength` (200), collation `utf8mb4_0900_as_ci`; Unit: max 16
  - LabourRate `HasPrecision(18,2)`, LabourNorm and ConsumptionNorm `HasPrecision(18,3)`, WastePercent `HasPrecision(5,2)`
  - unique index (CompanyId, Name, Unit)
  - Line → Work cascade; Line → Material `DeleteBehavior.Restrict`
  - no unique index on (WorkId, MaterialId), because the same material may repeat (BR-009)
- [X] T046 Add the EF Core migration `AddWork` in server/SmartEstimatePlatform.Api/Migrations (`dotnet ef migrations add AddWork` with a dummy `ConnectionStrings__DefaultConnection`), check that the generated tables, indexes and foreign keys match T164, then confirm `has-pending-model-changes` reports no changes
- [X] T166 Add sealed records with init properties in server/SmartEstimatePlatform.Api/Models/DTOs/Works/:
  - `WorkRequest`: `[Required]` Name, Unit and Lines; `[StringLength(WorkRules.NameMaxLength)]` Name; `IValidatableObject` → `WorkRules.Validate`
  - `WorkMaterialLineRequest`: `IValidatableObject` → `WorkRules.ValidateLine`
  - `WorkResponse`
  - `WorkMaterialLineResponse` (Id, MaterialId, MaterialName, MaterialUnit, ConsumptionNorm, WastePercent)
  - add `ToResponse(this Work)` to server/SmartEstimatePlatform.Api/Services/Mapping/DtoMapper.cs
- [X] T053 Add request-validation tests in tests/server/RequestValidationTests/WorkRequestValidationTests.cs:
  - waste −1 and 101 rejected, 0 and 100 accepted, 12.345 rejected
  - consumption norm 0 and −0.001 rejected, 0.001 accepted, 1.2345 rejected
  - labour norm 0 accepted, −0.001 rejected
  - labour rate −0.01 and 1.234 rejected
  - a unit outside the list rejected
  - empty name rejected
  - an empty `Lines` list accepted

## Phase 3: User Story 1: Create and edit works with their composition (P1) 🎯 MVP

**Goal**: A Manager creates, edits and deletes works with material lines, and a material used in a work can't be deleted.
**Independent Test**: The US1 tests below pass. In the quickstart walk-through, steps 1–3 and 5–8 work.

- [X] T167 [P] [US1] Add `DuplicateWorkException`, `UnknownMaterialException` and `MaterialInUseException(IReadOnlyList<string> workNames)` (doc comments) in server/SmartEstimatePlatform.Api/Services, and map them in server/SmartEstimatePlatform.Api/Controllers/DomainExceptionHandler.cs:
  - 409 "A work with this name and unit already exists."
  - 400 "Each line must use a material from your catalog."
  - 409 "The material is used in works and cannot be deleted." with extension `works`
- [X] T047 [US1] Add `IWorkService` (doc comments) and `WorkService` in server/SmartEstimatePlatform.Api/Services/, registered in `AddApplicationServices()`, with `GetAsync`, `CreateAsync`, `UpdateAsync` and `DeleteAsync`:
  - name trimmed
  - every line's MaterialId must be a material of the current company, else `UnknownMaterialException`
  - `UpdateAsync` replaces the whole composition (`ReplaceLines`)
  - missing work → `NotFoundException("Work not found.")`
  - responses come through DtoMapper, with each line's material name and unit loaded
- [X] T048 [US1] Reject a duplicate Work name+unit in WorkService (case-insensitive through the Name collation, per company, the work itself excluded on edit) through `UniqueSave.SaveAsync` with `DuplicateWorkException`, in server/SmartEstimatePlatform.Api/Services/WorkService.cs
- [X] T049 [US1] Add `SearchAsync(string? search)` to IWorkService/WorkService: name contains the search (via `SearchPattern.Contains`), ordered A–Z by name then unit, empty search → all; the Works list in US1 uses it with an empty search, in server/SmartEstimatePlatform.Api/Services/WorkService.cs
- [X] T050 [US1] Add server/SmartEstimatePlatform.Api/Controllers/WorksController.cs: `ApiControllerBase`, `[Route("api/works")]`, `[Authorize(Roles = Manager)]`, class-level 401/403; endpoints GET ?search= (list), GET {id}, POST (201), PUT {id}, DELETE {id} (204) as in contracts/works-api.md; one service call per action; success types declared
- [X] T038 [US1] In MaterialService.DeleteAsync, collect the distinct names (A–Z) of works that have a line using the material, and throw `MaterialInUseException` when there are any, in server/SmartEstimatePlatform.Api/Services/MaterialService.cs
- [X] T054 [US1] Add integration tests in tests/server/WorkServiceTests/WorkEndpointsTests.cs: a duplicate name+unit is rejected with 409 on create and on edit (for example "sienų dažymas " vs "Sienų dažymas"); the same name with a different unit is accepted
- [X] T055 [US1] Add integration tests in tests/server/WorkServiceTests/WorkEndpointsTests.cs:
  - a work with the same material on two lines is saved with both lines
  - a work with zero lines is accepted
  - editing replaces the composition (a removed line is gone, a changed norm is stored)
  - a line with an unknown material ID, or another company's material, gives 400 and saves nothing
  - edit/delete/get of a missing ID gives 404
- [X] T168 [US1] Add an integration test in tests/server/WorkServiceTests/WorkAuthorizationTests.cs: a Viewer gets 403 on listing, getting, creating, editing and deleting works
- [X] T043 [US1] Add integration tests in tests/server/MaterialServiceTests/MaterialDeleteInUseTests.cs: deleting a material used in a work gives 409 with `works` listing the work names, and the material remains; after the work is deleted, the material can be deleted (204)
- [X] T057 [US1] Add integration tests in tests/server/CompanyIsolationTests/WorkIsolationTests.cs: another company's work ID gives 404 on get, edit and delete, and its works never appear in the list
- [X] T169 [P] [US1] Add the client types and API: `Work`, `WorkLine`, `WorkRequest` in client/src/interfaces/work.ts; `getWorks(search)`, `getWork(id)`, `createWork`, `updateWork`, `deleteWork` in client/src/api/works.ts, following client/src/api/materials.ts
- [X] T051 [US1] Add the Works pages in client/src/pages/Works, Tailwind via `utils/ui.ts`, one component per file:
  - `WorksPage`: heading, "+ Naujas darbas", notice/error, `WorksTable`, delete through `ConfirmDialog`
  - `WorksTable`: name, unit, rate, norm, number of materials, edit/delete actions
  - `WorkFormPage`: create/edit; name, unit select from /api/units, rate, norm; client validation mirroring `WorkRules` with Lithuanian messages; 400/404/409 messages
  - `WorkLinesEditor`: one row per line with a material select from /api/materials, consumption norm, waste %, and a remove button; "+ Pridėti medžiagą"
- [X] T170 [US1] Add `/works`, `/works/new` and `/works/:id/edit` under `ManagerRoute` in client/src/App.tsx, and link the "Darbų katalogas" quick action to `/works` in client/src/pages/Dashboard/QuickActions.tsx
- [X] T171 [US1] When deleting a material is rejected with 409, show "Medžiagos negalima ištrinti, nes ji naudojama darbuose: <work names>." using the `works` extension, in client/src/pages/Materials/MaterialsPage.tsx
- [X] T172 [US1] Add a frontend test in tests/client/ConfirmDialogTests/RemoveWork.test.tsx (like RemoveViewer.test.tsx): deleting a work opens the confirmation; cancelling sends no DELETE; confirming sends one DELETE and removes the row; and a Viewer's Dashboard shows no "Darbų katalogas" link

**Checkpoint**: US1 is done when all US1 tests pass and quickstart steps 1–3 and 5–8 work.

## Phase 4: User Story 2: Search works (P2)

**Goal**: A Manager finds works by part of the name, in any letter case. The search itself is built in T049 (US1, needed by the list); US2 adds its tests and the search field.
**Independent Test**: T056 passes; quickstart step 4 works.

- [X] T056 [US2] Add integration tests in tests/server/WorkServiceTests/WorkSearchTests.cs: "siena", "SIENA" and " Siena " find "Laikančioji siena" (and not "Sienų dažymas", which does not contain that text); results are in A–Z order; an empty search returns all; `%` and `_` match literally
- [X] T173 [US2] Replace client/src/pages/Materials/MaterialSearch.tsx with a shared client/src/components/SearchField.tsx (props: id, label, value, onChange) and use it in MaterialsPage
- [X] T052 [US2] Add search to WorksPage with the same delay as materials (300 ms), plus the empty states "Pagal paiešką darbų nerasta." and "Katalogas tuščias. Pridėkite pirmą darbą.", in client/src/pages/Works/WorksPage.tsx

## Phase 5: Polish

- [X] T174 Run the quickstart.md walk-through against the running app, and the full check set (backend tests, `has-pending-model-changes`, lint, build, frontend tests); fix any regression

## Dependencies

- **Setup**: T161–T163 first; they only refactor and must stay green.
- **Foundational**: T045 and T165 → T164 → T046. T165 → T166 → T053.
- **US1**:
  - T167 is independent.
  - T047 (needs T164–T166) → T048 → T049 → T050.
  - T038 needs T164 and T167.
  - Tests T054, T055, T168, T043 and T057 come after their code.
  - Client: T169 → T051 → T170; T171 and T172 after T051.
- **US2**: T056 after T049. T173 is independent. T052 after T051 and T173.
- **Polish**: T174 last.

## Parallel Execution Examples

- **Setup**: T161 and T162 touch different files and can run in parallel. T163 also changes MaterialService, so it runs after T162.
- **US1**: T167 and T169 can run in parallel with T047.
- **US2**: T173 (client) can run in parallel with T056 (server tests).

## Implementation Strategy

1. **MVP = US1** (P1): finish it, review it, and commit it before US2.
2. Keep spec/plan/tasks commits separate from code commits (Article VI), and use Conventional Commits, e.g. `feat(work-catalog): …`.
