# Plan: Sections and Section Templates

**Branch**: `019-estimate-sections` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

## Summary

Gap D, approved by the instructor. Catalog sections (templates) are named sets of catalog works with coefficients. Adding one to a Draft estimate creates an estimate section with copies of its works: quantity = section quantity × coefficient, rounded to 3 decimals. Changing the section quantity recalculates those lines after a warning (spec Q1). Estimates are grouped by section with subtotals on screen and in the export, but only when they have sections (spec Q2). BR-016 is unchanged.

- **Server:**
  - entities `CatalogSection`, `CatalogSectionLine`, `EstimateSection`; `EstimateWorkLine` + `SectionId`, `Coefficient`; migration `AddSections`;
  - `ICatalogSectionService` / `CatalogSectionService` with `CatalogSectionsController`;
  - three new `IEstimateService` methods, and an optional `sectionId` on adding a work line;
  - `EstimateCalculator`: section work quantity and subtotals;
  - the work delete guard;
  - the Viewer names view grouped by section;
  - export grouping.
- **Client:**
  - a "Skyriai" catalog page and form;
  - an "add section" form and a section select on "add work";
  - section heading and subtotal rows in the editor, with the quantity confirmation;
  - Viewer grouping.

Decisions are in [research.md](research.md), entities in [data-model.md](data-model.md), and the API in [contracts/sections-api.md](contracts/sections-api.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged. No new dependencies.

**Storage**: MySQL; migration `AddSections` (3 tables, 2 nullable columns), required by the task (Constitution VIII)

**Testing**:
- xUnit unit tests:
  - `EstimateCalculator` section quantity (rounding, zero), subtotals, and sum = direct costs;
  - `CatalogSectionRules`.
- xUnit + WebApplicationFactory:
  - catalog section CRUD, search, duplicate, validation, and company isolation;
  - the work delete guard;
  - adding a section: quantities, copies, and total = works added one by one;
  - changing the section quantity, including overwriting a hand-edited quantity;
  - removing a section;
  - adding a single work to a section;
  - Draft-only (409), Viewer 403, another company 404;
  - the Viewer names view with sections;
  - export grouping, and an unchanged export without sections.
- Vitest + Testing Library:
  - the catalog section form and list;
  - adding a section;
  - the section rows and subtotals;
  - the quantity confirmation dialog;
  - removing a section with ConfirmDialog;
  - the Viewer view with sections.

**Project Type**: Web application

**Constraints**:
- Amounts only in `EstimateCalculator`, money rounded only by `MoneyRounding` (BR-017).
- Copies, not references (BR-010).
- Draft only (BR-013).
- An estimate without sections is unchanged (spec Q2).

**Prerequisite**: T315 writes the requirements change before code (Constitution VII.3):
- remove "templates, sections" from Out of Scope;
- add new FRs;
- a BR-017 sentence on rounding a computed section work quantity;
- §10 entities;
- a change-log entry recording the instructor's approval.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| I / II.5 tests | Unit tests for the quantity rule and subtotals; integration tests for every endpoint, role, company and status rule; UI tests | Pass |
| II.1 / III business logic | Quantity, rounding and subtotals in `EstimateCalculator`; section copy and recalculation in `EstimateService`; React only displays the server's response | Pass |
| II.4 / X.1 DRY | Adding a section reuses the existing "copy a work into the estimate" code; the quantity rule reuses `EstimateRules.ValidateQuantity`; the coefficient reuses `NumberRules.NormError`; the catalog section form reuses the work form's patterns; `ConfirmDialog` for remove and recalculate | Pass |
| III scope | Out of Scope changed only with instructor approval (T315). No sub-sections, per-section percentages, moving or reordering | Pass |
| IV security | Manager-only catalog and edits; the company filter on `CatalogSection` and through parents on lines and sections; integration tests for 403/404 (NFR-005) | Pass |
| VI / VIII | Spec/plan/tasks and the requirements change committed before code; one required migration | Pass |
| X.2–X.10 | One service call per action; `ICatalogSectionService` registered in `AddApplicationServices()`; one DTO record per file in Models/DTOs/CatalogSections and Estimates; mapping only in `DtoMapper`; entity mutators without rules; declarative validation on requests; Tailwind, one component per file; no CancellationToken | Pass |

**Post-design re-check**: Pass. Complexity Tracking empty.

## Project Structure

```text
server/SmartEstimatePlatform.Api/
├── Models/Entities/CatalogSection.cs, CatalogSectionLine.cs, EstimateSection.cs     # new
├── Models/Entities/EstimateWorkLine.cs, Estimate.cs        # SectionId, Coefficient; sections collection and mutators
├── Data/AppDbContext.cs                                    # configuration and filters
├── Migrations/<timestamp>_AddSections.cs                   # new, generated
├── Models/DTOs/CatalogSections/*                           # new request and response records
├── Models/DTOs/Estimates/AddSectionRequest.cs, ChangeSectionQuantityRequest.cs, EstimateSectionResponse.cs, SectionNamesResponse.cs   # new
├── Models/DTOs/Estimates/AddWorkLineRequest.cs, EstimateResponse.cs, EstimateWorkLineResponse.cs, EstimateNamesResponse.cs
├── Services/CatalogSectionRules.cs, ICatalogSectionService.cs, CatalogSectionService.cs, DuplicateCatalogSectionException.cs, WorkInUseException.cs, ZeroSectionQuantityException.cs   # new
├── Services/EstimateCalculator.cs, EstimateCalculation.cs, SectionCalculation.cs (new)
├── Services/IEstimateService.cs, EstimateService.cs, WorkService.cs, EstimateExportService.cs, Mapping/DtoMapper.cs, ServiceCollectionExtensions.cs
└── Controllers/CatalogSectionsController.cs (new), EstimatesController.cs, DomainExceptionHandler.cs
tests/server/CatalogSectionTests/*, EstimateSectionTests/*  (+ updates in calculator, export, names and isolation tests)
client/src/
├── interfaces/catalogSection.ts (new), estimate.ts
├── api/catalogSections.ts (new), estimates.ts
├── pages/CatalogSections/CatalogSectionsPage.tsx, CatalogSectionsTable.tsx, CatalogSectionFormPage.tsx, CatalogSectionLinesEditor.tsx   # new
├── pages/Estimates/AddSectionForm.tsx, EstimateSectionHeaderRow.tsx, EstimateSubtotalRow.tsx   # new
├── pages/Estimates/EstimateEditorPage.tsx, AddWorkForm.tsx, EstimateViewPage.tsx
├── components/NavBar.tsx, App.tsx                          # "Skyriai" link and routes
tests/client/CatalogSectionTests/*, EstimateEditorTests/EstimateSections.test.tsx, ViewerTests (update)
docs/requirements.md, specs/shared-technical-rules.md, docs/review/change-log.md, docs/ux.md
```

## Implementation Order

1. **Requirements change** (T315), committed before code.
2. **Entities, DbContext, migration;** calculator rule and subtotals, with unit tests.
3. **US1 catalog sections:** service, rules, controller, work delete guard, tests; then the client page and form, with tests.
4. **US2 estimate sections:** add, change quantity, remove, add a work to a section; response fields; tests; then the client forms, rows and dialogs, with tests.
5. **US3 display:** export grouping, Viewer names; tests; Viewer page.
6. **Full checks,** docs/ux.md, quickstart.

## Complexity Tracking

No violations.
