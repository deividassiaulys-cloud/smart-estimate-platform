# Plan: Add a Work or a Section Through a Dialog

**Branch**: `021-add-dialogs` (on top of `020-fit-tables`) | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

## Summary

Client-only. The editor's two add forms become two buttons. Each opens a dialog: a search, a list, the quantity (and, for a work, the section), "Pridėti" and "+ Kurti naują…". "Create new" opens the catalog's own work or section form on top of the editor and returns with the new item chosen (spec Q1). Adding uses the existing API calls.

## Technical Context

**Language/Version**: TypeScript 6 + React 19, Tailwind 4.3 (no server change)

**Primary Dependencies**: unchanged. No new dependencies.

**Storage**: none

**Testing**:
- Vitest + Testing Library:
  - both dialogs: search, choose, quantity validation, section choice, Escape/cancel, keyboard, server refusal keeps the dialog open;
  - "create new" opens the prefilled form and returns with the item chosen;
  - the catalog pages still create and edit as before.
- Existing tests that use the add forms are changed only to open the dialog first.

**Project Type**: Web application

**Constraints**:
- No behaviour change in adding (FR-021-3, FR-021-5).
- One component per file, Tailwind only (X.7).
- No duplicated dialog, search or form code (X.1).

## Design (research)

- **R1 Shared dialog frame:** `components/DialogFrame.tsx`.
  - Rendered in a portal; overlay; role="dialog" with aria-modal, labelled by its title.
  - Escape closes; Tab stays inside (all focusable elements); focus returns to the opener on close.
  - The 020 `ReplaceMaterialDialog` moves onto it, so there is one frame (X.1, X.8).
- **R2 Shared searchable list:** `components/SearchableList.tsx`.
  - A search input focused on mount; items `{id, name, detail}` filtered with case-insensitive "contains" (lt-LT) and sorted with `localeCompare('lt')`.
  - A list of option buttons with arrow-key movement; `onPick(id)`; a no-match text; the chosen item marked with `aria-pressed`.
  - Used by the replacement dialog and both add dialogs.
- **R3 One add dialog:** `pages/Estimates/AddFromCatalogDialog.tsx`, used for both works and sections.
  - Props: title, items, no-match text, create label, optional estimate sections (only the work dialog passes them), `onAdd(id, quantity, sectionId)`, which resolves to an error message or null, `onCreate(searchText)`, and `onClose`.
  - It shows the server's message and stays open on a refusal (spec edge case).
- **R4 Forms reused, not copied:** the bodies of `WorkFormPage` and `CatalogSectionFormPage` move into `WorkForm` and `CatalogSectionForm` components.
  - Props: `id | null`, `initialName`, `onSaved(item)`, `onCancel?`.
  - The pages keep their route, heading and navigation and wrap the form; the dialogs show the same form in a wide `DialogFrame`.
  - Behaviour on the catalog pages is unchanged.
- **R5 Editor:** `EstimateEditorPage` replaces `AddSectionForm` / `AddWorkForm` with two buttons (Draft only).
  - It adds a newly created work or section to its loaded lists and reopens the add dialog with that item chosen.
  - `AddSectionForm.tsx` and `AddWorkForm.tsx` are deleted, because they are no longer used.

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III | No business logic in React: adding and creating call the existing endpoints; validation mirrors the server as today | Pass |
| II.4 / X.1 / X.8 | One dialog frame, one searchable list, one add dialog for works and sections, catalog forms reused by pages and dialogs; replaced forms deleted | Pass |
| II.5 tests | Dialog and create-new tests; existing tests keep their expected requests | Pass |
| III | No new dependency; nothing beyond the spec (no inline mini-form, no new endpoint) | Pass |
| IV | No permission change; buttons only in Draft for a Manager | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |

**Post-design re-check**: Pass. Complexity Tracking empty.

## Project Structure

```text
client/src/components/DialogFrame.tsx, SearchableList.tsx                   # new
client/src/pages/Estimates/AddFromCatalogDialog.tsx                         # new
client/src/pages/Estimates/ReplaceMaterialDialog.tsx                        # on DialogFrame + SearchableList
client/src/pages/Estimates/EstimateEditorPage.tsx                           # buttons, dialogs, create-new
client/src/pages/Estimates/AddSectionForm.tsx, AddWorkForm.tsx              # deleted
client/src/pages/Works/WorkForm.tsx (new), WorkFormPage.tsx                 # page wraps the form
client/src/pages/CatalogSections/CatalogSectionForm.tsx (new), CatalogSectionFormPage.tsx
tests/client/EstimateEditorTests/AddDialogs.test.tsx (new); EstimateSections, EstimateStatus, FitTables (open the dialog first)
docs/ux.md
```

## Complexity Tracking

No violations.
