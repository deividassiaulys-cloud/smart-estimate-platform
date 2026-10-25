# Feature 8: Estimate Status Workflow

## Specification
As a Manager, I confirm an estimate to lock it from edits, and can return it to Draft. Any edit attempt on a Confirmed estimate is rejected, by any means. New estimates always start as Draft. The status of every estimate is shown in the project’s estimate list (and hidden from Viewers).

## User Stories
**US1 (P1):** As a Manager, I want to confirm an estimate, so that its data becomes locked and safe to send to a client.
**Independent verification:** A confirmed estimate’s lines cannot be edited, even via a direct request.

**US2 (P2):** As a Manager, I want to return a confirmed estimate to Draft, so that I can make a necessary correction.
**Independent verification:** A returned estimate becomes editable again.

**US3 (P1):** As a Manager, I want to see each estimate’s status in the list, so that I can tell drafts from final versions at a glance.
**Independent verification:** The project’s estimate list shows Draft or Confirmed for every estimate.

## Acceptance Scenarios
Given a Draft estimate, when the Manager confirms it, then its status becomes Confirmed and all line edits are disabled. Given a project with several estimates, when the Manager opens it, then each estimate is listed with its status (Draft or Confirmed). Negative case: when an edit is attempted on a Confirmed estimate by any means (change quantity, add or remove a line, change a percentage), the change is rejected.

## Edge Cases
- Status changes ask for confirmation.
- A Viewer never receives the status.

## Clarifications
- Can a Confirmed estimate be deleted directly, or must it first be returned to Draft? Deleting a single estimate is not part of the Must Have requirements (FR-011 covers projects only); estimates are deleted together with their project (BR-011). If single-estimate deletion is added later, a Confirmed estimate can be deleted without returning it to Draft.
- Does returning to Draft require a reason or comment? No — a simple confirmation is enough for MVP.
