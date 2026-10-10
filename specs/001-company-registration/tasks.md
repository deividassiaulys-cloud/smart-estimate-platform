# Tasks: Company Registration

- [ ] T001 Add Company and User entities in server/Models/Entities
- [ ] T002 Add RegisterRequest/RegisterResponse DTOs in server/Models/DTOs
- [ ] T003 Add EF Core migration for Company/User in server/Migrations
- [ ] T004 Implement registration logic (create Company + Manager, hash password) in server/Services/AuthService
- [ ] T005 Reject duplicate email (case-insensitive) in server/Services/AuthService
- [ ] T006 Add POST /api/auth/register endpoint in server/Controllers/AuthController
- [ ] T007 Add Register page with validation in client/src/pages/Register
- [ ] T008 Add integration test for duplicate-email rejection in tests/server/AuthServiceTests
