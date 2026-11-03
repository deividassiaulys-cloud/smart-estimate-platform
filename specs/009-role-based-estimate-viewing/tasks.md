# Tasks: Role-Based Estimate Viewing

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/estimate-names-api.md, quickstart.md in `specs/009-role-based-estimate-viewing/`

`[US#]` refers to the user stories of this feature in `spec.md`. The IDs T100–T104 are kept. New tasks continue the project-wide numbering from T208.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. There's no migration in this feature.

## Already delivered by earlier features

- [X] T102 Role check returning 403 for Manager-only endpoints in server/SmartEstimatePlatform.Api/Controllers/WorksController.cs and server/SmartEstimatePlatform.Api/Controllers/MaterialsController.cs (`[Authorize(Roles = Manager)]` since 004/005, tested by MaterialAuthorizationTests and WorkAuthorizationTests). Estimates and status are also Manager-only since 007/008

## Phase 1: User Story 1: A Viewer sees only names (P1) 🎯 MVP

**Goal**: A Viewer lists a project's estimates and opens one to see only its work and material names.
**Independent Test**: T104, T209 and T212 pass, and quickstart.md steps 2–4 work.

- [ ] T101 [US1] Add sealed records with init properties (doc comments) in server/SmartEstimatePlatform.Api/Models/DTOs/Estimates/:
  - `EstimateNameResponse` (Id, Name)
  - `EstimateNamesResponse` (Name, Works)
  - `WorkNamesResponse` (WorkName, MaterialNames as `List<string>`)

  None of them may have a numeric or status property (data-model.md invariant). Add `ToNameResponse(this Estimate)` and `ToNamesResponse(this Estimate)` to Services/Mapping/DtoMapper.cs: works in the order they were added (by ID), materials in their order, a repeated material kept each time.
- [ ] T100 [US1] Add `ListNamesForProjectAsync(projectId)` and `GetNamesAsync(id)` (doc comments) to IEstimateService/EstimateService in server/SmartEstimatePlatform.Api/Services/EstimateService.cs. Reuse `EnsureProjectExistsAsync`, `EstimatesWithLines` and `FindAsync`:
  - every estimate, Draft and Confirmed, A–Z by name then by ID
  - 404 for a missing or another company's project or estimate
  - no calculation
- [ ] T208 [US1] Add server/SmartEstimatePlatform.Api/Controllers/EstimateNamesController.cs: `ApiControllerBase`, `[Route("api")]`, class-level `[Authorize]` (any role) and 401/404, with `GET projects/{projectId}/estimate-names` and `GET estimates/{id}/names`. One service call per action; success types declared
- [ ] T104 [US1] Add integration tests in tests/server/EstimateServiceTests/EstimateNamesTests.cs:
  - a Viewer lists both a Draft and a Confirmed estimate by name, A–Z
  - a Viewer opens the worked-example estimate and gets "Siena A++" with Gipso plokštė, Varžtai and Plėvelė in order
  - a repeated material appears twice
  - the raw JSON of both responses contains only the property names `id`, `name`, `works`, `workName` and `materialNames`
  - a Viewer still gets 403 on `GET /api/estimates/{id}` and `GET /api/projects/{id}/estimates`
  - a Manager can call both new endpoints
  - anonymous requests get 401
- [ ] T209 [US1] Add a structural test in tests/server/EstimateServiceTests/EstimateNamesTests.cs: by reflection, `EstimateNameResponse`, `EstimateNamesResponse` and `WorkNamesResponse` (and their nested element types) have no property of a numeric type other than `EstimateNameResponse.Id`, and none of type `EstimateStatus`
- [ ] T210 [US1] Extend tests/server/CompanyIsolationTests/EstimateIsolationTests.cs: another company's project and estimate give 404 on both name endpoints
- [ ] T211 [P] [US1] Add `EstimateName`, `EstimateNames` and `WorkNames` types in client/src/interfaces/estimate.ts, and `getProjectEstimateNames(projectId)` and `getEstimateNames(id)` in client/src/api/estimates.ts
- [ ] T103 [US1] Add client/src/pages/Estimates/EstimateViewPage.tsx: the estimate name as the heading, each work name as a sub-heading with its material names as a list (semantic HTML, Tailwind via `utils/ui.ts`), a back link to the project, loading and error states, and no numbers. Add the route `/estimates/:id/view` under `ProtectedRoute` in client/src/App.tsx
- [ ] T212 [US1] Show estimates to a Viewer on Project Detail with `ProjectEstimateNamesTable` (name and "Atidaryti" → `/estimates/:id/view`; "Sąmatų dar nėra." when there are none). The Manager's table is unchanged. In client/src/pages/Projects/ProjectDetailPage.tsx and ProjectEstimateNamesTable.tsx
- [ ] T213 [US1] Add a frontend test in tests/client/ViewerTests/ViewerEstimateView.test.tsx:
  - a Viewer's Project Detail lists the estimate names with "Atidaryti" and no status or total
  - the Estimate View shows the work and material names, and no "€", none of the example's quantities or amounts ("35", "77", "918,75", "420,00"), and no "Juodraštis" or "Patvirtinta". Names may themselves contain digits, so "no digits at all" isn't the rule

## Phase 2: Polish

- [ ] T214 Run the full check set, plus quickstart.md (against the real app if the local database is configured, otherwise a mock-API visual check of the Viewer's Project Detail and Estimate View); fix any regression

## Dependencies

- T101 → T100 → T208 → T104, T209, T210.
- T211 is independent. T103 and T212 need T211. T213 needs T103 and T212. T214 runs last.

## Parallel Execution Examples

- T211 (client types/API) in parallel with T101/T100/T208 (server).
- After T208: T104, T209 and T210 can be written in parallel; T209 lives in the same file as T104, so write them in turn.

## Implementation Strategy

One user story, so the MVP is the whole feature. The server comes first, because it alone guarantees that no number reaches a Viewer; the pages only display what they receive. Spec/plan/tasks commits stay separate from code commits (Article VI).
