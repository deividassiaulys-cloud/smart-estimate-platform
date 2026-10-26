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
- The Manager is listed on the Team page but cannot be removed, demoted, or have their password reset there.
- The Manager types the Viewer’s password both when creating the account and when resetting it; the same password rules as registration apply (at least 8 characters, not equal to the email). The Manager passes it on to the Viewer in person.
- Resetting a Viewer’s password immediately invalidates every token issued to that Viewer before the reset; their open sessions must log in again with the new password.
- Removing a Viewer deletes the account; its email becomes free to use again, and restoring access means creating the Viewer again.

## Clarifications
- Can a Viewer’s email match an email already used by a Manager in a different company? No — email is unique across the whole system, not just within one company.
- Is there a maximum number of Viewer accounts per company? No limit for MVP.

### Session 2026-10-26
- Q: How should a Viewer's password be set when the Manager creates the account, and when the Manager resets it? → A: The Manager types it in both cases; registration's password rules apply (at least 8 characters, not equal to the email).
- Q: When a Manager resets a Viewer's password, should the Viewer's devices that are already logged in be logged out? → A: Yes; a reset immediately invalidates all tokens issued before it.
- Q: When a Manager removes a Viewer, should the account be deleted from the database or only deactivated? → A: Deleted; the email becomes free again and restoring means creating the Viewer again.
