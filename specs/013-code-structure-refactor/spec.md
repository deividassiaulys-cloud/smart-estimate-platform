# Feature Specification: Code Structure Refactor (Article X)

**Feature Branch**: `013-code-structure-refactor`
**Created**: 2026-11-02
**Status**: Draft
**Input**: Instructor feedback (docs/review/instructor-feedback.md) and Constitution Article X (v1.2.0, including X.9 and X.10). Restructure the existing code of features 001–004 without changing what users see; the API keeps its routes, status codes and response fields, with the exceptions listed in FR-014.

## User Scenarios & Testing *(mandatory)*

The "users" of this feature are the project owner and the instructor who review and extend the code. End users (Managers and Viewers) must notice no difference.

### User Story 1 - Backend follows Article X (Priority: P1)

As the project owner, I want the backend of features 001–004 restructured per Article X, so that every new feature can follow one consistent pattern and the instructor's remarks 1–8, 11 and 12 are resolved.

**Why this priority**: Features 005+ will copy the existing backend structure; fixing it first prevents the problems from spreading.

**Independent Test**: Run the full backend test suite and the new error-response tests; review the code against Article X.2–X.6 and X.8.

**Acceptance Scenarios**:

1. **Given** the refactored backend, **When** the existing backend tests run, **Then** all pass and no test assertion was changed (only test setup or object construction may change). Exceptions: tests that expect a service to reject an invalid request move to request-validation tests that reject the same inputs (FR-009); a test that reads a service result may read the equivalent DTO field instead of the entity field (e.g. `UserId` instead of `Id`, `CompanyName` instead of `Company.Name`) with the same expected value.
2. **Given** a request that fails (duplicate email, duplicate material, wrong credentials, locked account, record of another company), **When** it is sent before and after the refactor, **Then** the status code, error title, error text and extra fields (minutes remaining for a locked account) are the same.
4. **Given** an invalid request (weak password, invalid material name, unit or price, missing field), **When** it is sent before and after the refactor, **Then** it is rejected with status 400 in both cases and nothing is saved; after the refactor the response lists the errors per field.
3. **Given** the refactored data model, **When** the database model is compared with the latest migration, **Then** there are no differences and no new migration exists.

---

### User Story 2 - Frontend follows Article X (Priority: P2)

As the project owner, I want the frontend styled only with Tailwind utility classes and split into one component per page section, so that the instructor's remarks 9 and 10 are resolved.

**Why this priority**: Styling and component structure do not affect correctness, but every new page will copy them.

**Independent Test**: Run the frontend tests, lint and production build; open every page and compare it with the current version.

**Acceptance Scenarios**:

1. **Given** the refactored frontend, **When** the frontend tests, lint and production build run, **Then** all pass.
2. **Given** any existing page (Landing, Login, Register, Dashboard, Team, Viewer form, Reset password, Materials, Material form, the delete confirmation dialog), **When** a user performs the same actions as before, **Then** the same content, links, buttons, messages and navigation are available.
3. **Given** the client source, **When** it is searched for stylesheet files, **Then** only the single Tailwind entry stylesheet remains, containing only the Tailwind import and the theme tokens.

---

### Edge Cases

- A failed login on a locked account still returns the minutes remaining in the error response.
- A record of another company is still reported exactly like a record that does not exist (feature 011).
- A password reset still invalidates earlier tokens and lifts the lockout (feature 003).
- When a Manager resets a Viewer's password, the request does not contain the Viewer's email, so the "password must not equal the email" check needs stored data and stays in the service. It still uses the one shared password-rule definition (FR-009).
- Duplicate email and duplicate material checks need stored data and stay in the services (409).
- The framework's own cancellation parameters (exception handler, database context save) stay, because they come from the framework's signatures and are not added by the project (FR-010).
- A concurrent duplicate email or duplicate material is still reported as a duplicate, not as a server error.
- The merged login/registration response keeps exactly the same fields, so the client needs no change for it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every application service MUST have its own interface and be registered in one application-services registration method; consumers MUST depend on the interface (X.3, remark 8).
- **FR-002**: Every DTO MUST be a record declared in its own file under the folder of its feature, MUST NOT reference entity classes, and MUST be mapped from entities in one mapper (X.4, X.5, remarks 2–4). The role enumeration may stay with the entities and be referenced by DTOs (owner decision).
- **FR-003**: DTOs with identical content MUST be merged whenever possible; the registration and login responses become one response (X.1, remark 12).
- **FR-004**: Configuration options MUST be records; they MUST keep binding from configuration and their start-up validation (X.5).
- **FR-005**: Controller actions MUST only bind the request, make one service call and return its result; they MUST NOT contain error handling, mapping or claim parsing (X.2, remark 1). An action that only returns a fixed constant list (the measurement units) needs no service, because it contains no logic (X.1).
- **FR-006**: Domain errors MUST be turned into HTTP error responses in one place, with the same status codes, titles, texts and extra fields as before (X.8).
- **FR-007**: Response metadata common to all endpoints MUST be declared once and reused (X.8, remark 11).
- **FR-008**: Entity properties MUST have private setters; state MUST be set through constructors and simple mutators without business rules. Lockout and password rules stay in the services (X.6, remark 5, Decision A1).
- **FR-009**: Request validation MUST be declared on the request records, with one shared definition per rule (for example the password rules: at least 8 characters, not equal to the email); services MUST NOT repeat it. Only checks that need stored data (duplicates, a reset password versus the Viewer's stored email) stay in the services, using the same shared rule definition. The client keeps its documented mirror for immediate feedback in Lithuanian (X.10, remark 7).
- **FR-010**: Cancellation tokens MUST be removed from the project's own controllers and services, because no specification requires cancellation of a long-running operation (X.9, remark 6).
- **FR-011**: The frontend MUST be styled only with Tailwind utility classes; all page and component stylesheets, including the unused application stylesheet, MUST be removed, keeping one Tailwind entry stylesheet that holds only the Tailwind import and a theme block with the shared colour and font tokens; it contains no other style rules (X.7, remark 9).
- **FR-012**: The Dashboard header, user summary and quick actions, and the distinct sections of the Materials and Team pages MUST be extracted into their own components, one component per file (X.7, remark 10).
- **FR-013**: Tests that pin down the current error responses (400, 401, 404, 409, 423) MUST be added before any production code is changed.
- **FR-014**: The refactor MUST NOT change behaviour: same routes, status codes, response field names, accepted and rejected inputs, authorization and company isolation, and the same error texts for 401, 404, 409 and 423. Allowed differences: (a) the name of the merged auth response in the API description; (b) the body of a 400 response for an invalid request becomes the standard per-field validation-error format (status and rejected inputs stay the same; the client reacts only to the status).

### Key Entities

- **Company, User, Material**: unchanged data and database schema; only how their values may be set changes (FR-008).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the existing automated tests (all backend tests as counted in the baseline run, and the 2 frontend test files) pass with unchanged assertions, except the 2 service-level tests that expect a weak password to be rejected (one is a theory with 2 cases); they move to request-validation tests with the same inputs and the same outcome; and assertions that read a service result may read the equivalent DTO field instead of the entity field, with the same expected value.
- **SC-002**: 0 differences between the data model and the latest migration; 0 new migrations.
- **SC-003**: 0 differences in status code, title, text and extra fields across the 401/404/409/423 error-response tests before and after the refactor; 0 differences in status code and rejected inputs for 400.
- **SC-004**: 0 try/catch blocks and 0 DTO constructions in controllers; 0 DTOs referencing entity classes; 0 services without an interface; 0 public entity setters; 0 cancellation tokens in the project's own controllers and services; 0 request-only validation checks in services.
- **SC-005**: 1 stylesheet left in the client (the Tailwind entry); lint and production build finish with 0 errors.
- **SC-006**: All 12 instructor remarks are marked resolved (or decided) in docs/review/instructor-feedback.md.

## Assumptions

- Article X is part of the constitution (v1.1.0 in PR #9; X.9 and X.10 added in v1.2.0 on this branch); this branch is created on top of PR #9.
- Visual details may shift slightly when moving to Tailwind (base style reset); this is accepted as long as content, layout order and behaviour stay the same, checked manually page by page.
- Adding Tailwind is the only new dependency; its justification goes into plan.md (Article III).
- Test setup and object construction may change (e.g. building users through constructors); test assertions may not.
- Features 005–012 are not implemented yet and are out of scope.

## Clarifications

### Session 2026-11-02

- Q: Should entities get private setters, and where do the lockout and password rules live then? → A: Private setters with constructors and simple mutators without business rules; the rules stay in the services (Decision A1).
- Q: Should cancellation tokens stay in controllers and services? → A: No; removed, because no specification requires cancellation of a long-running operation (Constitution X.9, replaces Decision B1).
- Q: Where should request validation such as the password rules live? → A: Declared on the request records with one shared definition per rule; services do not repeat it; checks that need stored data stay in services (Constitution X.10, replaces Decision C1).
- Q: May DTOs reference the role enumeration that lives with the entities? → A: Yes; `UserRole` stays in Models/Entities and DTOs may reference it.
- Q: Should DTOs with identical content be merged? → A: Yes, whenever possible; registration and login responses become one response.
- Q: Is Tailwind mandatory for the frontend, and does the order of component extraction and the Tailwind migration matter? → A: Tailwind is mandatory; the order is free.
- Q: When services return DTOs instead of entities, may existing tests read the DTO's field in place of the entity's, with the same expected value? → A: Yes; only the access path changes, the expected values stay the same.
- Q: Does the units endpoint, which only returns the fixed list of measurement units, have to go through a service? → A: No; returning a fixed constant list counts as no logic, so no unit service is added (YAGNI).
- Q: Besides the Tailwind import, may the single remaining stylesheet hold the shared colour and font tokens in a theme block? → A: Yes; the import plus a theme block with the tokens, and no other style rules.
