# Feature 19: Sections and Section Templates

**Feature Branch**: `019-estimate-sections`
**Created**: 2026-11-17
**Status**: Draft
**Input**: Gap D in docs/reference/ANALYSIS.md; the instructor approved removing "templates, sections" from Out of Scope. An estimate is structured as estimate → sections → work lines → material lines. A Manager defines catalog sections (section templates), e.g. "A++ siena", as a named set of existing catalog works, each with a coefficient (work quantity per 1 unit of the section, e.g. tašeliai 1 m²/m², pelių barjeras 0,4 m/m²). In a Draft estimate the Manager adds a catalog section with one section quantity (e.g. 121,7 m² of wall). A section is created in the estimate with copies of its works (quantity = section quantity × coefficient) and their material lines, copied like single works today (BR-010: copies, not references). Each work line's quantity can still be changed afterwards. Estimates show work lines grouped by section with a subtotal per section, on screen and in the Excel export. The BR-016 overhead/profit/VAT calculation over the whole estimate does not change. Existing estimates keep working (their lines without a section). Viewers see section names with work and material names only (BR-005).

## Specification
A real estimate is built from building elements, not from loose works. An exterior wall "A++ siena" always consists of the same works: battens with their labour, timber and screws; the main frame with timber and screws; exterior gypsum board with labour, screws and materials; and so on. Today the Manager adds each of these works one by one, for every wall in every estimate, and the estimate is one long list with no structure. This feature lets the Manager describe an element once in the catalog, as a section with its works and how much of each work one unit of the element needs. Building an estimate then becomes: choose "A++ siena", enter 121,7 m², and every work with its materials is added at once. The estimate shows its sections with a subtotal for each, as in the local estimates clients are used to.

## User Scenarios & Testing

### User Story 1: Describe a building element once in the catalog (Priority: P1)
As a Manager, I want to create a catalog section such as "A++ siena" (unit m²) from my catalog works, each with a coefficient, so that I never have to assemble the same element by hand again.

**Why this priority**: the catalog section is what makes "choose A++ siena and everything is added" possible; US2 depends on it.

**Independent Test**: create "A++ siena" with three works and their coefficients, open it again, edit a coefficient, search for it, and delete it.

**Acceptance Scenarios**:
1. Given catalog works "Tašelis 28x45 mm" (m²), "Pelių barjeras" (m) and "Gipso plokštės montavimas" (m²), when the Manager creates the catalog section "A++ siena" with unit m² and the lines Tašelis × 1, Pelių barjeras × 0,4 and Gipso plokštės montavimas × 1, then it is saved with these works in this order.
2. Given the section list, then it shows each section's name, unit and number of works; it can be searched by part of the name, case-insensitively (as FR-010).
3. Given an existing catalog section, when the Manager changes a coefficient, adds or removes a work, or renames it, then the change is saved. Estimates that already contain the section do not change (BR-010).
4. Given a coefficient of 0, a negative one or one with more than 6 decimal places, or a section with no works, then saving is refused with a Lithuanian message naming the problem.
5. Given a section with the same name and unit as another section of the company, then saving is refused as a duplicate (like BR-006).
6. Given a catalog work used in a catalog section, when the Manager tries to delete the work, then the deletion is refused with a message naming the section(s), as a material used in a work is refused today.
7. Given a catalog section, when the Manager deletes it and confirms (NFR-011), then it is removed from the catalog; estimates that used it keep their sections and lines.

### User Story 2: Add a whole section to an estimate with one quantity (Priority: P1)
As a Manager, I want to add "A++ siena" with 121,7 m² to a Draft estimate, so that all its works, labour and materials are added at once with the right quantities.

**Why this priority**: the purpose of the feature; together with US1 it is the minimum useful feature.

**Independent Test**: add the section of US1-1 with 121,7 m² and check the created section, its work lines, their quantities, the material lines and the totals.

**Acceptance Scenarios**:
1. Given the catalog section of US1-1, when the Manager adds it to a Draft estimate with quantity 121,7, then the estimate gets a section "A++ siena" (121,7 m²) with three work lines: Tašelis 121,7 m², Pelių barjeras 48,68 m (121,7 × 0,4) and Gipso plokštės montavimas 121,7 m². Each work line gets its material lines and copied prices and norms exactly as when the work is added on its own (FR-012–FR-014).
2. Given the added section, then every amount and the estimate total are calculated with the existing rules (BR-015–BR-017). The total equals the total of the same works added one by one with the same quantities.
3. Given a work line inside a section, when the Manager changes its quantity, labour rate or a material price, or replaces or removes a material line (FR-017, FR-023), then this works exactly as for any other work line, and the section subtotal and the totals are recalculated.
4. Given a section in a Draft estimate, when the Manager removes it and confirms (NFR-011), then the section and all its work and material lines are removed and the totals recalculated.
5. Given a Draft estimate, when the Manager adds a single catalog work, then they choose an existing section of the estimate or "Be skyriaus" (no section); it is added there.
6. Given a section quantity of 0 or less, or with more than 3 decimal places, then adding is refused (BR-007).
7. Given a Confirmed estimate, then sections cannot be added, removed or changed (BR-013).
8. Given a section in a Draft estimate whose catalog lines gave Tašelis 121,7 and Pelių barjeras 48,68 (changed by hand to 50), when the Manager changes the section quantity to 130 and confirms the warning that every work quantity in the section will be recalculated, then Tašelis becomes 130 and Pelių barjeras 52 (130 × 0,4), and the totals are recalculated (spec Q1).

### User Story 3: See and export the estimate by sections (Priority: P2)
As a Manager, I want the estimate grouped by section with a subtotal per section, on screen and in Excel, so that the client sees the familiar structure.

**Why this priority**: it makes the structure visible and is required by FR-027 (export = screen); the calculation itself already works after US2.

**Independent Test**: open and export an estimate with two sections and one work without a section, and compare the grouping and the subtotals.

**Acceptance Scenarios**:
1. Given an estimate with sections "A++ siena" and "Stogas" and one work without a section, then the editor shows each section as a heading row (name, quantity and unit) followed by its work lines and a subtotal row. Lines without a section come last under "Be skyriaus", with their own subtotal; this group is shown only when the estimate has at least one section. Sections appear in the order they were added.
2. Given the section subtotals, then their sum equals the direct costs. Overhead, profit, VAT and the total are calculated once for the whole estimate as before (BR-016).
3. Given the Excel export, then it shows the same sections, rows and subtotals as the screen (FR-027), with formula-like text still written as text (NFR-007).
4. Given an estimate with no sections (including every estimate created before this feature), then it looks exactly as today: no section headings, no "Be skyriaus" group and the same totals.
5. Given a Viewer, then the names view shows each section's name with its work and material names, in the same order, and nothing else (BR-005, FR-026).

### Edge Cases
- A section's work quantity is section quantity × coefficient. A work-line quantity has at most 3 decimal places (BR-017), so the result is rounded to 3 decimal places, half away from zero, when the line is created (e.g. 121,7 × 0,3955 = 48,13235 → 48,132). This is the only place a quantity is rounded, and the rounded value is what the estimate stores and shows.
- A computed quantity that rounds to 0 (a tiny coefficient on a small section) is refused, with a message naming the work, because an estimate quantity must be greater than 0 (BR-007); nothing is added.
- The same catalog work may appear twice in a catalog section (e.g. two layers), each with its own coefficient, like a material repeated in a work (BR-009).
- A catalog section and its works must all belong to the Manager's company (FR-006); another company's section or work is "not found".
- If a catalog work in a section was changed after the section was created, adding the section copies the work as it is now. Catalog changes never reach estimates already created (BR-010).
- Refreshing an estimate from the catalog (FR-024) still matches work lines by name and unit; sections and section quantities are not changed by a refresh.
- Removing the last work line from a section leaves the empty section, with subtotal 0,00 €, until the Manager removes it.
- Two sections with the same name may exist in one estimate (e.g. two walls of the same type).
- A Viewer cannot see or open the catalog sections (they belong to the priced catalog, like materials and works).

## Requirements

### Functional Requirements
- **FR-019-1**: A Manager can create, edit, delete and search catalog sections: name, unit (from the fixed list, BR-008), and one or more lines, each a catalog work with a coefficient greater than 0 with at most 6 decimal places. Name and unit are unique within the company (like BR-006). Deleting asks for confirmation (NFR-011).
- **FR-019-2**: A catalog work used in a catalog section cannot be deleted; the refusal names the section(s).
- **FR-019-3**: In a Draft estimate, a Manager can add a catalog section with a section quantity (greater than 0, at most 3 decimal places). The estimate gets a section with the catalog section's name, unit and the quantity. For each line, a work line is created in the section with quantity = section quantity × coefficient, rounded to 3 decimal places (half away from zero), and with material lines and copied prices and norms as in FR-012–FR-014.
- **FR-019-4**: Every work line belongs to one section of its estimate or to no section. A single catalog work can be added to an existing section or to no section.
- **FR-019-5**: A Manager can remove a section from a Draft estimate, with confirmation (NFR-011). Its work lines and their material lines are removed with it.
- **FR-019-6**: Each section has a subtotal = the sum of its work lines' amounts (labour and material costs, BR-015). Lines without a section have their own subtotal. The subtotals add up to the direct costs; BR-016 is unchanged.
- **FR-019-7**: The Estimate Editor and the Excel export show the sections in the order added: a heading row (name, quantity, unit), the section's work lines with their material lines, and a subtotal row. Lines without a section come last under "Be skyriaus". An estimate without sections is shown and exported exactly as today (spec Q2).
- **FR-019-8**: A Viewer's names view lists each section's name with its work and material names (BR-005); nothing else is added.
- **FR-019-9**: Sections cannot be added, removed or changed while the estimate is Confirmed (BR-013).
- **FR-019-10**: A Manager can change a section's quantity in a Draft estimate. After a confirmation that warns that quantities changed by hand will be overwritten, every work line created from the catalog section gets quantity = new section quantity × its coefficient (rounded as in FR-019-3). Each estimate section therefore keeps each of its work lines' coefficients. A single work added to the section by hand has no coefficient and keeps its quantity (spec Q1).
- **FR-019-11**: Existing estimates keep all their lines, without a section, and their totals.

### Key Entities
- **Catalog section** (new, company catalog): name, unit, lines. Unique by name and unit within the company.
- **Catalog section line** (new): a catalog work and a coefficient (work quantity per 1 unit of the section), in order. The same work may appear on several lines.
- **Estimate section** (new): belongs to an estimate; a copy of the catalog section's name and unit, and the section quantity. Not linked to the catalog section (BR-010).
- **Estimate work line** (existing): may belong to one estimate section; a line created from a catalog section also keeps a copy of its coefficient, used when the section quantity changes (FR-019-10).

## Success Criteria

### Measurable Outcomes
- **SC-019-1**: A Manager adds a whole building element (e.g. 3–8 works with their materials) to an estimate in one step, in under 30 seconds, instead of adding each work separately.
- **SC-019-2**: The total of an estimate built from a catalog section equals, to the cent, the total of the same works added one by one with the same quantities (automated test).
- **SC-019-3**: For 100% of estimates, the section subtotals add up to the direct costs exactly (automated test).
- **SC-019-4**: 100% of existing estimates keep their totals and lines after the change (automated test on the worked example: 1263,26 €).
- **SC-019-5**: The exported file shows the same sections, rows and subtotals as the screen (automated test). A Viewer receives section names and nothing else new (automated role tests). All existing tests keep passing.

## Assumptions
- docs/requirements.md is updated before any code (Constitution VII.3). "templates" and "sections" are removed from Out of Scope, and new FRs, the BR-017 sentence on rounding a computed section quantity, and §10 entities are added. The instructor's approval is recorded in docs/review/change-log.md.
- Sections have one level (no sub-sections); the sample estimates use one level.
- No per-section overhead, profit or discount; percentages stay estimate-wide (BR-016).
- Moving a work line from one section to another, and reordering sections or lines, are not required ("Reordering estimate lines" is a §6 Could Have and stays out).
- A catalog section has no price of its own; its cost comes only from its works when added to an estimate.
- Catalog sections are company catalog data, visible and editable only by the Manager (BR-004).

## Clarifications

### Session 2026-11-17
- Q: What happens when the Manager changes an estimate section's quantity after it was added? → A: Every work line created from the catalog section is recalculated as new section quantity × its coefficient, after a confirmation warning that quantities changed by hand are overwritten. Works added to the section by hand keep their quantity.
- Q: How is an estimate with no sections (e.g. every existing estimate) shown? → A: Exactly as today, without section headings or a "Be skyriaus" group; that group appears only when the estimate has at least one section. (Decided during clarification as the low-impact default, so existing exports stay unchanged.)
