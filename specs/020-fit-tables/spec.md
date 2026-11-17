# Feature 20: Tables That Fit Without Sideways Scrolling

**Feature Branch**: `020-fit-tables`
**Created**: 2026-11-17
**Status**: Draft
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

### Edge Cases
- A name of exactly 200 characters with no spaces (e.g. a code-like name) is broken across lines rather than widening the column.
- A wrapped name never hides the row's amounts or actions.
- Money amounts, quantities and units never wrap; only names and descriptions do.
- The replacement dialog with 200+ materials scrolls inside itself; the page behind it does not scroll.
- The dialog lists the materials loaded with the editor. If the catalog changes while the editor is open, the server still checks the chosen material, as today (FR-023).
- Keyboard: the dialog traps focus while open, Escape closes it, and focus returns to the "Keisti" button.
- The small buttons look small (about 28–32 px high, short labels) and keep a clear focus outline. On touch screens their clickable area is still at least 44 px, as docs/ux.md requires (spec Q2).
- Viewers' pages have no tables with actions; their lists also wrap long names.

## Requirements

### Functional Requirements
- **FR-020-1**: Every table of the application fits the page width at 1280 px and wider, with no sideways scrolling of the table or the page, for names up to the 200-character maximum.
- **FR-020-2**: Name and description cells have a limited width and wrap onto at most 2 lines. Beyond 2 lines the text ends with "…", and the full text is available as a tooltip and to screen readers. Numbers, amounts and units do not wrap.
- **FR-020-3**: Row actions are small buttons with short visible labels ("Keisti", "Šalinti", "Redaguoti", "Ištrinti") and accessible names that include the item's name.
- **FR-020-4**: In the Estimate Editor, a material line's replacement is chosen in a dialog. The dialog has a search field at the top (focused when it opens) and the same-unit catalog materials except the current one, sorted by name, filtered by the search text (contains, ignoring letter case). Choosing one replaces the material exactly as FR-023 does; Escape or "Atšaukti" changes nothing.
- **FR-020-5**: Behaviour, calculations, permissions, data and the Excel export do not change. Every existing automated test keeps passing; only tests that operate the old replacement dropdown are changed to use the dialog.
- **FR-020-6**: Below 768 px, the lists keep their card layout, and the Estimate Editor table may scroll inside its own frame; the page never scrolls sideways (spec Q1).
- **FR-020-7**: Small buttons are about 28–32 px high on screens with a mouse, and their clickable area is at least 44 px on touch screens (docs/ux.md, spec Q2).

## Success Criteria

### Measurable Outcomes
- **SC-020-1**: At 1280 px and 1920 px, 0 of the 10 tables need sideways scrolling, with 80–200-character names (manual check on every table, recorded in tasks.md).
- **SC-020-2**: A Manager finds and chooses a replacement material from a 200-material catalog in under 10 seconds by typing part of its name.
- **SC-020-3**: 100% of existing automated tests keep passing; new tests cover the replacement dialog (search, choose, cancel, keyboard) and the wrapped names' full text.
- **SC-020-4**: Every action keeps an accessible name naming its item (automated test on the editor and one list).

## Assumptions
- "Laptop" means 1280 px wide or more; 1920 px is the upper size in NFR-008.
- The 10 tables are: Estimate Editor; materials, works and catalog-sections lists; the work form's material lines; the catalog section form's work lines; projects list; a project's estimates list (Manager) and estimate names list (Viewer); team list.
- The replacement dialog reuses the materials already loaded by the editor; no new server endpoint is needed.
- Column order and the information shown stay as they are; only widths, wrapping and the form of the actions change.
- This builds on feature 019 (sections), whose editor rows it restyles; it is merged after 019.

## Clarifications

### Session 2026-11-17
- Q: Below 768 px (e.g. a 360 px phone), must the Estimate Editor also fit without any sideways scrolling, or may its table keep scrolling inside its own frame? → A: It may keep scrolling inside its frame; fitting is required from about 1280 px (laptop) up.
- Q: How small are the small buttons, given docs/ux.md's ~44 px tap target? → A: They look small (about 28–32 px, short labels) with a clickable area of at least 44 px on touch screens.
