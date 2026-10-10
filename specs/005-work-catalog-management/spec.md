# Feature: Work Catalog Management

## Specification
As a Manager, I create a work with a labour rate/norm and linked materials (consumption norm, waste %), so adding it to an estimate auto-generates all its materials. Waste % must be 0–100; a work can have zero materials.

## User Story (P1)
As a Manager, I want to create a work with labour rate/norm and linked materials, so that adding it to an estimate auto-generates all its materials.

**Independent Verification:** A work with 2+ linked materials, added to an estimate, produces matching material lines.

## Acceptance Scenarios
Given a Manager, when they define a work with a labour rate/norm and linked materials, then the work is saved with its full composition.

**Negative case:** when a Manager sets a waste percentage outside 0–100, the system rejects it.

## Edge Cases
- The same material can be reused across works.

## Clarifications
- Can the same material appear more than once in one work's composition, for two different purposes? Yes — a work may include the same material as multiple separate lines, each with its own consumption norm and waste %.
