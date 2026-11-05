# Tasks: Estimate Line Editing

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/estimate-line-editing-api.md, quickstart.md in `specs/014-estimate-line-editing/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T233.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. There's no migration in this feature.

## Phase 1: Foundational (blocks every story)

- [ ] T233 Add `NumberRules.MoneyError(value, label)` in server/SmartEstimatePlatform.Api/Services/NumberRules.cs, which returns "{label} cannot be negative.", "{label} can have at most 2 decimal places." or "{label} is too large." (above `MaxMoney`), or null when the value is valid. Use it for the price in MaterialRules.cs and the labour rate in WorkRules.cs, keeping their messages and member names exactly as they are (research R2). The existing tests must pass unchanged

## Phase 2: User Story 1: Change a price in one estimate (P1) 🎯 MVP

**Goal**: A Manager changes a work line's labour rate or a material line's price in a Draft estimate; the totals update and the catalog is untouched.
**Independent Test**: T237 and T240 pass, and quickstart.md steps 1–3 and 6 work.

- [ ] T234 [US1] Add `ChangeLabourRate(decimal)` to server/SmartEstimatePlatform.Api/Models/Entities/EstimateWorkLine.cs and `ChangePrice(decimal)` to EstimateMaterialLine.cs (private setters, already validated values). Add one request record per file in Models/DTOs/Estimates/:
  - `ChangeLabourRateRequest { LabourRate }`
  - `ChangePriceRequest { Price }`

  Each is an `IValidatableObject` using `NumberRules.MoneyError` with member names `LabourRate` and `Price` (X.10)
- [ ] T235 [US1] Add `ChangeLabourRateAsync(id, lineId, request)` and `ChangeMaterialPriceAsync(id, lineId, materialLineId, request)` to server/SmartEstimatePlatform.Api/Services/IEstimateService.cs and EstimateService.cs, with XML docs listing their exceptions. Both go through `FindDraftAsync` and `FindLine`; add a private `FindMaterialLine(workLine, materialLineId)` that throws `NotFoundException("Line not found.")`. Save, then `Respond`
- [ ] T236 [US1] Add `PATCH estimates/{id:int}/lines/{lineId:int}/labour-rate` and `PATCH estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}/price` to server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs. Each makes one service call and declares 200 `EstimateResponse` and 409, like `ChangeQuantity`
- [ ] T237 [US1] Add server tests in tests/server/EstimateLineEditingTests/EstimateLineEditingTests.cs, on the worked example (spec 007; extend the existing `EstimateRequests` helper only if needed):
  - a labour rate of 18 gives labour cost 504,00 and correctly recalculated totals
  - a "Gipso plokštė" price of 4 gives cost 308,00 and correct totals
  - the catalog material and work keep their prices, and a second estimate with the same work is unchanged
  - -1, 4.001 and a too-large value are refused with 400 and nothing changes
  - saving the same price again answers 200 with the same totals
  - a Confirmed estimate gives 409
  - a Viewer gives 403
  - another company's estimate gives 404, a line of another estimate gives 404, and a material line of another work line gives 404
- [ ] T238 [US1] Add `changeLabourRate(id, lineId, labourRate)` and `changeMaterialPrice(id, lineId, materialLineId, price)` to client/src/api/estimates.ts. Add client/src/pages/Estimates/InlineNumberField.tsx, extracted from the quantity field in EstimateWorkLineRow.tsx, with these props: the value, decimals, a validity check, a Lithuanian error text, an aria-label, readOnly, and `onSave(number) => Promise<void>`. It saves on blur or Enter, does nothing when the value is unchanged, and shows the error under the field (`ui.fieldError`). Use it for the quantity (same texts and behaviour as today) and for the labour rate in EstimateWorkLineRow.tsx
- [ ] T239 [US1] Add client/src/pages/Estimates/EstimateMaterialLineRow.tsx, one material row of the existing table, with the price as an `InlineNumberField` (2 decimals, ≥ 0). Use it from EstimateWorkLineRow.tsx. Wire `onLabourRateChange` and `onMaterialPriceChange` in EstimateEditorPage.tsx the same way as `onQuantityChange` (the response replaces the estimate; a 409 shows the existing confirmed-estimate message). The field error text is "Kaina turi būti 0 arba didesnė, ne daugiau kaip 2 skaitmenys po kablelio."; a 400 from the server shows the same text under the field
- [ ] T240 [US1] Add frontend tests in tests/client/EstimateEditorTests/EstimateLineEditing.test.tsx:
  - typing 18 into the labour rate and pressing Enter sends `PATCH …/lines/21/labour-rate` with `{ labourRate: 18 }` and shows the returned totals
  - leaving the price field sends the price request
  - "-1" shows the Lithuanian message and sends nothing
  - a Confirmed estimate shows no editable price or rate fields
  - no line has a field for norms or waste % (FR-014-2a is met by having no such endpoint or field)
  - a 400 from the server on a price shows the field text, not the server's text
  - the existing quantity tests keep passing unchanged

## Phase 3: User Story 2: Remove a material line (P1)

**Goal**: A Manager removes one material line after confirming.
**Independent Test**: T243 passes, and quickstart.md step 4 works.

- [ ] T241 [US2] Add `RemoveMaterialLine(EstimateMaterialLine)` to EstimateWorkLine.cs, plus `RemoveMaterialLineAsync(id, lineId, materialLineId)` in IEstimateService.cs and EstimateService.cs (through `FindDraftAsync`, `FindLine`, `FindMaterialLine`). Add `DELETE estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}` to EstimatesController.cs. Add tests to EstimateLineEditingTests.cs:
  - removing "Plėvelė" drops 40,43 from direct costs with the totals recalculated, and the other lines stay
  - removing the last material line leaves the work line with labour cost only
  - 409, 403 and 404 as in T237
- [ ] T242 [US2] Add `removeMaterialLine(id, lineId, materialLineId)` to client/src/api/estimates.ts. Add a "Pašalinti" button to EstimateMaterialLineRow.tsx (Draft only, aria-label "Pašalinti {materialName}"). In EstimateEditorPage.tsx it opens the shared `ConfirmDialog` (NFR-011) with the text "Pašalinti medžiagą „{name}“ iš šios sąmatos?"; cancelling changes nothing. A 409 shows the existing confirmed-estimate message
- [ ] T243 [US2] Add frontend tests to EstimateLineEditing.test.tsx:
  - cancelling the dialog sends no request
  - confirming sends `DELETE …/lines/21/materials/53`, and the line disappears from the returned estimate
  - a Confirmed estimate has no material "Pašalinti" button
  - a 409 on removal shows the existing confirmed-estimate message

## Phase 4: User Story 3: Replace a line's material (P2)

**Goal**: A Manager replaces a material line's material with a same-unit catalog material.
**Independent Test**: T246 passes, and quickstart.md step 5 works.

- [ ] T244 [US3] Server changes:
  - Add `ReplaceMaterial(name, unit, price)` to EstimateMaterialLine.cs.
  - Add server/SmartEstimatePlatform.Api/Services/MaterialUnitMismatchException.cs, mapped to 400 "The replacement material must have the same unit." in Controllers/DomainExceptionHandler.cs.
  - Add `ReplaceMaterialRequest { MaterialId }` (Models/DTOs/Estimates/).
  - Add `ReplaceMaterialAsync(id, lineId, materialLineId, request)` in IEstimateService.cs and EstimateService.cs. It loads the catalog material through the company-scoped `db.Materials`; a missing or foreign material throws `UnknownMaterialException` (existing 400), and a different unit throws `MaterialUnitMismatchException`. It copies the name, unit and current price and keeps the norm and waste % (research R3).
  - Add `PATCH estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}/material` to EstimatesController.cs.
- [ ] T245 [US3] Add server tests to EstimateLineEditingTests.cs:
  - replacing "Gipso plokštė" with a 6,20 € m² material gives the new name, unit and price, the same norm, waste and quantity (77), cost 477,40 and recalculated totals
  - a later change of that catalog material's price does not change the line (BR-010)
  - a different-unit material gives 400, another company's material gives 400, and nothing changes
  - 409, 403 and 404 as in T237
- [ ] T246 [US3] Client changes:
  - Add `replaceMaterial(id, lineId, materialLineId, materialId)` to client/src/api/estimates.ts.
  - EstimateEditorPage.tsx loads the catalog materials once with `getMaterials()`, for a Draft estimate only.
  - EstimateMaterialLineRow.tsx gets a "Pakeisti medžiagą" select (aria-label "Pakeisti medžiagą: {materialName}"). It lists only the catalog materials with the line's unit, other than the line's current name, and saves the choice at once; it's hidden when no such material exists. A 400 shows "Pasirinkite to paties mato vieneto medžiagą iš katalogo.", and a 409 shows the existing confirmed-estimate message
  - Add frontend tests to EstimateLineEditing.test.tsx: the select lists only same-unit materials, choosing one sends `{ materialId }` and shows the returned line, a Confirmed estimate has no select, a Confirmed estimate requests no materials, and a 409 shows the confirmed-estimate message.

## Phase 5: Polish

- [ ] T247 Add server tests to EstimateLineEditingTests.cs that the Viewer names view (`GET /api/estimates/{id}/names`) and the Excel export reflect the edits: after a removal and a replacement, the names list has no "Plėvelė" and has the new material name, and the export shows the edited price and the new material (FR-014-8). Run `dotnet ef migrations has-pending-model-changes` (dummy connection string) and confirm that no migration is needed
- [ ] T248 Run the full check set and the quickstart.md steps that don't need the real database. Check the editor at 360 px for horizontal page scroll (the table may scroll inside its frame, per spec 012). Add the new editor controls (price fields, replace, remove a material) to docs/ux.md §14. Fix any regression

## Dependencies

- T233 → T234 (the requests use `MoneyError`).
- US1: T234 → T235 → T236 → T237; T238 → T239 → T240 (T238 needs T236's routes only for the real API; the tests mock it).
- US2: T241 needs T235 (`FindMaterialLine`); T242 needs T239 (the material row); T243 after T242.
- US3: T244 needs T235; T245 after T244; T246 needs T239.
- T247 after T241 and T244. T248 runs last.

## Parallel Execution Examples

- Server (T234–T237) in parallel with client (T238–T240) after T233.
- Within US2 and US3, the server tasks (T241, T244–T245) in parallel with the client tasks (T242, T246).

## Implementation Strategy

US1 (prices) is the MVP and also lays the shared pieces (`MoneyError`, `FindMaterialLine`, `InlineNumberField`, `EstimateMaterialLineRow`). Then US2 (removal), then US3 (replacement), then the cross-checks of the Viewer view and the export. Spec/plan/tasks commits stay separate from code commits (Article VI).
