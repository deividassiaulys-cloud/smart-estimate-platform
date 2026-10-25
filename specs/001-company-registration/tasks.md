# Tasks: Company Registration

- [x] T001 Add Company and User entities in server/SmartEstimatePlatform.Api/Models/Entities
- [x] T002 Add RegisterRequest/RegisterResponse DTOs in server/SmartEstimatePlatform.Api/Models/DTOs
- [x] T003 Add EF Core migration for Company/User in server/SmartEstimatePlatform.Api/Migrations
- [x] T004 Implement registration logic (create Company + Manager in one transaction, hash password, enforce password rules: at least 8 characters and not equal to the email) in server/SmartEstimatePlatform.Api/Services/AuthService.cs
- [x] T005 Reject duplicate email (case-insensitive) in server/SmartEstimatePlatform.Api/Services/AuthService.cs
- [x] T006 Add POST /api/auth/register endpoint in server/SmartEstimatePlatform.Api/Controllers/AuthController.cs
- [x] T007 Add Register page with validation in client/src/pages/Register
- [x] T008 Add integration tests for duplicate-email rejection and weak-password rejection in tests/server/AuthServiceTests
