# Tasks: Viewer Account Management

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T022 Add shared ConfirmDialog component (title, message, confirm and cancel buttons, keyboard accessible) in client/src/components/ConfirmDialog.tsx
- [ ] T023 [US1] Add CreateViewerRequest, ResetPasswordRequest and TeamMemberResponse DTOs in server/SmartEstimatePlatform.Api/Models/DTOs
- [ ] T117 [US1] Move the password rules (at least 8 characters, not equal to the email) into one shared helper in server/SmartEstimatePlatform.Api/Services, used by registration, Viewer creation and reset
- [ ] T024 [US1] Implement list/create/remove in server/SmartEstimatePlatform.Api/Services/TeamService.cs, scoped to the Manager’s company: list returns all company members (Manager included); create makes a Viewer and rejects duplicate emails (case-insensitive) and weak passwords; remove deletes only a Viewer (never the Manager)
- [ ] T026 [US2] Add TokenVersion to User with an EF Core migration; include it in the JWT and reject tokens whose version no longer matches, in Models/Entities, Migrations, Services/TokenService.cs, Services/AuthService.cs and Program.cs
- [ ] T025 [US2] Implement password reset in server/SmartEstimatePlatform.Api/Services/TeamService.cs: apply the password rules, re-hash, and increment TokenVersion (Viewers of the Manager’s company only)
- [ ] T027 [US1] Add GET /api/team, POST /api/team/viewers and DELETE /api/team/viewers/{id} in server/SmartEstimatePlatform.Api/Controllers/TeamController.cs (Manager role only, CompanyScopeFilter applied)
- [ ] T028 [US2] Add PATCH /api/team/viewers/{id}/password in server/SmartEstimatePlatform.Api/Controllers/TeamController.cs (Manager role only, CompanyScopeFilter applied)
- [ ] T029 [US1] Add Team List page (/team) and Viewer Account Form (/team/new, plus the reset variant with only "New password" and a "Password updated." banner) in client/src/pages/Team; removal asks for confirmation with ConfirmDialog; /team is Manager-only in the client; the Dashboard "Komanda" action links to /team
- [ ] T030 [US1] Add integration tests in tests/server/TeamServiceTests: Viewer created in the Manager’s company with role Viewer; duplicate email (any case) rejected; weak password rejected on create and reset; Viewer logs in with the new password after a reset; a token issued before a reset is rejected; a removed Viewer cannot log in and their token is rejected; the Manager cannot be removed; a Viewer calling Team endpoints gets 403
- [ ] T031 Add integration test confirming cross-company Viewer access (list, remove, reset) returns 404, in tests/server/CompanyIsolationTests
- [ ] T118 Add frontend test: confirming in ConfirmDialog runs the action and cancelling keeps the Viewer, in tests/client/ConfirmDialogTests
