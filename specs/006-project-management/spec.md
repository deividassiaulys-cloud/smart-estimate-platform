# Feature: Project Management

## Specification
As a Manager, I create, edit, and delete projects to group estimates. Deleting a project also deletes its estimates and requires confirmation.

## User Story (P1)
As a Manager, I want to create a project, so that I can group related estimates under one client or job.

**Independent Verification:** A created project appears in the Projects list and can hold estimates.

## Acceptance Scenarios
Given a Manager, when they submit a project name, then the project is created and visible.

**Negative case:** when a Manager deletes a project with existing estimates, all of its estimates are deleted along with it, and the action requires confirmation.
