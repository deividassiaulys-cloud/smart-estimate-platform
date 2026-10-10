# Tasks: Company Data Isolation

- [ ] T017 Implement a shared company-scoping authorization filter in server/Services/CompanyScopeFilter
- [ ] T018 Return 404 (not 403) for cross-company resource access by ID in server/Services/CompanyScopeFilter
- [ ] T019 Add integration test scaffold for the filter (generic, reusable across future controllers) in tests/server/CompanyIsolationTests

Note: this filter is applied to every controller as it is created in Features 3–10 (see T027, T035, T042, T049, T063 in the respective feature tasks.md files).
