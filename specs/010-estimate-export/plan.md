# Plan: Estimate Export

**Branch**: `010-estimate-export` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

## Summary

A Manager exports an estimate (Draft or Confirmed) to an `.xlsx` file after a confirmation dialog (wireframe wf15). The file is built on the server from **the same `EstimateResponse` the editor shows** (`IEstimateService.GetAsync`), so its data matches the screen by construction (FR-027).

The file is one sheet:
- a header: project, estimate, status, export date
- each work line followed by its material lines
- the totals with their percentages

Amounts are number cells holding exactly the server's values, formatted like the screen, with no formulas. Every text cell is stored explicitly as text, which stops formula injection (NFR-007). The endpoint is Manager-only, so a Viewer gets 403 (BR-004, BR-005). Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: new `ClosedXML` 0.105.1 (MIT, published 2026-07-25, targets .NET Standard 2.0/2.1, so it runs on .NET 10).

- **Justification** (AGENTS.md, Article III): FR-027 requires an Excel file generated on the backend (requirements §11 "Dependencies"). ClosedXML is the established MIT library over Microsoft's `DocumentFormat.OpenXml`, and it supports number formats and quote-prefixed text (NFR-007) without hand-writing OpenXML parts.
- No new frontend dependency.

**Storage**: no schema change, no migration

**Testing**: xUnit + WebApplicationFactory/SQLite (the downloaded file is read back with ClosedXML), Vitest + Testing Library

**Project Type**: Web application (server/ + client/)

**Constraints**: the file matches the screen (same source data), no formulas, Manager only, company isolation (404)

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | Building the file is in the Application layer (`EstimateExportService`); the controller returns the file; React only downloads it | Pass |
| II.4 / X.1 DRY | The export reuses `IEstimateService.GetAsync` (calculation and scoping) and `IProjectService.GetAsync`; no second calculation. The status names are defined once on the server for the file. One `send` helper serves both JSON and file downloads on the client | Pass |
| II.5 tests | The file matches the worked example's screen data, number cells and formats, no formulas, formula-like names stored as text, status, Viewer 403, isolation | Pass |
| III dependencies | `ClosedXML` justified above. No other dependency | Pass |
| III no features beyond spec | One sheet, no PDF (Should Have FR-028, deferred), no Viewer export | Pass |
| IV security | Manager only; isolation tested; injection-safe text (NFR-007) | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |
| X.2–X.10 | `IEstimateExportService` in `AddApplicationServices()`; result type is a record; the controller action makes one call; `ConfirmDialog` for the export; Tailwind | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── SmartEstimatePlatform.Api.csproj                   # + ClosedXML 0.105.1
├── Services/IEstimateExportService.cs, EstimateExportService.cs   # new
├── Services/ExportFile.cs                             # new record: FileName, ContentType, Content
├── Services/EstimateStatusNames.cs                    # new: Lithuanian status names for the file
└── Controllers/EstimatesController.cs                 # + GET estimates/{id}/export
tests/server/EstimateExportTests/EstimateExportTests.cs
client/src/
├── api/http.ts                                        # + apiDownload (shares one send helper with apiFetch)
├── api/estimates.ts                                   # + exportEstimate
├── components/ConfirmDialog.tsx                       # + confirmTone ("danger" default | "primary")
├── pages/Estimates/EstimateExportButton.tsx           # new: button + ConfirmDialog + download
└── pages/Estimates/EstimateEditorPage.tsx, EstimateStatusActions.tsx   # use the export button; primary tone for status changes
tests/client/EstimateEditorTests/ExportEstimate.test.tsx
```

## Implementation Order

1. Add the dependency, `ExportFile`, the status names, `EstimateExportService` (layout, formats, quote-prefixed text), registration.
2. The endpoint and the server tests (read the file back).
3. Client: `apiDownload`, the export button with the dialog (primary tone), the frontend test.

## Complexity Tracking

No violations.
