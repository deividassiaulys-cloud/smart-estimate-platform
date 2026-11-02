# Data Model: Work Catalog Management

## Entities

| Entity | Fields | Constraints |
|---|---|---|
| **Work** (`ICompanyScoped`) | Id, Name, Unit, LabourRate, LabourNorm, CompanyId, Company, Lines | Name: trimmed, max 200, collation `utf8mb4_0900_as_ci`; unique index (CompanyId, Name, Unit). Unit: one of `MeasurementUnit.All`, max 16. LabourRate: DECIMAL(18,2), ≥ 0, at most 2 decimals. LabourNorm: DECIMAL(18,3), ≥ 0, at most 3 decimals |
| **WorkMaterialLine** | Id, WorkId, MaterialId, Material, ConsumptionNorm, WastePercent | FK Work → cascade; FK Material → restrict. ConsumptionNorm: DECIMAL(18,3), > 0, at most 3 decimals. WastePercent: DECIMAL(5,2), 0–100 inclusive, at most 2 decimals. No uniqueness on (WorkId, MaterialId) (BR-009) |

Construction (Article X.6, private setters):
- `Work(name, unit, labourRate, labourNorm)`, plus `Update(name, unit, labourRate, labourNorm)` and `ReplaceLines(IEnumerable<WorkMaterialLine>)`.
- `WorkMaterialLine(materialId, consumptionNorm, wastePercent)`.
- Each has a private parameterless constructor for EF Core.

## DTOs (`Models/DTOs/Works/`, sealed records, init properties)

| Record | Fields | Validation (X.10) |
|---|---|---|
| WorkRequest | Name, Unit, LabourRate, LabourNorm, Lines | `[Required]` Name/Unit/Lines, `[StringLength(WorkRules.NameMaxLength)]` Name; `Validate` → `WorkRules.Validate(unit, labourRate, labourNorm)` |
| WorkMaterialLineRequest | MaterialId, ConsumptionNorm, WastePercent | `Validate` → `WorkRules.ValidateLine(consumptionNorm, wastePercent)` |
| WorkResponse | Id, Name, Unit, LabourRate, LabourNorm, Lines | — |
| WorkMaterialLineResponse | Id, MaterialId, MaterialName, MaterialUnit, ConsumptionNorm, WastePercent | — |

## Errors

| Exception | Status | Detail |
|---|---|---|
| DuplicateWorkException | 409 | "A work with this name and unit already exists." |
| UnknownMaterialException | 400 | "Each line must use a material from your catalog." |
| MaterialInUseException | 409 | "The material is used in works and cannot be deleted." + `works: [names]` |
