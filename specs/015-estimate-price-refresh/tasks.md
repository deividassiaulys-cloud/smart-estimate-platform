# Tasks: Estimate Price Refresh

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/estimate-refresh-api.md, quickstart.md in `specs/015-estimate-price-refresh/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T249.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. There's no migration in this feature.

## Phase 1: Foundational: the matching core (blocks both stories)

- [X] T249 Entity mutators:
  - Add `ChangeLabourNorm(decimal)` to server/SmartEstimatePlatform.Api/Models/Entities/EstimateWorkLine.cs.
  - Add `ChangeNorms(decimal consumptionNorm, decimal wastePercent)` to EstimateMaterialLine.cs.
  - Both use private setters and take values copied from the catalog, which are already valid (data-model.md).
- [X] T250 Add, in server/SmartEstimatePlatform.Api/Services/, one type per file:
  - `RefreshedField` enum: LabourRate, LabourNorm, Price, ConsumptionNorm, WastePercent
  - `RefreshChange` record: WorkName, MaterialName (nullable), Field, OldValue, NewValue
  - `RefreshMiss` record: WorkName, MaterialName (nullable)
  - `CatalogRefreshResult` record: `Changes`, `NotFound`

  Add the catalog input records `CatalogMaterial` (Name, Unit, Price), `CatalogComponent` (MaterialName, MaterialUnit, ConsumptionNorm, WastePercent) and `CatalogWork` (Name, Unit, LabourRate, LabourNorm, Components), which the service builds from the database, so the matching can be unit-tested without one. Add `EstimateCatalogRefresh.Apply(Estimate estimate, IReadOnlyList<CatalogWork> works, IReadOnlyList<CatalogMaterial> materials)`, which is pure and static and returns `CatalogRefreshResult`. Its rules (research R2):
  - names are compared case-insensitively (`OrdinalIgnoreCase`) and units exactly
  - a work line takes the matching work's labour rate and labour norm
  - each material line takes the matching material's price, whether or not its work was found
  - the norm and waste % come from the found work's composition: the first unused composition line with the same material name and unit, so duplicates are matched in order
  - a value equal to the catalog's is not a change
  - a work line without a matching work, or a material line without a matching material, is added to `NotFound` once and left unchanged
  - quantities, percentages and the set of lines are never touched
- [X] T251 Add unit tests in tests/server/EstimateRefreshTests/EstimateCatalogRefreshTests.cs, building entities in memory:
  - changed rate, norm, price, consumption norm and waste % are applied and reported with old and new values
  - equal values give no change
  - a deleted or renamed work or material is reported as not found and left unchanged, while a material of a missing work still takes its catalog price
  - names that differ only in case still match
  - a composition containing the same material twice is matched in order
  - a material that replaced another (FR-023) and isn't in the composition takes its price but keeps its norm and waste
  - quantities and the number of lines are unchanged (SC-015-3)

## Phase 2: User Story 1: See what a refresh would change (P1) 🎯 MVP

**Goal**: A Manager sees the changes, the lines not found and the total old → new; nothing is saved.
**Independent Test**: T254 passes, and quickstart.md steps 1–3 work.

- [X] T252 [US1] Add `PreviewRefreshAsync(int id)` to server/SmartEstimatePlatform.Api/Services/IEstimateService.cs and EstimateService.cs, with XML docs listing its exceptions. It works as follows:
  - goes through `FindDraftAsync`
  - loads the company's works with `Lines` and their `Material`, and all materials
  - records the current total with `EstimateCalculator`
  - calls `EstimateCatalogRefresh.Apply`
  - calculates the new total and maps the result, **without** `SaveChanges`

  Add one record per file in Models/DTOs/Estimates/: `EstimateRefreshPreviewResponse` (`Changes`, `NotFound`, `CurrentTotal`, `NewTotal`), `RefreshChangeResponse` and `RefreshMissResponse`. The mapping goes in Services/Mapping/DtoMapper.cs.
- [X] T253 [US1] Add `GET estimates/{id:int}/refresh` to server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs. It makes one service call and declares 200 `EstimateRefreshPreviewResponse` and 409.
- [X] T254 [US1] Add integration tests in tests/server/EstimateRefreshTests/EstimateRefreshEndpointTests.cs, on the worked example (spec 007, total 1263,26) after setting "Siena A++" to 16 €/h and "Gipso plokštė" to 5,00 € in the catalog:
  - the preview lists exactly LabourRate 15 → 16 and Price 4,50 → 5,00, with `currentTotal` 1263,26 and `newTotal` 1365,05
  - a renamed "Plėvelė" appears in `notFound`
  - a second `GET` of the estimate still shows the old values and total, so nothing was saved
  - an estimate that matches the catalog gives no changes and equal totals
  - a Confirmed estimate gives 409, a Viewer gets 403, and another company's estimate gives 404

## Phase 3: User Story 2: Update the estimate from the catalog (P1)

**Goal**: Confirming applies every listed change and recalculates the estimate.
**Independent Test**: T257 passes, and quickstart.md steps 4, 7 and 8 work.

- [X] T255 [US2] Add `RefreshAsync(int id)` to IEstimateService.cs and EstimateService.cs. It does the same as `PreviewRefreshAsync` up to `Apply`, then saves and returns `Respond(estimate)`. Share the loading code through one private method, so the two don't duplicate it (X.1).
- [X] T256 [US2] Add `POST estimates/{id:int}/refresh` to EstimatesController.cs. It makes one service call and declares 200 `EstimateResponse` and 409.
- [X] T257 [US2] Add integration tests to EstimateRefreshEndpointTests.cs:
  - after the same catalog changes, `POST` gives labour cost 448,00, board cost 385,00 and totals of 891,81 direct, 89,18 overhead, 147,15 profit, 236,91 VAT and 1365,05 total, equal to the preview's `newTotal` (SC-015-2)
  - quantities, percentages and the not-found line are unchanged (SC-015-3)
  - a catalog price changed between `GET` and `POST` is applied on `POST` (the update reads the current catalog)
  - a changed consumption norm and waste % in the work's composition are applied, and the quantity is recalculated
  - the catalog materials and works, and a second estimate with the same work, are unchanged
  - an estimate with nothing to update answers 200 unchanged
  - a Confirmed estimate gives 409, a Viewer gets 403, and another company's estimate gives 404
  - the Viewer names view and the Excel export show the refreshed values

## Phase 4: Client (US1 + US2)

- [X] T258 [US1] Add an optional `children?: ReactNode` prop to client/src/components/ConfirmDialog.tsx, rendered under the message in `max-h-[50vh] overflow-y-auto`. When `children` are given, the list area gets `tabIndex={0}`, joins the Tab cycle (Cancel → list → confirm) and is included in `aria-describedby` (research R4). The existing uses stay unchanged. Then:
  - add `RefreshedField` (a const object like `EstimateStatus`) and `EstimateRefreshPreview` to client/src/interfaces/estimate.ts
  - add `previewRefresh(id)` and `refreshEstimate(id)` to client/src/api/estimates.ts
- [X] T259 [US1] [US2] Add client/src/pages/Estimates/EstimateRefreshButton.tsx with an "Atnaujinti kainas iš katalogo" button. On click it loads the preview, then:
  - with no changes, it shows the status message "Sąmatos kainos ir normos jau sutampa su katalogu."
  - with changes, it opens `ConfirmDialog` (title "Atnaujinti kainas iš katalogo?", confirm label "Atnaujinti", tone "primary"). The dialog lists:
    - each change as "{work}[ / {material}] — {label}: {old} → {new}", with the labels and formats from research R5
    - "Kataloge nerasta, liks nepakeista: …" for each line not found
    - "Iš viso su PVM: {old} → {new}"
  - a failure to load shows a 409 as the editor's status message and anything else as `unexpectedError('įkelti kainų atnaujinimo')`

  Show the button in EstimateEditorPage.tsx for Draft estimates only, next to the export button. Confirming calls the editor's `apply(() => refreshEstimate(estimateId))`.
- [X] T260 [US1] [US2] Add frontend tests in tests/client/EstimateEditorTests/EstimateRefresh.test.tsx:
  - the dialog lists the changes, the not-found line and the totals in Lithuanian format
  - cancelling sends no `POST`
  - confirming sends `POST /api/estimates/9/refresh` and shows the returned total
  - an empty preview shows the "jau sutampa" message and no dialog
  - a Confirmed estimate has no refresh button
  - a 409 on the preview shows the status message, not the server text
  - Tab moves from Cancel to the list and then to Atnaujinti
  - the existing dialog tests (RemoveWorkLine, EstimateStatus, ExportEstimate) keep passing unchanged

## Phase 5: Polish

- [X] T261 Add the refresh action to docs/ux.md §14 (Estimate Editor). Run `dotnet ef migrations has-pending-model-changes` (dummy connection string) and confirm that no migration is needed. Check the refresh dialog at 360 px: no horizontal page scroll, and the list scrolls inside the dialog.
- [X] T262 Run the full check set and the quickstart.md steps that don't need the real database. Fix any regression.

## Dependencies

- T249 → T250 → T251 (Phase 1 blocks everything).
- US1: T252 → T253 → T254. US2: T255 needs T252 (the shared loading); T256 → T257.
- Client: T258 → T259 → T260 (the tests mock the API, so they can run before the server endpoints exist).
- T261 and T262 run last.

## Parallel Execution Examples

- After Phase 1, the server tasks (T252–T257) and the client tasks (T258–T260) can run in parallel.
- T251 (unit tests) can be written alongside T250.

## Implementation Strategy

The pure `EstimateCatalogRefresh` comes first, because both stories and both endpoints rely on its matching rules, and the unit tests pin those rules. Then the read-only preview (US1, the MVP: it already shows what is out of date), then the update (US2) on the same code path, then the client. Spec/plan/tasks commits stay separate from code commits (Article VI).
