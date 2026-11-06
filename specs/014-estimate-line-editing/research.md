# Research: Estimate Line Editing

## R1. Where the endpoints go
- **Decision**: four actions under the existing estimate routes, each answering the whole recalculated `EstimateResponse` like the other edits:
  - `PATCH /api/estimates/{id}/lines/{lineId}/labour-rate`
  - `PATCH /api/estimates/{id}/lines/{lineId}/materials/{materialLineId}/price`
  - `PATCH /api/estimates/{id}/lines/{lineId}/materials/{materialLineId}/material`
  - `DELETE /api/estimates/{id}/lines/{lineId}/materials/{materialLineId}`
- **Rationale**: the material line is addressed through its work line, the same way the work line is addressed through its estimate, so the existing scoping (estimate → company) covers every level and a foreign ID is simply "not found". One action per change keeps each request record to one field and one rule (X.10).
- **Alternatives**: one `PATCH` on the line accepting any field (mixes rules and needs "which fields were sent" logic); a `PUT` of the whole estimate (the client would have to send back calculated data).

## R2. Validation: one price rule
- **Decision**: add `NumberRules.MoneyError(value, label)` (≥ 0, at most 2 decimals, within DECIMAL(18,2)), and use it in `MaterialRules` and `WorkRules` (same messages as today) and in the two new request records.
- **Rationale**: X.10 requires one shared rule definition; the price and labour rate rules are today written out twice. Messages are unchanged, so the existing tests pin the refactor.
- **Alternatives**: calling `MaterialRules.Validate` with a dummy unit (hides intent); a third copy of the rule (violates X.1).

## R3. Replacing a material
- **Decision**: the service loads the catalog material by ID (company-scoped by the query filter). Missing or foreign → `UnknownMaterialException` (400, existing). A different unit → new `MaterialUnitMismatchException` (400, "The replacement material must have the same unit."). The line copies the material's name, unit and current price; norm and waste stay (spec Q1).
- **Rationale**: reuses the existing "not in your catalog" path; the unit check is a business rule, so it lives in the service.
- **Alternatives**: 409 for a unit mismatch (it is invalid input, not a conflict with state); letting the client filter only (the server must enforce it, Article IV).

## R4. Entity changes
- **Decision**: behaviour methods only, no new columns: `EstimateWorkLine.ChangeLabourRate`, `EstimateWorkLine.RemoveMaterialLine`, `EstimateMaterialLine.ChangePrice`, `EstimateMaterialLine.ReplaceMaterial(name, unit, price)`. No migration (checked with `dotnet ef migrations has-pending-model-changes`).
- **Rationale**: private setters (X), every column already exists.

## R5. Editor UI
- **Decision**:
  - Extract the quantity field's save-on-blur/Enter logic into one shared `InlineNumberField` component (client/src/pages/Estimates), used for the quantity, the labour rate and each material price (spec Q3).
  - Move each material row into its own `EstimateMaterialLineRow` component with the price field, a "Pakeisti medžiagą" select of same-unit catalog materials (the change is saved when chosen), and "Pašalinti" with `ConfirmDialog` (NFR-011).
  - The editor loads the catalog materials once (`getMaterials()`), only for a Draft estimate.
- **Rationale**: one inline-edit implementation (X.1); one component per file (X.7). A select is the simplest way to offer only same-unit materials. An estimate line keeps no reference to the catalog (BR-010), so the line's current material is left out of the list by its name and unit, and the select is hidden when no other same-unit material exists.
- **Alternatives**: a modal per line (spec Q3 chose inline); a search box (the catalog list is short, YAGNI).

## R6. Viewer view and export
- **Decision**: no change. Both read the stored lines (`GetNamesAsync`, `IEstimateService.GetAsync`), so they reflect edits by construction; tests prove it.
