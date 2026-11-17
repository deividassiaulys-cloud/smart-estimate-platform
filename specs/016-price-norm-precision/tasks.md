# Tasks: Higher Price and Norm Precision

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/precision-api.md, quickstart.md in `specs/016-price-norm-precision/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T263.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. This feature has one migration (T268), which the task explicitly requires (Constitution VIII).

## Phase 1: Setup: requirements prerequisite

- [X] T263 Write the approved Step 1 requirements change. **Blocked until the student approves it** (plan.md Prerequisite, Constitution VII.3).
  - docs/requirements.md:
    - NFR-006: amounts are shown with 2 decimals; unit prices (material price, labour rate) with 2–4.
    - BR-017: unit prices are stored with up to 4 decimals, norms with up to 6 and quantities with up to 3. A value with more decimals is rejected, not rounded. Quantities are shown with 3 decimals, norms with up to 6 (spec Q1) and unit prices with up to 4.
  - specs/shared-technical-rules.md, "Money and quantities": prices DECIMAL(18,4), norms DECIMAL(18,6), quantities DECIMAL(18,3), percentages DECIMAL(5,2).
  - docs/review/change-log.md: a dated entry.
  - Only the parts for gap A. FR-030–FR-032, BR-008, BR-010 and BR-015 go with feature 017 or gap F.

## Phase 2: Foundational: shared rules (blocks every story)

- [X] T264 In server/SmartEstimatePlatform.Api/Services/NumberRules.cs (research R3, data-model.md):
  - Replace `MaxMoney` and `MoneyError` with `MaxUnitPrice = 99_999_999_999_999.9999m` and `UnitPriceError(value, label)`: "cannot be negative" / "can have at most 4 decimal places" / "is too large".
  - Add `MaxNorm = 999_999_999_999.999999m` and `NormError(value, label)`: "cannot be negative" / "can have at most 6 decimal places" / "is too large".
  - Keep `PositiveQuantityError` (3 decimals, `MaxQuantity`), now documented as quantities only.
  - Update the doc comments.
- [X] T265 Use the rules:
  - server/SmartEstimatePlatform.Api/Services/MaterialRules.cs: `UnitPriceError` for the price.
  - WorkRules.cs:
    - `UnitPriceError` for the labour rate.
    - `NormError` for the labour norm, replacing the inline checks.
    - For the consumption norm: "must be greater than 0" first, then `NormError`.
  - Models/DTOs/Estimates/ChangePriceRequest.cs and ChangeLabourRateRequest.cs: `UnitPriceError`.
  - Update the doc comments that say "2 decimal places" or "3 decimal places" for these values (Material.cs, the request records, WorkRules).
  - The quantity in EstimateRules stays on `PositiveQuantityError`.
- [X] T266 [P] Unit tests in tests/server/RequestValidationTests/ (RequestValidationTests.cs, WorkRequestValidationTests.cs, EstimateRequestValidationTests.cs):
  - price and labour rate: 0,0616 and 25,1234 accepted; 0,06164 refused; −0,01 refused; `MaxUnitPrice` accepted; one step above it refused
  - labour norm: 0,042002 accepted; 0,0000884 refused; 0 accepted
  - consumption norm: 0,000884 accepted; 0 refused; 7 decimals refused
  - quantity: 3 decimals accepted; 4 refused (unchanged)
  - change-price and change-labour-rate requests: 4 decimals accepted; 5 refused
  - Update the existing tests that expect "2 decimal places" for these values.

## Phase 3: User Story 1: Enter precise prices and norms in the catalog (P1) 🎯 MVP

**Goal**: the catalog stores and shows 0,0616 € and 0,000884 exactly; saving unchanged never rounds.
**Independent Test**: spec US1.

- [X] T267 [US1] In server/SmartEstimatePlatform.Api/Data/AppDbContext.cs (data-model.md):
  - `Material.Price` HasPrecision(18, 4).
  - `Work.LabourRate` (18, 4); `Work.LabourNorm` (18, 6).
  - `WorkMaterialLine.ConsumptionNorm` (18, 6).
  - `EstimateWorkLine.LabourRate` (18, 4); `EstimateWorkLine.LabourNorm` (18, 6). `Quantity` stays (18, 3).
  - `EstimateMaterialLine.Price` (18, 4); `EstimateMaterialLine.ConsumptionNorm` (18, 6).
  - Leave the WastePercent and percentage columns at (5, 2).
- [X] T268 [US1] Generate the migration `WidenPriceAndNormPrecision` with `dotnet ef migrations add` in server/SmartEstimatePlatform.Api, using the dummy connection string command in quickstart.md "Migration commands". Check that it only alters those 8 columns' type, with no data or other schema change (research R2), and that `has-pending-model-changes` then reports none.
- [X] T269 [P] [US1] Add tests/server/CrossCuttingTests/NumberPrecisionModelTests.cs. From `AppDbContext.Model`, assert precision 18 and scale 4 for the 4 price/rate properties, scale 6 for the 4 norm properties, scale 3 for `EstimateWorkLine.Quantity`, and (5, 2) for the 5 percentage properties.
- [X] T270 [P] [US1] Integration test in tests/server/MaterialServiceTests/ and WorkServiceTests/ (existing ApiFactory). Create a material at 0,0616 and a work with labour rate 25,1234, labour norm 0,042002 and consumption norm 0,000884. GET returns exactly these values; PUT with the same body leaves them unchanged.
- [X] T271 [P] [US1] In client/src/utils/format.ts:
  - Add `formatPrice(value)`: € with `minimumFractionDigits: 2, maximumFractionDigits: 4` (lt-LT).
  - Add `formatNorm(value)`: `maximumFractionDigits: 6`.
  - Change `toInputNumber(value, minDecimals?)` to pad to at least `minDecimals` but never cut digits (4.5 → "4,50", 0.0616 → "0,0616"). Update its doc comment.
  - `formatQuantity` and `formatMoney` stay.
- [X] T272 [P] [US1] In client/src/utils/validation.ts, add exported `priceDecimals = 4`, `normDecimals = 6` and `quantityDecimals = 3`, each with a comment naming the server rule it mirrors.
- [X] T273 [US1] Catalog pages, using T271/T272:
  - client/src/pages/Materials/MaterialFormPage.tsx: `decimalPattern(priceDecimals)`; message "Kaina turi būti 0 arba didesnė, ne daugiau kaip 4 skaitmenys po kablelio."; `toInputNumber(material.price, 2)` keeps its call and now never cuts digits.
  - MaterialsTable.tsx: `formatPrice`.
  - client/src/pages/Works/WorkFormPage.tsx:
    - labour rate: `decimalPattern(priceDecimals)` and a 4-decimal message
    - labour norm and consumption norm: `decimalPattern(normDecimals)` and 6-decimal messages
  - WorksTable.tsx: `formatPrice` for the rate, `formatNorm` for the norm.
- [X] T274 [US1] Add client tests in tests/client/PrecisionTests/Precision.test.tsx (mock the API as the other client tests do):
  - the material form shows 0,0616 and submits 0.0616 when saved unchanged
  - 0,06164 shows the 4-decimal message and is not submitted
  - the work form accepts 0,000884 and refuses 0,0000884
  - the materials list shows "0,0616 €"; 4.5 shows "4,50 €"
  - the works list shows 0,042002

## Phase 4: User Story 2: Estimates calculate with the precise values (P1)

**Goal**: estimate lines copy, edit, refresh, show and export the precise values; amounts rounded to 2 as before.
**Independent Test**: spec US2.

- [X] T275 [P] [US2] In tests/server/EstimateCalculatorTests/EstimateCalculatorTests.cs, add:
  - 121,7 × 0,042002 × 25,00 → labour 127,79 €
  - 608,5 vnt. × 0,1000 → 60,85 €
  - 1 234,5 × 0,0616 → 76,05 € (spec US2-1, US2-2)

  The worked example still gives 1263,26 €.
- [X] T276 [P] [US2] Integration tests in tests/server/EstimateLineEditingTests/ and EstimateRefreshTests/:
  - Adding a work with 0,042002 / 0,000884 copies them exactly.
  - Inline price 0,0616 and labour rate 25,1234 are accepted; 5 decimals return 400 (US2-4).
  - Refresh: an estimate at 0,06 and a catalog at 0,0616 give preview 0,06 → 0,0616 and the update stores 0,0616 (US2-5).
- [X] T277 [US2] In server/SmartEstimatePlatform.Api/Services/EstimateExportService.cs:
  - Add `PriceFormat = "#,##0.00## \"€\""` for the work-line labour rate and the material price cells.
  - Add `NormFormat = "#,##0.######"` for the labour norm and consumption norm cells.
  - Amounts keep `MoneyFormat`; quantities keep `QuantityFormat` (contracts/precision-api.md).
- [X] T278 [P] [US2] In tests/server/EstimateExportTests/EstimateExportTests.cs, assert:
  - a price cell holds 0.0616 with format `#,##0.00## "€"`
  - a norm cell holds 0.000884 with format `#,##0.######`
  - amount cells keep 2-decimal formats
- [X] T279 [US2] Estimate editor, using T271/T272:
  - client/src/pages/Estimates/InlineNumberField.tsx: the `money` kind uses `decimalPattern(priceDecimals)` and the message "Kaina turi būti 0 arba didesnė, ne daugiau kaip 4 skaitmenys po kablelio."; `toInputNumber(value, 2)` stays and now never cuts digits.
  - EstimateWorkLineRow.tsx: `formatNorm` for the labour norm.
  - EstimateMaterialLineRow.tsx: `formatPrice` in the replace-material list. Material quantities keep `formatQuantity`.
  - EstimateRefreshButton.tsx: `formatPrice` for LabourRate/Price, `formatNorm` for LabourNorm/ConsumptionNorm.
- [X] T280 [US2] Extend tests/client/EstimateEditorTests/EstimateLineEditing.test.tsx and EstimateRefresh.test.tsx:
  - the inline price field shows 0,0616 and accepts it; 0,06164 shows the 4-decimal message
  - the refresh dialog shows "0,06 € → 0,0616 €" and a norm 0,000884
- [X] T281 [US2] Add tests/server/SampleEstimateTests/SampleEstimateAcceptanceTests.cs (research R6, spec "Acceptance test", FR-016-9):
  - Find the repository root by walking up from `AppContext.BaseDirectory` to the folder that contains `docs/reference/sample-estimates`. Read `resources.json` and `sample-estimate.json` (read-only).
  - Through the API, as one logged-in Manager:
    - one material per (resource code, price rounded to 4 decimals, half away from zero): name "<code> <name>", with " – <price> €" added when the same code has more than one rounded price (BR-006: name + unit must be unique; 16 sample codes have several prices), unit mapped m2→m², m3→m³, vnt→vnt., ltr→l, and any other unit not in `MeasurementUnit.All` → "vnt." with "(<unit>)" added to the name
    - one work per line with quantity > 0: name "<no>. <name>", unit mapped the same way
      - labour rate = the first labour resource's price (or 0)
      - labour norm = Σ(norm × price) of the labour resources ÷ that rate, rounded to 6 decimals (0 when there is no labour)
      - one material line per non-labour resource: norm rounded to 6 decimals, waste 0
  - Create one estimate with overhead 1,5 %, profit 6 %, VAT 21 %, add every work with its quantity, and assert the total is within 135 414,29 ± 0,50.
  - Also assert that no material needed a pack conversion and no norm was dropped (SC-016-2).

## Phase 5: User Story 3: Existing data keeps its values (P1)

**Goal**: the upgrade changes no stored value or total.
**Independent Test**: spec US3.

- [X] T282 [US3] Check the migration from T268 against existing data:
  - It must contain only `AlterColumn` type changes from `decimal(18,2)`/`(18,3)` to `(18,4)`/`(18,6)`, with no `Sql()`, default or nullability change.
  - Record in research.md R2 that the existing tests (worked example 1263,26 €, refresh, export) pass unchanged on the new model (SC-016-3).
  - The before/after comparison on a real MySQL database (quickstart.md step 2) is a required manual step before merging, done by the student. Note in tasks.md whether it has been run.

## Phase 6: Polish

- [ ] T283 docs/ux.md: where the catalog forms and the editor describe price and norm inputs, say "iki 4 skaitmenų po kablelio" for prices and "iki 6" for norms. Check the materials, works and estimate editor pages at 360 px with 0,0616 € and 0,000884: no horizontal page scroll (NFR-008).
- [X] T284 Run the full check set (`dotnet build`, `dotnet test`, `npm run lint`, `npm run build`, `npm run test`) and the quickstart.md steps that don't need the real database. Fix any regression.

**Status notes (2026-11-17)**:
- T282: the migration was reviewed (research R2). The real-MySQL before/after comparison (quickstart.md step 2) has **not** been run yet; the student must run it before merging.
- T283: the docs/ux.md limits are updated. The 360 px browser check has **not** been done in this session, so T283 stays open.
- T284: `dotnet build` (0 warnings), `dotnet test` (337 passed), `npm run lint`, `npm run build` and `npm run test` (87 passed) all pass. The manual quickstart steps 1–8 need the running app and were not run.

## Dependencies

- T263 → T264: the requirements change is written and committed before any code (Constitution VII.3).
- T264 → T265 → T266 (Phase 2 blocks everything).
- US1: T267 → T268. T269 and T270 need T267. T271 and T272 → T273 → T274.
- US2 needs Phase 2 and T267. T277 → T278. T271/T272 → T279 → T280. T281 needs T265 and T267.
- US3: T282 needs T268.
- T283 and T284 run last.

## Parallel Execution Examples

- After Phase 2, the server storage tasks (T267–T270) and the client helpers (T271, T272) can run in parallel.
- In US2, the calculator test (T275), the editing/refresh tests (T276) and the export test (T278) touch different files.

## Implementation Strategy

The shared rules come first, because every input path goes through them. US1 (catalog) is the MVP: precise values can be entered and kept. US2 makes estimates use and show them and proves the sample total. US3 checks the migration. Spec/plan/tasks commits stay separate from code commits (Article VI).
