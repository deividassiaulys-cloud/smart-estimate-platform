# Tasks: User Login

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T009 [US1] Add LoginRequest/LoginResponse DTOs in server/SmartEstimatePlatform.Api/Models/DTOs
- [ ] T010 [US1] Implement credential verification and JWT issuance in server/SmartEstimatePlatform.Api/Services/AuthService.cs
- [ ] T011 [US1] Add failed-attempt counter and 15-minute lockout logic in server/SmartEstimatePlatform.Api/Services/AuthService.cs
- [ ] T012 [US1] Add POST /api/auth/login endpoint in server/SmartEstimatePlatform.Api/Controllers/AuthController.cs
- [ ] T013 [US1] Add Login page with generic error message in client/src/pages/Login
- [ ] T014 [US1] Add integration test for lockout after 5 failed attempts in tests/server/AuthServiceTests
- [ ] T015 Add Landing Page in client/src/pages/Landing
- [ ] T016 Add Dashboard page (summary + role-based quick actions) in client/src/pages/Dashboard
- [ ] T017 [US2] Implement logout in the client: remove the stored JWT and redirect to Login, in client/src/utils/auth.ts and the navigation bar
- [ ] T018 [US2] Add frontend test: after logout a protected page redirects to Login in tests/client/LogoutTests
