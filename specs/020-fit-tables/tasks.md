# Tasks: Tables That Fit Without Sideways Scrolling

**Input**: spec.md, plan.md, research.md, quickstart.md in `specs/020-fit-tables/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T335 (feature 019 ended at T334).

**Rule for every task**: after each task, `npm run lint`, `npm run build` and `npm run test` must pass, and `dotnet test` must stay green (no server change). There's no migration in this feature.

## Phase 1: Foundational: shared styles (blocks every story)

- [ ] T335 In client/src/utils/ui.ts, add (research R2, R3):
  - `nameCell` (cell + `whitespace-normal max-w-[22rem] min-w-[10rem] break-words`);
  - `smallButton` / `smallDangerButton` (`px-2 py-1 text-sm` with the existing colours, plus `pointer-coarse:min-h-11 pointer-coarse:min-w-11`);
  - `smallLink` (same size as `smallButton`, link style).

  `cell` stays `whitespace-nowrap` for numbers, units and amounts. Add a doc comment on each.
- [ ] T336 Add client/src/components/ClampedText.tsx: `ClampedText({ text })` renders `<span className="line-clamp-2 break-words" title={text}>{text}</span>`, with a doc comment (FR-020-2).

## Phase 2: User Story 2: Replacement dialog (P1)

**Goal**: "Keisti" opens a dialog with search; choosing replaces as before.
**Independent Test**: spec US2.

- [ ] T337 [US2] Add client/src/pages/Estimates/ReplaceMaterialDialog.tsx (research R4):
  - props: `materialName`, `options: Material[]` (same unit, current excluded, sorted by name with `localeCompare('lt')`), `onChoose(id)`, `onCancel`.
  - role="dialog", aria-modal, the title "Pakeisti medžiagą: <name>".
  - The search input (label "Paieška") is focused when the dialog opens. It filters by `toLocaleLowerCase('lt-LT')` contains; with no match it shows "Medžiagų nerasta.".
  - Options are buttons in a list (name and `formatPrice(price)`), scrolling inside the dialog.
  - ArrowDown/ArrowUp move between the search field and the options; Enter or a click chooses.
  - Escape and "Atšaukti" cancel; Tab stays inside the dialog.
- [ ] T338 [US2] In client/src/pages/Estimates/EstimateMaterialLineRow.tsx, replace the `<select>` with a small "Keisti" button (aria-label "Pakeisti medžiagą: <name>"), shown only when there are replacements. It opens `ReplaceMaterialDialog`; on choose it calls the existing `onReplace(id)` and shows the existing refusal message; on close it returns focus to "Keisti".
- [ ] T339 [US2] Add tests/client/EstimateEditorTests/ReplaceMaterialDialog.test.tsx:
  - the dialog opens with focus in the search field and lists only other same-unit materials, sorted;
  - typing "DRĖG" filters, ignoring case;
  - no match shows "Medžiagų nerasta.";
  - click and ArrowDown+Enter both send the same PATCH as before;
  - Escape and "Atšaukti" send nothing and return focus to "Keisti";
  - no "Keisti" button when there is no replacement.

  Update tests/client/EstimateEditorTests/EstimateLineEditing.test.tsx: the replacement steps now use the dialog, with the same expected requests and messages.

## Phase 3: User Story 1: The Estimate Editor fits (P1) 🎯 MVP

**Goal**: no sideways scrolling at 1280 px.
**Independent Test**: spec US1 (manual width check plus automated accessible names).

- [ ] T340 [US1] Editor rows:
  - EstimateWorkLineRow: name in `ui.nameCell` with `ClampedText`; the norm-and-rate cell may wrap (`flex-wrap`); remove → small "Šalinti" with aria-label "Pašalinti <work>".
  - EstimateMaterialLineRow: name in `ui.nameCell` with `ClampedText`; remove → small "Šalinti" with aria-label "Pašalinti <material>".
  - EstimateSectionHeaderRow: name with `ClampedText`; remove → small "Šalinti" with aria-label "Pašalinti skyrių <name>".
  - InlineNumberField: `w-28` → `w-24`.
  - The visible labels shorten, but every aria-label stays the same as today, so existing tests and screen-reader users are unaffected.
- [ ] T341 [US1] Add tests/client/EstimateEditorTests/FitTables.test.tsx:
  - a 200-character work, material and section name is rendered in full with a `title` equal to the name;
  - every action keeps its accessible name ("Pašalinti <name>", "Pakeisti medžiagą: <name>", "Pašalinti skyrių <name>");
  - Confirmed shows no actions.

## Phase 4: User Story 3: The other tables fit (P2)

**Goal**: the 9 other tables fit at 1280 px.
**Independent Test**: spec US3.

- [ ] T342 [US3] Use `ui.nameCell`/`ClampedText` for the name or description column and small buttons or links for the actions in:
  - MaterialsTable, WorksTable, CatalogSectionsTable;
  - WorkLinesEditor, CatalogSectionLinesEditor (the select cell may shrink: `min-w-0`);
  - ProjectsTable, ProjectEstimatesTable, ProjectEstimateNamesTable;
  - TeamTable.

  The lists' card layout below 768 px (`listTitleCell`, `data-label`) is kept; labels and accessible names are unchanged.
- [ ] T343 [US3] Extend FitTables.test.tsx: the materials list and the works list show a 200-character name with its `title`, and the actions keep their names. The existing list and navigation tests keep passing unchanged.

## Phase 5: Polish

- [ ] T344 docs/ux.md: §14 Estimate Editor (small "Keisti"/"Šalinti", the replacement dialog with search) and the wrapping rule for names in tables (§8). Do the quickstart.md manual check at 1280 px, 1920 px and 360 px and record the result here; touch-target check in DevTools.
- [ ] T345 Run the full check set (`npm run lint`, `npm run build`, `npm run test`, `dotnet test`). Fix any regression.

## Dependencies

- T335 → T336 → all other tasks.
- US2: T337 → T338 → T339. US1: T340 (after T338, same file) → T341. US3: T342 → T343.
- T344 and T345 run last.

## Parallel Execution Examples

- After Phase 1, the US3 tables (T342) touch different files from the editor (T337–T341).

## Implementation Strategy

Shared styles first, so every table uses one definition (X.1, X.8). The replacement dialog (US2) removes the widest element; then the editor rows (US1, the reported problem) and the other tables (US3). Spec/plan/tasks commits stay separate from code commits (Article VI).
