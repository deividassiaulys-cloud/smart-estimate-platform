# Feature: Company Data Isolation

## Specification
As the system, I guarantee no user can see or act on another company's data — a request for another company's resource is treated as not found, everywhere data can be requested.

## User Story (P1)
As any user, I want my requests to only ever return my own company's data, so that my business information stays confidential from other companies.

**Independent Verification:** A request for another company's resource (by ID) is treated as not found.

## Acceptance Scenarios
Given a user from one company, when they request a resource belonging to another company by its identifier, then the system responds as if it doesn't exist.

**Negative case:** when a user lists their projects, materials, or works, only their own company's records are returned.

## Success Criteria
- Automated tests cover every company-scoped access point with zero leaks.

## Clarifications
- Does "not found" apply to all entity types, or only some? Applies uniformly to all company-scoped entities, with no exceptions.
