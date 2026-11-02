# Research: Work Catalog Management

## R1: Labour fields

- **Decision**: `LabourRate` (€/h, DECIMAL(18,2), ≥ 0, at most 2 decimals) and `LabourNorm` (h per unit of work, DECIMAL(18,3), ≥ 0, at most 3 decimals). Feature 007 computes the labour cost per unit as norm × rate.
- **Source**: spec Clarifications Q1 and Q4.

## R2: Composition lines

- **Decision**: `WorkMaterialLine` has its own ID, `MaterialId`, `ConsumptionNorm` (DECIMAL(18,3), > 0, at most 3 decimals) and `WastePercent` (DECIMAL(5,2), 0–100 inclusive, at most 2 decimals). There's no uniqueness on (work, material), so the same material can repeat (BR-009).
  - Line → Work: cascade delete.
  - Line → Material: **restrict**, so the database itself also blocks deleting a material in use.
- **Editing (Q2)**: saving replaces the whole composition. `Work.ReplaceLines` clears the collection and adds the new lines, and EF deletes the orphans. Line IDs change on edit, which nothing depends on.
- **Company scoping**: only `Work` implements `ICompanyScoped`. A line is always reached through its work, so it needs no `CompanyId` of its own. EF logs a warning about a required navigation to a filtered entity (10622). It's harmless here, because lines are never queried without their work, except in the in-use check, which already filters by a material of the current company.
- **Alternatives considered**: putting `CompanyId` on lines too. Rejected as redundant data (YAGNI).

## R3: Validation split (X.10)

- **Request records**:
  - `WorkRequest`: `[Required]` name and unit, `[StringLength(WorkRules.NameMaxLength)]` name, `Lines` required (may be empty).
  - `Validate` calls `WorkRules.Validate(unit, labourRate, labourNorm)`.
  - `WorkMaterialLineRequest.Validate` calls `WorkRules.ValidateLine(consumptionNorm, wastePercent)`.
  - MVC validates collection elements recursively, including their `IValidatableObject`.
- **Shared limits**: `NumberRules` holds `MaxMoney` (DECIMAL(18,2), moved from `MaterialRules.MaxPrice`), `MaxQuantity` (DECIMAL(18,3)) and `HasAtMostDecimals(value, places)`. `MaterialRules` and `WorkRules` both use it, so each limit is defined once.
- **In services** (stored data):
  - every line's material exists in the current company, else `UnknownMaterialException` → 400
  - duplicate name+unit → `DuplicateWorkException` → 409
  - a material in use → `MaterialInUseException` → 409
- **Texts**: English server texts, like 004. The client shows Lithuanian texts based on the status code.

## R4: Shared helpers (X.1)

- **`SearchPattern.Contains(string search)`**: escapes `\`, `%` and `_` for LIKE, used by MaterialService and WorkService. Same behaviour as today.
- **`UniqueSave.SaveAsync(AppDbContext db, Func<Task<bool>> duplicateExists, Func<Exception> duplicateError)`**:
  1. Checks for a duplicate first.
  2. Saves.
  3. On `DbUpdateException`, clears the change tracker and checks again, to catch a concurrent duplicate.

  Today this pattern exists twice (AuthService.CreateAccountAsync, MaterialService.SaveAsync), and WorkService would be the third. Same behaviour; the existing duplicate tests guard it.

## R5: Material in use (004 T038)

- **Decision**: `MaterialService.DeleteAsync` collects the names of works with a line using the material, ordered A–Z and distinct. If there are any, it throws `MaterialInUseException(workNames)`. `DomainExceptionHandler` maps that to 409, detail "The material is used in works and cannot be deleted.", with an extension `works: [names]`.
- **Client**: MaterialsPage already shows "Medžiagos negalima ištrinti, nes ji naudojama darbuose." on 409. It now appends the work names from `works`, as 004's spec requires ("the works are listed").

## R6: API and DTOs

- `WorkResponse` (Id, Name, Unit, LabourRate, LabourNorm, Lines) serves the list, get, create and update. Per the merge-identical-DTOs rule, there's no separate summary DTO.
- `WorkMaterialLineResponse` (Id, MaterialId, MaterialName, MaterialUnit, ConsumptionNorm, WastePercent). The material's name and unit come from the catalog when read.
- Endpoints mirror the materials API; details in [contracts/works-api.md](contracts/works-api.md).

## R7: Client

- **`components/SearchField`**: label + input. It replaces `Materials/MaterialSearch` and is used by both catalogs (X.1).
- **`WorkFormPage`**: work fields, plus `WorkLinesEditor`, a table of lines with a material select (from `/api/materials`), norm and waste inputs, remove-line buttons and "+ Pridėti medžiagą".
  - Removing a line from the unsaved form changes nothing stored, so it needs no ConfirmDialog. Deleting a work does use ConfirmDialog (NFR-011).
- **Client validation** mirrors `WorkRules` for immediate Lithuanian feedback, like the material form. The server stays authoritative.
