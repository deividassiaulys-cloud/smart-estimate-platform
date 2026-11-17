# Research: Higher Price and Norm Precision

## R1. Column types
- **Decision**: material price and labour rate DECIMAL(18,4); consumption norm and labour norm DECIMAL(18,6). This applies to `Material`, `Work`, `WorkMaterialLine`, `EstimateWorkLine` and `EstimateMaterialLine`. Quantities stay DECIMAL(18,3), and waste % and the estimate percentages stay DECIMAL(5,2).
- **Rationale**: the sample price list needs sub-cent prices and norms below 0,0005 (spec). Desk checks on the sample estimate:
  - 4 / 6 decimals: 135 414,34 €
  - 6 / 6 decimals: 135 414,62 €
  - today's 2 / 3 decimals: 135 385,31 €
  - PDF: 135 414,29 €

  4 decimals for prices is enough; 6 would add nothing measurable.
- **Alternatives**: DECIMAL(18,6) for prices, rejected by the spec. A free-precision column was rejected because NFR-006 asks for fixed, reproducible values.

## R2. Migration keeps existing values
- **Decision**: one EF Core migration that only widens the scale of the nine columns. MySQL `ALTER … DECIMAL(18,4)` / `(18,6)` pads existing values with zeros (4,50 → 4,5000), so no value changes (FR-016-8).
- **Risk**: the largest storable price falls from 10¹⁶ to 10¹⁴ €. If an existing value were larger, the ALTER would fail. Such a value is unrealistic (no real price is anywhere near 10¹⁴ €), and the migration would stop rather than change data.
- **Note**: `MaxUnitPrice` (18 significant digits) cannot be represented exactly as a browser number. This is the same as today's `MaxMoney`, and the server remains the authoritative check.
- **Alternatives**: DECIMAL(20,4) / (21,6) would keep the old maximum. Rejected: the spec states (18,4) and (18,6), and the edge cases document the new maximum.

## R3. One validation rule per value kind (Constitution X.10)
- **Decision**: `NumberRules` gets three rules:
  - `UnitPriceError`: ≥ 0, ≤ 4 decimals, ≤ `MaxUnitPrice`. It replaces `MoneyError`, which today is used only for unit prices (catalog price, labour rate, estimate price and labour-rate edits).
  - `NormError`: ≥ 0, ≤ 6 decimals, ≤ `MaxNorm`. Used for the labour norm. `WorkRules` adds the existing "> 0" check for the consumption norm.
  - `PositiveQuantityError` (3 decimals, `MaxQuantity`) stays for estimate quantities only.
- **Rationale**: the quantity and the consumption norm share `PositiveQuantityError` today, so raising it to 6 would change quantities too (FR-016-4). `MoneyError` is renamed so its name says what it checks; no money *amount* is ever validated.
- **Alternatives**: a `places` parameter on the existing rules. Rejected: each call site would repeat the limit, so the rule would no longer be defined once.

## R4. Display and form values on the client
- **Decision**: `utils/format.ts` gets:
  - `formatPrice`: € with 2–4 decimals.
  - `formatNorm`: up to 6 decimals, trailing zeros dropped.
  - `toInputNumber(value, minDecimals)`: pads to at least `minDecimals` but never cuts digits, so 4,5 → "4,50" and 0,0616 → "0,0616".

  `utils/validation.ts` gets the shared limits `priceDecimals = 4` and `normDecimals = 6`, used by `decimalPattern` and the messages. It mirrors the server, which stays the authoritative check.
- **Rationale**: today the forms put `toFixed(2)` into the price field, so saving an unchanged form would round 0,0616 to 0,06 (FR-016-7). `formatMoney` stays for amounts (NFR-006).
- **Alternatives**: a separate input component, rejected as YAGNI. Locale-dependent `Intl` input formatting was rejected because it adds thousands separators, which the parser does not accept.

## R5. Excel export formats
- **Decision**: price and labour-rate cells use `#,##0.00## "€"`, norm cells `#,##0.######`, amounts still `#,##0.00 "€"`, quantities still `#,##0.###`. Cells hold the full stored value (as today).

## R6. Acceptance test
- **Decision**: an xUnit integration test (WebApplicationFactory, SQLite as in the other tests) reads `docs/reference/sample-estimates/resources.json` and `sample-estimate.json` (the repository root is found by walking up from the test's base directory). It builds the catalog through the real API as in the spec's "Acceptance test" and asserts the total is 135 414,29 ± 0,50.
  - Labour resources are merged into one rate and norm.
  - Each (resource, price) pair becomes one material.
  - Prices are rounded to 4 decimals and norms to 6.
  - Lines with quantity 0 are left out.
  - Units not in BR-008 (pak., tub., reisas, km, d.d., rul., ind., žm.val.) are mapped to "vnt." with the original unit kept in the name. m2/m3/vnt/ltr map to m²/m³/vnt./l. The resource code is put in front of material names and the line number in front of work names, so BR-006 never clashes.
- **Rationale**: it exercises validation, copying and calculation end to end. The unit mapping does not affect money; gap F stays a separate feature.
- **Storage precision**: SQLite does not enforce DECIMAL scale, so a model test asserts the configured precision and scale of the nine properties. This guards the migration.
