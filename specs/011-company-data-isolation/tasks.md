# Tasks: Company Data Isolation

Built right after Login. Every controller created in features 003–010 applies this filter as part of its own task.

- [ ] T019 Implement a shared company-scoping authorization filter in server/SmartEstimatePlatform.Api/Services/CompanyScopeFilter.cs
- [ ] T020 Return 404 (not 403) for cross-company resource access by ID in server/SmartEstimatePlatform.Api/Services/CompanyScopeFilter.cs
- [ ] T021 Add integration test scaffold for the filter (generic, reusable across controllers) in tests/server/CompanyIsolationTests
