# Tasks: Remove the OpenAPI Document

**Input**: spec.md, plan.md, research.md, contracts/api-changes.md, quickstart.md in `specs/018-remove-openapi/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T310 (T285–T309 belonged to feature 017, reverted in PR #24, and are not reused).

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. There's no migration in this feature.

## Phase 1: Setup: baseline

- [ ] T310 On the unchanged branch, run the full check set and record here the exact backend and frontend test counts, which must be all green. These are the baseline for SC-018-1.

## Phase 2: User Story 2: No unused API description (P1) 🎯 MVP

**Goal**: the document, its package and all 89 attributes are gone.
**Independent Test**: spec US2.

- [ ] T311 [US2] Remove every `[ProducesResponseType…]` line from server/SmartEstimatePlatform.Api/Controllers/*.cs:
  - AuthController (7), EstimatesController (31), EstimateNamesController (4), MaterialsController (12), ProjectsController (12), TeamController (9), UnitsController (1), WorksController (12), ApiControllerBase (1).
  - `ApiControllerBase` keeps `[ApiController]`. Update its doc comment so it no longer mentions declaring responses (research R2).
  - Remove any `using` that becomes unused.
  - Remove or update controller doc comments that mention response-type attributes (Constitution X.8 wording stays as is).
- [ ] T312 [US2] In server/SmartEstimatePlatform.Api/Program.cs, remove `builder.Services.AddOpenApi();` with its comment and the `if (app.Environment.IsDevelopment()) { app.MapOpenApi(); }` block. In SmartEstimatePlatform.Api.csproj, remove the `Microsoft.AspNetCore.OpenApi` PackageReference. Run `dotnet restore` and `dotnet build`: 0 warnings, 0 errors.
- [ ] T313 [US2] docs/architecture.md, "API documentation" row: no generated API description is published; the feature contracts in specs/*/contracts document the endpoints (spec Q1, feature 018). docs/review/change-log.md: a dated entry (what was removed, why, and that behaviour is unchanged).

## Phase 3: User Story 1: The application behaves exactly as before (P1)

**Goal**: nothing a user does changes.
**Independent Test**: spec US1.

- [ ] T314 [US1] Run the full check set. The backend and frontend test counts equal T310's, with no test file changed (`git diff --stat main -- tests` is empty). Then do the quickstart.md manual check: `/openapi/v1.json` gives 404 in Development, `/api/units` still answers 401, and the grep finds nothing.

## Dependencies

- T310 → T311 → T312 → T313 → T314. All tasks are sequential (the same files and the same build).

## Parallel Execution Examples

- None worth the coordination: the feature is four small sequential steps.

## Implementation Strategy

Record the baseline first, so "unchanged" is measurable. Remove the attributes and the document (US2), record the decision, then prove that nothing changed for users (US1). Spec/plan/tasks commits stay separate from code commits (Article VI).
