# Tasks: Estimate Creation and Calculation

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/estimates-api.md, quickstart.md in `specs/007-estimate-creation-and-calculation/`

`[US#]` refers to the user stories of this feature in `spec.md`. The IDs T065–T088 and T185–T187 (moved from 006) are kept; T071 and T073 were checks already covered by T193 and T086, so they were merged into them. New tasks continue the project-wide numbering from T188.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass.

## Phase 1: Setup (rounding and shared checks)

- [ ] T068 Add `MoneyRounding.Round(decimal amount)` = `decimal.Round(amount, 2, MidpointRounding.AwayFromZero)` (doc comments; the only money rounding in the codebase) in server/SmartEstimatePlatform.Api/Services/MoneyRounding.cs
- [ ] T083 Add unit tests in tests/server/MoneyRoundingTests/MoneyRoundingTests.cs: 0.005 → 0.01, 0.004 → 0.00, 0.015 → 0.02, 40.425 → 40.43, −0.005 → −0.01
- [ ] T188 Extract the shared checks into server/SmartEstimatePlatform.Api/Services/NumberRules.cs:
  - `PositiveQuantityError(decimal value, string label)` → "{label} must be greater than 0." / "{label} can have at most 3 decimal places." / "{label} is too large."
  - `PercentageError(decimal value, string label)` → "{label} must be between 0 and 100." / "{label} can have at most 2 decimal places."

  Make `WorkRules.ValidateLine` use them for the consumption norm ("Consumption norm") and the waste ("Waste percentage"). The texts and 005's tests stay unchanged.
## Phase 2: Foundational (data model)

- [ ] T065 Add the entities in server/SmartEstimatePlatform.Api/Models/Entities, as data-model.md describes (private setters, private EF constructor, doc comments, mutators without rules):
  - `EstimateStatus { Draft, Confirmed }`
  - `Estimate` (`ICompanyScoped`): `Estimate(projectId, name)` sets Draft and 0/0/21 %; `ChangePercentages`, `AddWorkLine`, `RemoveWorkLine`
  - `EstimateWorkLine`: copied WorkName, WorkUnit, LabourRate, LabourNorm, plus Quantity and MaterialLines; `ChangeQuantity`
  - `EstimateMaterialLine`: copied MaterialName, MaterialUnit, Price, ConsumptionNorm, WastePercent
  - no navigation or FK to Work or Material (BR-010)
- [ ] T069 Add the pure static `EstimateCalculator` (doc comments) in server/SmartEstimatePlatform.Api/Services/EstimateCalculator.cs:
  - `MaterialQuantity(workQuantity, consumptionNorm, wastePercent)` = q × n × (1 + w/100), not rounded
  - `Calculate(Estimate)` → `EstimateCalculation`:
    - per work line: LabourCost = Round(q × labourNorm × labourRate)
    - per material line: Quantity and Cost = Round(quantity × price)
    - DirectCosts = Σ rounded costs
    - Overhead = Round(direct × o/100)
    - Profit = Round((direct + overhead) × p/100)
    - Vat = Round((direct + overhead + profit) × v/100)
    - Total = the sum of the four rounded parts

  It uses only `MoneyRounding` (BR-015–BR-017).

- [ ] T066 Configure the estimate tables in server/SmartEstimatePlatform.Api/Data/AppDbContext.cs:
  - precision: money DECIMAL(18,2), quantities and norms DECIMAL(18,3), percentages DECIMAL(5,2)
  - Name max `EstimateRules.NameMaxLength`; WorkName/MaterialName max 200; units max 16
  - query filters: `EstimateWorkLine` through `Estimate.CompanyId`, `EstimateMaterialLine` through `WorkLine.Estimate.CompanyId`
  - cascades: WorkLine → Estimate, MaterialLine → WorkLine
- [ ] T185 Give `Estimate` a required `ProjectId` with a cascade foreign key to `Project` (deleting a project deletes its estimates, BR-011) in server/SmartEstimatePlatform.Api/Data/AppDbContext.cs
- [ ] T067 Add the EF Core migration `AddEstimate` in server/SmartEstimatePlatform.Api/Migrations. Check the three tables, precisions and cascades against T066/T185, that there's no FK to Works/Materials, and that EF logs no warning 10622; then confirm `has-pending-model-changes` reports no changes
- [ ] T189 Add `EstimateRules` (doc comments) in server/SmartEstimatePlatform.Api/Services/EstimateRules.cs: `NameMaxLength = 200`, `ValidateQuantity(quantity)` via `NumberRules.PositiveQuantityError(…, "Quantity")`, `ValidatePercentages(o, p, v)` via `NumberRules.PercentageError` for "Overhead percentage", "Profit percentage" and "VAT percentage"
- [ ] T190 Add sealed records with init properties (doc comments) in server/SmartEstimatePlatform.Api/Models/DTOs/Estimates/:
  - `CreateEstimateRequest` (`[Required]`, `[StringLength(EstimateRules.NameMaxLength)]` Name)
  - `AddWorkLineRequest` (WorkId, Quantity; `Validate` → ValidateQuantity)
  - `ChangeQuantityRequest`
  - `ChangePercentagesRequest` (`Validate` → ValidatePercentages)
  - `EstimateResponse`, `EstimateWorkLineResponse`, `EstimateMaterialLineResponse`, `EstimateSummaryResponse`
  - add `ToResponse(this Estimate, EstimateCalculation)` and `ToSummary(this Estimate, EstimateCalculation)` to DtoMapper
- [ ] T191 Add request-validation tests in tests/server/RequestValidationTests/EstimateRequestValidationTests.cs:
  - quantity 0 and −1 rejected, 0.001 accepted, 1.2345 rejected
  - each percentage −0.01 and 100.01 rejected, 0 and 100 accepted, 12.345 rejected (one case per percentage)
  - an empty name rejected

## Phase 3: User Story 1: Add a work and generate materials and costs (P1) 🎯 MVP

**Goal**: A Manager creates an estimate in a project, adds a work with a quantity, and sees the generated material lines and totals.
**Independent Test**: T081, T082 and T193 pass, and quickstart.md steps 1–3 and 8 work.

- [ ] T081 [US1] Add unit tests in tests/server/EstimateCalculatorTests/EstimateCalculatorTests.cs:
  - `MaterialQuantity` (35 × 2 × 1.10 = 77; 35 × 25 × 1.05 = 918.75)
  - a work without materials gives only its labour cost
  - the same work added twice gives two independent lines
- [ ] T192 [P] [US1] Add `UnknownWorkException` and `EstimateNotDraftException` (doc comments) in server/SmartEstimatePlatform.Api/Services, mapped in Controllers/DomainExceptionHandler.cs to 400 "The work is not in your catalog." and 409 "The estimate is confirmed and cannot be changed."
- [ ] T070 [US1] Add `IEstimateService` (doc comments) and `EstimateService` in server/SmartEstimatePlatform.Api/Services/, registered in `AddApplicationServices()`, with:
  - `ListForProjectAsync(projectId)`: summaries A–Z by name, 404 if the project is missing
  - `CreateAsync(projectId, request)`: name trimmed; 404 if the project is missing
  - `GetAsync(id)`
  - `AddWorkLineAsync(id, request)`: loads the work with its composition from the current company's catalog, else `UnknownWorkException`; copies the work and every material line's name, unit, price, norm and waste (BR-010)

  Every write goes through one private `FindDraftAsync(id)` that throws `NotFoundException("Estimate not found.")` or `EstimateNotDraftException`. Responses are built from `EstimateCalculator` + DtoMapper.
- [ ] T075 [US1] Add server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs (`ApiControllerBase`, Manager only, class-level 401/403) with GET/POST `/api/projects/{projectId}/estimates`, GET `/api/estimates/{id}` and POST `/api/estimates/{id}/lines`, as in contracts/estimates-api.md; one service call per action; success types declared
- [ ] T193 [US1] Add integration tests in tests/server/EstimateServiceTests/EstimateEndpointsTests.cs:
  - create returns Draft and 0/0/21 %
  - adding the worked-example work (35) returns three material lines with quantities 77, 918.75 and 36.75; with 10/15/21 % the total is 1263.26
  - quantity 0 gives 400 and adds nothing (covers the former T071: the request's validation reaches the API before the service runs)
  - an unknown or other-company work gives 400
  - a missing estimate or project gives 404
- [ ] T082 [US1] Add an integration test in tests/server/EstimateServiceTests/EstimateCopyIsolationTests.cs: after adding a work, changing the catalog work's labour rate and a material's price, and then deleting the work and the material, leaves the estimate's lines and total unchanged
- [ ] T194 [P] [US1] Add `Estimate`, `EstimateWorkLine`, `EstimateMaterialLine`, `EstimateSummary`, `EstimateStatus` and the request types in client/src/interfaces/estimate.ts, and `getProjectEstimates`, `createEstimate`, `getEstimate`, `addWorkLine`, `changeQuantity`, `removeWorkLine`, `changePercentages` in client/src/api/estimates.ts
- [ ] T078 [US1] Add the Estimate Editor in client/src/pages/Estimates, Tailwind via `utils/ui.ts`, one component per file:
  - `EstimateCreatePage` (name form → creates and opens `/estimates/:id`)
  - `EstimateEditorPage` (name, "Juodraštis", back link to the project)
  - `AddWorkForm` (work select from /api/works + quantity, mirroring `EstimateRules`)
  - `EstimateWorkLineRow` (work name, unit, quantity, labour norm × rate, labour cost, then the material lines: name, unit, quantity to 3 decimals, price, cost)
  - `EstimateTotals` (direct, overhead, profit, VAT, total)

  The client never calculates; it formats with `formatMoney` / `formatQuantity`.
- [ ] T186 [US1] On Project Detail, list the project's estimates for a Manager (`ProjectEstimatesTable`: name, "Juodraštis", total with VAT, "Atidaryti"), keeping "Sąmatų dar nėra." when there are none, plus a "+ Nauja sąmata" link. A Viewer's Project Detail shows no estimates section. In client/src/pages/Projects/ProjectDetailPage.tsx and ProjectEstimatesTable.tsx
- [ ] T195 [US1] Add the routes `/projects/:projectId/estimates/new` and `/estimates/:id` under `ManagerRoute` in client/src/App.tsx

**Checkpoint**: US1 is done when its tests pass and quickstart steps 1–3 and 8 work.

## Phase 4: User Story 2: Recalculate on every change (P1)

**Goal**: Changing a quantity or a percentage updates every amount at once.
**Independent Test**: T084, T085 and T086 pass, and quickstart steps 4–5 work.

- [ ] T072 [US2] Add `ChangeQuantityAsync(id, lineId, request)` and `ChangePercentagesAsync(id, request)` to IEstimateService/EstimateService (both through `FindDraftAsync`; a line of another estimate → `NotFoundException("Line not found.")`), returning the recalculated estimate, in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T076 [US2] Add PATCH `/api/estimates/{id}/lines/{lineId}` and PATCH `/api/estimates/{id}/percentages` in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T084 [US2] Add the unit test `WorkedExample_TotalIs1263_26` in tests/server/EstimateCalculatorTests/EstimateCalculatorTests.cs: every row of the spec's Worked Example table (labour 420.00; 346.50; 18.38; 40.43, not 40.42; direct 825.31; overhead 82.53; profit 136.18; VAT 219.24; total 1263.26)
- [ ] T085 [US2] Add a unit test in tests/server/EstimateCalculatorTests/EstimateCalculatorTests.cs: quantities and norms stay unrounded (918.75 and 36.75 keep their decimals, and a 3-decimal norm × waste gives a quantity with more than 3 decimals that isn't rounded); only the money amounts are rounded
- [ ] T086 [US2] Add integration tests in tests/server/EstimateServiceTests/EstimateEndpointsTests.cs:
  - changing the quantity 35 → 40 returns recalculated amounts
  - changing the percentages returns a recalculated total
  - overhead, profit and VAT each at −1 and 101 → 400, one case per percentage, and nothing changes (covers the former T073)
- [ ] T079 [US2] In the editor, save a changed quantity (in `EstimateWorkLineRow`) or percentage (new `EstimatePercentagesForm`) on blur/Enter, replace the shown estimate with the server's answer, and show a Lithuanian message on 400 while keeping the typed value, in client/src/pages/Estimates

## Phase 5: User Story 3: Remove a work from a Draft estimate (P1)

**Goal**: A Manager removes a work line together with its material lines, after confirmation.
**Independent Test**: T087 and T196 pass, and quickstart step 7 works.

- [ ] T074 [US3] Add `RemoveWorkLineAsync(id, lineId)` (through `FindDraftAsync`; its material lines are removed by the cascade) returning the recalculated estimate, in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T077 [US3] Add DELETE `/api/estimates/{id}/lines/{lineId}` in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T087 [US3] Add an integration test in tests/server/EstimateServiceTests/EstimateEndpointsTests.cs: removing a work line removes it and its material lines and updates the totals; the other line stays; a missing line gives 404
- [ ] T080 [US3] Add a "Pašalinti" button with `ConfirmDialog` to each work line in client/src/pages/Estimates/EstimateWorkLineRow.tsx (NFR-011)
- [ ] T196 [US3] Add a frontend test in tests/client/EstimateEditorTests/RemoveWorkLine.test.tsx: removing asks for confirmation; cancelling sends no DELETE; confirming sends one DELETE and shows the returned totals

## Phase 6: Cross-cutting (security, status, cascade)

- [ ] T197 Add an integration test in tests/server/EstimateServiceTests/EstimateDraftGuardTests.cs: with the estimate's status set to Confirmed directly in the database, adding a work, changing a quantity or percentages, and removing a line each give 409, and nothing changes
- [ ] T198 Add an integration test in tests/server/EstimateServiceTests/EstimateAuthorizationTests.cs: a Viewer gets 403 on every estimate endpoint; an anonymous request gets 401
- [ ] T088 Add integration tests in tests/server/CompanyIsolationTests/EstimateIsolationTests.cs:
  - another company's estimate, line and project give 404 on every estimate endpoint, and its estimates never appear
  - another company's estimate lines are invisible to a scoped DbContext
- [ ] T187 Add an integration test (formerly 006 T063) in tests/server/ProjectServiceTests/ProjectDeleteCascadeTests.cs: deleting a project with estimates (and lines) deletes them all; another project's estimates are untouched
- [ ] T199 Run the full check set, plus quickstart.md (against the real app if the local database is configured, otherwise the automated reference example plus a mock-API visual check of the editor); fix any regression

## Dependencies

- **Setup**: T068 → T083. T188 is independent.
- **Foundational**: T065 → T069 (the calculator takes the entity; needs T068) → T066 → T185 → T067. T081, T084 and T085 need T069. T189 needs T188. T190 needs T189. T191 needs T190.
- **US1**: T192 is independent. T070 needs T067 and T190 → T075 → T193, T082. Client: T194 → T078 → T186 → T195.
- **US2**: T072 needs T070 → T076 → T086. T079 needs T078.
- **US3**: T074 needs T070 → T077 → T087. T080 needs T078 → T196.
- **Cross-cutting**: T197, T198, T088 and T187 after T077. T199 runs last.

## Parallel Execution Examples

- Setup: T188 (NumberRules) in parallel with T068/T083 (rounding).
- US1: T192 (exceptions) and T194 (client types/API) in parallel with T070.
- After T077: T197, T198, T088 and T187 are separate test files and can be written in parallel.

## Implementation Strategy

1. Build rounding first, then the pure calculator right after the entities, so the formulas are proven before the database tables and the API exist.
2. MVP = US1 (add a work, see the totals); then US2 and US3, which are small additions to the same service and editor.
3. Spec/plan/tasks commits stay separate from code commits (Article VI); Conventional Commits, e.g. `feat(estimates): …`.
