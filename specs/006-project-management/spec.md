# Feature 6: Project Management

## Specification
As a Manager, I create, edit, and delete projects to group estimates. Project Detail lists the project’s estimates. Deleting a project also deletes its estimates and requires confirmation. Estimates arrive in feature 007, so in 006 the Project Detail shows the project's name and description with "Sąmatų dar nėra.", and the estimate list and cascade delete of estimates (with its test) are delivered in 007.

## User Stories
**US1 (P1):** As a Manager, I want to create a project, so that I can group related estimates under one client or job.
**Independent verification:** A created project appears in the Projects list and its Project Detail shows its name and description.

## Acceptance Scenarios
Given a Manager, when they submit a project name, then the project is created and visible. Given a project, when the Manager changes its name or description and saves, then the change is shown. Given a project, when the Manager deletes it and confirms, then it is removed; when the confirmation is cancelled, nothing is deleted. Deleting the project's estimates along with it is verified in feature 007. Given a Viewer, when they open Projects, then they see the company's projects (name and description) but no create, edit or delete actions, and any create, edit or delete request from a Viewer gets 403.

## Edge Cases
- Project deletion is permanent; there is no undo.
- A project has a name (required, trimmed, at most 200 characters) and an optional description (at most 2000 characters); an empty or whitespace-only name is rejected. The description is stored trimmed, and a whitespace-only description is stored as no description.
- Project names are not unique: two projects of the same company may have the same name.

## Clarifications

### Session 2026-11-02
- Q: Which fields does a project have? → A: Name (required, trimmed, at most 200 characters) and description (optional, at most 2000 characters).
- Q: What can Viewers do with projects? → A: View only (list and Project Detail: name and description); create, edit and delete are Manager-only (403 for Viewers, no buttons).
- Q: Must a project name be unique within the company? → A: No; the same name is allowed, projects are told apart by description and ID.
- Q: Estimates only arrive in feature 007; what does 006 do with the estimate list and the cascade delete? → A: Deferred to 007. In 006 the Project Detail shows name, description and "Sąmatų dar nėra."; the estimate list, cascade delete and its test move to 007 (as 004's in-use check moved to 005).
