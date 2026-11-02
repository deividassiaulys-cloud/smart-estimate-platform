# Data Model: Estimate Creation and Calculation

## Entities (private setters, private EF constructor, X.6)

| Entity | Fields | Constraints |
|---|---|---|
| **Estimate** (`ICompanyScoped`) | Id, ProjectId, Project, Name, Status, OverheadPercent, ProfitPercent, VatPercent, CompanyId, Company, WorkLines | Name trimmed, varchar(200). Status `EstimateStatus` (Draft on creation). Percentages DECIMAL(5,2), defaults 0 / 0 / 21. FK Project cascade (BR-011) |
| **EstimateWorkLine** | Id, EstimateId, Estimate, WorkName, WorkUnit, LabourRate, LabourNorm, Quantity, MaterialLines | Copies of the work at add-time (BR-010). LabourRate DECIMAL(18,2); LabourNorm and Quantity DECIMAL(18,3). FK Estimate cascade. Query filter through Estimate |
| **EstimateMaterialLine** | Id, WorkLineId, WorkLine, MaterialName, MaterialUnit, Price, ConsumptionNorm, WastePercent | Copies of the composition line and its material. Price DECIMAL(18,2), ConsumptionNorm DECIMAL(18,3), WastePercent DECIMAL(5,2). FK WorkLine cascade. Query filter through WorkLine.Estimate. No FK to Material; the quantity isn't stored (computed, Q4) |
| **EstimateStatus** (enum) | Draft, Confirmed | Only Draft is used in 007; 008 adds the transitions |

Constructors and mutators (no rules inside):
- `Estimate(projectId, name)` sets the defaults and Draft; `ChangePercentages(o, p, v)`; `AddWorkLine(EstimateWorkLine)`; `RemoveWorkLine(EstimateWorkLine)`.
- `EstimateWorkLine(workName, workUnit, labourRate, labourNorm, quantity, materialLines)`; `ChangeQuantity(q)`.
- `EstimateMaterialLine(materialName, materialUnit, price, consumptionNorm, wastePercent)`.

Unchanged from 005: `WorkMaterialLine` (own ID, restrict FK to Material) and `MeasurementUnit` (a fixed list in code). No relationships beyond what the spec needs.

## Calculation result (not stored)

`EstimateCalculation`: per work line `LabourCost` and, per material line, `Quantity` and `Cost`; `DirectCosts`, `Overhead`, `Profit`, `Vat`, `Total`. It's computed by `EstimateCalculator` and mapped into the response.

## DTOs (`Models/DTOs/Estimates/`, sealed records)

| Record | Fields | Validation (X.10) |
|---|---|---|
| CreateEstimateRequest | Name | `[Required]`, `[StringLength(EstimateRules.NameMaxLength)]` |
| AddWorkLineRequest | WorkId, Quantity | `Validate` → `EstimateRules.ValidateQuantity` |
| ChangeQuantityRequest | Quantity | `Validate` → `EstimateRules.ValidateQuantity` |
| ChangePercentagesRequest | OverheadPercent, ProfitPercent, VatPercent | `Validate` → `EstimateRules.ValidatePercentages` |
| EstimateResponse | Id, ProjectId, Name, Status, OverheadPercent, ProfitPercent, VatPercent, WorkLines, DirectCosts, Overhead, Profit, Vat, Total | — |
| EstimateWorkLineResponse | Id, WorkName, WorkUnit, Quantity, LabourNorm, LabourRate, LabourCost, MaterialLines | — |
| EstimateMaterialLineResponse | Id, MaterialName, MaterialUnit, ConsumptionNorm, WastePercent, Quantity, Price, Cost | — |
| EstimateSummaryResponse | Id, Name, Status, Total | — (Project Detail list) |

## Errors

| Exception | Status | Detail |
|---|---|---|
| UnknownWorkException | 400 | "The work is not in your catalog." |
| EstimateNotDraftException | 409 | "The estimate is confirmed and cannot be changed." |
| NotFoundException | 404 | "Project not found." / "Estimate not found." / "Line not found." |
