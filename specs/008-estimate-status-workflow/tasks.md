# Tasks: Estimate Status Workflow

- [ ] T064 Add Status field to Estimate entity in server/Models/Entities
- [ ] T065 Add EF Core migration for Status field in server/Migrations
- [ ] T066 [US1] Implement confirm transition and edit-lock enforcement in server/Services/EstimateService
- [ ] T067 [US2] Implement return-to-Draft transition in server/Services/EstimateService
- [ ] T068 [US1] Add PATCH /api/estimates/{id}/confirm endpoint in server/Controllers/EstimatesController
- [ ] T069 [US2] Add PATCH /api/estimates/{id}/draft endpoint in server/Controllers/EstimatesController
- [ ] T070 [US1] Add Confirm/Return-to-Draft actions with confirmation dialog in client/src/pages/EstimateEditor
- [ ] T071 [US1] Add integration test rejecting edits on a Confirmed estimate in tests/server/EstimateServiceTests
