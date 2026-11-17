# Data Model: Higher Price and Norm Precision

No new entities or relationships. Column scale changes only (research R1, R2).

| Entity | Property | Before | After | Validation (request records) |
|---|---|---|---|---|
| Material | Price | DECIMAL(18,2) | DECIMAL(18,4) | `NumberRules.UnitPriceError`: ≥ 0, ≤ 4 decimals, ≤ 99 999 999 999 999,9999 |
| Work | LabourRate | DECIMAL(18,2) | DECIMAL(18,4) | `UnitPriceError` |
| Work | LabourNorm | DECIMAL(18,3) | DECIMAL(18,6) | `NormError`: ≥ 0, ≤ 6 decimals, ≤ 999 999 999 999,999999 |
| WorkMaterialLine | ConsumptionNorm | DECIMAL(18,3) | DECIMAL(18,6) | > 0, then `NormError` |
| EstimateWorkLine | LabourRate | DECIMAL(18,2) | DECIMAL(18,4) | `UnitPriceError` (ChangeLabourRateRequest) |
| EstimateWorkLine | LabourNorm | DECIMAL(18,3) | DECIMAL(18,6) | copied from the catalog |
| EstimateMaterialLine | Price | DECIMAL(18,2) | DECIMAL(18,4) | `UnitPriceError` (ChangePriceRequest) |
| EstimateMaterialLine | ConsumptionNorm | DECIMAL(18,3) | DECIMAL(18,6) | copied from the catalog |

Unchanged:
- EstimateWorkLine.Quantity: DECIMAL(18,3), `PositiveQuantityError`.
- WastePercent and the estimate percentages: DECIMAL(5,2), `PercentageError`.

`NumberRules.MaxMoney` and `MoneyError` are removed; nothing validates a money *amount*.

Calculated values are not stored:
- Line costs and totals are rounded to 2 decimals by `MoneyRounding`.
- A material quantity (work quantity × norm × (1 + waste %)) can now have up to 11 decimals and is not rounded.
