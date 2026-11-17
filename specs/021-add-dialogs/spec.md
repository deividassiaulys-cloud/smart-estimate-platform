# Feature 21: Add a Work or a Section Through a Dialog

**Feature Branch**: `021-add-dialogs` (on top of `020-fit-tables`)
**Created**: 2026-11-17
**Status**: Draft
**Input**: In the Estimate Editor the always-visible "add work" and "add section" forms are replaced by two buttons, "+ Pridėti darbą" and "+ Pridėti skyrių". Each opens a pop-up dialog with a search field and the list of catalog works (or catalog sections) to choose from, the quantity field (and, for a work, the section to put it in), and the add button. The dialog also has a "create new" button, so that when the needed work or section is not in the catalog the Manager can create it without first closing the dialog and going to the catalog separately. Adding behaves exactly as today (FR-012, FR-031, FR-033).

## Specification
Today the Estimate Editor shows two forms above the table, each with a dropdown of the whole catalog: one for adding a section, one for adding a work. A dropdown of hundreds of works is hard to search, and when the needed work does not exist yet, the Manager must leave the estimate, create it in the catalog, come back and find it again. This feature replaces both forms with two buttons that open a dialog. The dialog has a search field, a list, the quantity (and, for a work, its section), and a "create new" button for the missing item. Adding works as today.

## User Scenarios & Testing

### User Story 1: Add a work through a dialog with search (Priority: P1)
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

### User Story 2: Add a section through a dialog with search (Priority: P1)
As a Manager, I want "+ Pridėti skyrių" to open the same kind of dialog for catalog sections with the section quantity, so that adding a building element works the same way.

**Why this priority**: same frequency and the same problem as US1 for estimates built from sections.

**Independent Test**: open the dialog, search "siena", choose "A++ siena", enter 121,7 m², add; the section appears as today (FR-031).

**Acceptance Scenarios**:
1. When the Manager clicks "+ Pridėti skyrių", then a dialog "Pridėti skyrių" opens. It has a search field with the focus and the list of catalog sections (name, unit, number of works), filtered the same way; with no match it says "Skyrių nerasta.".
2. When the Manager chooses a section, enters its quantity (labelled with the section's unit) and clicks "Pridėti", then the section is added exactly as today (FR-031), and the dialog closes.
3. Validation, Escape and "Atšaukti" behave as in US1.

### User Story 3: Create the missing work or section without leaving the estimate (Priority: P2)
As a Manager, when the work or section I need is not in the catalog, I want to create it from the dialog, so that I don't have to close the dialog, go to the catalog, create it and come back to find it again.

**Why this priority**: it saves the round trip; the dialogs (US1, US2) are useful without it.

**Independent Test**: in "Pridėti darbą", search for a name that doesn't exist, click "+ Kurti naują darbą", create the work, and find it chosen in the dialog, ready for its quantity.

**Acceptance Scenarios**:
1. Given the "Pridėti darbą" dialog, then it has a "+ Kurti naują darbą" button (in "Pridėti skyrių": "+ Kurti naują skyrių"), also when the list is empty.
2. When the Manager clicks it, then the full catalog form opens on top of the editor: for a work, name, unit, labour rate and norm, and material lines; for a section, name, unit and works with coefficients. It is the same form as on the catalog pages. After "Išsaugoti" the Manager is back in the add dialog with the new item chosen and the cursor in the quantity field (spec Q1).
3. Given the new work's name typed in the search field, then the creation form starts with that name filled in.
4. Given the Manager cancels the creation, then they are back in the add dialog with nothing added.
5. The new work or section is saved in the catalog exactly as from the catalog pages (FR-009, FR-030: same validation, duplicates refused with the same messages).

### Edge Cases
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
- **FR-021-1**: In a Draft estimate, the Estimate Editor shows the buttons "+ Pridėti skyrių" and "+ Pridėti darbą" instead of the add forms.
- **FR-021-2**: Each button opens a dialog with:
  - a search field (focused when the dialog opens);
  - the catalog list sorted by name and filtered by the search text (contains, ignoring letter case);
  - the quantity field labelled with the chosen item's unit (and, for a work, the "Skyrius" choice when the estimate has sections);
  - "Pridėti" and "Atšaukti".
- **FR-021-3**: "Pridėti" validates as today (an item chosen; quantity greater than 0 with at most 3 decimal places) and adds exactly as today (FR-012, FR-031, FR-033). On success the dialog closes; on a server refusal it shows the existing message and stays open.
- **FR-021-4**: Each dialog has a "create new" button. It opens the catalog's own work or section form on top of the editor (the same form and rules as the catalog pages), prefilled with the search text as the name. After saving, the new item is chosen in the add dialog; cancelling returns to the dialog with nothing added (spec Q1).
- **FR-021-5**: Behaviour, calculations, permissions and data do not change; existing tests keep their expected requests and messages, changed only to open the dialog first.

### Key Entities
None new; the dialogs use the existing catalog works, catalog sections and estimates.

## Success Criteria

### Measurable Outcomes
- **SC-021-1**: A Manager adds a work from a 100-work catalog by typing part of its name in under 10 seconds.
- **SC-021-2**: A Manager adds a work that is not yet in the catalog without leaving the estimate (US3), with 0 extra page changes made by hand.
- **SC-021-3**: 100% of existing tests keep passing with the same expected requests; new tests cover search, choose, quantity validation, section choice, cancel, keyboard and "create new".

## Assumptions
- The dialogs reuse the patterns of the replacement dialog from feature 020: portal, focus, Escape, Tab, scrolling list.
- The search filters the works and sections already loaded by the editor; no new server endpoint is needed.
- The editor's other controls (percentages, status, refresh, export) do not change.

## Clarifications

### Session 2026-11-17
- Q: How does "create new" create the missing work or section? → A: The full catalog form opens on top of the editor; after saving, the Manager is back in the add dialog with the new item chosen.
