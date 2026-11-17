# Plan: Higher Price and Norm Precision

**Branch**: `016-price-norm-precision` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

## Summary

Gap A (ANALYSIS.md §3). Prices and labour rates are stored and validated with up to 4 decimals, and consumption and labour norms with up to 6, in the catalog and in estimate lines. Quantities, percentages and the calculation are unchanged. Norms are shown with up to 6 decimals (spec Q1) and prices with 2–4.

- **Server:**
  - Eight `HasPrecision` changes in `AppDbContext` and one migration that only widens columns.
  - `NumberRules.MoneyError` is replaced by `UnitPriceError` (4 decimals), and `NormError` (6 decimals) is added. `PositiveQuantityError` now covers quantities only.
  - The export's price and norm number formats change.
- **Client:**
  - `formatPrice` and `formatNorm` are added.
  - `toInputNumber` pads to at least N decimals and never cuts digits.
  - The shared `priceDecimals` and `normDecimals` limits are used in the forms and the inline editor.
- **Acceptance:** an integration test builds the sample estimate through the API and checks 135 414,29 ± 0,50 €.

Decisions are in [research.md](research.md), columns in [data-model.md](data-model.md), and API limits in [contracts/precision-api.md](contracts/precision-api.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged. No new dependencies.

**Storage**: MySQL, one migration widening 8 DECIMAL columns (research R2). Tests run on SQLite, so a model test checks the configured precision.

**Testing**:
- xUnit unit tests:
  - `NumberRules` limits: 4 and 6 decimals accepted, 5 and 7 refused, maximum values, quantity still 3.
  - Request validation of `MaterialRequest`, `WorkRequest`, `ChangePriceRequest` and `ChangeLabourRateRequest`.
  - `EstimateCalculator` with precise values (US2-1: 127,79 / 60,85 €; US2-2: 76,05 €).
  - The model precision test.
- xUnit + WebApplicationFactory:
  - The sample-estimate acceptance test (research R6).
  - A precise price and norm round-trip through create, get and add-to-estimate.
  - The refresh preview with a 4-decimal price (US2-5).
  - The worked example 1263,26 € (existing).
  - Export cell formats.
- Vitest + Testing Library:
  - The form keeps 0,0616 when saved unchanged.
  - Refusal messages for 5 and 7 decimals.
  - Lists and the editor show 0,0616 € and 0,000884.

**Project Type**: Web application

**Constraints**:
- Money is rounded only by `MoneyRounding` (BR-017).
- Existing values unchanged (FR-016-8).
- Quantities stay at 3 decimals (FR-016-4).

**Prerequisite**: the Step 1 change to docs/requirements.md (NFR-006, BR-017) and specs/shared-technical-rules.md, approved by the student before implementation. Today's BR-017 says norms are shown with 3 decimals, which conflicts with spec Q1 (Constitution VII.3).

## Constitution Check

| Article | Check | Result |
|---|---|---|
| I correctness / II.5 tests | Acceptance test on the 81-line sample, unit tests for every limit, unchanged worked example | Pass |
| II.1 / III business logic | Limits live in `NumberRules` (Application layer); React only mirrors them for quick feedback; calculation unchanged in `EstimateCalculator` | Pass |
| II.4 / X.1 DRY | One rule per value kind (`UnitPriceError`, `NormError`, `PositiveQuantityError`); client limits defined once in `validation.ts`; one `formatPrice` / `formatNorm` | Pass |
| III no features beyond spec | No new units, kinds or unit prices (gaps B, C, F); quantities and percentages untouched | Pass |
| IV security | No endpoint or permission change; existing role and isolation tests keep passing | Pass |
| VI / VIII | Spec, plan and tasks are committed separately from code. The migration is required by the task (Article VIII allows it). No .env or build output is touched. | Pass |
| VII ambiguity | BR-017 conflict resolved by spec Q1; requirements update is a listed prerequisite | Pass (pending approval) |
| X.2–X.10 | No controller change; validation stays declarative on the request records through the shared rules (X.10); DTOs unchanged; Tailwind only; no CancellationToken | Pass |

**Post-design re-check**: Pass. Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Data/AppDbContext.cs                         # 8 × HasPrecision (18,4) / (18,6)
├── Migrations/<timestamp>_WidenPriceAndNormPrecision.cs   # new, generated
├── Services/NumberRules.cs                      # UnitPriceError, NormError, MaxUnitPrice, MaxNorm; MoneyError/MaxMoney removed
├── Services/MaterialRules.cs, WorkRules.cs      # use the new rules
├── Models/DTOs/Estimates/ChangePriceRequest.cs, ChangeLabourRateRequest.cs   # use UnitPriceError
├── Models/Entities/Material.cs                  # doc comment: 4 decimal places
└── Services/EstimateExportService.cs            # PriceFormat, NormFormat
tests/server/
├── RequestValidationTests/*                     # updated limits
├── CrossCuttingTests/NumberPrecisionModelTests.cs        # new: configured precision and scale
├── EstimateCalculatorTests/EstimateCalculatorTests.cs    # + precise values
├── EstimateExportTests/EstimateExportTests.cs            # + formats
└── SampleEstimateTests/SampleEstimateAcceptanceTests.cs  # new (research R6)
client/src/
├── utils/format.ts                              # formatPrice, formatNorm, toInputNumber(minDecimals)
├── utils/validation.ts                          # priceDecimals, normDecimals
├── pages/Materials/MaterialFormPage.tsx, MaterialsTable.tsx
├── pages/Works/WorkFormPage.tsx, WorkLinesEditor.tsx, WorksTable.tsx
└── pages/Estimates/InlineNumberField.tsx, EstimateWorkLineRow.tsx, EstimateMaterialLineRow.tsx, EstimateRefreshButton.tsx
tests/client/PrecisionTests/Precision.test.tsx   # new
docs/requirements.md, specs/shared-technical-rules.md, docs/review/change-log.md   # prerequisite (Step 1)
```

## Implementation Order

1. **Prerequisite:** write the approved Step 1 requirements change.
2. **Server rules:** `NumberRules`, `MaterialRules`, `WorkRules`, request records, and their unit tests (US1, US2-4).
3. **Storage:** `AppDbContext` precision, the migration, and the model test (FR-016-1/2/8, US3).
4. **Calculation and export:** calculator tests with precise values, export formats and their tests, and the acceptance test (US2, FR-016-9).
5. **Client:** format and validation helpers, then the forms, lists, inline editor and refresh dialog, then the tests (FR-016-6/7).
6. **Full checks:** `dotnet test`, `npm run test`, `npm run build`, the quickstart manual check, and the 360 px check.

## Complexity Tracking

No violations.
