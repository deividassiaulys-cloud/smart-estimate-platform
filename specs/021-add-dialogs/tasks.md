# Tasks: Add a Work or a Section Through a Dialog

**Input**: spec.md, plan.md in `specs/021-add-dialogs/`

`[US#]` refers to the user stories of this feature in `spec.md`. Task IDs continue the project-wide numbering from T346 (feature 020 ended at T345).

**Rule for every task**: after each task, `npm run lint`, `npm run build` and `npm run test` must pass, and `dotnet test` stays green (no server change).

## Phase 1: Foundational: shared dialog pieces (blocks every story)

- [ ] T346 Add client/src/components/DialogFrame.tsx (plan R1):
  - props: `title`, `onClose`, `wide?`, `children`;
  - rendered in a portal with role="dialog", aria-modal and aria-labelledby;
  - Escape → onClose; Tab cycles through the focusable elements inside;
  - on unmount, focus returns to the element that had it before opening.
- [ ] T347 Add client/src/components/SearchableList.tsx (plan R2):
  - props: `items: {id, name, detail?}[]`, `noMatch`, `chosenId?`, `onPick(id)`, `onSearchChange?(text)`;
  - the search input (label "Paieška") is focused on mount;
  - case-insensitive lt-LT "contains" filter, sorted with `localeCompare('lt')`;
  - option buttons with `aria-pressed` for the chosen one; ArrowDown/ArrowUp move between the search field and the options.
- [ ] T348 Move `ReplaceMaterialDialog` onto `DialogFrame` + `SearchableList` (picking an item chooses it at once). All 7 of its existing tests pass unchanged.

## Phase 2: User Stories 1 and 2: Add dialogs (P1) 🎯 MVP

**Goal**: "+ Pridėti darbą" / "+ Pridėti skyrių" open dialogs that add exactly as before.
**Independent Test**: spec US1, US2.

- [ ] T349 [US1][US2] Add client/src/pages/Estimates/AddFromCatalogDialog.tsx (plan R3):
  - layout: `SearchableList`; the chosen item's name; the quantity field labelled "Kiekis, <unit>" (`decimalPattern(quantityDecimals)`, message as today); "Skyrius" with "Be skyriaus" plus the estimate's sections, only when sections are given and not empty; "Pridėti", "Atšaukti" and "+ Kurti naują…";
  - choosing an item moves the focus to the quantity field;
  - `onAdd` resolves to an error message (shown, dialog stays open) or null (the dialog closes).
- [ ] T350 [US1][US2] `EstimateEditorPage`:
  - replace `AddSectionForm`/`AddWorkForm` with the buttons "+ Pridėti skyrių" and "+ Pridėti darbą" (Draft only), which open `AddFromCatalogDialog` with the catalog sections (detail: unit · number of works) or the works (detail: unit);
  - adding uses the existing `addSection`/`addWorkLine`; a refusal returns `saveErrorMessage(err)` and still reloads on 409 as today;
  - delete `AddSectionForm.tsx` and `AddWorkForm.tsx`.
- [ ] T351 [US1][US2] Update the existing tests that used the add forms (EstimateSections, EstimateStatus, FitTables, and any other found by grep) to open the dialog first, with the same expected requests and messages. Add tests/client/EstimateEditorTests/AddDialogs.test.tsx:
  - both buttons only in Draft;
  - the dialog has the focus in search; filter ignoring case; no-match text;
  - choose → focus in quantity; invalid quantity refused and nothing sent;
  - work with section → body includes sectionId; section → body `{catalogSectionId, quantity}`;
  - a server 400 shows the message and keeps the dialog open;
  - Escape and "Atšaukti" send nothing.

## Phase 3: User Story 3: Create new without leaving (P2)

**Goal**: "+ Kurti naują…" opens the catalog form on top of the editor and returns with the item chosen.
**Independent Test**: spec US3.

- [ ] T352 [US3] Extract client/src/pages/Works/WorkForm.tsx and client/src/pages/CatalogSections/CatalogSectionForm.tsx from their pages (plan R4):
  - props `id | null`, `initialName?`, `onSaved(item)`, `onCancel?`;
  - the pages keep the route, heading, navigation and notices, and render the form;
  - the catalog pages' behaviour and existing tests are unchanged.
- [ ] T353 [US3] In `EstimateEditorPage`, "+ Kurti naują darbą/skyrių":
  - opens a wide `DialogFrame` with `WorkForm`/`CatalogSectionForm` (`initialName` = the search text);
  - on save, adds the item to the loaded list and reopens the add dialog with it chosen;
  - on cancel, returns to the add dialog.
- [ ] T354 [US3] Tests in AddDialogs.test.tsx:
  - "+ Kurti naują darbą" opens the form with the search text as its name;
  - saving POSTs `/api/works` as the catalog page does and returns to "Pridėti darbą" with the new work chosen;
  - cancelling returns with nothing added;
  - the same for "+ Kurti naują skyrių".

## Phase 4: Polish

- [ ] T355 docs/ux.md §14: the add buttons, the dialogs with search, and create-new. Manual check in the browser (dialogs at 1280 px and 360 px, keyboard).
- [ ] T356 Run the full check set (`npm run lint`, `npm run build`, `npm run test`, `dotnet test`). Fix any regression.

## Dependencies

T346 → T347 → T348 → T349 → T350 → T351 → T352 → T353 → T354 → T355 → T356 (one editor page and shared components, so sequential).

## Implementation Strategy

The shared frame and list first (they also simplify the 020 dialog), then the add dialogs (the MVP), then create-new on top of the reused forms. Spec/plan/tasks commits stay separate from code commits (Article VI).
