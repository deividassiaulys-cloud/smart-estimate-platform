# Tasks: Project Management

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/projects-api.md, quickstart.md in `specs/006-project-management/`

`[US#]` refers to the user stories of this feature in `spec.md`. The IDs T058–T062 and T064 are kept from the earlier task list. New tasks continue the project-wide numbering from T175. T063 (cascade delete of estimates) moved to feature 007 (spec Clarifications Q4).

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass.

## Phase 1: Foundational (data and validation)

- [X] T175 Add `ProjectRules` (with purpose doc comments) in server/SmartEstimatePlatform.Api/Services/ProjectRules.cs: `NameMaxLength = 200`, `DescriptionMaxLength = 2000`
- [X] T058 Add the `Project` entity (`ICompanyScoped`; Id, Name, Description (nullable), CompanyId, Company), with private setters, a private EF constructor, `Project(name, description)` and `Update(name, description)` (doc comments; mutators hold no rules), in server/SmartEstimatePlatform.Api/Models/Entities/Project.cs. Configure it in server/SmartEstimatePlatform.Api/Data/AppDbContext.cs:
  - `DbSet<Project> Projects`
  - Name max `ProjectRules.NameMaxLength`, Description max `ProjectRules.DescriptionMaxLength`
  - FK Company
  - no unique index
- [X] T059 Add the EF Core migration `AddProject` in server/SmartEstimatePlatform.Api/Migrations (`dotnet ef migrations add AddProject` with a dummy `ConnectionStrings__DefaultConnection`). Check that the table matches T058 (varchar(200) not null, varchar(2000) null, FK to Companies), then confirm `has-pending-model-changes` reports no changes
- [X] T176 Add sealed records with init properties (with purpose doc comments) in server/SmartEstimatePlatform.Api/Models/DTOs/Projects/:
  - `ProjectRequest`: `[Required]` and `[StringLength(ProjectRules.NameMaxLength)]` Name; `[StringLength(ProjectRules.DescriptionMaxLength)]` Description (nullable)
  - `ProjectResponse` (Id, Name, Description)
  - add `ToResponse(this Project)` to server/SmartEstimatePlatform.Api/Services/Mapping/DtoMapper.cs
- [X] T177 Add request-validation tests in tests/server/RequestValidationTests/ProjectRequestValidationTests.cs:
  - empty and whitespace-only names rejected
  - a 200-character name accepted, a 201-character name rejected
  - no description accepted
  - a 2000-character description accepted, a 2001-character description rejected

## Phase 2: User Story 1: Create, view, edit and delete projects (P1) 🎯 MVP

**Goal**: A Manager manages projects; every user of the company can view them.
**Independent Test**: The tests below pass, and quickstart.md steps 1–8 work.

- [X] T060 [US1] Add `IProjectService` (doc comments) and `ProjectService` in server/SmartEstimatePlatform.Api/Services/, registered in `AddApplicationServices()`, with:
  - `ListAsync()`: A–Z by name, then by ID
  - `GetAsync(id)`
  - `CreateAsync(request)` and `UpdateAsync(id, request)`: name trimmed; description trimmed, and whitespace-only becomes null
  - `DeleteAsync(id)`
  - missing project → `NotFoundException("Project not found.")`
  - responses come through DtoMapper

  The cascade delete of estimates is added in 007.
- [X] T061 [US1] Add server/SmartEstimatePlatform.Api/Controllers/ProjectsController.cs: `ApiControllerBase`, `[Route("api/projects")]`, class-level `[Authorize]` and 401.
  - `GET` (list) and `GET {id}`: any logged-in user
  - `POST` (201), `PUT {id}` and `DELETE {id}` (204): each with `[Authorize(Roles = Manager)]` and 403 declared
  - one service call per action; success types declared, as in contracts/projects-api.md
- [X] T178 [US1] Add integration tests in tests/server/ProjectServiceTests/ProjectEndpointsTests.cs:
  - create stores a trimmed name and description, and a whitespace-only description as null
  - two projects with the same name are both accepted
  - the list is ordered A–Z
  - edit changes name and description
  - delete removes the project (204)
  - an empty name gives 400 and saves nothing
  - get, edit or delete of a missing ID gives 404
- [X] T179 [US1] Add integration tests in tests/server/ProjectServiceTests/ProjectAuthorizationTests.cs:
  - a Viewer gets 200 for the list and the detail
  - a Viewer gets 403 for create, edit and delete, and nothing changes
  - an anonymous request gets 401
- [X] T064 [US1] Add integration tests in tests/server/CompanyIsolationTests/ProjectIsolationTests.cs: another company's project ID gives 404 on get, edit and delete, and its projects never appear in the list
- [X] T180 [P] [US1] Add `Project` and `ProjectRequest` in client/src/interfaces/project.ts, and `getProjects`, `getProject`, `createProject`, `updateProject`, `deleteProject` in client/src/api/projects.ts, following client/src/api/works.ts
- [X] T062 [US1] Add the Projects pages in client/src/pages/Projects, Tailwind via `utils/ui.ts`, one component per file:
  - `ProjectsPage`: heading, "+ Naujas projektas" for a Manager only, notice/error, "Projektų dar nėra." when empty, `ProjectsTable`
  - `ProjectsTable`: name as a link to the detail, plus the description
  - `ProjectDetailPage`: name, description, "Sąmatų dar nėra."; "Redaguoti" and "Ištrinti" for a Manager only; delete through `ConfirmDialog`, then back to `/projects` with the notice "Projektas ištrintas."
  - `ProjectFormPage`: create/edit; name and description (textarea); client checks for a required name, ≤ 200 and ≤ 2000, with Lithuanian messages; 400/404 messages
- [X] T181 [US1] Add routes in client/src/App.tsx (`/projects` and `/projects/:id` under `ProtectedRoute`; `/projects/new` and `/projects/:id/edit` under `ManagerRoute`), and link the "Projektai" quick action to `/projects` in client/src/pages/Dashboard/QuickActions.tsx
- [X] T182 [US1] Add a frontend test in tests/client/ConfirmDialogTests/RemoveProject.test.tsx:
  - deleting a project from Project Detail opens the confirmation
  - cancelling sends no DELETE
  - confirming sends one DELETE and returns to the list
  - a Viewer sees the project's name and description, but no "Redaguoti", "Ištrinti" or "+ Naujas projektas"

**Checkpoint**: US1 is done when all tests pass and quickstart.md steps 1–8 work.

## Phase 3: Polish

- [X] T183 Move the estimate part of this feature into specs/007-estimate-creation-and-calculation/tasks.md, as new tasks:
  - the estimate list on Project Detail (each estimate's name, plus its status from 008)
  - the cascade delete of a project's estimates (FK Estimate → Project, cascade)
  - its integration test (the former T063)
- [X] T184 Run the full check set, plus quickstart.md against the running app if the local database is configured; fix any regression

## Dependencies

- T175 → T058 → T059.
- T175 → T176 → T177.
- T060 needs T058 and T176. T060 → T061 → T178, T179, T064.
- Client: T180 → T062 → T181 → T182.
- T183 can be done at any time. T184 runs last.

## Parallel Execution Examples

- T180 (client API) can run in parallel with T060/T061 (server).
- T178, T179 and T064 are separate test files and can be written in parallel after T061.

## Implementation Strategy

One user story, so the MVP is the whole feature. Commit spec/plan/tasks separately from the code (Article VI), and use Conventional Commits, e.g. `feat(projects): …`.
