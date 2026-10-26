# Plan: Viewer Account Management

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Reuse the User entity (role = Viewer). TeamService scopes list/create/remove/reset to the acting Manager’s company, using the company-scoping filter from feature 011 (404 for another company’s IDs). All Team endpoints are Manager-only (403 otherwise). Password rules live in one shared helper used by registration, Viewer creation and reset; duplicate emails reuse DuplicateEmailException. Removing a Viewer deletes the account; the existing per-request user check rejects its tokens at once. Resetting a password increments a TokenVersion on User; the version is carried in the JWT and a token with an old version is rejected. Removal uses the shared ConfirmDialog component.

## Dependencies
None beyond those added by features 002 and 011.
