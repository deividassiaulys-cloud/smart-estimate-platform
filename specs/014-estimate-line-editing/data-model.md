# Data Model: Estimate Line Editing

No new entity, column or migration. Existing entities gain behaviour only.

## EstimateWorkLine (existing)
| Field | Change |
|---|---|
| LabourRate | editable through `ChangeLabourRate(decimal)`; ≥ 0, at most 2 decimals, ≤ DECIMAL(18,2) max (`NumberRules.MoneyError`) |
| MaterialLines | `RemoveMaterialLine(EstimateMaterialLine)` removes one; zero lines is allowed |
| LabourNorm, Quantity, WorkName, WorkUnit | unchanged by this feature (quantity edit exists, FR-017) |

## EstimateMaterialLine (existing)
| Field | Change |
|---|---|
| Price | editable through `ChangePrice(decimal)`; same rule as above |
| MaterialName, MaterialUnit, Price | replaced together by `ReplaceMaterial(name, unit, price)`, copied from a catalog material of the same company **with the same unit** |
| ConsumptionNorm, WastePercent | never changed (spec Q2) |

## Rules
- All changes only while the estimate is Draft (BR-013) → otherwise `EstimateNotDraftException` (409).
- Lines are found through their estimate; another company's estimate or a line of another estimate → 404.
- Amounts are not stored; `EstimateCalculator` recalculates them (BR-015–BR-017).
- The catalog is never written (BR-010).
