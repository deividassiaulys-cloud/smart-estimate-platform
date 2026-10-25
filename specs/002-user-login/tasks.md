# Tasks: User Login

`[US#]` refers to the user stories of this feature in `spec.md`.

- [x] T009 [US1] Add LoginRequest/LoginResponse DTOs in server/SmartEstimatePlatform.Api/Models/DTOs
- [x] T019 [US1] Add FailedLoginCount, FirstFailedLoginAt and LockoutEnd to User, with an EF Core migration, in server/SmartEstimatePlatform.Api/Models/Entities and server/SmartEstimatePlatform.Api/Migrations
- [x] T010 [US1] Implement credential verification (case-insensitive email, same generic error for unknown email or wrong password) and JWT issuance via TokenService in server/SmartEstimatePlatform.Api/Services/AuthService.cs
- [x] T011 [US1] Add failed-attempt counting (streak from first failure, resets after 10 minutes or on success) and 15-minute lockout returning minutes remaining in server/SmartEstimatePlatform.Api/Services/AuthService.cs
- [x] T012 [US1] Add POST /api/auth/login endpoint (401 generic error, 423 locked with minutes remaining) in server/SmartEstimatePlatform.Api/Controllers/AuthController.cs
- [x] T020 [US1] Configure JWT Bearer validation that rejects tokens of users who no longer exist (with the user-exists check in AuthService), and add GET /api/auth/me returning name, company name and role, in server/SmartEstimatePlatform.Api/Program.cs, Services/AuthService.cs and Controllers/AuthController.cs
- [x] T014 [US1] Add integration tests in tests/server/AuthServiceTests: generic error for unknown email and wrong password; lock after 5 failures in 10 minutes; streak resets after 10 minutes and after a successful login; locked attempt with correct password is rejected with minutes remaining; login works again after 15 minutes; /me returns the user's name, company name and role; a token for a user who no longer exists is rejected
- [x] T021 Add client routing (react-router-dom) with a protected-route guard, and frontend test setup (vitest, Testing Library, `npm run test`), in client/src and client/package.json
- [x] T013 [US1] Add Login page with generic error message and locked message showing minutes remaining in client/src/pages/Login
- [x] T015 Add Landing Page linking to Login and Register in client/src/pages/Landing
- [x] T016 Add Dashboard page (greeting with name, company and role; role-based quick actions, disabled until their page exists; Log out in the header) in client/src/pages/Dashboard
- [x] T017 [US2] Implement logout and session expiry in the client: remove the stored JWT and redirect to Login on Log out or on a 401 (with "Your session has expired, please log in again."), in client/src/utils/auth.ts, client/src/api and the Dashboard header
- [x] T018 [US2] Add frontend test: after logout a protected page redirects to Login in tests/client/LogoutTests
