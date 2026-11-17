# Tasks: Resource Kinds and Estimate Columns

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/estimate-columns-api.md, quickstart.md in `specs/017-resource-kinds-columns/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T285 (feature 016 ends at T284).

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. This feature has one migration (T288), which the task explicitly requires (Constitution VIII).

## Phase 1: Setup: requirements prerequisite

- [ ] T285 Write the approved requirements change for gaps B and C before any code (Constitution VII.3).
  - docs/requirements.md:
    - Add FR-030 (material kind), FR-031 (Labour / Materials / Machinery on screen and in the export) and FR-032 (work-line unit price), all Should have. Use the Step 1 wording, and add to FR-031 the labour row and the "Atliekos %" column (spec Q1, Q2).
    - FR-008: "(name, unit of measurement, kind (FR-030), price)".
    - BR-010: a material line also copies its kind.
    - BR-015: work-line Labour / Materials / Machinery / amount; direct costs = Σ amounts.
    - BR-017: "A work line's unit price = its amount ÷ its quantity, rounded to 2 decimal places; it is shown only and never used in another calculation."
    - NFR-006: work-line unit prices are shown with 2 decimals.
    - §6 Should Have: the two bullets.
    - §10: Material and Estimate Material Line gain a kind.
  - specs/shared-technical-rules.md: the "Material kind" bullet; the unit-price sentence under "Rounding"; the split under "Estimate calculation".
  - docs/review/change-log.md: a dated entry.
  - The Out of Scope list is not changed.

## Phase 2: Foundational: kind on entities and the split in the calculator (blocks every story)

- [ ] T286 Add server/SmartEstimatePlatform.Api/Models/Entities/MaterialKind.cs: `public enum MaterialKind { Material = 0, Machinery = 1 }`, with doc comments (data-model.md).
  - In Material.cs, add `Kind` (private setter) to the constructor and to `Update(name, unit, kind, price)`.
  - In EstimateMaterialLine.cs:
    - add `Kind`, copied in the constructor
    - extend `ReplaceMaterial(name, unit, price, kind)`
    - add `ChangeKind(MaterialKind kind)`

  Update every caller: MaterialService, EstimateService (`new EstimateMaterialLine(…, l.Material.Kind)` when adding a work line, and the kind on replace) and the test builders.
- [ ] T287 In server/SmartEstimatePlatform.Api/Data/AppDbContext.cs, set `Material.Kind` and `EstimateMaterialLine.Kind` to `HasDefaultValue(MaterialKind.Material)`, stored as int (EF default).
- [ ] T288 Generate the migration `AddMaterialKind` (quickstart.md "Migration commands"). It must only add the two int NOT NULL DEFAULT 0 columns, with no other change. `has-pending-model-changes` then reports none.
- [ ] T289 Calculation (research R2):
  - `Services/WorkLineCalculation.cs`: add `Hours`, `Materials`, `Machinery`, `Amount` and `UnitPrice`.
  - `Services/EstimateCalculation.cs`: add `Labour`, `Materials` and `Machinery`.
  - `EstimateCalculator.CalculateWorkLine`:
    - `Hours = Quantity × LabourNorm`, not rounded.
    - `Materials` / `Machinery` = Σ rounded material costs by `Line.Kind`.
    - `Amount = LabourCost + Materials + Machinery`.
    - `UnitPrice = MoneyRounding.Round(Amount / Quantity)`.
  - `Calculate`: the three totals are sums over the work lines, and `DirectCosts = Labour + Materials + Machinery`. Overhead, profit, VAT and Total are unchanged (BR-016).
  - Update the doc comments.
- [ ] T290 [P] Unit tests in tests/server/EstimateCalculatorTests/EstimateCalculatorTests.cs:
  - Worked example: Labour 420,00, Materials 405,31, Machinery 0,00, Amount 825,31, UnitPrice 23,58, Hours 28; total still 1263,26.
  - Plus "Įrankiai ir įrengimai" (Machinery, norm 1, waste 0, 0,20): Machinery 7,00, Amount 832,31, UnitPrice 23,78.
  - A work line with no materials: Materials and Machinery 0.
  - Labour norm 0: Labour 0.
  - Two work lines: the estimate's three totals add up to DirectCosts exactly (SC-017-1).

## Phase 3: User Story 1: Mark materials as machinery (P1) 🎯 MVP

**Goal**: a Manager sets and sees a material's kind; the default is Medžiaga.
**Independent Test**: spec US1.

- [ ] T291 [US1] Server:
  - `Models/DTOs/Materials/MaterialRequest.cs`: + `Kind` (default `MaterialKind.Material`).
  - `MaterialResponse.cs`: + `Kind`.
  - `Services/MaterialRules.Validate(unit, kind, price)`: refuse an undefined value with "Kind must be Material or Machinery." on `Kind` (X.10).
  - `MaterialService` passes the kind; `DtoMapper` maps it.
- [ ] T292 [P] [US1] Tests:
  - tests/server/RequestValidationTests: kind 0 and 1 accepted, 2 refused; omitted means 0.
  - tests/server/MaterialServiceTests:
    - create with Machinery → GET returns 1
    - create without a kind → 0
    - update Material → Machinery changes the kind and leaves an estimate already using it unchanged (US1-4)
- [ ] T293 [US1] Client:
  - `client/src/interfaces/material.ts`: `MaterialKind = { Material: 0, Machinery: 1 } as const`, `materialKindLabels` ("Medžiaga", "Mechanizmas"), and `kind` on the material types.
  - `pages/Materials/MaterialFormPage.tsx`: a "Rūšis" select (default Medžiaga) that is sent and loaded with the material.
  - `MaterialsTable.tsx`: a "Rūšis" column with a `data-label` for the stacked mobile layout.
- [ ] T294 [US1] Add client tests in tests/client/MaterialKindTests/MaterialKind.test.tsx:
  - a new material defaults to Medžiaga and is sent with kind 0
  - choosing Mechanizmas sends 1
  - editing loads the stored kind
  - the list shows the kind label

## Phase 4: User Story 2: Labour / Materials / Machinery and unit price per work line (P1)

**Goal**: the editor shows the parts, the labour row and the unit price; the total is unchanged.
**Independent Test**: spec US2.

- [ ] T295 [US2] Server responses:
  - `EstimateWorkLineResponse`: + `Hours`, `Materials`, `Machinery`, `Amount`, `UnitPrice`.
  - `EstimateMaterialLineResponse`: + `Kind`.
  - `EstimateResponse`: + `Labour`, `Materials`, `Machinery`.
  - Map them in `Services/Mapping/DtoMapper.cs` (mapping only there, X.4).
- [ ] T296 [P] [US2] Integration tests in tests/server/EstimateServiceTests/:
  - Adding the work with a Machinery material copies `kind = 1`.
  - The response has the values of T290.
  - Changing the quantity recalculates the parts and the unit price (US2-4).
  - An estimate created before the kind existed (kind 0) keeps its total (SC-017-2).
- [ ] T297 [P] [US2] In tests/server/CompanyIsolationTests (or the existing Viewer tests), assert that `GET /api/estimates/{id}/names` as a Viewer contains no `kind`, `labour`, `materials`, `machinery`, `amount`, `hours` or `unitPrice` property (FR-017-9, research R7).
- [ ] T298 [US2] Client types: `client/src/interfaces/estimate.ts` gets the new fields on `EstimateMaterialLine` (`kind`), `EstimateWorkLine` (`hours`, `materials`, `machinery`, `amount`, `unitPrice`) and `Estimate` (`labour`, `materials`, `machinery`).
- [ ] T299 [US2] Editor layout (research R5, spec Q1, Q2):
  - `pages/Estimates/EstimateEditorPage.tsx`: the headings Nr., Pavadinimas, Mato vnt., Norma, Atliekos %, Kaina, Kiekis, Suma, Darbas, Medžiagos, Mechanizmai, plus the existing action column.
  - `EstimateWorkLineRow.tsx`: work row with No. (index + 1), name, unit, unit price (`formatMoney`), the editable quantity, amount and the three parts. It renders the labour row and then the material rows.
  - New `EstimateLabourRow.tsx` (one component per file, X.7): "Darbas", "val.", labour norm, the inline labour-rate field (moved from the work row, FR-023), hours (`formatQuantity`), labour cost in Suma and Darbas.
  - `EstimateMaterialLineRow.tsx`: norm, waste % (`formatPercent`), price field, quantity, cost in Suma and in the Medžiagos or Mechanizmai cell by `kind`.
  - Every cell has a `data-label` so rows stack below 768 px without horizontal page scroll (NFR-008). Tailwind only.
- [ ] T300 [US2] In `pages/Estimates/EstimateTotals.tsx`, add rows "Darbas", "Medžiagos" and "Mechanizmai" before "Tiesioginės išlaidos" (research R6).
- [ ] T301 [US2] Add tests/client/EstimateEditorTests/EstimateColumns.test.tsx (API mocked with the T290 values):
  - the 11 headings
  - the work row shows "1", "23,78 €", "832,31 €", "420,00 €", "405,31 €", "7,00 €"
  - the labour row shows "Darbas", 0,8, 15,00 €, 28, 420,00 €
  - the machinery line's cost is in the Mechanizmai cell, not Medžiagos
  - the totals list Darbas / Medžiagos / Mechanizmai before Tiesioginės išlaidos
  - the labour rate is still editable inline in Draft and read-only in Confirmed

  Update the existing editor tests (EstimateLineEditing, RemoveWorkLine, EstimateStatus) to the new rows without weakening their checks.
- [ ] T302 [P] [US2] Confirm tests/client/ViewerTests/ViewerEstimateView.test.tsx passes unchanged. Add an assertion that no "Mechanizmai" or "Darbas" amounts appear.

## Phase 5: User Story 3: Kinds follow edits and refreshes (P2)

**Goal**: replace and refresh carry the catalog kind.
**Independent Test**: spec US3.

- [ ] T303 [US3] Server:
  - `EstimateService.ReplaceMaterialAsync` passes `material.Kind` to `ReplaceMaterial`.
  - `Services/CatalogMaterial.cs`: + `Kind`, filled in `EstimateService` (`new CatalogMaterial(m.Name, m.Unit, m.Price, m.Kind)`).
  - `Services/RefreshedField.cs`: + `Kind`.
  - `EstimateCatalogRefresh.Apply`: when a matched material's kind differs, call `ChangeKind` and report `RefreshChange(work, material, Kind, (decimal)old, (decimal)new)` (research R3).
  - `RefreshChangeResponse` doc comment: for Kind, the values are MaterialKind numbers.
- [ ] T304 [P] [US3] Tests:
  - tests/server/EstimateRefreshTests/EstimateCatalogRefreshTests.cs: a kind change is applied and reported; an equal kind gives no change.
  - EstimateRefreshEndpointTests: a preview with a kind change and an unchanged total; the update moves the cost to Machinery.
  - tests/server/EstimateLineEditingTests: replacing with a Machinery material sets kind 1 and moves the cost (US3-1).
  - A Confirmed estimate still refuses both with 409 (US3-3).
- [ ] T305 [US3] In `client/src/pages/Estimates/EstimateRefreshButton.tsx`, add `RefreshedField.Kind` to `client/src/interfaces/estimate.ts` and a "Rūšis" entry whose format is `materialKindLabels[value]`. Extend tests/client/EstimateEditorTests/EstimateRefresh.test.tsx: the dialog shows "Rūšis: Medžiaga → Mechanizmas".

## Phase 6: User Story 4: Export in the familiar layout (P2)

**Goal**: the Excel table equals the screen in the 11-column layout.
**Independent Test**: spec US4.

- [ ] T306 [US4] Rewrite `WriteLines` and `WriteTotals` in `server/SmartEstimatePlatform.Api/Services/EstimateExportService.cs` (contracts/estimate-columns-api.md):
  - `LastColumn = 11`, with the headings in that order.
  - Work row: No. (1…), name (bold), unit, unit price in Kaina, quantity, amount, Darbas / Medžiagos / Mechanizmai (0 written as 0).
  - Labour row: "Darbas", "val.", labour norm, labour rate, hours, labour cost in Suma and Darbas.
  - Material rows: name (indented), unit, norm, waste %, price, quantity, cost in Suma and in its kind's column.
  - Totals:
    - Darbas, Medžiagos, Mechanizmai
    - Tiesioginės išlaidos, which also carries the three totals in columns 9–11
    - overhead, profit and VAT with their percentages
    - Iš viso su PVM

    Amounts go in column 8 (Suma).
  - Number cells and text protection are unchanged (NFR-007). Kind labels come from `EstimateStatusNames`-style static names on the server (`MaterialKindNames`), defined once.
- [ ] T307 [P] [US4] Rewrite the layout assertions in tests/server/EstimateExportTests/EstimateExportTests.cs, using the US2-2 estimate:
  - the 11 headings
  - work row cells (1, "Siena A++", m², 23.78, 35, 832.31, 420, 405.31, 7)
  - labour row (Darbas, val., 0.8, 15, 28, 420 in columns 8 and 9)
  - the machinery row's cost is in column 11 and column 10 is empty
  - direct costs row with 832.31 and the three totals
  - total 1263.26 + the machinery effect, computed by `EstimateCalculator`
  - the formula-injection test still passes

## Phase 7: Polish

- [ ] T308 docs/ux.md §14 (Estimate Editor) and the materials page: describe the kind, the 11 columns, the labour row and the new totals rows. Check the editor and the materials list at 360 px and 1920 px: no horizontal page scroll, and the rows stack (NFR-008).
- [ ] T309 Run the full check set and the quickstart.md steps that don't need the real database. Fix any regression. Step 2 (real MySQL before/after) is a required manual step for the student before merging.

## Dependencies

- T285 → T286 (requirements before code, VII.3).
- Phase 2: T286 → T287 → T288; T286 → T289 → T290.
- US1: T291 → T292; T293 → T294. US2 needs Phase 2: T295 → T296, T297; T298 → T299 → T300 → T301, T302.
- US3 needs T286 and T295: T303 → T304; T298 → T305. US4 needs T295: T306 → T307.
- T308 and T309 run last.
- If feature 016 is merged first, rebase this branch on main before T286; its price/norm formats then apply in the new cells.

## Parallel Execution Examples

- After Phase 2, US1 (catalog) and the US2 server work (T295–T297) touch different files.
- T304 (refresh/replace tests) and T307 (export tests) can be written in parallel once T303 and T306 exist.

## Implementation Strategy

The kind and the calculator split come first, because every story reads them. US1 (catalog kind) is the MVP: kinds can be recorded. US2 shows the split and the unit price on screen. US3 keeps them right through edits. US4 brings the export into the same layout. Spec/plan/tasks commits stay separate from code commits (Article VI).
