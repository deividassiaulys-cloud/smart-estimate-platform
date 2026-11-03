# Data Model: Role-Based Estimate Viewing

**No schema change and no migration.** The Viewer's view reads the existing `Estimates`, `EstimateWorkLines` and `EstimateMaterialLines` (007).

## Response records (`Models/DTOs/Estimates/`, sealed, init properties)

| Record | Fields | Notes |
|---|---|---|
| EstimateNameResponse | Id, Name | One estimate in a project's list for any user; no status, no total |
| EstimateNamesResponse | Name, Works | An opened estimate for any user |
| WorkNamesResponse | WorkName, MaterialNames (`List<string>`) | One work line and its material names, in order; a repeated material appears each time |

**Invariant**: these records have no property of a numeric type and no `EstimateStatus` (checked by a reflection test). The `Id` in `EstimateNameResponse` is only an identifier to open the estimate.
