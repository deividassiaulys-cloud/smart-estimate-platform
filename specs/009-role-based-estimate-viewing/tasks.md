# Tasks: Role-Based Estimate Viewing

- [ ] T072 Implement role-based DTO projection (Viewer excludes price/quantity/status) in server/Services/EstimateService
- [ ] T073 Add Viewer-facing EstimateViewerDto in server/Models/DTOs
- [ ] T074 Add role check returning 403 for Manager-only endpoints in server/Controllers/WorksController, MaterialsController
- [ ] T075 Add Estimate View page (role-aware rendering) in client/src/pages/EstimateView
- [ ] T076 Add integration test confirming zero price/quantity fields in Viewer response in tests/server/EstimateServiceTests
