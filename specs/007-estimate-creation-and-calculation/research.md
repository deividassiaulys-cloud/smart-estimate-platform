# Research: Estimate Creation and Calculation

## R1: Calculation in one pure service

- **Decision**: `EstimateCalculator.Calculate(Estimate)` returns an `EstimateCalculation`:
  - per work line: labour cost, and per material line its quantity and cost
  - direct costs, overhead, profit, VAT, total

  It applies BR-015–BR-017 exactly:
  - labour cost = `MoneyRounding.Round(quantity × labourNorm × labourRate)`
  - material quantity = `quantity × consumptionNorm × (1 + waste/100)`, never rounded
  - material cost = `Round(materialQuantity × price)`
  - direct costs = Σ rounded labour and material costs
  - overhead = `Round(direct × o/100)`
  - profit = `Round((direct + overhead) × p/100)`
  - VAT = `Round((direct + overhead + profit) × v/100)`
  - total = direct + overhead + profit + VAT
- **Rationale**:
  - One place for every calculation (Article II.4).
  - It's pure, so unit tests can pin the worked example (1263.26 €) and the midpoint (40.425 → 40.43) without a database.
  - Being static with no state or I/O, it needs no interface (X.3).
  - The formulas are fixed and simple, so a rules engine would add complexity without benefit.
- **Alternatives considered**: all rejected.
  - Calculating in the mapper or in the frontend: business logic belongs in the Application layer, and docs/architecture.md says the frontend doesn't calculate.
  - `double` arithmetic: inexact for money (NFR-006).
  - A third-party pricing or rules library: unjustified for this scope.

## R2: MoneyRounding

- **Decision**: `MoneyRounding.Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero)`, the only money rounding in the codebase (AGENTS.md, shared rules).
- **Rationale**: BR-017 requires 0.005 to become 0.01, while .NET's default banker's rounding would give 0.00.
- **Tests**: 0.005 → 0.01, 0.004 → 0.00, 0.015 → 0.02, 40.425 → 40.43, −0.005 → −0.01.

## R3: Stored data (spec Q4)

- **Decision**: only inputs are stored. Nothing derived is stored, not even a material line's quantity, so a total can never disagree with its lines.

  | Table | Columns |
  |---|---|
  | `Estimates` | ProjectId, Name, Status, OverheadPercent, ProfitPercent, VatPercent, CompanyId |
  | `EstimateWorkLines` | EstimateId, WorkName, WorkUnit, LabourRate (18,2), LabourNorm (18,3), Quantity (18,3) |
  | `EstimateMaterialLines` | WorkLineId, MaterialName, MaterialUnit, Price (18,2), ConsumptionNorm (18,3), WastePercent (5,2) |
- **No foreign keys to the catalog**: lines hold copies (BR-010), so editing or deleting a work or material never affects an estimate.
- **Order**: lines are shown in the order they were added (by ID).

## R4: Company scoping of the lines

- **Decision**: `Estimate` implements `ICompanyScoped`. The lines get matching query filters through their navigations, the same pattern as `WorkMaterialLine` (PR #12):
  - `EstimateWorkLine`: `l => l.Estimate.CompanyId == CurrentCompanyId`
  - `EstimateMaterialLine`: `l => l.WorkLine.Estimate.CompanyId == CurrentCompanyId`

  This avoids EF warning 10622 without suppressing it.

## R5: Cascades (BR-011, moved T185)

- **Decision**:
  - Estimate → Project: required, cascade.
  - WorkLine → Estimate: cascade.
  - MaterialLine → WorkLine: cascade.

  Deleting a project deletes its estimates and all their lines in the database. `ProjectService.DeleteAsync` stays unchanged. Removing a work line deletes its material lines (US3).
- **Note**: Company → Estimate is also a cascade path, and MySQL allows multiple cascade paths.

## R6: Draft-only check

- **Decision**: `EstimateStatus { Draft, Confirmed }`. A new estimate is Draft (BR-012). Every write in `EstimateService` loads the estimate through one private method that throws `EstimateNotDraftException` (→ 409 "The estimate is confirmed and cannot be changed.") unless it's Draft.
- **Status in 007**: nothing changes the status (008 adds the transitions). The integration test sets Confirmed directly in the database to prove the check works.

## R7: Validation (X.10)

- **Request records**:
  - `CreateEstimateRequest`: `[Required]`, `[StringLength(EstimateRules.NameMaxLength)]` name
  - `AddWorkLineRequest`: WorkId and Quantity
  - `ChangeQuantityRequest`: Quantity
  - `ChangePercentagesRequest`: OverheadPercent, ProfitPercent, VatPercent
- **Shared checks** (DRY): `NumberRules.PositiveQuantityError(value, label)` (> 0, ≤ 3 decimals, ≤ MaxQuantity) and `NumberRules.PercentageError(value, label)` (0–100, ≤ 2 decimals). `WorkRules` switches to them for the consumption norm and waste. The messages stay the same, so 005's tests are unchanged.
- **Stored-data checks** in the service:
  - the work exists in the company catalog → `UnknownWorkException` (400 "The work is not in your catalog.")
  - the line belongs to the estimate → `NotFoundException("Line not found.")`

## R8: API

- **Shape**: estimates are created under their project. Every editor action returns the whole recalculated `EstimateResponse`, so the editor just replaces its state (one request per change, Q4). Details in [contracts/estimates-api.md](contracts/estimates-api.md), which replaces the older `contracts.md`.
- **Project Detail**: `GET /api/projects/{id}/estimates` returns `EstimateSummaryResponse` (Id, Name, Status, Total), Manager only. A Viewer's Project Detail doesn't request it and shows no estimates section until 009.

## R9: Client

- **Pages**:
  - `EstimateCreatePage`: a name form, which creates the estimate and opens `/estimates/:id`.
  - `EstimateEditorPage`: the heading (name, "Juodraštis"), `AddWorkForm` (work select + quantity), one `EstimateWorkLineRow` per work line (an editable quantity saved on blur/Enter, the labour line, the material lines, and a "Pašalinti" button with `ConfirmDialog`), `EstimatePercentagesForm` (saved on blur/Enter), and `EstimateTotals`.
- **Display**: the client never calculates; it formats with `formatMoney` / `formatQuantity` (quantities to 3 decimals, NFR-012).
- **Client validation** mirrors the server limits for immediate Lithuanian messages, using `decimalPattern`.
