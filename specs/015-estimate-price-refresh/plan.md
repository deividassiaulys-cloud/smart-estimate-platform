# Plan: Estimate Price Refresh

**Branch**: `015-estimate-price-refresh` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

## Summary

FR-024: a Manager updates a Draft estimate's labour rates, labour norms, material prices, consumption norms and waste % from the current catalog, after a read-only preview in the shared confirmation dialog (spec Q2, Q3). Only existing lines change (Q1).

- **Server:**
  - One pure, static `EstimateCatalogRefresh.Apply` matches lines to the catalog by name and unit, updates them in memory and reports the changes and the lines not found.
  - `GET …/refresh` runs it and returns the changes and both totals **without saving**. `POST …/refresh` runs the same code and saves, so the preview always matches the result.
- **Client:** an `EstimateRefreshButton` loads the preview and shows it in `ConfirmDialog`, which gets an optional `children` slot for the list. When there's nothing to change, a status message is shown instead.

Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged. No new dependencies.

**Storage**: no schema change, no migration

**Testing**:
- xUnit unit tests of `EstimateCatalogRefresh`: matching, duplicates in order, a missing work or material, case-insensitive names, no change when equal.
- xUnit + WebApplicationFactory: the worked example (preview total equals the result total, 1365,05), the preview saves nothing, the catalog and other estimates are unchanged, 403/404/409.
- Vitest + Testing Library: the dialog list, cancel, confirm, the "nothing to update" message, Confirmed hides the button, the 409 message.

**Project Type**: Web application

**Constraints**: Draft only (BR-013); the catalog is never written (BR-010); quantities, percentages and the set of lines never change (FR-015-4); money is rounded only in `EstimateCalculator` (BR-017)

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | Matching and updating are in the Application layer (`EstimateCatalogRefresh`, `EstimateService`); React only lists what the server returns | Pass |
| II.4 / X.1 DRY | One `Apply` serves both the preview and the update; `EstimateCalculator` for both totals; `ConfirmDialog` reused with an optional list slot instead of a second dialog | Pass |
| II.5 tests | Unit tests of the matching rules; integration tests for the worked example, isolation and roles; UI tests | Pass |
| III no features beyond spec | No line addition or removal (Q1), no per-line choice (Q2), no stored preview | Pass |
| IV security | Manager only; the estimate and the catalog are both read through the company query filters; integration tests for 403/404 | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |
| X.2–X.10 | Two `IEstimateService` methods; one service call per action; DTO records one per file, mapping in `DtoMapper`; entity mutators without rules; Tailwind, one component per file; no CancellationToken | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/Entities/EstimateWorkLine.cs, EstimateMaterialLine.cs      # + ChangeLabourNorm, ChangeNorms
├── Models/DTOs/Estimates/EstimateRefreshPreviewResponse.cs, RefreshChangeResponse.cs, RefreshMissResponse.cs   # new
├── Services/RefreshedField.cs, RefreshChange.cs, RefreshMiss.cs, CatalogRefreshResult.cs   # new: enum and result records
├── Services/EstimateCatalogRefresh.cs                                 # new: pure matching and update
├── Services/IEstimateService.cs, EstimateService.cs                   # + PreviewRefreshAsync, RefreshAsync
├── Services/Mapping/DtoMapper.cs                                      # + preview mapping
└── Controllers/EstimatesController.cs                                 # + GET/POST estimates/{id}/refresh
tests/server/EstimateRefreshTests/EstimateCatalogRefreshTests.cs, EstimateRefreshEndpointTests.cs
client/src/
├── components/ConfirmDialog.tsx                                       # + optional children (scrollable list)
├── interfaces/estimate.ts                                             # + RefreshedField, EstimateRefreshPreview
├── api/estimates.ts                                                   # + previewRefresh, refreshEstimate
├── pages/Estimates/EstimateRefreshButton.tsx                          # new
└── pages/Estimates/EstimateEditorPage.tsx                             # shows the button for Draft estimates
tests/client/EstimateEditorTests/EstimateRefresh.test.tsx
```

## Implementation Order

1. **Matching core:** entity mutators, `RefreshedField` and the result records, `EstimateCatalogRefresh.Apply`, unit tests.
2. **US1 preview:** `PreviewRefreshAsync`, the DTOs and mapping, `GET` endpoint, tests (nothing saved, worked example).
3. **US2 update:** `RefreshAsync`, `POST` endpoint, tests (result equals preview, catalog and other estimates unchanged, refusals).
4. **Client:** the `ConfirmDialog` list slot, API calls, `EstimateRefreshButton`, editor wiring, tests.
5. Full checks; docs/ux.md; the 360 px check of the dialog.

## Complexity Tracking

No violations.
