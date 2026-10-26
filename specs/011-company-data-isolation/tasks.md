# Tasks: Company Data Isolation

Built right after Login. Every controller created in features 003–010 applies this filter as part of its own task.

- [x] T019 Add `ICurrentCompany` (company from the validated token, confirmed against the user record), the `ICompanyScoped` marker, EF Core global query filters for company-owned entities, and automatic `CompanyId` on save, in server/SmartEstimatePlatform.Api/Services and server/SmartEstimatePlatform.Api/Data/AppDbContext.cs; AuthService is the only code allowed to bypass the filters
- [x] T020 Add `NotFoundException` and one handler that returns 404 for it, so another company’s record (by URL or inside a request body) is reported exactly like an unknown ID, in server/SmartEstimatePlatform.Api/Services and server/SmartEstimatePlatform.Api/Program.cs
- [x] T021 Add a reusable HTTP integration test helper (WebApplicationFactory + SQLite, two companies with logged-in users) and the first isolation tests (users of another company are never visible; a token whose company does not match the user is rejected; only AuthService bypasses the scoping) in tests/server/CompanyIsolationTests
