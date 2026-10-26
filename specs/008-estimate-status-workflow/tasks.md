# Tasks: Estimate Status Workflow

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T089 [US1] Add Status field (default Draft) to the Estimate entity in server/SmartEstimatePlatform.Api/Models/Entities
- [ ] T090 [US1] Add EF Core migration for the Status field in server/SmartEstimatePlatform.Api/Migrations
- [ ] T091 [US1] Implement confirm transition and edit-lock enforcement for every write operation in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T092 [US2] Implement return-to-Draft transition in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T093 [US3] Include Status in the Manager’s estimate list and omit it for Viewers in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T094 [US1] Add PATCH /api/estimates/{id}/confirm endpoint in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T095 [US2] Add PATCH /api/estimates/{id}/draft endpoint in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T096 [US1] Add Confirm / Return-to-Draft actions with ConfirmDialog in client/src/pages/EstimateEditor
- [ ] T097 [US3] Show the estimate status (text, not colour only) in the estimate list of Project Detail in client/src/pages/Projects
- [ ] T098 [US1] Add integration tests rejecting add line, change quantity, remove line and change percentage on a Confirmed estimate in tests/server/EstimateServiceTests
- [ ] T099 [US3] Add test that the estimate list shows the status for a Manager in tests/server/EstimateServiceTests
