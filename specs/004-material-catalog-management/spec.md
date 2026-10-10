# Feature: Material Catalog Management

## Specification
As a Manager, I create, edit, delete, and search materials (name, unit, price). Negative prices and duplicate name+unit combinations are rejected.

## User Stories
**US1 (P1):** As a Manager, I want to add a material with its unit and price, so that I can reuse it across multiple works.
**Independent Verification:** A created material is selectable when building a work.

**US2 (P2):** As a Manager, I want to search materials by name, so that I can find them quickly in a large catalog.
**Independent Verification:** Searching a partial name returns only matching materials.

## Acceptance Scenarios
Given a Manager, when they submit a valid material, then it is saved and visible in the Materials list.

**Negative case:** when a Manager submits a material with a name and unit that already exist, the system rejects it as a duplicate.

## Edge Cases
- Deleting a material doesn't affect works/estimates already using it.
