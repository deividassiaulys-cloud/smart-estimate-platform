# Tasks: Role-Based Estimate Viewing

- [ ] T100 Implement role-based DTO projection (Viewer excludes price, quantity and status) in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T101 Add Viewer-facing EstimateViewerDto in server/SmartEstimatePlatform.Api/Models/DTOs
- [ ] T102 Add role check returning 403 for Manager-only endpoints in server/SmartEstimatePlatform.Api/Controllers/WorksController.cs and server/SmartEstimatePlatform.Api/Controllers/MaterialsController.cs
- [ ] T103 Add Estimate View page (role-aware rendering) in client/src/pages/EstimateView
- [ ] T104 Add integration test confirming zero price/quantity/status fields in the Viewer response in tests/server/EstimateServiceTests

> **Note from 008 (analysis C1):** until 009, Viewers get 403 on every estimate endpoint, so they never receive the status. The Viewer view added here must be tested directly to return no `status`, amounts, quantities, norms or percentages (BR-005, FR-026).
