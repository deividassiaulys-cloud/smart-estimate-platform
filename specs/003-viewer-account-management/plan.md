# Plan: Viewer Account Management

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Reuse the User entity (role = Viewer); the Application layer scopes create/remove/reset to the acting Manager’s company. Token validation checks that the user still exists, so a removed Viewer loses access at once. Removal uses the shared ConfirmDialog component.
