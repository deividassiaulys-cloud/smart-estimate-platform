# Plan: Project Management

**Branch**: `006-project-management` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

## Summary

A Manager creates, edits and deletes projects (name, optional description). Every user of the company, Viewers included, can list projects and open a Project Detail. In 006 the detail page shows the name, the description and "Sąmatų dar nėra.". The estimate list and the cascade delete of estimates come with feature 007 (spec Clarifications Q4). The feature follows the Article X structure. Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: ASP.NET Core 10, EF Core 10 (Pomelo MySQL; SQLite in tests), React Router 7, Tailwind CSS 4.3.3. No new dependencies.

**Storage**: MySQL. A new migration, `AddProject`, adds the `Projects` table. The task requires it, so AGENTS.md allows the migration.

**Testing**: xUnit + WebApplicationFactory/SQLite, Vitest + Testing Library

**Project Type**: Web application (server/ + client/)

**Performance / Scale**: Small lists; A–Z by name, no paging, no search (the spec asks for none)

**Constraints**: Viewers must never get write access. Company isolation (011) applies.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | The only rules are field rules: name required and ≤ 200, description ≤ 2000. They're declared on `ProjectRequest`, with their limits defined once in `ProjectRules` (X.10). No other business rule | Pass |
| II.5 tests | Field rules, Viewer read-only, isolation and confirmation all have tests | Pass |
| III no features beyond spec | No name uniqueness, no search, no estimate stub (Q3, Q4) | Pass |
| IV security | `Project` is `ICompanyScoped`. Read is allowed for any logged-in user of the company; write is Manager-only. Covered by integration tests | Pass |
| VI version control | Spec/plan/tasks committed separately; Conventional Commits | Pass |
| VIII autonomy | Migration only through the task that requires it (T059) | Pass |
| X.2–X.10 | `IProjectService` in `AddApplicationServices()`; records in `Models/DTOs/Projects/`; mapping in `DtoMapper`; thin `ProjectsController : ApiControllerBase`; private setters; Tailwind + `utils/ui.ts`; delete through `ConfirmDialog`; no CancellationToken | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/Entities/Project.cs                       # new
├── Models/DTOs/Projects/ProjectRequest.cs, ProjectResponse.cs
├── Services/ProjectRules.cs, IProjectService.cs, ProjectService.cs
├── Services/Mapping/DtoMapper.cs                    # + ToResponse(Project)
├── Controllers/ProjectsController.cs                # read: any user; write: Manager
├── Data/AppDbContext.cs                             # + Projects
└── Migrations/<timestamp>_AddProject.cs
tests/server/ProjectServiceTests/, CompanyIsolationTests/ProjectIsolationTests.cs,
tests/server/RequestValidationTests/ProjectRequestValidationTests.cs
client/src/
├── interfaces/project.ts, api/projects.ts
├── pages/Projects/ProjectsPage.tsx, ProjectsTable.tsx, ProjectDetailPage.tsx, ProjectFormPage.tsx
├── pages/Dashboard/QuickActions.tsx                 # "Projektai" → /projects
└── App.tsx                                          # read routes: ProtectedRoute; write routes: ManagerRoute
tests/client/ConfirmDialogTests/RemoveProject.test.tsx
```

## Implementation Order

1. Entity, DbContext, migration `AddProject`.
2. `ProjectRules`, DTOs, mapper, validation tests.
3. Service, controller (read/write authorization), and the endpoint, authorization and isolation tests (US1).
4. Client: list, detail, form, routes, quick action, delete confirmation and Viewer test.
5. Move the estimate-list and cascade-delete work (T063 and the estimate part of T060/T062) into `specs/007-estimate-creation-and-calculation/tasks.md`.

## Complexity Tracking

No violations.
