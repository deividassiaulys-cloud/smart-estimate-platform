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
- A Viewer sees every estimate of a project, Draft and Confirmed alike, listed the same way (name and an open action, as in wireframe wf12), so not even the status can be inferred from which estimates appear.
- An opened estimate shows the Viewer its name, then each work's name with the names of its materials listed under it, in the estimate's order. Nothing else is shown or sent: no units, quantities, norms, prices, percentages, totals or status. A material that appears more than once in a work is shown each time.
- The Viewer's data comes from separate read-only endpoints open to any logged-in user of the company: the project's estimate names (ID and name only) and an estimate's names (estimate name, works with their material names). Their response types have no number or status fields at all. The Manager's estimate endpoints stay Manager-only (403 for a Viewer) and the Manager's editor does not change.

## Clarifications
- Does a Viewer see the project name and description, or only the estimate’s work/material names? A Viewer sees the project name and description for navigation, but nothing commercial from the estimate itself.

### Session 2026-11-03
- Q: Does a Viewer see every estimate of a project, or only Confirmed ones? → A: Every estimate, Draft and Confirmed, shown the same way (name and open), so the status cannot be inferred (BR-005).
- Q: What does a Viewer see in an opened estimate? → A: The estimate name and each work name with its material names under it, in the estimate's order; no units, numbers or status; a repeated material is shown each time.
- Q: Separate Viewer endpoints, or the same endpoints shaped by role? → A: Separate read-only endpoints (project estimate names; estimate names) with response types that have no number or status fields; Manager endpoints unchanged and still 403 for Viewers.
