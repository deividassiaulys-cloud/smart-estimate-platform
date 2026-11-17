# Research: Resource Kinds and Estimate Columns

## R1. The kind as a fixed enum
- **Decision**: a `MaterialKind` enum in Models/Entities: `Material = 0`, `Machinery = 1`. It is sent as a number, like `EstimateStatus`, and the client mirrors it in interfaces/material.ts with the labels "Medžiaga" / "Mechanizmas". It is stored as an int column on `Material` and on `EstimateMaterialLine`, with default 0. `MaterialRules.Validate` refuses undefined values ("Kind must be Material or Machinery.").
- **Rationale**: a fixed system list, like the measurement units (FR-017-1, BR-008 style). A number with default 0 gives existing rows "Medžiaga" without a data script (FR-017-3).
- **Alternatives**: a string column, rejected because it is unlike `EstimateStatus` and needs a length. A Labour kind was rejected because labour is always the work's own (spec Edge Cases).

## R2. Where the split is calculated
- **Decision**: in `EstimateCalculator` only (Article II.4).
  - `WorkLineCalculation` gains `Materials`, `Machinery`, `Amount` and `UnitPrice`, plus `Hours` (quantity × labour norm, not rounded).
  - `EstimateCalculation` gains `Labour`, `Materials` and `Machinery`.
  - Direct costs = Σ work-line `Amount`, which is the same number as today's Σ(labour cost + material costs).
  - `UnitPrice = MoneyRounding.Round(Amount / Quantity)` (spec Q3).
  - The parts are sums of already-rounded costs, so no extra rounding is needed (FR-017-4).
- **Rationale**: one place for every amount; the export and the editor only read the response.

## R3. A changed kind in the refresh preview
- **Decision**: add `RefreshedField.Kind`. `EstimateCatalogRefresh.Apply` compares the line's kind with the catalog material's kind and applies it with `EstimateMaterialLine.ChangeKind`. In the change record, `OldValue`/`NewValue` hold the `MaterialKind` number (0 or 1), documented on the response. The client shows the label for `Field = Kind`.
- **Rationale**: reuses the existing preview, dialog and "nothing to update" logic (FR-017-2) with one new enum value; no second change list.
- **Alternatives**: a separate `KindChanges` list, rejected because it duplicates the preview path. Making `OldValue` a string was rejected because it would break the numeric formatting of the other fields.

## R4. Replacing a material
- **Decision**: `EstimateMaterialLine.ReplaceMaterial(name, unit, price, kind)`; `EstimateService.ReplaceMaterialAsync` passes the catalog material's kind (FR-017-2, US3-1).

## R5. Layout: work row, labour row, material rows (spec Q1, Q2)
- **Decision**: the same 11 columns on screen and in the export: Nr., Pavadinimas, Mato vnt., Norma, Atliekos %, Kaina, Kiekis, Suma, Darbas, Medžiagos, Mechanizmai.
  - Work row: No., name, unit, unit price in "Kaina", quantity, amount, and the three parts.
  - Labour row: "Darbas", "val.", labour norm, labour rate (edited inline, FR-023), hours, labour cost in "Suma" and "Darbas".
  - Material rows: name, unit, norm, waste %, price, quantity, cost in "Suma" and in the kind's column.

  On screen the editor table gets the remove and replace action cells after the eleven columns. Like the existing editor tables (utils/ui.ts, docs/ux.md §8), it stays a table and scrolls inside its own frame on narrow screens; the page itself never scrolls sideways (NFR-008). The work quantity stays editable on the work row. (Corrected during implementation: an earlier draft said the rows would stack, which is the lists' card layout, not the editors'.)
- **Rationale**: FR-027 (export = screen). Hours are shown with `formatQuantity` / `#,##0.###`.

## R6. Totals block
- **Decision**: the editor's totals and the export start with three rows, "Darbas", "Medžiagos" and "Mechanizmai", followed by "Tiesioginės išlaidos" and the existing overhead, profit, VAT and total rows. In the export, the direct-costs row also carries the three totals in their columns (spec US4-5).

## R7. Viewer
- **Decision**: no change to `EstimateNamesResponse` or the Viewer endpoints. The existing role tests (NFR-005) cover them, and one assertion is added: the names response contains no `kind`, `labour`, `materials`, `machinery` or `unitPrice` property.
