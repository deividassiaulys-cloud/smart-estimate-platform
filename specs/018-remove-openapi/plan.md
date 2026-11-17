# Plan: Remove the OpenAPI Document

**Branch**: `018-remove-openapi` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

## Summary

Remove the unused OpenAPI document (spec Q1):
- `builder.Services.AddOpenApi()` and `app.MapOpenApi()` in Program.cs;
- the `Microsoft.AspNetCore.OpenApi` package;
- all 89 `[ProducesResponseType]` attributes in the controllers and `ApiControllerBase`.

Behaviour does not change. The attributes are API-explorer metadata only, so routes, status codes, bodies, authorization and the automatic 400 validation response stay the same. docs/architecture.md and docs/review/change-log.md record the decision.

Decisions are in [research.md](research.md); the only external change is in [contracts/api-changes.md](contracts/api-changes.md).

## Technical Context

**Language/Version**: C# / .NET 10 (server only; no client change)

**Primary Dependencies**: two removed (`Microsoft.AspNetCore.OpenApi`, and the never-used `Swashbuckle.AspNetCore` found during implementation), none added

**Storage**: no change, no migration

**Testing**:
- The existing xUnit and Vitest suites must pass unchanged, with the counts equal to the baseline on `main` (SC-018-1).
- The tests run in the "Testing" environment, where the document was never mapped. So SC-018-3 (404 at `/openapi/v1.json` in Development) is checked by starting the API in Development and requesting the address (quickstart.md).

**Project Type**: Web application

**Constraints**: no behaviour change (FR-018-3, FR-018-4); no file outside server/, docs/, specs/ changes

## Constitution Check

| Article | Check | Result |
|---|---|---|
| I / II.5 tests | Existing tests cover every status code the attributes described (400/401/403/404/409/423, 201/204); they must pass unchanged | Pass |
| II.1 / III | No business logic touched | Pass |
| III dependencies | One dependency removed, none added (no plan justification needed for removal) | Pass |
| III no features beyond spec | Only the removal; no replacement documentation tool | Pass |
| IV security | `[Authorize]` attributes and role checks unchanged; existing 401/403 and isolation tests must pass | Pass |
| VI / VIII | Spec/plan/tasks committed separately from code; no migration, .env or build output | Pass |
| X.1 / X.2 / X.8 | Removes metadata no requirement uses (YAGNI); controllers keep only binding, one service call and the HTTP result | Pass |

**Post-design re-check**: Pass. Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Program.cs                                   # remove AddOpenApi() and the Development MapOpenApi() block
├── SmartEstimatePlatform.Api.csproj             # remove the Microsoft.AspNetCore.OpenApi and Swashbuckle.AspNetCore PackageReferences
└── Controllers/
    ├── ApiControllerBase.cs                     # keep [ApiController]; remove the 400 attribute; update the doc comment
    ├── AuthController.cs, EstimatesController.cs, EstimateNamesController.cs, MaterialsController.cs,
    │   ProjectsController.cs, TeamController.cs, UnitsController.cs, WorksController.cs   # remove every [ProducesResponseType]
docs/architecture.md                             # API documentation row
docs/review/change-log.md                        # dated entry
```

## Implementation Order

1. **Baseline:** run the full check set on the unchanged branch and record the test counts.
2. **Remove** the attributes, the registration, the endpoint and the package; build with 0 warnings and no unused `using` left.
3. **Docs:** docs/architecture.md and docs/review/change-log.md.
4. **Full checks:** same counts as the baseline; the Development 404 check (quickstart.md).

## Complexity Tracking

No violations.
