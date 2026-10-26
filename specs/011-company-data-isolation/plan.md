# Plan: Company Data Isolation

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

**Documented risk (Article IV):** login, registration and token validation must find users across all companies, so they bypass the company scoping. The bypass is allowed only inside AuthService, through explicitly named `IgnoreQueryFilters` calls, and a test asserts that no other code bypasses the scoping.

## Approach
The user’s company is taken only from their account: a per-request `ICurrentCompany` service reads it from the validated token, and token validation confirms the token’s company matches the user record. `AppDbContext` applies an EF Core global query filter to every company-owned entity (marked `ICompanyScoped`), so every query — including lookups of IDs sent in a request body — only ever sees the current company’s records; another company’s record is therefore simply “not found”. New company-owned records get their `CompanyId` from `ICurrentCompany` when saved; request DTOs never contain a `CompanyId`.

Services throw a single `NotFoundException` for a missing record, and one handler turns it into 404 (feature 012’s T111 later extends that handler with Lithuanian messages). New entities in features 003–010 are scoped just by implementing `ICompanyScoped`, and each feature adds cross-company tests for its endpoints using the shared HTTP test helper.

## Dependencies
- Microsoft.AspNetCore.Mvc.Testing (tests/server only): runs the API in-process (WebApplicationFactory) so isolation rules are covered by integration tests through the real token, routing and filters, as Article IV requires.
