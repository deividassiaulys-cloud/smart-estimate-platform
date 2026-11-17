# Tasks: Sections and Section Templates

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/sections-api.md, quickstart.md in `specs/019-estimate-sections/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T315 (feature 018 ended at T314).

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. This feature has one migration (T318), which the task explicitly requires (Constitution VIII).

## Phase 1: Setup: requirements prerequisite

- [ ] T315 Write and commit the requirements change before any code (Constitution VII.3):
  - docs/requirements.md:
    - §5 Out of Scope: "Estimate version history, templates, sections, discounts" becomes "Estimate version history, discounts". Nothing else on that list changes.
    - §5 In Scope: add "Catalog sections (templates of works) and estimate sections with subtotals".
    - §6 Should Have: add the same.
    - §7: add these, all Should have:
      - FR-030: catalog sections with works and coefficients, unique name and unit, delete with confirmation; a work used in a catalog section cannot be deleted.
      - FR-031: add a catalog section to a Draft estimate with one quantity, creating copies of its works with quantity = section quantity × coefficient and their material lines.
      - FR-032: change an estimate section's quantity, recalculating its catalog lines after a warning.
      - FR-033: remove an estimate section with its lines (Draft only, with confirmation), and add a single work to a section or to no section.
      - FR-034: show and export estimates grouped by section with subtotals (only when the estimate has sections); Viewers see section names with work and material names only.
    - BR-017: add "A work quantity computed from a section (section quantity × coefficient) is rounded to 3 decimal places, half away from zero, when it is created or recalculated; a result of 0 is refused."
    - BR-011: an estimate also has sections; deleting a section deletes its work lines.
    - §10 Main Data Entities: Catalog Section, Catalog Section Line, Estimate Section; Estimate Work Line may belong to a section.
  - specs/shared-technical-rules.md: the section-quantity rounding sentence under "Rounding", and coefficient DECIMAL(18,6) under "Money and quantities".
  - docs/review/change-log.md: a dated entry recording that the instructor approved removing "templates, sections" from Out of Scope (gap D), the new FRs and the changed rules.

## Phase 2: Foundational: entities, migration and the calculation (blocks every story)

- [ ] T316 Entities (data-model.md), each with private setters and doc comments (X.6):
  - `Models/Entities/CatalogSection.cs` (ICompanyScoped; constructor and `Update(name, unit)`, `ReplaceLines(lines)`).
  - `CatalogSectionLine.cs` (`WorkId`, `Work`, `Coefficient`).
  - `EstimateSection.cs` (`EstimateId`, `Name`, `Unit`, `Quantity`, `ChangeQuantity`).
  - `EstimateWorkLine`: + `SectionId` (int?), `Section`, `Coefficient` (decimal?), set by a new constructor overload or optional constructor parameters, + `ChangeQuantity` reused.
  - `Estimate`: + `Sections` list, `AddSection`, `RemoveSection`.
- [ ] T317 `Data/AppDbContext.cs`:
  - DbSets.
  - CatalogSection: Name max `CatalogSectionRules.NameMaxLength` with the case-insensitive collation, Unit max 16, unique (CompanyId, Name, Unit).
  - CatalogSectionLine: Coefficient (18,6), FK Work **Restrict**, cascade with its section, query filter through its section's company.
  - EstimateSection: Name max 200, Unit max 16, Quantity (18,3), cascade with its estimate, query filter through its estimate.
  - EstimateWorkLine: SectionId FK cascade, Coefficient (18,6).
- [ ] T318 Generate the migration `AddSections` (quickstart.md "Migration commands"). It must only create the 3 tables and add the 2 nullable columns, the index and the FKs. `has-pending-model-changes` then reports none.
- [ ] T319 `Services/EstimateCalculator.cs`:
  - `SectionWorkQuantity(sectionQuantity, coefficient)` = `decimal.Round(q × c, 3, MidpointRounding.AwayFromZero)` (BR-017 sentence from T315).
  - `Calculate` adds `Sections` (new record `SectionCalculation(EstimateSection Section, decimal Subtotal)`, in Id order) and `UnsectionedSubtotal` to `EstimateCalculation`. A subtotal is Σ(labour cost + material costs) of the section's lines.
  - DirectCosts and everything after it are unchanged.
- [ ] T320 [P] Unit tests in tests/server/EstimateCalculatorTests/EstimateCalculatorTests.cs:
  - 121,7 × 0,4 = 48,68
  - 121,7 × 0,3955 = 48,132 (rounded half away from zero)
  - 0,1 × 0,0001 = 0
  - two sections plus unsectioned lines: subtotals sum to DirectCosts exactly
  - the worked example without sections: Sections is empty, UnsectionedSubtotal = 825,31, total 1263,26

## Phase 3: User Story 1: Catalog sections (P1) 🎯 MVP

**Goal**: a Manager creates, edits, searches and deletes catalog sections; works used in them can't be deleted.
**Independent Test**: spec US1.

- [ ] T321 [US1] Server:
  - `Services/CatalogSectionRules.cs` (R3).
  - DTO records in `Models/DTOs/CatalogSections/`: `CatalogSectionRequest` (Name, Unit, Lines; IValidatableObject using the rules), `CatalogSectionLineRequest` (WorkId, Coefficient), `CatalogSectionResponse`, `CatalogSectionLineResponse` (Id, WorkId, WorkName, WorkUnit, Coefficient).
  - `ICatalogSectionService` / `CatalogSectionService` (Search, Get, Create, Update, Delete), registered in `AddApplicationServices()`:
    - names are trimmed;
    - every line's work must be a company work (otherwise `UnknownWorkException` → 400);
    - a duplicate gives `DuplicateCatalogSectionException` → 409 via `UniqueSave`, as for works.
  - Mapping in `DtoMapper`.
  - `Controllers/CatalogSectionsController.cs` ([Authorize Manager], route `api/catalog-sections`, one service call per action, 201 on create, 204 on delete).
- [ ] T322 [US1] `WorkService.DeleteAsync`: if any catalog section line uses the work, throw `WorkInUseException(sectionNames)` → 409 with a Lithuanian-safe detail naming the sections, as `MaterialInUseException` does. Map it in `DomainExceptionHandler`.
- [ ] T323 [P] [US1] Tests:
  - tests/server/CatalogSectionTests/CatalogSectionEndpointsTests.cs:
    - create and get in order; update replaces the lines; search (case-insensitive); delete 204;
    - duplicate 409; validation 400 (no lines, coefficient 0 / −1 / 7 decimals, bad unit);
    - unknown work 400; a repeated work allowed;
    - Viewer 403; another company 404.
  - tests/server/WorkServiceTests: deleting a work used in a catalog section gives 409 naming it; after the section is deleted, the work can be deleted.
  - tests/server/RequestValidationTests: the coefficient rules.
- [ ] T324 [US1] Client:
  - `interfaces/catalogSection.ts`, `api/catalogSections.ts`.
  - `pages/CatalogSections/CatalogSectionsPage.tsx` (search, list, delete with ConfirmDialog) and `CatalogSectionsTable.tsx` (name, unit, number of works; cards below 768 px with data-label).
  - `CatalogSectionFormPage.tsx` (name, unit, lines) and `CatalogSectionLinesEditor.tsx` (work select, coefficient with `decimalPattern(normDecimals)`, add and remove a line; Lithuanian messages).
  - Routes `/catalog-sections`, `/catalog-sections/new`, `/catalog-sections/:id/edit` (Manager only, like `/works`).
  - A "Skyriai" link in NavBar after "Darbai".
  - Tailwind only, one component per file.
- [ ] T325 [US1] Add tests/client/CatalogSectionTests/CatalogSections.test.tsx:
  - create sends name, unit and lines in order;
  - a coefficient of 0 or with 7 decimals is refused and not sent;
  - editing loads the lines;
  - the list shows the number of works;
  - delete asks for confirmation;
  - a 409 duplicate shows a Lithuanian message;
  - a Viewer has no "Skyriai" link.

## Phase 4: User Story 2: Estimate sections (P1)

**Goal**: add a catalog section with one quantity, change it, remove it, add a single work to a section.
**Independent Test**: spec US2.

- [ ] T326 [US2] Server:
  - DTOs `AddSectionRequest` (CatalogSectionId, Quantity via `EstimateRules.ValidateQuantity`), `ChangeSectionQuantityRequest`, `EstimateSectionResponse` (Id, Name, Unit, Quantity, Subtotal).
  - `AddWorkLineRequest` + optional `SectionId`.
  - `EstimateWorkLineResponse` + `SectionId`.
  - `EstimateResponse` + `Sections`, `UnsectionedSubtotal`.
  - `IEstimateService` / `EstimateService`:
    - `AddSectionAsync`: Draft check; load the catalog section with works, their lines and materials; for each line create a work line in the new section with `SectionWorkQuantity` and the coefficient, using the same copy code as `AddWorkLineAsync` (extract it once, X.1); a zero quantity throws `ZeroSectionQuantityException` → 400; one save.
    - `ChangeSectionQuantityAsync`: Draft; set the quantity and recalculate every line of the section that has a coefficient.
    - `RemoveSectionAsync`: Draft; removes the section and its lines.
    - `AddWorkLineAsync`: honours `SectionId`, which must be a section of this estimate (otherwise `NotFoundException`).
  - `EstimatesController`: POST/PATCH/DELETE `estimates/{id}/sections[/{sectionId}]`, each one service call.
  - Mapping in `DtoMapper`.
- [ ] T327 [P] [US2] Add tests/server/EstimateSectionTests/EstimateSectionEndpointsTests.cs:
  - add US1-1's section with 121,7: three lines 121,7 / 48,68 / 121,7, with material lines and copied prices; the total equals the same works added one by one (SC-019-2);
  - change Pelių barjeras to 50, then the section to 130: 130 / 52 / 130; a work added by hand to the section keeps its quantity;
  - remove the section: its lines and material lines are gone; the totals are recalculated;
  - add a single work to a section and with no section; another estimate's sectionId gives 404;
  - quantity 0 / 4 decimals gives 400; a zero computed quantity gives 400 and nothing is added;
  - a catalog section edited or deleted after being added leaves the estimate unchanged (BR-010);
  - Confirmed gives 409 for all three; Viewer 403; another company 404.
- [ ] T328 [US2] Client:
  - `interfaces/estimate.ts` (`sectionId`, `sections`, `unsectionedSubtotal`) and `api/estimates.ts` (addSection, changeSectionQuantity, removeSection; addWorkLine with sectionId).
  - `pages/Estimates/AddSectionForm.tsx`: catalog section select and quantity (`decimalPattern(quantityDecimals)`); Draft only.
  - `AddWorkForm.tsx`: a "Skyrius" select with the estimate's sections plus "Be skyriaus" (shown only when the estimate has sections).
  - `EstimateSectionHeaderRow.tsx`: name, unit, an inline quantity field whose save first opens ConfirmDialog ("Visų skyriaus darbų kiekiai bus perskaičiuoti; ranka pakeisti kiekiai bus perrašyti."), and a remove button with ConfirmDialog.
  - `EstimateSubtotalRow.tsx`.
  - `EstimateEditorPage.tsx`: groups the work lines by section only when `sections.length > 0` (spec Q2); otherwise it renders exactly as today.
- [ ] T329 [US2] Add tests/client/EstimateEditorTests/EstimateSections.test.tsx (API mocked):
  - adding a section sends `{catalogSectionId, quantity}`;
  - rows are grouped under section headings with subtotals and a "Be skyriaus" group;
  - changing the section quantity opens the warning, and confirming sends the PATCH while cancelling sends nothing;
  - removing a section asks for confirmation;
  - the add-work section select sends `sectionId`;
  - an estimate without sections renders as before;
  - Confirmed has no section controls.

## Phase 5: User Story 3: Display, export and Viewer (P2)

**Goal**: the export and the Viewer view show the sections.
**Independent Test**: spec US3.

- [ ] T330 [US3] `EstimateExportService`: when the estimate has sections, write per section:
  - a bold heading row "<name> — <quantity> <unit>",
  - its work and material rows as today,
  - a "Skyriaus suma" row with the subtotal in the amount column,

  then "Be skyriaus" (only if it has lines) with its subtotal. Without sections, the output is unchanged. Text cells keep the quote prefix (NFR-007).
- [ ] T331 [US3] Viewer names:
  - `EstimateNamesResponse` + `Sections` (new record `SectionNamesResponse`: Name, Works); `Works` keeps only the lines without a section.
  - Mapping in `DtoMapper`.
  - `pages/Estimates/EstimateViewPage.tsx` shows each section's name as a heading above its works, then the other works.
- [ ] T332 [P] [US3] Tests:
  - tests/server/EstimateExportTests: grouping, headings, subtotals and their sum; formula-like section names stay text; the existing tests without sections pass unchanged.
  - tests/server/EstimateServiceTests/EstimateNamesTests.cs: add `sections` to the allow-list; a Viewer gets section names with works and no numbers.
  - tests/client/ViewerTests: section headings shown; no amounts.

## Phase 6: Polish

- [ ] T333 docs/ux.md: the Skyriai catalog screen, the add-section form, section rows and subtotals in the Estimate Editor, and the Viewer grouping. Check 360 px and 1920 px in the browser.
- [ ] T334 Run the full check set and the quickstart.md steps that don't need the real database. Fix any regression. Quickstart step 2 (real MySQL before/after) is a required manual step for the student before merging.

## Dependencies

- T315 → T316 (requirements before code, VII.3).
- Phase 2: T316 → T317 → T318; T316 → T319 → T320.
- US1: T321 → T322 → T323; T324 → T325 (client tests mock the API).
- US2 needs Phase 2 and T321 (catalog sections to add): T326 → T327; T328 → T329.
- US3 needs T326: T330, T331 → T332.
- T333 and T334 run last.

## Parallel Execution Examples

- After Phase 2, the US1 server work (T321–T323) and the US1 client work (T324–T325) touch different files.
- In US3, the export (T330) and the Viewer names (T331) are independent.

## Implementation Strategy

The requirements change comes first, then the entities and the one calculation rule. US1 (catalog sections) is the MVP: elements can be described once. US2 makes "choose A++ siena, enter 121,7 m²" work. US3 brings the export and the Viewer view into the same structure. An estimate without sections never changes, so existing data and tests stay valid throughout. Spec/plan/tasks commits stay separate from code commits (Article VI).
