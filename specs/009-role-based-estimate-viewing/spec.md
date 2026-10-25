# Feature 9: Role-Based Estimate Viewing

## Specification
As a Viewer, I see only work and material names — never prices, quantities, or status, regardless of how I ask. Edge case: holds no matter how the estimate is reached. Success criteria: zero price/quantity data ever reaches a Viewer, verified by automated tests.

## User Stories
**US1 (P1):** As a Viewer, I want to see an estimate’s work and material names, so that I know what’s needed on site.
**Independent verification:** A Viewer opening an estimate sees only names, no numbers or status.

## Acceptance Scenarios
Given a Viewer opening an estimate, when the page loads, then only work and material names are shown, with no quantities, prices, or status. Negative case: when a Viewer requests the same data directly, bypassing the normal screen, no price or quantity data is returned.

## Edge Cases
- This holds no matter how the estimate is reached.

## Clarifications
- Does a Viewer see the project name and description, or only the estimate’s work/material names? A Viewer sees the project name and description for navigation, but nothing commercial from the estimate itself.
