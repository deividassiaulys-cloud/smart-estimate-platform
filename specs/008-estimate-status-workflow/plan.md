# Plan: Estimate Status Workflow

**Branch**: `008-estimate-status-workflow` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

## Summary

A Manager confirms a Draft estimate and can return a Confirmed one to Draft, each after a confirmation dialog. Feature 007 already built most of what this feature needs:
- the `Status` column (Draft on creation)
- the one Draft-only check that every change passes (409 for a Confirmed estimate)
- the status in the project's estimate list

008 adds the two transitions (with 409 for a repeated change), their endpoints, and the editor's read-only mode with the status buttons. It also replaces 007's database-level guard test with one that confirms through the real API. Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged; no new dependencies

**Storage**: no schema change, so **no migration**. `Estimates.Status` exists since `AddEstimate` (007); the old task T090 is obsolete.

**Testing**: xUnit + WebApplicationFactory/SQLite, Vitest + Testing Library

**Project Type**: Web application (server/ + client/)

**Constraints**: only a Manager changes the status (BR-014); Viewers keep getting 403 on every estimate endpoint until 009, so they never receive the status

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | The transition rules (only Draft → Confirmed, only Confirmed → Draft) live in `EstimateService`. The entity mutators only set the status (X.6). React only shows buttons by status | Pass |
| II.4 / X.1 DRY | Edits keep the single Draft-only check from 007. The two transitions share one private method. The editor gets one `readOnly` flag instead of a second read-only page (spec Q3) | Pass |
| II.5 tests | Every rule (confirm, return, repeated change, empty estimate, edits on Confirmed through the API) has an integration test | Pass |
| III no features beyond spec | No reason or comment for returning to Draft (existing clarification), no restriction on empty estimates (Q2), no Viewer view (009) | Pass |
| IV security | Manager-only endpoints; Viewer 403 and company isolation (404) tested for both transitions | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |
| X.2–X.10 | Two thin controller actions in `EstimatesController`; `EstimateStatusException` mapped in `DomainExceptionHandler`; `ConfirmDialog` for both changes (NFR-011); Tailwind | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/Entities/Estimate.cs                 # + Confirm(), ReturnToDraft()
├── Services/IEstimateService.cs, EstimateService.cs   # + ConfirmAsync, ReturnToDraftAsync
├── Services/EstimateStatusException.cs         # new: 409 for a repeated status change
├── Controllers/EstimatesController.cs          # + PATCH confirm, PATCH draft
└── Controllers/DomainExceptionHandler.cs       # + mapping
tests/server/EstimateServiceTests/EstimateStatusTests.cs (new), EstimateDraftGuardTests.cs (through the API),
tests/server/EstimateServiceTests/EstimateAuthorizationTests.cs, CompanyIsolationTests/EstimateIsolationTests.cs
client/src/
├── api/estimates.ts                            # + confirmEstimate, returnEstimateToDraft
├── pages/Estimates/EstimateEditorPage.tsx      # status actions + read-only mode
├── pages/Estimates/EstimateStatusActions.tsx   # new: badge + button + ConfirmDialog
├── pages/Estimates/EstimateWorkLineRow.tsx, EstimatePercentagesForm.tsx   # readOnly prop
tests/client/EstimateEditorTests/EstimateStatus.test.tsx
```

## Implementation Order

1. Entity mutators, exception, service methods and endpoints (US1 confirm, US2 return).
2. Server tests: transitions, repeated change, empty estimate, edits on Confirmed through the API, Viewer 403, isolation, status in the list (US3).
3. Client: status actions with confirmation, read-only mode, 409 handling (message + reload), frontend test.

## Complexity Tracking

No violations.
