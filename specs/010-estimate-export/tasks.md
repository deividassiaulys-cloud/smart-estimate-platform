# Tasks: Estimate Export

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/estimate-export-api.md, quickstart.md in `specs/010-estimate-export/`

`[US#]` refers to the user stories of this feature in `spec.md`. The IDs T105–T110 are kept. New tasks continue the project-wide numbering from T215.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. There's no migration in this feature.

## Phase 1: Setup

- [X] T215 Add the `ClosedXML` 0.105.1 package reference to server/SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj (justified in plan.md, Article III) and confirm the build and the existing tests still pass

## Phase 2: User Story 1: Export an estimate to Excel (P1) 🎯 MVP

**Goal**: A Manager exports a Draft or Confirmed estimate to an `.xlsx` that matches the screen, after a confirmation; a Viewer can't.
**Independent Test**: T109, T110 and T217 pass, and quickstart.md steps 1–5 work.

- [X] T216 [US1] Add (doc comments) in server/SmartEstimatePlatform.Api/Services/:
  - `ExportFile` (sealed record: FileName, ContentType, Content)
  - `EstimateStatusNames` (static: Draft → "Juodraštis", Confirmed → "Patvirtinta")
  - `IEstimateExportService` with `ExportAsync(int id)`, registered in `AddApplicationServices()`
- [X] T105 [US1] Implement `EstimateExportService.ExportAsync` in server/SmartEstimatePlatform.Api/Services/EstimateExportService.cs:
  - Take the estimate from `IEstimateService.GetAsync(id)` and the project name from `IProjectService.GetAsync`, so a missing or another company's estimate gives 404 as on screen.
  - Write one sheet "Sąmata": the header (Projektas, Sąmata, Būsena, Eksportuota as `yyyy-MM-dd` Europe/Vilnius via `TimeProvider`), the table header, each work line followed by its material lines (indented), then the totals with percentages, as in data-model.md.
  - Number cells hold exactly the response's values, with formats `#,##0.00 "€"` for money, `#,##0.###` for quantities and norms, and `0.## "%"` for percentages. No formulas.
  - Size the columns to their contents.
  - The file name is the estimate name with characters invalid in file names replaced by `_`, plus `.xlsx`.
- [X] T106 [US1] Write every text cell (names, units, labels, status) through one private helper that stores the value as text with `Style.IncludeQuotePrefix = true`, so a value starting with `=`, `+`, `-` or `@` is never evaluated (NFR-007), in server/SmartEstimatePlatform.Api/Services/EstimateExportService.cs
- [X] T107 [US1] Add `GET /api/estimates/{id}/export` in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs (Manager only, like the class). It makes one service call and returns `File(content, contentType, fileName)`, with the 200 content type and 404 declared
- [X] T109 [US1] Add integration tests in tests/server/EstimateExportTests/EstimateExportTests.cs (download through the API, read back with ClosedXML):
  - for the worked example, the header shows the project, the estimate and "Juodraštis", and the export date cell matches `yyyy-MM-dd` (only the format is checked, so the test can't fail around midnight)
  - every line value and every total equals `GET /api/estimates/{id}` (including 40.43 and 1263.26)
  - amounts are number cells with the expected formats, and no cell has a formula
  - a Confirmed estimate exports with "Patvirtinta"
  - the content type and the `Content-Disposition` file name are as in the contract (`Pradinė sąmata.xlsx`)
- [X] T217 [US1] Add an integration test in tests/server/EstimateExportTests/EstimateExportTests.cs: a material named `=HYPERLINK("http://x","y")` and a work named `-5` are exported as text cells with the quote prefix and no formula, reading exactly as typed (NFR-007)
- [X] T110 [US1] Add integration tests in tests/server/EstimateExportTests/EstimateExportTests.cs:
  - a Viewer gets 403 on the export endpoint (note from 009)
  - an anonymous request gets 401
  - another company's estimate gives 404 (also add the export endpoint to tests/server/CompanyIsolationTests/EstimateIsolationTests.cs)
- [X] T218 [P] [US1] Add `apiDownload(path)` in client/src/api/http.ts, returning `{ blob, fileName }` (from `filename*`, then `filename`, falling back to `samata.xlsx`). It shares one internal `send` helper with `apiFetch`, so the token and session-expiry handling exist once. Add `exportEstimate(id)` in client/src/api/estimates.ts
- [X] T219 [US1] Add `confirmTone: 'danger' | 'primary'` (default `danger`, so deletes don't change) to client/src/components/ConfirmDialog.tsx. Use `primary` in client/src/pages/Estimates/EstimateStatusActions.tsx for confirming and returning to Draft (closes 008's analysis U1)
- [X] T108 [US1] Add client/src/pages/Estimates/EstimateExportButton.tsx: "Eksportuoti į Excel", then `ConfirmDialog` (primary tone) with the title "Eksportuoti sąmatą?" and the message "Eksportuoti „<name>“ į Excel? Faile bus tie patys duomenys, kurie matomi ekrane."; busy while it runs; download via an object URL; on failure "Eksportuoti nepavyko. Bandykite dar kartą." (wireframe wf15). Show it in EstimateEditorPage for both statuses
- [X] T220 [US1] Add a frontend test in tests/client/EstimateEditorTests/ExportEstimate.test.tsx:
  - the export asks first; cancelling requests nothing
  - confirming requests `GET /api/estimates/9/export` once and starts a download with the server's file name
  - a failing export shows "Eksportuoti nepavyko. Bandykite dar kartą."
  - the Viewer's Estimate View has no export button

## Phase 3: Polish

- [X] T221 Run the full check set, plus quickstart.md: against the real app if the local database is configured; otherwise open a file produced by the T109 test in LibreOffice/Excel (or inspect it with ClosedXML) and check the layout visually. Fix any regression

## Dependencies

- T215 → T216 → T105 → T106 → T107 → T109, T217, T110.
- T218 is independent. T219 is independent. T108 needs T218 and T219. T220 needs T108. T221 runs last.

## Parallel Execution Examples

- T218 (client download) and T219 (dialog tone) in parallel with the server tasks T105–T107.
- T109, T217 and T110 share one test file, so write them in turn after T107.

## Implementation Strategy

One user story, so the MVP is the whole feature. The server comes first, because the file's correctness rests on reusing the editor's data. Spec/plan/tasks commits stay separate from code commits (Article VI).
