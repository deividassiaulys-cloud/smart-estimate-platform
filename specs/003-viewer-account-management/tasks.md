# Tasks: Viewer Account Management

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T022 Add shared ConfirmDialog component (title, message, confirm and cancel buttons, keyboard accessible) in client/src/components/ConfirmDialog.tsx
- [ ] T023 [US1] Add CreateViewerRequest DTO in server/SmartEstimatePlatform.Api/Models/DTOs
- [ ] T024 [US1] Implement create/remove Viewer logic, scoped to the Manager’s company, in server/SmartEstimatePlatform.Api/Services/TeamService.cs
- [ ] T025 [US2] Implement password reset logic in server/SmartEstimatePlatform.Api/Services/TeamService.cs
- [ ] T026 [US1] Reject requests that carry a valid JWT of a removed user (check that the user still exists when validating the token) in server/SmartEstimatePlatform.Api/Program.cs
- [ ] T027 [US1] Add POST /api/team/viewers and DELETE /api/team/viewers/{id} endpoints in server/SmartEstimatePlatform.Api/Controllers/TeamController.cs (apply CompanyScopeFilter)
- [ ] T028 [US2] Add PATCH /api/team/viewers/{id}/password endpoint in server/SmartEstimatePlatform.Api/Controllers/TeamController.cs
- [ ] T029 [US1] Add Team List page and Viewer Account Form (removal asks for confirmation with ConfirmDialog) in client/src/pages/Team
- [ ] T030 [US1] Add integration test: a removed Viewer cannot log in and an already issued token is rejected, in tests/server/TeamServiceTests
- [ ] T031 Add integration test confirming cross-company Viewer access is blocked in tests/server/CompanyIsolationTests
