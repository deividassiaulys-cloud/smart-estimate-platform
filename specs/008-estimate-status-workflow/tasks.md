# Tasks: Estimate Status Workflow

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/estimate-status-api.md, quickstart.md in `specs/008-estimate-status-workflow/`

`[US#]` refers to the user stories of this feature in `spec.md`. The IDs T089–T099 are kept. New tasks continue the project-wide numbering from T200.

Feature 007 already delivered the Status field, its column, the edit lock and the status in the list (research R1). Those tasks are marked done below, with what delivered them; nothing in them is repeated.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass, and `has-pending-model-changes` must report no changes (no migration in this feature).

## Already delivered by feature 007

- [X] T089 [US1] Status field (default Draft) on the Estimate entity: `Estimate.Status`, `EstimateStatus` in server/SmartEstimatePlatform.Api/Models/Entities (007 T065)
- [X] T090 [US1] Migration for the Status field: obsolete; the column is in 007's `AddEstimate` migration in server/SmartEstimatePlatform.Api/Migrations (007 T067)
- [X] T093 [US3] Status in the Manager's estimate list (`EstimateSummaryResponse.Status`), never reaching Viewers (403 on every estimate endpoint) in server/SmartEstimatePlatform.Api/Services/EstimateService.cs (007 T070, T198)
- [X] T097 [US3] Status shown as text in the Project Detail estimate list in client/src/pages/Projects/ProjectEstimatesTable.tsx (007 T186)

## Phase 1: User Story 1: Confirm an estimate (P1) 🎯 MVP

**Goal**: A Manager confirms a Draft estimate after a confirmation; it becomes read-only.
**Independent Test**: T098 and T201 pass; quickstart steps 1–2 work.

- [ ] T200 [US1] Add `Confirm()` and `ReturnToDraft()` (doc comments; they only set the status, X.6) to server/SmartEstimatePlatform.Api/Models/Entities/Estimate.cs. Add `EstimateStatusException(string message)` (doc comments) in server/SmartEstimatePlatform.Api/Services/EstimateStatusException.cs, mapped to 409 with detail = message in Controllers/DomainExceptionHandler.cs
- [ ] T091 [US1] Add `ConfirmAsync(id)` to IEstimateService/EstimateService in server/SmartEstimatePlatform.Api/Services/EstimateService.cs, through a private `ChangeStatusAsync(id, from, to, message)`: 404 if missing; `EstimateStatusException("The estimate is already confirmed.")` unless Draft; works with any number of lines (spec Q2); returns the recalculated estimate
- [ ] T094 [US1] Add `PATCH /api/estimates/{id}/confirm` (no body, 200 `EstimateResponse`, 409 declared) in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T098 [US1] Rewrite tests/server/EstimateServiceTests/EstimateDraftGuardTests.cs to confirm through `PATCH …/confirm` instead of setting the status in the database. Then adding a work, changing a quantity or the percentages, and removing a line each give 409, and nothing changes
- [ ] T201 [US1] Add integration tests in tests/server/EstimateServiceTests/EstimateStatusTests.cs:
  - confirming a Draft estimate returns `status` Confirmed with unchanged amounts
  - confirming an estimate without works succeeds
  - confirming twice gives 409 "The estimate is already confirmed."
- [ ] T202 [P] [US1] Add `confirmEstimate(id)` and `returnEstimateToDraft(id)` in client/src/api/estimates.ts
- [ ] T096 [US1] Add client/src/pages/Estimates/EstimateStatusActions.tsx:
  - a badge with the status name as text
  - "Patvirtinti" (Draft) or "Grąžinti į juodraštį" (Confirmed), each through `ConfirmDialog` (NFR-011)

  Use it in EstimateEditorPage. On 409, show "Sąmatos būsena jau pasikeitė." and reload the estimate (spec Q1)
- [ ] T203 [US1] Add a read-only mode to the editor, `readOnly` when Confirmed (spec Q3):
  - `AddWorkForm` isn't rendered
  - `EstimateWorkLineRow` disables its quantity input and hides "Pašalinti"
  - `EstimatePercentagesForm` disables its inputs

  On any 409 (an edit or a status change from a stale page), the editor shows the server's reason and reloads the estimate, so a page left open as Draft switches to read-only.

  In client/src/pages/Estimates/EstimateEditorPage.tsx, EstimateWorkLineRow.tsx and EstimatePercentagesForm.tsx

**Checkpoint**: US1 is done when T098 and T201 pass and quickstart steps 1–2 work.

## Phase 2: User Story 2: Return to Draft (P2)

**Goal**: A Manager returns a Confirmed estimate to Draft after a confirmation; it becomes editable again.
**Independent Test**: T204 passes; quickstart step 5 works.

- [ ] T092 [US2] Add `ReturnToDraftAsync(id)` to IEstimateService/EstimateService through the same `ChangeStatusAsync`: `EstimateStatusException("The estimate is already a draft.")` unless Confirmed, in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T095 [US2] Add `PATCH /api/estimates/{id}/draft` (no body, 200 `EstimateResponse`, 409 declared) in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T204 [US2] Add integration tests in tests/server/EstimateServiceTests/EstimateStatusTests.cs:
  - returning a Confirmed estimate to Draft makes it editable again: a quantity change then succeeds
  - returning a Draft estimate gives 409 "The estimate is already a draft."

## Phase 3: User Story 3: Status in the list (P1)

**Goal**: The project's estimate list shows Draft or Confirmed for every estimate.
**Independent Test**: T099 passes; quickstart step 3 works.

- [ ] T099 [US3] Add an integration test in tests/server/EstimateServiceTests/EstimateStatusTests.cs: with one Draft and one Confirmed estimate in a project, `GET /api/projects/{id}/estimates` returns each one's status for a Manager

## Phase 4: Cross-cutting

- [ ] T205 Extend tests/server/EstimateServiceTests/EstimateAuthorizationTests.cs (a Viewer gets 403 on `…/confirm` and `…/draft`) and tests/server/CompanyIsolationTests/EstimateIsolationTests.cs (another company's estimate gives 404 on both, and its status doesn't change)
- [ ] T206 Add a frontend test in tests/client/EstimateEditorTests/EstimateStatus.test.tsx:
  - "Patvirtinti" asks for confirmation; cancelling sends nothing; confirming sends one `PATCH …/confirm`
  - after confirming, the editor is read-only: no "Pridėti darbą", no "Pašalinti", disabled inputs, badge "Patvirtinta"
  - "Grąžinti į juodraštį" makes it editable again
- [ ] T207 Run the full check set, plus quickstart.md (against the real app if the local database is configured, otherwise a mock-API visual check of both editor modes); fix any regression

## Dependencies

- T200 → T091 → T094 → T098, T201. T202 is independent. T096 needs T202. T203 needs T096.
- T092 needs T091 (the shared `ChangeStatusAsync`) → T095 → T204.
- T099 needs T094.
- T205 needs T094 and T095. T206 needs T203 and T095. T207 runs last.

## Parallel Execution Examples

- T202 (client API) in parallel with T091/T094 (server).
- After T095: T204, T099 and T205 are independent test changes.

## Implementation Strategy

US1 (confirm) is the MVP. US2 reuses its transition method, and US3 was delivered by 007, so it only gets its test. Spec/plan/tasks commits stay separate from code commits (Article VI).
