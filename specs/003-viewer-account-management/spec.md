# Feature 3: Viewer Account Management

## Specification
As a Manager, I create, remove, and reset the password of Viewer accounts, so workers can access project data without self-registering. Removing a Viewer requires confirmation and takes effect immediately, also for sessions that are already open. Edge case: Viewer accounts are scoped to the creating Manager’s company only.

## User Stories
**US1 (P1):** As a Manager, I want to create a Viewer account, so that I can give a worker access to project data without them self-registering.
**Independent verification:** A newly created Viewer account can log in and see only permitted data.

**US2 (P2):** As a Manager, I want to reset a Viewer’s password, so that I can restore their access without email-based recovery.
**Independent verification:** A Viewer logs in successfully after their password is reset.

## Acceptance Scenarios
Given a Manager on the Team page, when they submit valid details for a new Viewer, then the Viewer account is created and listed. Negative cases: when a Manager removes a Viewer and confirms, that Viewer can no longer log in and an already issued token is rejected; when the Manager cancels the confirmation, the Viewer is kept.

## Edge Cases
- Viewer accounts are scoped to the creating Manager’s company only.
- A Manager cannot create a Viewer with their own email.

## Clarifications
- Can a Viewer’s email match an email already used by a Manager in a different company? No — email is unique across the whole system, not just within one company.
- Is there a maximum number of Viewer accounts per company? No limit for MVP.
