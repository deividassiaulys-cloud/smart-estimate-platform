# Plan: Resource Kinds and Estimate Columns

**Branch**: `017-resource-kinds-columns` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

## Summary

Gaps B and C (ANALYSIS.md §3, proposed FR-030–FR-032). A catalog material gets a kind, Material or Machinery, and estimate material lines copy it. Every work line and the estimate get Labour / Materials / Machinery amounts, and every work line gets a unit price rounded to 2 decimals (spec Q3). The editor and the Excel export use one 11-column layout with a labour row under each work line (spec Q1) and the waste % column kept (spec Q2). BR-016 is unchanged.

- **Server:**
  - `MaterialKind` enum; `Kind` on `Material` and `EstimateMaterialLine`; migration `AddMaterialKind` (default 0).
  - `EstimateCalculator` adds the parts, hours and unit price (research R2).
  - Replace and refresh carry the kind (R3, R4).
  - The export is rewritten to the new layout (R5, R6).
- **Client:**
  - A kind select and a kind column in the catalog.
  - The editor table gets the new columns and a labour row.
  - The totals start with the three parts.
  - The refresh dialog shows kind changes.

Decisions are in [research.md](research.md), entities and DTOs in [data-model.md](data-model.md), and the API and export layout in [contracts/estimate-columns-api.md](contracts/estimate-columns-api.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged. No new dependencies.

**Storage**: MySQL; migration `AddMaterialKind` adds two int columns with default 0 (required by the task, Constitution VIII)

**Testing**:
- xUnit unit tests:
  - `EstimateCalculator` parts, hours and unit price (worked example, plus a machinery line, zero labour, no materials).
  - `MaterialRules` kind validation.
  - `EstimateCatalogRefresh` kind change.
- xUnit + WebApplicationFactory:
  - The material kind round-trip and the default.
  - Kind copied when a work is added; replace takes the new kind.
  - The refresh preview and update with a kind change.
  - Export columns and rows.
  - The Viewer names response has no new fields.
  - Existing totals unchanged.
- Vitest + Testing Library:
  - The material form kind select and the list column.
  - The editor's work, labour and material rows and its totals.
  - The refresh dialog's kind label.
  - The Viewer view is unchanged.

**Project Type**: Web application

**Constraints**:
- Amounts are calculated only in `EstimateCalculator` and rounded only by `MoneyRounding` (BR-017).
- The total is unchanged (BR-016).
- Viewers see nothing new (BR-005).

**Prerequisite**: the student approves and writes FR-030–FR-032 and the BR-010 / BR-015 wording (from the Step 1 proposal, with BR-017's unit-price sentence) in docs/requirements.md, with a change-log entry, before code (Constitution VII.3). Feature 016 writes the gap A part. This feature writes the B and C part.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| I / II.5 tests | Calculator unit tests for every part; integration tests for copy, replace, refresh, export, Viewer; UI tests | Pass |
| II.1 / III business logic | Parts, hours, unit price only in `EstimateCalculator`; React and the export only read the response | Pass |
| II.4 / X.1 DRY | One calculation for screen and export; the refresh reuses its path with one new `RefreshedField`; kind labels defined once on the client | Pass |
| III no features beyond spec | No Labour kind, no per-column surcharges, no sections, no filter by kind, no resource codes | Pass |
| IV security | Manager-only fields; Viewer response unchanged and tested (NFR-005); company filters unchanged | Pass |
| VI / VIII | Spec, plan and tasks committed separately; one required migration; no .env or build output | Pass |
| VII ambiguity | Layout, waste column and unit price resolved by spec Q1–Q3; requirements update is a listed prerequisite | Pass (pending T285) |
| X.2–X.10 | No controller logic; DTO records one per file with mapping only in `DtoMapper`; entity mutators without rules; kind validated in `MaterialRules` (X.10); Tailwind, one component per file (the labour row is its own component); no CancellationToken | Pass |

**Post-design re-check**: Pass. Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/Entities/MaterialKind.cs                       # new enum
├── Models/Entities/Material.cs, EstimateMaterialLine.cs  # + Kind, ReplaceMaterial(…, kind), ChangeKind
├── Data/AppDbContext.cs                                  # Kind columns, default Material
├── Migrations/<timestamp>_AddMaterialKind.cs             # new, generated
├── Models/DTOs/Materials/MaterialRequest.cs, MaterialResponse.cs
├── Models/DTOs/Estimates/EstimateWorkLineResponse.cs, EstimateMaterialLineResponse.cs, EstimateResponse.cs, RefreshChangeResponse.cs
├── Services/MaterialRules.cs, MaterialService.cs
├── Services/WorkLineCalculation.cs, EstimateCalculation.cs, EstimateCalculator.cs
├── Services/RefreshedField.cs, CatalogMaterial.cs, EstimateCatalogRefresh.cs, EstimateService.cs
├── Services/Mapping/DtoMapper.cs
└── Services/EstimateExportService.cs
tests/server/ (EstimateCalculatorTests, MaterialServiceTests, EstimateServiceTests, EstimateLineEditingTests, EstimateRefreshTests, EstimateExportTests, CompanyIsolationTests, RequestValidationTests)
client/src/
├── interfaces/material.ts, estimate.ts                   # MaterialKind, kind labels, new fields
├── pages/Materials/MaterialFormPage.tsx, MaterialsTable.tsx
├── pages/Estimates/EstimateEditorPage.tsx                # 11 headings
├── pages/Estimates/EstimateWorkLineRow.tsx               # work row with parts
├── pages/Estimates/EstimateLabourRow.tsx                 # new: the labour row
├── pages/Estimates/EstimateMaterialLineRow.tsx           # norm, waste, kind column
├── pages/Estimates/EstimateTotals.tsx, EstimateRefreshButton.tsx
tests/client/ (EstimateEditorTests/EstimateColumns.test.tsx new, MaterialKind test, refresh, Viewer)
docs/requirements.md, docs/review/change-log.md, docs/ux.md
```

## Implementation Order

1. **Prerequisite:** write the approved requirements change for B and C.
2. **Kind:** enum, entities, `AppDbContext`, migration, DTOs, `MaterialRules`, service and mapping, tests (US1).
3. **Calculation:** calculator parts, hours, unit price, response fields, tests (US2 server).
4. **Kind in edits:** replace and refresh carry the kind, tests (US3 server).
5. **Export:** new layout and tests (US4).
6. **Client:** catalog kind; editor layout, labour row and totals; refresh label; tests.
7. **Full checks:** quickstart, 360 px check, docs/ux.md.

## Complexity Tracking

No violations.
