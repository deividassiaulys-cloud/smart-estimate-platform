# Feature 6: Project Management

## Specification
As a Manager, I create, edit, and delete projects to group estimates. Project Detail lists the project’s estimates. Deleting a project also deletes its estimates and requires confirmation.

## User Stories
**US1 (P1):** As a Manager, I want to create a project, so that I can group related estimates under one client or job.
**Independent verification:** A created project appears in the Projects list and can hold estimates.

## Acceptance Scenarios
Given a Manager, when they submit a project name, then the project is created and visible. Negative case: when a Manager deletes a project with existing estimates and confirms, all of its estimates are deleted along with it; when the confirmation is cancelled, nothing is deleted.

## Edge Cases
- Project deletion is permanent; there is no undo.
