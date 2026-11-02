# Feature 5: Work Catalog Management

## Specification
As a Manager, I create and edit a work with a labour rate/norm and linked materials (consumption norm, waste %), so adding it to an estimate auto-generates all its materials. Waste % must be 0–100; a work can have zero materials; the same material may appear on several lines of one work; a work with the same name and unit as an existing one is rejected. I can search works by part of the name (case-insensitive), and deleting a work requires confirmation. Edge case: the same material can be reused across works.

## User Stories
**US1 (P1):** As a Manager, I want to create a work with labour rate/norm and linked materials, so that adding it to an estimate auto-generates all its materials.
**Independent verification:** A saved work with 2+ lines, including the same material twice, is returned with exactly that composition; generating estimate lines from it is verified in feature 007.

**US2 (P2):** As a Manager, I want to search works by name, so that I can find a work quickly when the catalog grows.
**Independent verification:** Searching a partial name in any letter case returns only matching works.

## Acceptance Scenarios
Given a Manager, when they define a work with a labour rate/norm and linked materials, then the work is saved with its full composition, including the same material on more than one line. Given several works, when the Manager searches “siena” or “SIENA”, then every work whose name contains that text is listed regardless of letter case. Negative cases: a waste percentage outside 0–100 is rejected; a consumption norm of 0 or less, a negative labour norm or rate, or a norm with more than 3 decimals is rejected; a work with the same name and unit as an existing one is rejected; cancelling the delete confirmation keeps the work. Given an existing work, when the Manager changes its fields or material lines and saves, then the saved composition is exactly the one in the form, and the same validation and duplicate name+unit rules apply as on create. A Viewer calling any work endpoint gets 403 and sees no Works link or page.

## Edge Cases
- The same material can be linked to several different works and can appear several times in one work (each line has its own norm and waste %).
- Work name + unit must be unique within the company; the same name with a different unit is allowed.
- 0 and 100 are valid waste percentages (both limits are inclusive). Waste % has at most 2 decimal places (DECIMAL(5,2)); more decimals are rejected, not rounded.
- A work has a labour rate in €/h (≥ 0, at most 2 decimal places) and a labour norm in hours per unit of work (up to 3 decimal places); the labour cost per unit of work is norm × rate.
- The labour norm may be 0 (a work without labour, e.g. material supply); a material line's consumption norm must be greater than 0. Both norms have at most 3 decimal places and fit DECIMAL(18,3); more decimals are rejected, not rounded.

## Clarifications
- Can the same material appear more than once in one work’s composition, if it’s used for two different purposes within that work? Yes — a work may include the same material as multiple separate lines, each with its own consumption norm and waste %, so different purposes stay visible and individually editable. This rule is now part of BR-009.
- Are 0 and 100 valid percentages? Yes, both limits are inclusive.
- Can two works have the same name with different units? Yes — name and unit together must be unique (BR-006).

### Session 2026-11-02
- Q: What do a work's labour fields mean? → A: Two fields: a labour rate in €/h (money, ≥ 0, at most 2 decimals) and a labour norm in hours per unit of work (up to 3 decimals); labour cost per unit = norm × rate.
- Q: Can a Manager edit an existing work, including its material lines? → A: Yes; every field and line can be edited, saving replaces the whole composition with the one in the form, and the duplicate and validation rules apply as on create.
- Q: Can Viewers see the work catalog? → A: No, Managers only. Every /api/works endpoint returns 403 to Viewers, and Viewers get no Works link or page (works contain labour prices; feature 009).
- Q: Must the labour norm and a material line's consumption norm be greater than 0? → A: Labour norm ≥ 0 (0 = no labour); consumption norm > 0; both at most 3 decimals and within DECIMAL(18,3).
