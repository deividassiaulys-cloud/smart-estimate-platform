# Research: Estimate Price Refresh

## R1. One code path for the preview and the update
- **Decision**: `EstimateService` loads the Draft estimate and the company's catalog works (with their composition) and materials, then calls a pure static `EstimateCatalogRefresh.Apply(estimate, works, materials)`. The catalog is passed as small input records (`CatalogWork` with its `CatalogComponent`s, and `CatalogMaterial`) built by the service, because a catalog work's composition refers to its materials by navigation, which can't be set up without a database. It updates the estimate's lines in memory and returns the list of changes and of lines not found. The preview calculates the result with `EstimateCalculator` and does **not** save. The update does the same and saves.
- **Rationale**: the preview's resulting total is the update's total by construction (SC-015-2), and the matching rules exist once (II.4, X.1). The DbContext is per request, so unsaved changes are discarded when the preview request ends. The function is pure and static like `EstimateCalculator`, so it can be unit-tested without a database.
- **Alternatives**: a separate "what would change" calculation next to the apply step (two copies of the matching rules); storing a preview and confirming it by ID (a new stored entity, which the spec excludes).

## R2. Matching by name and unit
- **Decision**: names are compared case-insensitively (`StringComparison.OrdinalIgnoreCase`), and units are compared exactly.
  - **Work line**: matched to the catalog work with the same name and unit. If one is found, its labour rate and labour norm are taken.
  - **Material line**: its price comes from the catalog material with the same name and unit, whether or not its work was found (FR-015-3 matches each part on its own). Its consumption norm and waste % come from the found work's composition: the first composition line with that material name and unit that no earlier material line of this work line has used, so duplicates are matched in order (spec Assumptions).
  - **Not found**: a line with no catalog match is reported with its work name (and material name).
- **Rationale**: the catalog's unique keys are (company, name, unit), and in MySQL the names use the case-insensitive `utf8mb4_0900_as_ci` collation, so case-insensitive comparison finds the same item the catalog would. All of this runs in memory, because a catalog is small.
- **Alternatives**: adding catalog IDs to the estimate lines (a migration, and it changes BR-010's "no reference" design); exact case-sensitive matching (a renamed-case item would be "not found", which doesn't match the catalog's uniqueness rule).

## R3. Preview response
- **Decision**: `GET /api/estimates/{id}/refresh` returns `EstimateRefreshPreviewResponse` with:
  - `Changes`: a list of `{ WorkName, MaterialName?, Field, OldValue, NewValue }`, where `Field` is the enum `RefreshedField` (LabourRate, LabourNorm, Price, ConsumptionNorm, WastePercent), serialized as a number like `EstimateStatus`
  - `NotFound`: a list of `{ WorkName, MaterialName? }`
  - `CurrentTotal` and `NewTotal`

  `POST /api/estimates/{id}/refresh` performs the update and returns `EstimateResponse`.
- **Rationale**: GET is safe (it changes nothing). POST changes the estimate, like the status actions. Each record goes in its own file in `Models/DTOs/Estimates/` (X.4), with the mapping in `DtoMapper` (X.8). A Confirmed estimate answers 409 in both cases, through the existing `FindDraftAsync`.

## R4. The confirmation dialog shows a list
- **Decision**: add an optional `children` prop to the shared `ConfirmDialog`, rendered under the message in a scrollable area (`max-h-[50vh] overflow-y-auto`). A new `EstimateRefreshButton` component asks for the preview when clicked, then:
  - with no changes, shows the status message "Sąmatos kainos ir normos jau sutampa su katalogu." next to the button, and no dialog opens
  - with changes, opens the dialog with the list and the total, confirm tone "primary", label "Atnaujinti"

  Confirming runs the editor's `apply(() => refreshEstimate(id))`, so a 409 is handled as for every other change.
- **Keyboard access**: when `children` are given, the list area gets `tabIndex={0}` and joins the dialog's Tab cycle (Cancel → list → confirm), so a keyboard user can scroll it; it is also referenced by `aria-describedby`.
- **Rationale**: one dialog for every confirmation (spec Q3, NFR-011). The list scrolls inside the dialog on a phone. A dialog that offers nothing to confirm would need yet another prop, so the "nothing to update" case is a message instead (the spec was adjusted during planning).
- **Alternatives**: a separate dialog component (duplicates the focus and Escape handling).

## R5. Lithuanian labels
- `RefreshedField` labels on the client:
  - LabourRate: "Darbo įkainis"
  - LabourNorm: "Darbo norma"
  - Price: "Kaina"
  - ConsumptionNorm: "Sunaudojimo norma"
  - WastePercent: "Nuostoliai"
- Values are formatted with the existing helpers: `formatMoney` for rates and prices, `formatQuantity` for norms, `formatPercent` for waste.
- A list line reads, for example, "Siena A++ — Darbo įkainis: 15,00 € → 16,00 €" or "Siena A++ / Gipso plokštė — Kaina: 4,50 € → 5,00 €".
