# Feature 16: Higher Price and Norm Precision

**Feature Branch**: `016-price-norm-precision`
**Created**: 2026-11-17
**Status**: Draft
**Input**: Gap A in docs/reference/ANALYSIS.md §3. Material price and labour rate are stored with up to 4 decimal places, consumption norm and labour norm with up to 6, in the catalog and in estimate lines. Validation accepts up to 4 and 6 decimals and rejects more. The UI shows norms with up to 3 decimals (changed to up to 6 by Clarification Q1) and prices with up to 4. Money amounts are still rounded only to 2 decimals (BR-017). Existing estimates keep their values. Acceptance test: the sample estimate (docs/reference/sample-estimates/sample-estimate.json), created as catalog works with waste 0 and added to one estimate with overhead 1.5 %, profit 6 %, VAT 21 %, totals 135,414.29 EUR ± 0.50 EUR.

## Specification
Real construction estimates use sub-cent unit prices (a screw costs 0,0616 €, a staple 0,001 €) and small norms (0,000884 m³ of timber per m² of wall). Today a price can have only 2 decimal places and a norm only 3. The Manager must work around this: priced packs ("Kabė, pak. 1000 vnt.") instead of single items, and rounded norms. Both make the estimate differ from the source price list. On the 81-line sample estimate the difference is about 50 €. This feature lets the Manager enter prices with up to 4 decimal places and norms with up to 6, so an estimate built from the real price list matches it to within a few cents. The way amounts are calculated and rounded does not change.

## User Scenarios & Testing

### User Story 1: Enter precise prices and norms in the catalog (Priority: P1)
As a Manager, I want to enter a material price or labour rate with up to 4 decimal places, and a consumption norm or labour norm with up to 6, so that my catalog holds the real values from the supplier price list and the norm tables.

**Why this priority**: every estimate copies its values from the catalog (BR-010). Without precise catalog values nothing else in this feature has any effect.

**Independent Test**: create a material at 0,0616 € and a work with labour norm 0,042002 h and a material line with consumption norm 0,000884. Open them again: the values are exactly as entered, and saving the form again without changes keeps them.

**Acceptance Scenarios**:
1. Given the material form, when the Manager saves "Sraigtai terasoms" (vnt.) at 0,0616 €, then the material is saved and the catalog list shows 0,0616 €.
2. Given the work form, when the Manager saves labour rate 25,00 €/h, labour norm 0,042002 h/m² and a material line with consumption norm 0,000884, then the work is saved with exactly these values.
3. Given a price with 5 decimal places (0,06164) or a norm with 7 (0,0000884), when the Manager saves, then the form is refused with a message naming the field and the allowed number of decimal places, and nothing is saved.
4. Given a saved material at 0,0616 €, when the Manager opens its form and saves it without changes, then the price stays 0,0616 € (it is not rounded to 0,06).
5. Given a price with fewer decimals (4,5), then it is saved and shown as 4,50 €: prices always show at least 2 decimal places.

### User Story 2: Estimates calculate with the precise values (Priority: P1)
As a Manager, I want estimate lines to copy and use the precise prices and norms, so that the estimate total matches the source price list to the cent.

**Why this priority**: this is the purpose of the feature (G-2, correctness). Together with US1 it is the minimum useful feature.

**Independent Test**: add a work with precise values to an estimate and check the line amounts and the total against a hand calculation.

**Acceptance Scenarios**:
1. Given a work "Tašelis 28x45 mm" (m²) with labour 25,00 €/h × 0,042002 h and the material "Medsraigčiai" (vnt.) at 0,1000 € with norm 5, when the Manager adds 121,7 m², then the labour cost is 121,7 × 0,042002 × 25,00 = 127,79 € and the material cost is 608,5 × 0,1000 = 60,85 €. Each amount is rounded to 2 decimal places as before (BR-017).
2. Given a material line at 0,0616 € and a calculated quantity of 1 234,5 vnt., then its cost is 76,05 € (1 234,5 × 0,0616 = 76,0452, rounded).
3. Given the sample estimate built as described in "Acceptance test" below, then its total is 135 414,29 € ± 0,50 €.
4. Given a Draft estimate, when the Manager changes a material line's price to 0,0616 € or a work line's labour rate to 25,1234 €/h (FR-023), then the value is accepted and the amounts are recalculated. A fifth decimal is refused.
5. Given a catalog material at 0,0616 €, when an estimate using it at 0,06 € is refreshed from the catalog (FR-024), then the preview shows 0,06 → 0,0616 € and the update stores 0,0616 €.

### User Story 3: Existing data keeps its values (Priority: P1)
As a Manager, I want my existing catalog and estimates to stay exactly as they are when this change goes live, so that no confirmed price changes.

**Why this priority**: BR-010 and G-5. A confirmed estimate handed to a client must not change.

**Independent Test**: before the upgrade, record every material price, work rate and norm and every estimate's totals. After the upgrade, compare: all values and totals are identical.

**Acceptance Scenarios**:
1. Given existing materials, works, and Draft and Confirmed estimates, when the upgrade is applied, then every stored price, rate, norm, quantity and percentage is unchanged and every estimate total is the same to the cent.
2. Given an existing estimate, then the Viewer's names view and the Excel export show the same data as before, apart from the display rules of FR-016-6.

### Acceptance test (sample estimate)
The 81 lines of docs/reference/sample-estimates/sample-estimate.json are turned into catalog works as follows. Each line becomes one work with its unit. Its labour resources become the work's labour: the rate of its first labour resource, and a labour norm equal to the line's total labour cost per unit divided by that rate, rounded to 6 decimal places. Its material and machinery resources become material lines with their norm and price, waste 0 %. A resource with a per-line price override becomes its own catalog material at that price. Prices are rounded to 4 decimal places and norms to 6. Lines with quantity 0 (alternative options) are left out, because an estimate quantity must be greater than 0 (BR-007). All works are added to one estimate with their quantities, overhead 1,5 %, profit 6 % and VAT 21 %. The estimate's total must be 135 414,29 € ± 0,50 € (a desk check of this procedure gives 135 414,35 €).

### Edge Cases
- Prices and norms with fewer decimals than allowed are valid; trailing zeros are not stored as a separate value (0,10 and 0,1000 are the same price).
- Estimate quantities keep up to 3 decimal places; they are not part of this change.
- Waste % and the estimate percentages keep up to 2 decimal places.
- A value is never rounded silently on input: a value with too many decimal places is refused, never cut or rounded (as today).
- The largest accepted price and labour rate are lower than today because more digits go to the decimals: up to 99 999 999 999 999,9999. The largest norm is 999 999 999 999,999999. Larger values are refused as "too large", as today.
- A material line quantity (work quantity × norm × (1 + waste %)) can now have up to 11 decimal places; it is still not rounded in the calculation and is shown with up to 3 decimal places, as today.
- A small norm such as 0,000884 is shown in full, never as 0 (FR-016-6); calculated material quantities are still shown with up to 3 decimal places.
- The Excel export writes prices and norms as numbers with their full stored value; only their display format follows FR-016-6.
- A Viewer still sees no prices or norms (BR-005); nothing new is shown to them.

## Requirements

### Functional Requirements
- **FR-016-1**: A material price and a labour rate, in the catalog (FR-008, FR-009) and in estimate lines (FR-014, FR-023), are stored with up to 4 decimal places without loss.
- **FR-016-2**: A consumption norm and a labour norm, in the catalog (FR-009) and in estimate lines (FR-014), are stored with up to 6 decimal places without loss.
- **FR-016-3**: Validation accepts a price or labour rate with up to 4 decimal places and a norm with up to 6, and refuses more with a message naming the field and the limit (NFR-010). The other rules are unchanged (BR-007: not negative; consumption norm greater than 0). This applies to every place these values are entered: the material form, the work form and its material lines, and the estimate editor's price and labour-rate edits.
- **FR-016-4**: Estimate quantities, waste % and the estimate percentages keep their current precision and validation.
- **FR-016-5**: Calculation is unchanged (BR-015–BR-017). Line costs, overhead, profit and VAT are rounded to 2 decimal places with the one existing rounding rule. Prices, rates, norms and quantities are never rounded in the calculation.
- **FR-016-6**: Prices and labour rates are shown with at least 2 and up to 4 decimal places (e.g. 4,50 €; 0,0616 €), in the catalog lists, the forms, the estimate editor and the Excel export. Norms (consumption norm, labour norm) are shown with up to 6 decimal places, trailing zeros dropped (0,042; 0,000884; 5), in the catalog, the forms, the estimate editor and the Excel export (spec Q1). Money amounts (line costs, totals) are still shown with 2 decimal places (NFR-006).
- **FR-016-7**: Editing a form never changes a stored value the user did not change: a price or norm is put into the form with all its stored decimals.
- **FR-016-8**: Existing materials, works and estimates keep every stored value, and every estimate keeps its totals, when this change is applied.
- **FR-016-9**: The sample estimate built as in "Acceptance test" totals 135 414,29 € ± 0,50 € (automated test).

### Key Entities
- **Material** (existing): price now has up to 4 decimal places.
- **Work** (existing): labour rate up to 4 decimal places, labour norm up to 6.
- **Work material line** (existing): consumption norm up to 6 decimal places.
- **Estimate work line / estimate material line** (existing): their copies of the labour rate, price and norms have the same precision as the catalog. No new entity.

## Success Criteria

### Measurable Outcomes
- **SC-016-1**: The sample estimate (81 lines, 24 sections) built from catalog works totals within 0,50 € of the source document, compared with about 50 € today (automated test).
- **SC-016-2**: 0 "pack" materials and 0 dropped norms are needed to load the sample price list (204 resources, 102 rate templates). Only rounding prices to 4 and norms to 6 decimal places is needed.
- **SC-016-3**: 100% of existing estimates have the same totals to the cent before and after the change (automated test on the worked example in specs/007-estimate-creation-and-calculation/spec.md: 1263,26 €).
- **SC-016-4**: 100% of values with too many decimal places are refused on every input: catalog price, labour rate, labour norm, consumption norm, estimate price and labour rate (automated tests).
- **SC-016-5**: Saving any form without changes leaves 100% of stored values unchanged (automated test). All existing tests keep passing.

## Assumptions
- docs/requirements.md (NFR-006, BR-017) and specs/shared-technical-rules.md are updated with the Step 1 proposal before this feature is planned. That proposal sets prices to 4 decimals, norms to 6 and quantities to 3, and the display rules. Until then this spec refines those rules.
- 4 decimal places are enough for prices: the sample price list has 9 prices with more than 2 decimals, and rounding them to 4 changes the sample total by less than 0,30 €.
- Merging several labour resources of one line into one labour rate and norm (3 sample lines) is acceptable for the acceptance test. More than one labour resource per work stays out of scope (ANALYSIS.md §3).
- New measurement units (gap F), material kinds (gap B) and the work-line unit price (gap C) are separate features.
- The acceptance test reads the sample data from docs/reference/sample-estimates/ as test input; the files are not changed.

## Clarifications

### Session 2026-11-17
- Q: When a norm such as 0,000884 is stored with up to 6 decimal places, how many decimal places should the screen and the Excel export show for norms? → A: Up to 6 decimal places everywhere, trailing zeros dropped (this replaces "up to 3" in the feature description; BR-017 wording follows).
