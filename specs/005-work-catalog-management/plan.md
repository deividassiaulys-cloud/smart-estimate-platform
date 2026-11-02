# Plan: Work Catalog Management

**Branch**: `005-work-catalog-management` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

## Summary

Managers create, edit, delete and search works. A work has a name, a unit, a labour rate (€/h) and a labour norm (h per unit), plus material lines (material, consumption norm, waste %). The same material may appear on several lines. A material that is used in a work can no longer be deleted (409, listing the works), which completes 004's US3 (T038, T043). The feature follows the Article X structure built in 013. Decisions are in [research.md](research.md).

Shared technical rules (money, rounding, percentages, fixed units, confirmations) are in `specs/shared-technical-rules.md` and apply to this feature.

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: ASP.NET Core 10, EF Core 10 (Pomelo MySQL; SQLite in tests), React Router 7, Tailwind CSS 4.3.3. No new dependencies.

**Storage**: MySQL. A new migration `AddWork` adds the `Works` and `WorkMaterialLines` tables. The task requires it, so AGENTS.md allows the migration.

**Testing**: xUnit + WebApplicationFactory/SQLite, Vitest + Testing Library

**Project Type**: Web application (server/ + client/)

**Performance Goals / Scale**: Same as the material catalog: server-side search, A–Z order, no paging.

**Constraints**: Money DECIMAL(18,2), norms DECIMAL(18,3), percentages DECIMAL(5,2). Values are validated, never rounded (shared rules).

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | Request-only rules are defined in `WorkRules` and applied by the request records (X.10). Rules that need stored data (material belongs to the company, duplicate name+unit, material in use) live in services | Pass |
| II.4 / X.1 DRY | Before the third copy appears, the LIKE search pattern and the "save, then catch a unique-index race" step are extracted from MaterialService and AuthService into shared helpers. The DECIMAL(18,2)/(18,3) limits get one definition. The search field becomes one shared component | Pass |
| II.5 tests | Every rule (waste 0–100, norm > 0, duplicates, repeated material, in-use material) gets a test | Pass |
| III dependencies | None added | Pass |
| IV security | Works and lines are company-scoped through `Work` (`ICompanyScoped`). A line may only reference a material of the current company. Viewer → 403. All covered by integration tests | Pass |
| VI version control | Spec/plan/tasks committed separately from code | Pass |
| VIII autonomy | Migration only through the task that requires it (T046) | Pass |
| X.2–X.8 | `IWorkService` registered in `AddApplicationServices()`; records in `Models/DTOs/Works/`; mapping in `DtoMapper`; thin `WorksController : ApiControllerBase`; new errors handled in `DomainExceptionHandler`; entities with private setters | Pass |
| X.7 | Tailwind only, using `utils/ui.ts`; one component per file; delete uses `ConfirmDialog` (NFR-011) | Pass |
| X.9 / X.10 | No CancellationToken; validation declared on `WorkRequest` / `WorkMaterialLineRequest` | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Controllers/WorksController.cs                 # new
├── Controllers/DomainExceptionHandler.cs          # + DuplicateWork, UnknownMaterial, MaterialInUse
├── Models/Entities/Work.cs, WorkMaterialLine.cs   # new
├── Models/DTOs/Works/                              # WorkRequest, WorkMaterialLineRequest, WorkResponse, WorkMaterialLineResponse
├── Services/IWorkService.cs, WorkService.cs        # new
├── Services/WorkRules.cs                           # new: waste %, norms, rate
├── Services/NumberRules.cs                         # new: DECIMAL limits + decimal-places check (shared)
├── Services/SearchPattern.cs, UniqueSave.cs        # new: extracted shared helpers
├── Services/MaterialService.cs                     # in-use check; uses the shared helpers
├── Services/Mapping/DtoMapper.cs                   # + Work mapping
├── Data/AppDbContext.cs                            # + Works, WorkMaterialLines
└── Migrations/<timestamp>_AddWork.cs               # new
tests/server/WorkServiceTests/, CompanyIsolationTests/WorkIsolationTests.cs, MaterialServiceTests/
client/src/
├── components/SearchField.tsx                      # shared (replaces Materials/MaterialSearch.tsx)
├── interfaces/work.ts, api/works.ts
├── pages/Works/WorksPage.tsx, WorksTable.tsx, WorkFormPage.tsx, WorkLinesEditor.tsx
├── pages/Materials/MaterialsPage.tsx               # shows the works when a delete is rejected
├── pages/Dashboard/QuickActions.tsx                # "Darbų katalogas" → /works
└── App.tsx                                         # /works routes under ManagerRoute
tests/client/ConfirmDialogTests/RemoveWork.test.tsx
```

## Implementation Order

1. **Shared helpers first** (no behaviour change; existing tests stay green): `NumberRules`, `SearchPattern`, `UniqueSave`, used by MaterialService/MaterialRules/AuthService.
2. **Data**: entities, DbContext configuration, migration `AddWork`.
3. **Validation**: `WorkRules` and the request records.
4. **Service and API**: `WorkService` (CRUD, composition replace, duplicates, unknown material, search), controller, exception mapping, mapper.
5. **Material in use** (004 T038/T043).
6. **Client**: shared search field, Works pages, routes, quick action, the Materials 409 message lists the works.
7. **Tests** throughout, as listed in tasks.md.

## Complexity Tracking

No violations.
