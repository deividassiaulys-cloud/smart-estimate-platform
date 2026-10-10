# Feature: Viewer Account Management

## Specification
As a Manager, I create, remove, and reset the password of Viewer accounts, so workers can access project data without self-registering.

## User Stories
**US1 (P1):** As a Manager, I want to create a Viewer account, so that I can give a worker access to project data without them self-registering.
**Independent Verification:** A newly created Viewer account can log in and see only permitted data.

**US2 (P2):** As a Manager, I want to reset a Viewer's password, so that I can restore their access without email-based recovery.
**Independent Verification:** A Viewer logs in successfully after their password is reset.

## Acceptance Scenarios
Given a Manager on the Team page, when they submit valid details for a new Viewer, then the Viewer account is created and listed.

**Negative case:** when a Manager removes a Viewer, that Viewer can no longer log in.

## Edge Cases
- Viewer accounts are scoped to the creating Manager's company only.

## Clarifications
- Can a Viewer's email match an email already used by a Manager in a different company? No — email is unique across the whole system.
- Is there a maximum number of Viewer accounts per company? No limit for MVP.
