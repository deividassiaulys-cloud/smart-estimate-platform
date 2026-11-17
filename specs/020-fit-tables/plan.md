# Plan: Tables That Fit Without Sideways Scrolling

**Branch**: `020-fit-tables` (on top of `019-estimate-sections`; extended with User Stories 4–6) | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

## Summary

Client-only. All 10 tables fit the page from 1280 px:
- names wrap onto up to 2 lines (with "…" and a tooltip);
- row actions become small buttons with a 44 px touch target (spec Q2);
- the editor's always-visible replacement dropdown becomes a "Keisti" button that opens a dialog with search.

Below 768 px, the lists stay cards and the editor may scroll inside its frame (spec Q1). The server, calculations, data and export do not change.

Decisions are in [research.md](research.md); the manual width check is in [quickstart.md](quickstart.md). No data model and no API contract change, so there are no data-model.md or contracts/ files.

## Technical Context

**Language/Version**: TypeScript 6 + React 19, Tailwind 4.3 (no server change)

**Primary Dependencies**: unchanged. No new dependencies.

**Storage**: none

**Testing**:
- Vitest + Testing Library:
  - the replacement dialog: opens with focus in the search field, filters case-insensitively, keyboard choose, Escape/cancel sends nothing, focus returns, "Medžiagų nerasta.";
  - small actions keep accessible names naming the item;
  - long names carry their full text as `title`.
- Existing tests that operate the replacement `<select>` are changed to use the dialog; every other expectation stays.
- jsdom has no layout, so the fit itself is a manual check (SC-020-1).

**Project Type**: Web application

**Constraints**:
- No behaviour change (FR-020-5).
- Tailwind only, no CSS files, one component per file (X.7).
- No new dependency (Article III).

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III | No business logic touched; the dialog reuses the existing replace call (FR-023) | Pass |
| II.5 tests | Tests for the dialog and accessible names; existing tests keep their expectations | Pass |
| III dependencies | No new dependency; no combobox library | Pass |
| III scope | Only widths, wrapping and the form of actions; nothing new is shown | Pass |
| IV security | No permission change; Confirmed estimates still show no actions | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |
| X.1 / X.7 / X.8 | Shared `ui.nameCell`, `ui.smallButton`, `ui.smallDangerButton` and one `ClampedText` component instead of repeating classes in 10 tables; `ReplaceMaterialDialog` in its own file; Tailwind only | Pass |

**Post-design re-check**: Pass. Complexity Tracking empty.

## Project Structure

```text
client/src/
├── utils/ui.ts                                   # nameCell, smallButton, smallDangerButton, smallLink; cell stays nowrap for numbers
├── components/ClampedText.tsx                    # new: 2-line clamp with title
├── pages/Estimates/ReplaceMaterialDialog.tsx     # new: search plus listbox dialog
├── pages/Estimates/EstimateMaterialLineRow.tsx   # "Keisti" + "Šalinti" small buttons; opens the dialog
├── pages/Estimates/EstimateWorkLineRow.tsx, EstimateSectionHeaderRow.tsx, EstimateSubtotalRow.tsx, InlineNumberField.tsx
├── pages/Materials/MaterialsTable.tsx, pages/Works/WorksTable.tsx, WorkLinesEditor.tsx
├── pages/CatalogSections/CatalogSectionsTable.tsx, CatalogSectionLinesEditor.tsx
├── pages/Projects/ProjectsTable.tsx, ProjectEstimatesTable.tsx, ProjectEstimateNamesTable.tsx
└── pages/Team/TeamTable.tsx
tests/client/EstimateEditorTests/ReplaceMaterialDialog.test.tsx (new), EstimateLineEditing.test.tsx (replacement steps), FitTables.test.tsx (new)
docs/ux.md                                        # editor actions, the dialog, wrapping
```

## Implementation Order

1. Shared styles and `ClampedText`.
2. **US2:** `ReplaceMaterialDialog`, wired into the material row; tests.
3. **US1:** editor rows (names, small actions, narrower fields, wrapping norm-and-rate cell); tests for accessible names.
4. **US3:** the other 9 tables.
5. Full checks; the manual width check at 1280 px and 1920 px; docs/ux.md.

## Add dialogs (User Stories 4–6): design

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

Files added or changed for User Stories 4–6:

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

The Constitution Check above also holds for them: one dialog frame, one searchable list and one add dialog; the catalog forms are reused, not copied; the replaced forms are deleted; no new dependency; no server change.

## Complexity Tracking

No violations.
