# Plan: Role-Based Estimate Viewing

**Branch**: `009-role-based-estimate-viewing` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

## Summary

A Viewer can list a project's estimates by name and open one to see its work names with their material names, and nothing else (BR-005, FR-026).

The data comes from two new read-only endpoints for any logged-in user of the company. Their response records have **no number or status fields at all**, so prices, quantities and status cannot leak even through a coding mistake (spec Q3). The Manager's estimate endpoints and editor don't change and stay 403 for Viewers.

On the client, Project Detail lists the estimate names for a Viewer, and a new read-only Estimate View page shows the names. Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged; no new dependencies

**Storage**: no schema change, no migration

**Testing**: xUnit + WebApplicationFactory/SQLite (including raw-JSON checks and a structural test of the response types), Vitest + Testing Library

**Project Type**: Web application (server/ + client/)

**Constraints**:
- Zero price, quantity, norm, percentage, total, unit or status data in any response a Viewer can receive.
- Company isolation (404).
- The Manager experience is unchanged.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | Which data a Viewer may see is decided by the server (new endpoints and records); the client only displays them | Pass |
| II.4 / X.1 DRY | Reuses the estimate loading (company-scoped, with lines) in `EstimateService`; one mapper for the names; Project Detail switches its estimates section by role instead of duplicating the page | Pass |
| II.5 tests | Viewer content, ordering, Draft and Confirmed alike, raw JSON with no forbidden fields, a structural test of the response types, isolation, 401 | Pass |
| III no features beyond spec | No units, no flat material list, no Viewer export (010) | Pass |
| IV security | All endpoints authenticated; company scoping (404); integration tests for Viewer data and for Manager endpoints staying 403 (NFR-005) | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |
| X.2–X.10 | Thin `EstimateNamesController : ApiControllerBase` with class-level `[Authorize]`; sealed records in `Models/DTOs/Estimates/`; mapping in `DtoMapper`; Tailwind; one component per file | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/DTOs/Estimates/EstimateNameResponse.cs, EstimateNamesResponse.cs, WorkNamesResponse.cs   # new
├── Services/IEstimateService.cs, EstimateService.cs     # + ListNamesForProjectAsync, GetNamesAsync
├── Services/Mapping/DtoMapper.cs                        # + ToNameResponse, ToNamesResponse
└── Controllers/EstimateNamesController.cs               # new, any logged-in user
tests/server/EstimateServiceTests/EstimateNamesTests.cs, CompanyIsolationTests/EstimateIsolationTests.cs
client/src/
├── interfaces/estimate.ts, api/estimates.ts             # + names types and calls
├── pages/Projects/ProjectDetailPage.tsx, ProjectEstimateNamesTable.tsx   # Viewer list
├── pages/Estimates/EstimateViewPage.tsx                 # new read-only names page
└── App.tsx                                              # /estimates/:id/view under ProtectedRoute
tests/client/ViewerTests/ViewerEstimateView.test.tsx
```

## Implementation Order

1. Response records, mapper, service methods, controller.
2. Server tests: content and order, Draft and Confirmed alike, raw JSON has only name fields, the response types have no numeric or status properties, isolation, 401, Manager endpoints still 403.
3. Client: Viewer estimate list on Project Detail, Estimate View page, route, frontend test.

## Complexity Tracking

No violations.
