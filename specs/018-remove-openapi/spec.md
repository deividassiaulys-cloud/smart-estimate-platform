# Feature 18: Remove the OpenAPI Document

**Feature Branch**: `018-remove-openapi`
**Created**: 2026-11-17
**Status**: Draft
**Input**: The API no longer generates an OpenAPI description. Remove its registration and endpoint, its package, and every response-type annotation in the controllers and their shared base (89 today). Nothing consumes the document: the React client uses its own TypeScript interfaces, there is no Swagger UI and no client generation. Every endpoint keeps exactly the same routes, status codes, bodies, authorization and validation; all existing tests pass unchanged. docs/architecture.md (API documentation row) and docs/review/change-log.md record the decision.

## Specification
The API publishes a machine-readable description of its endpoints, but no part of the system reads it. The client is written against its own types, there is no documentation viewer, and no code is generated from the description. Keeping it costs about 89 annotations that have to be maintained by hand on every endpoint. The annotations have no effect on what the API does, and instructor feedback item 11 already asked for this boilerplate to be reduced. This feature removes the description and everything that exists only to feed it, so the controllers show only what each endpoint does (Constitution X.1, X.2, X.8). Users notice nothing: every screen, request and response stays the same.

## User Scenarios & Testing

### User Story 1: The application behaves exactly as before (Priority: P1)
As a Manager or Viewer, I want every page and action to work as it does today, so that removing internal API documentation never affects my work.

**Why this priority**: the change must be invisible to users. A behaviour change would be a regression, not a cleanup.

**Independent Test**: run the full backend and frontend test suites. Every test passes without changing any test's expectations.

**Acceptance Scenarios**:
1. Given any existing endpoint, when it is called with the same request as before, then it returns the same status code and body. This includes the validation error (400), unauthorized (401), forbidden (403), not found (404), conflict (409) and locked (423) responses.
2. Given the full test suites, then they pass with the same number of tests as before this feature, and no test's expectations changed.
3. Given a Viewer, then they still receive only the data they received before (BR-005); authorization is unchanged.

### User Story 2: No unused API description is published or maintained (Priority: P1)
As the developer, I want the unused API description and its annotations gone, so that the controllers contain only the code that affects behaviour and need no parallel bookkeeping.

**Why this priority**: the purpose of the feature (YAGNI, Constitution X.1).

**Independent Test**: start the API in Development and request the old description address; search the server code for the annotations and the package.

**Acceptance Scenarios**:
1. Given the API running in Development, when `/openapi/v1.json` is requested, then it is not found (404), the same as any unknown address.
2. Given the server code and project file, then no response-type annotation, description registration or description package remains.
3. Given docs/architecture.md, then its API documentation row records that no generated API description is published and points to the feature contracts in specs/ as the endpoint documentation. docs/review/change-log.md has an entry for the decision.

### Edge Cases
- The automatic 400 validation response comes from the controllers' shared behaviour, not from the annotations, so it stays exactly as today (Constitution X.10).
- An endpoint that returns 201 or 204 sets that status in its own code, not through an annotation, so it is unchanged.
- The description was only served in Development, so production behaviour does not change at all.
- Earlier feature documents (e.g. specs/013 research and tasks) that mention the description are history and stay as they are.
- No dependency is added. One dependency is removed, so no new plan.md justification is needed (AGENTS.md).

## Requirements

### Functional Requirements
- **FR-018-1**: The API no longer registers, generates or serves an API description in any environment.
- **FR-018-2**: The packages that exist only to generate an API description are removed from the server project: the built-in OpenAPI package and Swashbuckle.AspNetCore (referenced since 2026-10-12 but never used, contrary to docs/architecture.md; found during implementation).
- **FR-018-3**: No controller or controller base declares response-type annotations; each controller action keeps its current behaviour (route, authorization, validation, status code, body).
- **FR-018-4**: Every existing automated test passes unchanged. The backend and frontend test counts equal those on `main` before this feature.
- **FR-018-5**: docs/architecture.md and docs/review/change-log.md record the decision and its reason; the feature contracts in specs/*/contracts remain the endpoint documentation.

### Key Entities
None: no data, entity or response shape changes.

## Success Criteria

### Measurable Outcomes
- **SC-018-1**: 100% of existing backend and frontend tests pass with unchanged expectations (counts recorded before and after).
- **SC-018-2**: 0 response-type annotations and 0 references to the API description remain in the server code and project file (89 annotations and 3 references today).
- **SC-018-3**: Requesting the old description address in Development returns "not found".
- **SC-018-4**: Users see no difference: the same screens and results for the same actions (covered by SC-018-1 and the unchanged frontend tests).

## Assumptions
- The student chose this removal, reversing the "API documentation" decision in docs/architecture.md, and confirmed that Independent Work 5 does not require a generated API description (spec Q1).
- The hand-written contracts in specs/*/contracts (e.g. specs/016-price-norm-precision/contracts/precision-api.md) are enough endpoint documentation for the project.
- No tool, script or external system reads `/openapi/v1.json` (checked: no test, client code, script or document uses it, apart from historic specs/013 notes).

## Clarifications

### Session 2026-11-17
- Q: Does Independent Work 5 (or the instructor) require a generated API description of the endpoints, which this feature removes? → A: No. Remove it; the hand-written contracts in specs/*/contracts are the endpoint documentation.
