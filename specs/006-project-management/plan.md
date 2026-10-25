# Plan: Project Management

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Standard CRUD; deleting a project cascades to its estimates in the Application layer. Project Detail lists the project’s estimates. Delete uses the shared ConfirmDialog.
