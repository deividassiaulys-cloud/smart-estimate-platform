# Tasks: Tables That Fit Without Sideways Scrolling

**Input**: spec.md, plan.md, research.md, quickstart.md in `specs/020-fit-tables/` (User Stories 4–6 added during review: T346–T356)

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T335 (feature 019 ended at T334).

**Rule for every task**: after each task, `npm run lint`, `npm run build` and `npm run test` must pass, and `dotnet test` must stay green (no server change). There's no migration in this feature.

## Phase 1: Foundational: shared styles (blocks every story)

- [X] T335 In client/src/utils/ui.ts, add (research R2, R3):
  - `nameCell` (cell + `whitespace-normal max-w-[22rem] min-w-[10rem] break-words`);
  - `smallButton` / `smallDangerButton` (`px-2 py-1 text-sm` with the existing colours, plus `pointer-coarse:min-h-11 pointer-coarse:min-w-11`);
  - `smallLink` (same size as `smallButton`, link style).

  `cell` stays `whitespace-nowrap` for numbers, units and amounts. Add a doc comment on each.
- [X] T336 Add client/src/components/ClampedText.tsx: `ClampedText({ text })` renders `<span className="line-clamp-2 break-words" title={text}>{text}</span>`, with a doc comment (FR-020-2).

## Phase 2: User Story 2: Replacement dialog (P1)

**Goal**: "Keisti" opens a dialog with search; choosing replaces as before.
**Independent Test**: spec US2.

- [X] T337 [US2] Add client/src/pages/Estimates/ReplaceMaterialDialog.tsx (research R4):
  - props: `materialName`, `options: Material[]` (same unit, current excluded, sorted by name with `localeCompare('lt')`), `onChoose(id)`, `onCancel`.
  - role="dialog", aria-modal, the title "Pakeisti medžiagą: <name>".
  - The search input (label "Paieška") is focused when the dialog opens. It filters by `toLocaleLowerCase('lt-LT')` contains; with no match it shows "Medžiagų nerasta.".
  - Options are buttons in a list (name and `formatPrice(price)`), scrolling inside the dialog.
  - ArrowDown/ArrowUp move between the search field and the options; Enter or a click chooses.
  - Escape and "Atšaukti" cancel; Tab stays inside the dialog.
- [X] T338 [US2] In client/src/pages/Estimates/EstimateMaterialLineRow.tsx, replace the `<select>` with a small "Keisti" button (aria-label "Pakeisti medžiagą: <name>"), shown only when there are replacements. It opens `ReplaceMaterialDialog`; on choose it calls the existing `onReplace(id)` and shows the existing refusal message; on close it returns focus to "Keisti".
- [X] T339 [US2] Add tests/client/EstimateEditorTests/ReplaceMaterialDialog.test.tsx:
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

- [X] T340 [US1] Editor rows:
  - EstimateWorkLineRow: name in `ui.nameCell` with `ClampedText`; the norm-and-rate cell may wrap (`flex-wrap`); remove → small "Šalinti" with aria-label "Pašalinti <work>".
  - EstimateMaterialLineRow: name in `ui.nameCell` with `ClampedText`; remove → small "Šalinti" with aria-label "Pašalinti <material>".
  - EstimateSectionHeaderRow: name with `ClampedText`; remove → small "Šalinti" with aria-label "Pašalinti skyrių <name>".
  - InlineNumberField: `w-28` → `w-24`.
  - The visible labels shorten, but every aria-label stays the same as today, so existing tests and screen-reader users are unaffected.
- [X] T341 [US1] Add tests/client/EstimateEditorTests/FitTables.test.tsx:
  - a 200-character work, material and section name is rendered in full with a `title` equal to the name;
  - every action keeps its accessible name ("Pašalinti <name>", "Pakeisti medžiagą: <name>", "Pašalinti skyrių <name>");
  - Confirmed shows no actions.

## Phase 4: User Story 3: The other tables fit (P2)

**Goal**: the 9 other tables fit at 1280 px.
**Independent Test**: spec US3.

- [X] T342 [US3] Use `ui.nameCell`/`ClampedText` for the name or description column and small buttons or links for the actions in:
  - MaterialsTable, WorksTable, CatalogSectionsTable;
  - WorkLinesEditor, CatalogSectionLinesEditor (the select cell may shrink: `min-w-0`);
  - ProjectsTable, ProjectEstimatesTable, ProjectEstimateNamesTable;
  - TeamTable.

  The lists' card layout below 768 px (`listTitleCell`, `data-label`) is kept; labels and accessible names are unchanged.
- [X] T343 [US3] Extend FitTables.test.tsx: the materials list and the works list show a 200-character name with its `title`, and the actions keep their names. The existing list and navigation tests keep passing unchanged.

## Phase 5: Polish

- [ ] T344 docs/ux.md: §14 Estimate Editor (small "Keisti"/"Šalinti", the replacement dialog with search) and the wrapping rule for names in tables (§8). Do the quickstart.md manual check at 1280 px, 1920 px and 360 px and record the result here; touch-target check in DevTools.
- [X] T345 Run the full check set (`npm run lint`, `npm run build`, `npm run test`, `dotnet test`). Fix any regression.

**Status notes (2026-11-17)**:
- T336: `ClampedText` also sets `whitespace-normal`, so it wraps inside cells that otherwise keep text on one line.
- T340: the visible labels shortened to "Keisti" / "Šalinti". The accessible names ("Pakeisti medžiagą: …", "Pašalinti …", "Pašalinti skyrių …") are unchanged, so existing tests and screen-reader users are unaffected.
- T342: list actions keep their labels and accessible names (FR-020-3 clarified accordingly); the project description wraps like names.
- T344: docs/ux.md updated (§8 rule, §14 editor). The manual width check at 1280 / 1920 / 360 px and the touch-target check have **not** been done in this session (jsdom has no layout), so T344 stays open.
- Found in the student's review: row action cells had `flex` on the `<td>`, so with a 2-line name their bottom line sat higher than the row's. Fixed in `ui.ts` (the cell stays a table cell; cards below 768 px keep the flex row), with a regression test in FitTables.test.tsx.
- T345: `npm run lint`, `npm run build`, `npm run test` (114 passed, twice) and `dotnet test` (368 passed) all pass.

## Phase 6: Shared dialog pieces (User Stories 4–6)

- [X] T346 Add client/src/components/DialogFrame.tsx (plan R1):
  - props: `title`, `onClose`, `wide?`, `children`;
  - rendered in a portal with role="dialog", aria-modal and aria-labelledby;
  - Escape → onClose; Tab cycles through the focusable elements inside;
  - on unmount, focus returns to the element that had it before opening.
- [X] T347 Add client/src/components/SearchableList.tsx (plan R2):
  - props: `items: {id, name, detail?}[]`, `noMatch`, `chosenId?`, `onPick(id)`, `onSearchChange?(text)`;
  - the search input (label "Paieška") is focused on mount;
  - case-insensitive lt-LT "contains" filter, sorted with `localeCompare('lt')`;
  - option buttons with `aria-pressed` for the chosen one; ArrowDown/ArrowUp move between the search field and the options.
- [X] T348 Move `ReplaceMaterialDialog` onto `DialogFrame` + `SearchableList` (picking an item chooses it at once). All 7 of its existing tests pass unchanged.

## Phase 7: User Stories 4 and 5: Add dialogs (P1)

**Goal**: "+ Pridėti darbą" / "+ Pridėti skyrių" open dialogs that add exactly as before.
**Independent Test**: spec US4, US5.

- [X] T349 [US4][US5] Add client/src/pages/Estimates/AddFromCatalogDialog.tsx (plan R3):
  - layout: `SearchableList`; the chosen item's name; the quantity field labelled "Kiekis, <unit>" (`decimalPattern(quantityDecimals)`, message as today); "Skyrius" with "Be skyriaus" plus the estimate's sections, only when sections are given and not empty; "Pridėti", "Atšaukti" and "+ Kurti naują…";
  - choosing an item moves the focus to the quantity field;
  - `onAdd` resolves to an error message (shown, dialog stays open) or null (the dialog closes).
- [X] T350 [US4][US5] `EstimateEditorPage`:
  - replace `AddSectionForm`/`AddWorkForm` with the buttons "+ Pridėti skyrių" and "+ Pridėti darbą" (Draft only), which open `AddFromCatalogDialog` with the catalog sections (detail: unit · number of works) or the works (detail: unit);
  - adding uses the existing `addSection`/`addWorkLine`; a refusal returns `saveErrorMessage(err)` and still reloads on 409 as today;
  - delete `AddSectionForm.tsx` and `AddWorkForm.tsx`.
- [X] T351 [US4][US5] Update the existing tests that used the add forms (EstimateSections, EstimateStatus, FitTables, and any other found by grep) to open the dialog first, with the same expected requests and messages. Add tests/client/EstimateEditorTests/AddDialogs.test.tsx:
  - both buttons only in Draft;
  - the dialog has the focus in search; filter ignoring case; no-match text;
  - choose → focus in quantity; invalid quantity refused and nothing sent;
  - work with section → body includes sectionId; section → body `{catalogSectionId, quantity}`;
  - a server 400 shows the message and keeps the dialog open;
  - Escape and "Atšaukti" send nothing.

## Phase 8: User Story 6: Create new without leaving (P2)

**Goal**: "+ Kurti naują…" opens the catalog form on top of the editor and returns with the item chosen.
**Independent Test**: spec US6.

- [X] T352 [US6] Extract client/src/pages/Works/WorkForm.tsx and client/src/pages/CatalogSections/CatalogSectionForm.tsx from their pages (plan R4):
  - props `id | null`, `initialName?`, `onSaved(item)`, `onCancel?`;
  - the pages keep the route, heading, navigation and notices, and render the form;
  - the catalog pages' behaviour and existing tests are unchanged.
- [X] T353 [US6] In `EstimateEditorPage`, "+ Kurti naują darbą/skyrių":
  - opens a wide `DialogFrame` with `WorkForm`/`CatalogSectionForm` (`initialName` = the search text);
  - on save, adds the item to the loaded list and reopens the add dialog with it chosen;
  - on cancel, returns to the add dialog.
- [X] T354 [US6] Tests in AddDialogs.test.tsx:
  - "+ Kurti naują darbą" opens the form with the search text as its name;
  - saving POSTs `/api/works` as the catalog page does and returns to "Pridėti darbą" with the new work chosen;
  - cancelling returns with nothing added;
  - the same for "+ Kurti naują skyrių".

## Phase 9: Polish (User Stories 4–6)

- [ ] T355 docs/ux.md §14: the add buttons, the dialogs with search, and create-new. Manual check in the browser (dialogs at 1280 px and 360 px, keyboard).
- [X] T356 Run the full check set (`npm run lint`, `npm run build`, `npm run test`, `dotnet test`). Fix any regression.

**Status notes for User Stories 4–6 (2026-11-17)**:
- T348: the replacement dialog now uses DialogFrame + SearchableList; all its tests pass unchanged.
- T351: besides the request-level tests, two section tests were rewritten to check the section choice inside the dialog (the old checks would have passed trivially after the forms were removed).
- T352: the form-state type in WorkForm was renamed `WorkFields` so it doesn't clash with the component name; the catalog pages' behaviour and tests are unchanged.
- T355: docs/ux.md §14 updated. The manual browser check (dialogs at 1280 px and 360 px, keyboard) has **not** been done in this session, so T355 stays open.
- T356: `npm run lint`, `npm run build`, `npm run test` (122 passed, twice) and `dotnet test` (368 passed) all pass.

## Dependencies

- User Stories 4–6: T346 → T347 → T348 → T349 → T350 → T351 → T352 → T353 → T354 → T355 → T356 (one editor page and shared components, so sequential).

- T335 → T336 → all other tasks.
- US2: T337 → T338 → T339. US1: T340 (after T338, same file) → T341. US3: T342 → T343.
- T344 and T345 run last.

## Parallel Execution Examples

- After Phase 1, the US3 tables (T342) touch different files from the editor (T337–T341).

## Implementation Strategy

Shared styles first, so every table uses one definition (X.1, X.8). The replacement dialog (US2) removes the widest element; then the editor rows (US1, the reported problem) and the other tables (US3). Spec/plan/tasks commits stay separate from code commits (Article VI).
