# Research: Sections and Section Templates

## R1. Data model
- **Decision**: three new entities and one changed entity.
  - `CatalogSection` (`ICompanyScoped`): Name, Unit, Lines.
  - `CatalogSectionLine`: WorkId with FK Restrict, Coefficient DECIMAL(18,6), ordered by Id. No company column; it is filtered through its section, like `WorkMaterialLine`.
  - `EstimateSection`: EstimateId with FK Cascade, Name, Unit, Quantity DECIMAL(18,3), filtered through its estimate.
  - `EstimateWorkLine`: + nullable `SectionId` (FK Cascade to `EstimateSection`), + nullable `Coefficient` DECIMAL(18,6).
- **Rationale**: mirrors the existing Work → WorkMaterialLine → Material pattern (Restrict delete, filtered child lines) and the Estimate → EstimateWorkLine copy pattern (BR-010).
  - A nullable SectionId keeps every existing line valid without a data script (FR-019-11).
  - The coefficient is copied onto the work line because FR-019-10 must recalculate after the catalog section may have changed or been deleted.
  - A line added by hand has no coefficient and is never recalculated.
- **Cascade**: removing a section deletes its work lines (FR-019-5), and their material lines cascade already. MySQL allows the two cascade paths Estimate → WorkLine and Estimate → Section → WorkLine.
- **Alternatives**: a single "group" entity shared by catalog and estimate. Rejected: catalog data must never be referenced by estimates (BR-010).

## R2. Rounding a computed work quantity
- **Decision**: `EstimateCalculator.SectionWorkQuantity(sectionQuantity, coefficient)` = `decimal.Round(q × c, 3, MidpointRounding.AwayFromZero)`. If the result is 0, the add (or quantity change) is refused with `ZeroSectionQuantityException` → 400 naming the work.
- **Rationale**: the work-line quantity column is DECIMAL(18,3) and is validated to 3 decimal places (BR-017). The rounding lives in the one calculation class (Article II.4), not in `MoneyRounding` (which is for money only). BR-017 gets a sentence for this single quantity rounding (T315).

## R3. Validation (Constitution X.10)
- **Decision**: `CatalogSectionRules`:
  - name max 200;
  - unit in `MeasurementUnit`;
  - at least one line;
  - each coefficient > 0, then `NumberRules.NormError` (6 decimals, `MaxNorm`), since a coefficient is a norm of works per section unit.
  - The section quantity uses `EstimateRules.ValidateQuantity` (> 0, 3 decimals), the same rule as a work quantity.

  All of these are declared on the request records.

## R4. Calculation and response shape
- **Decision**:
  - `EstimateCalculation` gains `Sections` (each: the section, its subtotal) and `UnsectionedSubtotal`; a subtotal is Σ work-line labour + material costs.
  - `EstimateResponse` keeps the flat `workLines` (every line in order, each with `sectionId`, nullable) and adds `sections` [{id, name, unit, quantity, subtotal}] and `unsectionedSubtotal`.
  - The client and the export group lines by `sectionId`.
- **Rationale**: keeps the existing contract (`workLines`) that the refresh, export, tests and client already use; grouping is display only.
- **Alternatives**: nest work lines under sections in the response. Rejected because it breaks every existing consumer of `workLines`.

## R5. Viewer names
- **Decision**: `EstimateNamesResponse` gains `sections` [{name, works:[{workName, materialNames}]}]. The existing `works` holds only the lines without a section. The Viewer page lists sections first (heading per section), then the other works. The Viewer allow-list test gets `sections` added (FR-019-8).

## R6. Endpoints
- `GET/POST /api/catalog-sections`, `GET/PUT/DELETE /api/catalog-sections/{id}` (Manager), `?search=`.
- `POST /api/estimates/{id}/sections` {catalogSectionId, quantity} → EstimateResponse.
- `PATCH /api/estimates/{id}/sections/{sectionId}` {quantity} → EstimateResponse (FR-019-10).
- `DELETE /api/estimates/{id}/sections/{sectionId}` → EstimateResponse.
- `POST /api/estimates/{id}/lines` gains optional `sectionId`, which must be a section of that estimate (otherwise 404).
- Status and company rules are the same as the other estimate edits: 409 when Confirmed, 403 for a Viewer, 404 for another company.

## R7. Display
- Sections are shown only when the estimate has at least one (spec Q2), so an estimate without sections renders and exports byte-for-byte as today.
- **Editor:** per section, a heading row (name, inline section-quantity field with a ConfirmDialog warning before saving, unit, remove button), its work rows, and a subtotal row. Then "Be skyriaus" with its own subtotal row.
- **Export:** the same rows. Section headings are bold text; the subtotal row has its amount in the amount column.
- **Catalog:** a "Skyriai" page (list, search, delete with ConfirmDialog) and a form whose line editor uses a work select plus coefficient, mirroring the work form.
