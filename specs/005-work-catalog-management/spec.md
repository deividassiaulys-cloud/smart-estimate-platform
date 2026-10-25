# Feature 5: Work Catalog Management

## Specification
As a Manager, I create a work with a labour rate/norm and linked materials (consumption norm, waste %), so adding it to an estimate auto-generates all its materials. Waste % must be 0–100; a work can have zero materials; the same material may appear on several lines of one work; a work with the same name and unit as an existing one is rejected. I can search works by part of the name (case-insensitive), and deleting a work requires confirmation. Edge case: the same material can be reused across works.

## User Stories
**US1 (P1):** As a Manager, I want to create a work with labour rate/norm and linked materials, so that adding it to an estimate auto-generates all its materials.
**Independent verification:** A work with 2+ linked materials, added to an estimate, produces matching material lines.

**US2 (P2):** As a Manager, I want to search works by name, so that I can find a work quickly when the catalog grows.
**Independent verification:** Searching a partial name in any letter case returns only matching works.

## Acceptance Scenarios
Given a Manager, when they define a work with a labour rate/norm and linked materials, then the work is saved with its full composition, including the same material on more than one line. Given several works, when the Manager searches “siena” or “SIENA”, then every work whose name contains that text is listed regardless of letter case. Negative cases: a waste percentage outside 0–100 is rejected; a work with the same name and unit as an existing one is rejected; cancelling the delete confirmation keeps the work.

## Edge Cases
- The same material can be linked to several different works and can appear several times in one work (each line has its own norm and waste %).
- Work name + unit must be unique within the company; the same name with a different unit is allowed.
- 0 and 100 are valid waste percentages (both limits are inclusive).

## Clarifications
- Can the same material appear more than once in one work’s composition, if it’s used for two different purposes within that work? Yes — a work may include the same material as multiple separate lines, each with its own consumption norm and waste %, so different purposes stay visible and individually editable. This rule is now part of BR-009.
- Are 0 and 100 valid percentages? Yes, both limits are inclusive.
- Can two works have the same name with different units? Yes — name and unit together must be unique (BR-006).
