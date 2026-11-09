# Data Model: Estimate Price Refresh

No new stored entity, column or migration.

## Existing entities: new mutators
| Entity | Mutator | Note |
|---|---|---|
| EstimateWorkLine | `ChangeLabourRate(decimal)` (exists, spec 014), `ChangeLabourNorm(decimal)` (new) | values copied from the catalog work, already valid |
| EstimateMaterialLine | `ChangePrice(decimal)` (exists), `ChangeNorms(decimal consumptionNorm, decimal wastePercent)` (new) | values copied from the catalog, already valid |

## Not stored
**EstimateCatalogRefresh result** (Services, record): `Changes` (list of `RefreshChange`: work name, material name or null, `RefreshedField`, old value, new value) and `NotFound` (list of `RefreshMiss`: work name, material name or null).

**RefreshedField** (Services, enum): LabourRate, LabourNorm, Price, ConsumptionNorm, WastePercent.

## Rules
- Draft only (BR-013), Manager only, company-scoped (catalog queries and the estimate both go through the company query filters).
- Matching (research R2): by name (case-insensitive) and unit. Material price is independent of the work. Norm and waste % come from the found work's composition, with duplicates matched in order.
- Quantities, percentages and the set of lines never change (FR-015-4, spec Q1).
- A value equal to the catalog's is not a change and is not listed.
