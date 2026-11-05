# Plan: Estimate Line Editing

**Branch**: `014-estimate-line-editing` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

FR-023: in a Draft estimate a Manager can change a work line's labour rate and a material line's price, remove a material line, and replace a material line's material with a same-unit catalog material (spec Q1). Norms and waste % stay as copied (Q2). Prices are edited inline like quantity (Q3).

- **Server:** four endpoints under the existing estimate routes, each answering the recalculated `EstimateResponse`. Entity behaviour methods, no new column and no migration. One shared money rule (`NumberRules.MoneyError`) replaces the two copies in `MaterialRules` and `WorkRules`.
- **Client:** one shared inline number field for quantity, labour rate and price; a material row component with the price, a same-unit material select and a confirmed removal.
- The Viewer names view and the Excel export read the stored lines, so they reflect the edits without changes.

Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged. No new dependencies.

**Storage**: no schema change, no migration (checked with `dotnet ef migrations has-pending-model-changes`)

**Testing**: xUnit + WebApplicationFactory/SQLite (worked example numbers, catalog untouched, other estimates untouched, 403/404/409/400, Viewer names and export reflect edits), Vitest + Testing Library (inline price save, validation message, ConfirmDialog removal, same-unit select, Confirmed read-only)

**Project Type**: Web application

**Constraints**: Draft only (BR-013); the catalog is never written (BR-010); money rounded only in `EstimateCalculator` through `MoneyRounding` (BR-017)

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | The Draft check, the same-unit rule and the copying of the material are in `EstimateService` and the entities; React only shows the server's numbers | Pass |
| II.4 / X.1 DRY | One `MoneyError` rule for price and labour rate (replacing two copies); one `FindDraftAsync`/`FindLine` path for every change; one inline field component for quantity, rate and price; Viewer view and export reuse the stored lines | Pass |
| II.5 tests | Worked example recalculation for each change, catalog and other estimates unchanged, every refusal (403, 404, 409, 400), Viewer view and export, the editor UI | Pass |
| III no features beyond spec | No norm or waste editing (Q2), no adding material lines, no search in the replacement list | Pass |
| IV security | Manager only; lines reached through the company-scoped estimate; foreign material IDs answered like missing ones | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |
| X.2–X.10 | Methods added to `IEstimateService`; one request record per file in `Models/DTOs/Estimates/` validated with the shared rule; one service call per action; `ConfirmDialog` for removal; Tailwind, one component per file; no CancellationToken | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/Entities/EstimateWorkLine.cs, EstimateMaterialLine.cs   # + ChangeLabourRate, RemoveMaterialLine, ChangePrice, ReplaceMaterial
├── Models/DTOs/Estimates/ChangeLabourRateRequest.cs, ChangePriceRequest.cs, ReplaceMaterialRequest.cs   # new
├── Services/NumberRules.cs                                        # + MoneyError
├── Services/MaterialRules.cs, WorkRules.cs                        # use MoneyError (same messages)
├── Services/IEstimateService.cs, EstimateService.cs               # + 4 methods, FindMaterialLine
├── Services/MaterialUnitMismatchException.cs                      # new
├── Controllers/DomainExceptionHandler.cs                          # + 400 for the unit mismatch
└── Controllers/EstimatesController.cs                             # + 4 actions
tests/server/EstimateLineEditingTests/EstimateLineEditingTests.cs
client/src/
├── api/estimates.ts                                               # + 4 calls
├── pages/Estimates/InlineNumberField.tsx                          # new: save on blur/Enter, error under the field
├── pages/Estimates/EstimateMaterialLineRow.tsx                    # new: price, replace select, remove
├── pages/Estimates/EstimateWorkLineRow.tsx                        # uses InlineNumberField for quantity and rate
└── pages/Estimates/EstimateEditorPage.tsx                         # loads materials, handlers, ConfirmDialog for material removal
tests/client/EstimateEditorTests/EstimateLineEditing.test.tsx
```

## Implementation Order

1. **Shared rule:** `NumberRules.MoneyError`, used by `MaterialRules` and `WorkRules` (existing tests pin the messages).
2. **US1 prices:** entity methods, requests, service, endpoints, server tests; `InlineNumberField`, rate and price fields, client tests.
3. **US2 removal:** entity method, service, endpoint, tests; the material row's "Pašalinti" with `ConfirmDialog`.
4. **US3 replacement:** exception and mapping, service, endpoint, tests; the same-unit select.
5. Viewer view and export tests, full checks.

## Complexity Tracking

No violations.
