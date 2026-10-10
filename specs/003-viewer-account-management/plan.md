# Plan: Viewer Account Management

## Constitution Check
No business logic in controllers or React components. No conflicts with the Constitution.

## Approach
Reuse the User entity (role = Viewer); Application layer scopes create/remove/reset actions to the acting Manager's company.
