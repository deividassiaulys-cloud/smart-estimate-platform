# Feature 4: Material Catalog Management

## Specification
As a Manager, I create, edit, delete, and search materials (name, unit, price). Negative prices and duplicate name+unit combinations are rejected, and the unit must come from the fixed unit list (vnt., m, m², m³, kg, t, l, val., kompl.). Deleting a material requires confirmation and is rejected while the material is used in any work’s composition; the works are listed. Edge case: a material used only in existing estimates can be deleted and the estimates stay unchanged.

## User Stories
**US1 (P1):** As a Manager, I want to add a material with its unit and price, so that I can reuse it across multiple works.
**Independent verification:** A created material is selectable when building a work.

**US2 (P2):** As a Manager, I want to search materials by name, so that I can find them quickly in a large catalog.
**Independent verification:** Searching a partial name returns only matching materials.

**US3 (P2):** As a Manager, I want to delete a material I no longer need but be prevented from deleting one used in a work, so that no work composition refers to a missing material.
**Independent verification:** Deleting an unused material removes it; deleting a material used in a work is rejected and lists the works.

## Acceptance Scenarios
Given a Manager, when they submit a valid material with a unit from the fixed list, then it is saved and visible in the Materials list. Given a material that is not used in any work, when the Manager deletes it and confirms, then it is removed. Negative cases: a material with a name and unit that already exist is rejected as a duplicate; a negative price or a unit outside the fixed list is rejected; deleting a material used in a work is rejected and the works using it are listed; cancelling the delete confirmation changes nothing.

## Edge Cases
- Deleting a material used only in existing estimates does not change those estimates (they store their own copies, BR-010).
- Search is case-insensitive and matches part of the name.

## Clarifications
- Which measurement units are allowed? Only the fixed list from BR-008 (vnt., m, m², m³, kg, t, l, val., kompl.); users cannot add their own.
- What happens when a material that is used in a work is deleted? Deletion is rejected and the works using it are listed; the Manager must first remove the material from those works. Estimates are unaffected because they store their own copies (BR-010).
