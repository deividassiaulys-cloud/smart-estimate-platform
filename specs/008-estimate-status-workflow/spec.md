# Feature: Estimate Status Workflow

## Specification
As a Manager, I confirm an estimate to lock it from edits, and can return it to Draft. Any edit attempt on a Confirmed estimate is rejected, by any means. New estimates always start as Draft.

## User Stories
**US1 (P1):** As a Manager, I want to confirm an estimate, so that its data becomes locked and safe to send to a client.
**Independent Verification:** A confirmed estimate's lines cannot be edited, even via a direct request.

**US2 (P2):** As a Manager, I want to return a confirmed estimate to Draft, so that I can make a necessary correction.
**Independent Verification:** A returned estimate becomes editable again.

## Acceptance Scenarios
Given a Draft estimate, when the Manager confirms it, then its status becomes Confirmed and all line edits are disabled.

**Negative case:** when an edit is attempted on a Confirmed estimate by any means, the change is rejected.

## Clarifications
- Can a Confirmed estimate be deleted directly, or must it first be returned to Draft? It can be deleted directly — status blocks editing of lines, not deletion.
- Does returning to Draft require a reason or comment? No — a simple confirmation is enough for MVP.
