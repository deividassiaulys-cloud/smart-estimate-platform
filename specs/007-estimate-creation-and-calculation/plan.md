# Plan: Estimate Creation and Calculation

**Branch**: `007-estimate-creation-and-calculation` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

## Summary

A Manager creates an estimate in a project and builds it in the Estimate Editor. Adding a work with a quantity copies the work's labour data and its materials' prices and norms into the estimate (BR-010). The material lines and every amount are then calculated by one pure calculation service, using BR-015–BR-017.

Only inputs are stored, and amounts are computed on every read. Each change is saved at once, and the server returns the whole recalculated estimate (spec Clarifications Q4). Every change passes one Draft-only check, so feature 008 only adds the status transitions.

This feature also delivers the parts moved from 006: the estimate list on Project Detail, and deleting a project's estimates with it (T185–T187). Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: ASP.NET Core 10, EF Core 10 (Pomelo MySQL; SQLite in tests), React Router 7, Tailwind CSS 4.3.3. No new dependencies.

**Storage**: MySQL. A new migration, `AddEstimate`, adds three tables: `Estimates`, `EstimateWorkLines`, `EstimateMaterialLines`. The task requires it, so AGENTS.md allows the migration.

**Testing**: xUnit (pure unit tests for the calculator and rounding; WebApplicationFactory/SQLite integration tests), Vitest + Testing Library

**Performance Goals**: Totals within 1 second of a change, and an estimate of up to 200 lines loads within 3 seconds (NFR-009). Calculating 200 lines in memory takes microseconds; one request per change.

**Constraints**:
- Money is `decimal`, rounded only through `MoneyRounding` (half away from zero, AGENTS.md).
- Quantities and norms are never rounded.
- No foreign keys from estimate lines to the catalog.
- Viewers get 403 on every estimate endpoint until 009.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| I correctness | The worked example (1263.26 €) and the midpoint case are pinned by unit tests | Pass |
| II.1 / III business logic | Every calculation lives in `EstimateCalculator` (Application layer). Controllers and React only display the amounts the API returns (docs/architecture.md) | Pass |
| II.4 / X.1 DRY | Calculation in exactly one place. The "0–100, ≤ 2 decimals" percentage rule and the "> 0, ≤ 3 decimals" quantity rule are extracted into `NumberRules`, used by `WorkRules` (waste, consumption norm) and `EstimateRules` (percentages, quantity). The Draft-only check is defined once | Pass |
| II.5 tests | Each rule (formula, rounding, sequential percentages, ranges, copy isolation, line removal, Draft check, cascade) has an automated test | Pass |
| III no features beyond spec | No estimate rename or delete, no status transitions, no Viewer view (Q3) | Pass |
| IV security | `Estimate` is `ICompanyScoped`. Lines are filtered through their estimate, like work lines (PR #12). Manager-only endpoints; isolation and 403 integration tests | Pass |
| VI / VIII | Spec/plan/tasks committed separately; migration only through T067 | Pass |
| X.2–X.10 | `IEstimateService` in `AddApplicationServices()`; records in `Models/DTOs/Estimates/`; mapping in `DtoMapper`; thin `EstimatesController : ApiControllerBase`; private setters; Tailwind + `utils/ui.ts`; `ConfirmDialog` for removing a work line; no CancellationToken; validation on the request records | Pass |
| X.3 static helpers | `MoneyRounding` and `EstimateCalculator` are pure static helpers with no state or I/O, so they need no interface | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/Entities/Estimate.cs, EstimateStatus.cs, EstimateWorkLine.cs, EstimateMaterialLine.cs
├── Models/DTOs/Estimates/  CreateEstimateRequest, AddWorkLineRequest, ChangeQuantityRequest,
│                            ChangePercentagesRequest, EstimateResponse, EstimateWorkLineResponse,
│                            EstimateMaterialLineResponse, EstimateSummaryResponse
├── Services/MoneyRounding.cs, EstimateCalculator.cs     # pure static
├── Services/EstimateRules.cs, NumberRules.cs (shared quantity/percentage checks)
├── Services/IEstimateService.cs, EstimateService.cs
├── Services/UnknownWorkException.cs, EstimateNotDraftException.cs
├── Services/Mapping/DtoMapper.cs                        # + estimate mapping (from entity + calculation)
├── Controllers/EstimatesController.cs, DomainExceptionHandler.cs
├── Data/AppDbContext.cs                                 # + 3 tables, filters through the estimate, cascades
└── Migrations/<timestamp>_AddEstimate.cs
tests/server/MoneyRoundingTests/, EstimateCalculatorTests/, EstimateServiceTests/,
tests/server/CompanyIsolationTests/EstimateIsolationTests.cs, ProjectServiceTests/ProjectDeleteCascadeTests.cs
client/src/
├── interfaces/estimate.ts, api/estimates.ts
├── pages/Estimates/EstimateCreatePage.tsx, EstimateEditorPage.tsx, AddWorkForm.tsx,
│                   EstimateWorkLineRow.tsx, EstimatePercentagesForm.tsx, EstimateTotals.tsx
├── pages/Projects/ProjectDetailPage.tsx, ProjectEstimatesTable.tsx   # estimate list (Manager)
└── App.tsx                                              # /projects/:id/estimates/new, /estimates/:id (ManagerRoute)
tests/client/EstimateEditorTests/
```

## Implementation Order

1. **Setup**: `MoneyRounding` and its tests; shared `NumberRules` checks (no behaviour change for 005).
2. **Calculator**: `EstimateCalculator` and its unit tests (worked example, midpoint, unrounded quantities). This is pure logic, before any database work.
3. **Data**: entities, DbContext (precision, filters, cascades), migration.
4. **US1**: create an estimate; add a work (copy + generate); editor UI; estimate list on Project Detail.
5. **US2**: change a quantity and the percentages; totals refresh in the editor.
6. **US3**: remove a work line, with confirmation.
7. **Cross-cutting**: Draft check test, Viewer 403, isolation, project cascade delete.

## Complexity Tracking

No violations.
