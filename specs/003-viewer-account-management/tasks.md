# Tasks: Viewer Account Management

- [ ] T020 [US1] Add CreateViewerRequest DTO in server/Models/DTOs
- [ ] T021 [US1] Implement create/remove Viewer logic, scoped to Manager's company, in server/Services/TeamService
- [ ] T022 [US2] Implement password reset logic in server/Services/TeamService
- [ ] T023 [US1] Add POST /api/team/viewers, DELETE /api/team/viewers/{id} endpoints in server/Controllers/TeamController (apply CompanyScopeFilter)
- [ ] T024 [US2] Add PATCH /api/team/viewers/{id}/password endpoint in server/Controllers/TeamController
- [ ] T025 [US1] Add Team List page and Viewer Account Form in client/src/pages/Team
- [ ] T026 [US1] Add integration test confirming a removed Viewer cannot log in in tests/server/TeamServiceTests
- [ ] T027 Add integration test confirming cross-company Viewer access is blocked in tests/server/CompanyIsolationTests
