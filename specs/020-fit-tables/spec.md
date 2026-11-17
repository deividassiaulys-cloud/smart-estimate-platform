# Feature 20: Tables That Fit Without Sideways Scrolling

**Feature Branch**: `020-fit-tables`
**Created**: 2026-11-17
**Status**: Draft
**Extended** (2026-11-17): adding a work or a section through a dialog with search and "create new" (User Stories 4–6), requested during the review of this feature.
**Input**: On a laptop (from about 1280 px wide) the Estimate Editor table is too wide and must be scrolled sideways; everything must fit. Make the actions small: "Pakeisti medžiagą" and "Pašalinti" become small buttons. "Pakeisti medžiagą" opens a pop-up dialog with a search field at the top and the list of same-unit catalog materials to choose from (replacing the always-visible dropdown). Limit the width of the name column and let long names wrap onto 2 lines inside the cell. Apply the same treatment to the other tables of the application. Behaviour, calculations and data do not change.

## Specification
The Manager builds estimates on a laptop. Today the Estimate Editor table is wider than a laptop screen, so the Manager scrolls sideways to see amounts and actions. There are three causes:
- names never wrap, so a long work or material name stretches its column;
- every material line shows a full-width "Pakeisti medžiagą…" dropdown;
- the actions are full-size buttons.

This feature makes every table of the application fit its screen width. Names wrap onto at most 2 lines, the actions become small buttons, and choosing a replacement material moves into a pop-up with search. The pop-up is also easier to use with a long catalog. Nothing about what the tables show or what the actions do changes.

## User Scenarios & Testing

### User Story 1: The Estimate Editor fits a laptop screen (Priority: P1)
As a Manager working on a laptop, I want to see the whole estimate table, with names, quantities, amounts and actions, without scrolling sideways, so that I can read and edit it comfortably.

**Why this priority**: it is the problem reported; the editor is the main working screen.

**Independent Test**: open a Draft estimate with sections, long work and material names (≥ 80 characters) and many materials at 1280 px and at 1920 px. The table is no wider than the page, no sideways scroll bar appears, and all columns and actions are visible.

**Acceptance Scenarios**:
1. Given a Draft estimate whose work and material names are up to 200 characters, when it is shown 1280 px wide, then the table fits the page width: no sideways scrolling of the table or the page.
2. Given a name longer than the name column allows, then it wraps onto up to 2 lines. A longer name is cut with "…" after the 2nd line, and the full name is shown on hover (tooltip) and read in full by screen readers; the row's actions name the item for keyboard users.
3. Given a material line in a Draft estimate, then its actions are two small buttons, "Keisti" (replace) and "Šalinti" (remove), each with an accessible name naming the material (e.g. "Pakeisti medžiagą: Gipso plokštė"). No dropdown is shown in the row.
4. Given a work line or a section heading, then its remove action is a small "Šalinti" button with an accessible name naming the work or section.
5. Given the editable cells (quantity, labour rate, price, section quantity), then they keep working exactly as before, in narrower fields that still show the longest allowed value (e.g. "999999,999" or "0,0616").
6. Given a Confirmed estimate, then there are no action buttons, as before.
7. Given a screen narrower than 768 px (e.g. a 360 px phone), then the editor table may still scroll inside its own frame, as today; the page itself never scrolls sideways (NFR-008, spec Q1).

### User Story 2: Choose a replacement material in a pop-up with search (Priority: P1)
As a Manager, I want "Keisti" to open a pop-up listing the materials I can replace with, with a search field at the top, so that I can find the right one quickly even in a catalog of hundreds of materials.

**Why this priority**: it removes the widest element of the editor and makes replacing usable with a large catalog (the sample catalog has 200 materials).

**Independent Test**: click "Keisti" on a material line, search, choose a material, and see the line replaced; cancel and see nothing change.

**Acceptance Scenarios**:
1. Given a material line "Gipso plokštė" (m²), when the Manager clicks "Keisti", then a dialog "Pakeisti medžiagą: Gipso plokštė" opens. It has a search field with the focus and the list of catalog materials with the same unit, excluding the current one, each with its name and price, sorted by name.
2. Given the dialog, when the Manager types part of a name (e.g. "drėg"), then the list shows only materials whose name contains it, ignoring letter case, as the Manager types. With no match, the list says "Medžiagų nerasta.".
3. Given the dialog, when the Manager chooses a material (click, or keyboard arrows and Enter), then the dialog closes and the line is replaced exactly as today (FR-023): new name and price, same norm and waste, recalculated amounts.
4. Given the dialog, when the Manager presses Escape or "Atšaukti", then it closes and nothing changes.
5. Given no other material with the same unit in the catalog, then "Keisti" is not shown, as the dropdown is not shown today.
6. Given the server refuses the replacement (e.g. the estimate was confirmed in another tab), then the existing messages are shown, as today.

### User Story 3: The other tables fit too (Priority: P2)
As a Manager, I want the catalog lists, the work and section forms, the projects, estimates and team lists to fit the screen in the same way, so that no table needs sideways scrolling.

**Why this priority**: same problem, smaller tables; asked for "analogiškai ir kitose lentelėse".

**Independent Test**: at 1280 px and 1920 px, open each table with long names (≥ 80 characters). None is wider than the page, names wrap onto up to 2 lines, and the actions are small buttons.

**Acceptance Scenarios**:
1. Given the materials, works and catalog-sections lists, the work form's material lines, the catalog section form's work lines, the projects list, a project's estimates list and the team list, then at 1280 px each fits the page width with no sideways scrolling.
2. Given a long name in any of these tables, then it wraps onto up to 2 lines, with "…" and the full name on hover (tooltip) beyond that.
3. Given the row actions in these tables ("Redaguoti", "Ištrinti", "Pašalinti"), then they are small buttons or links with the same labels and accessible names.
4. Given a screen narrower than 768 px, then the lists keep their existing card layout (docs/ux.md §8).

### User Story 4: Add a work through a dialog with search (Priority: P1)
As a Manager, I want "+ Pridėti darbą" to open a dialog where I search for a work, choose it, enter its quantity (and section) and add it, so that I find the right work quickly in a large catalog.

**Why this priority**: adding works is the most frequent action in the editor.

**Independent Test**: open the dialog, search "taš", choose "Tašelis 28x45 mm", enter 121,7, choose the section, add; the work line appears as today.

**Acceptance Scenarios**:
1. Given a Draft estimate, then above the table there are only the buttons "+ Pridėti skyrių" and "+ Pridėti darbą" (no always-visible forms).
2. When the Manager clicks "+ Pridėti darbą", then a dialog "Pridėti darbą" opens. It has a search field with the focus and the list of catalog works (name, unit) sorted by name. Typing filters the list by part of the name, ignoring letter case; with no match it says "Darbų nerasta.".
3. When the Manager chooses a work, then it is shown as chosen, and the quantity field (labelled with the work's unit) and, when the estimate has sections, the "Skyrius" choice ("Be skyriaus" by default) become usable.
4. When the Manager enters a valid quantity and clicks "Pridėti", then the work is added exactly as today (FR-012, FR-033), the dialog closes and the editor shows the recalculated estimate.
5. Given no work chosen, or a quantity of 0, with more than 3 decimal places or not a number, then "Pridėti" shows the existing Lithuanian message and sends nothing.
6. Given the dialog, Escape or "Atšaukti" closes it and adds nothing; focus returns to "+ Pridėti darbą".

### User Story 5: Add a section through a dialog with search (Priority: P1)
As a Manager, I want "+ Pridėti skyrių" to open the same kind of dialog for catalog sections with the section quantity, so that adding a building element works the same way.

**Why this priority**: same frequency and the same problem as US4 for estimates built from sections.

**Independent Test**: open the dialog, search "siena", choose "A++ siena", enter 121,7 m², add; the section appears as today (FR-031).

**Acceptance Scenarios**:
1. When the Manager clicks "+ Pridėti skyrių", then a dialog "Pridėti skyrių" opens. It has a search field with the focus and the list of catalog sections (name, unit, number of works), filtered the same way; with no match it says "Skyrių nerasta.".
2. When the Manager chooses a section, enters its quantity (labelled with the section's unit) and clicks "Pridėti", then the section is added exactly as today (FR-031), and the dialog closes.
3. Validation, Escape and "Atšaukti" behave as in US4.

### User Story 6: Create the missing work or section without leaving the estimate (Priority: P2)
As a Manager, when the work or section I need is not in the catalog, I want to create it from the dialog, so that I don't have to close the dialog, go to the catalog, create it and come back to find it again.

**Why this priority**: it saves the round trip; the dialogs (US4, US5) are useful without it.

**Independent Test**: in "Pridėti darbą", search for a name that doesn't exist, click "+ Kurti naują darbą", create the work, and find it chosen in the dialog, ready for its quantity.

**Acceptance Scenarios**:
1. Given the "Pridėti darbą" dialog, then it has a "+ Kurti naują darbą" button (in "Pridėti skyrių": "+ Kurti naują skyrių"), also when the list is empty.
2. When the Manager clicks it, then the full catalog form opens on top of the editor: for a work, name, unit, labour rate and norm, and material lines; for a section, name, unit and works with coefficients. It is the same form as on the catalog pages. After "Išsaugoti" the Manager is back in the add dialog with the new item chosen and the cursor in the quantity field (spec Q3).
3. Given the new work's name typed in the search field, then the creation form starts with that name filled in.
4. Given the Manager cancels the creation, then they are back in the add dialog with nothing added.
5. The new work or section is saved in the catalog exactly as from the catalog pages (FR-009, FR-030: same validation, duplicates refused with the same messages).

### Edge Cases
- A name of exactly 200 characters with no spaces (e.g. a code-like name) is broken across lines rather than widening the column.
- A wrapped name never hides the row's amounts or actions.
- Money amounts, quantities and units never wrap; only names and descriptions do.
- The replacement dialog with 200+ materials scrolls inside itself; the page behind it does not scroll.
- The dialog lists the materials loaded with the editor. If the catalog changes while the editor is open, the server still checks the chosen material, as today (FR-023).
- Keyboard: the dialog traps focus while open, Escape closes it, and focus returns to the "Keisti" button.
- The small buttons look small (about 28–32 px high, short labels) and keep a clear focus outline. On touch screens their clickable area is still at least 44 px, as docs/ux.md requires (spec Q2).
- Viewers' pages have no tables with actions; their lists also wrap long names.
- A Confirmed estimate shows neither button (BR-013), as the forms are not shown today.
- An empty catalog: the dialog's list says "Darbų nerasta." / "Skyrių nerasta." and offers "create new".
- A catalog of hundreds of items scrolls inside the dialog; the page behind does not scroll.
- Long names wrap inside the dialog (feature 020).
- The dialog is not inside the table and inherits nothing from it (see the 020 fix).
- If adding fails on the server (e.g. the estimate was confirmed in another tab, or a computed section quantity rounds to 0), the dialog shows the existing message and stays open, so the Manager can correct the quantity or cancel.
- The section choice in "Pridėti darbą" lists the estimate's sections in their order; it is hidden when the estimate has none (as today).
- Keyboard: arrows move through the list, Enter chooses, Tab stays inside the dialog, Escape closes.

## Requirements

### Functional Requirements
- **FR-020-1**: Every table of the application fits the page width at 1280 px and wider, with no sideways scrolling of the table or the page, for names up to the 200-character maximum.
- **FR-020-2**: Name and description cells have a limited width and wrap onto at most 2 lines. Beyond 2 lines the text ends with "…", and the full text is available as a tooltip and to screen readers. Numbers, amounts and units do not wrap.
- **FR-020-3**: Row actions are small buttons with short visible labels ("Keisti", "Šalinti", "Redaguoti", "Ištrinti"). In the Estimate Editor their accessible names include the item's name (as today). In the lists, they keep their current accessible names, because the row already names the item (US3-3).
- **FR-020-4**: In the Estimate Editor, a material line's replacement is chosen in a dialog. The dialog has a search field at the top (focused when it opens) and the same-unit catalog materials except the current one, sorted by name, filtered by the search text (contains, ignoring letter case). Choosing one replaces the material exactly as FR-023 does; Escape or "Atšaukti" changes nothing.
- **FR-020-5**: Behaviour, calculations, permissions, data and the Excel export do not change. Every existing automated test keeps passing; only tests that operate the old replacement dropdown are changed to use the dialog.
- **FR-020-6**: Below 768 px, the lists keep their card layout, and the Estimate Editor table may scroll inside its own frame; the page never scrolls sideways (spec Q1).
- **FR-020-7**: Small buttons are about 28–32 px high on screens with a mouse, and their clickable area is at least 44 px on touch screens (docs/ux.md, spec Q2).
- **FR-020-8**: In a Draft estimate, the Estimate Editor shows the buttons "+ Pridėti skyrių" and "+ Pridėti darbą" instead of the add forms.
- **FR-020-9**: Each button opens a dialog with:
  - a search field (focused when the dialog opens);
  - the catalog list sorted by name and filtered by the search text (contains, ignoring letter case);
  - the quantity field labelled with the chosen item's unit (and, for a work, the "Skyrius" choice when the estimate has sections);
  - "Pridėti" and "Atšaukti".
- **FR-020-10**: "Pridėti" validates as today (an item chosen; quantity greater than 0 with at most 3 decimal places) and adds exactly as today (FR-012, FR-031, FR-033). On success the dialog closes; on a server refusal it shows the existing message and stays open.
- **FR-020-11**: Each dialog has a "create new" button. It opens the catalog's own work or section form on top of the editor (the same form and rules as the catalog pages), prefilled with the search text as the name. After saving, the new item is chosen in the add dialog; cancelling returns to the dialog with nothing added (spec Q3).
- **FR-020-12**: Behaviour, calculations, permissions and data do not change; existing tests keep their expected requests and messages, changed only to open the dialog first.

## Success Criteria

### Measurable Outcomes
- **SC-020-1**: At 1280 px and 1920 px, 0 of the 10 tables need sideways scrolling, with 80–200-character names (manual check on every table, recorded in tasks.md).
- **SC-020-2**: A Manager finds and chooses a replacement material from a 200-material catalog in under 10 seconds by typing part of its name.
- **SC-020-3**: 100% of existing automated tests keep passing; new tests cover the replacement dialog (search, choose, cancel, keyboard) and the wrapped names' full text.
- **SC-020-4**: Every action keeps an accessible name naming its item (automated test on the editor and one list).
- **SC-020-5**: A Manager adds a work from a 100-work catalog by typing part of its name in under 10 seconds.
- **SC-020-6**: A Manager adds a work that is not yet in the catalog without leaving the estimate (US6), with 0 extra page changes made by hand.
- **SC-020-7**: 100% of existing tests keep passing with the same expected requests; new tests cover search, choose, quantity validation, section choice, cancel, keyboard and "create new".

## Assumptions
- "Laptop" means 1280 px wide or more; 1920 px is the upper size in NFR-008.
- The 10 tables are: Estimate Editor; materials, works and catalog-sections lists; the work form's material lines; the catalog section form's work lines; projects list; a project's estimates list (Manager) and estimate names list (Viewer); team list.
- The replacement dialog reuses the materials already loaded by the editor; no new server endpoint is needed.
- Column order and the information shown stay as they are; only widths, wrapping and the form of the actions change.
- This builds on feature 019 (sections), whose editor rows it restyles; it is merged after 019.
- The dialogs reuse the patterns of the replacement dialog from feature 020: portal, focus, Escape, Tab, scrolling list.
- The search filters the works and sections already loaded by the editor; no new server endpoint is needed.
- The editor's other controls (percentages, status, refresh, export) do not change.

## Clarifications

### Session 2026-11-17
- Q: Below 768 px (e.g. a 360 px phone), must the Estimate Editor also fit without any sideways scrolling, or may its table keep scrolling inside its own frame? → A: It may keep scrolling inside its frame; fitting is required from about 1280 px (laptop) up.
- Q: How small are the small buttons, given docs/ux.md's ~44 px tap target? → A: They look small (about 28–32 px, short labels) with a clickable area of at least 44 px on touch screens.
- Q: How does "create new" create the missing work or section? → A: The full catalog form opens on top of the editor; after saving, the Manager is back in the add dialog with the new item chosen.
