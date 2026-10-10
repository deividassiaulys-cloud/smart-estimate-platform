# Tasks: Estimate Export

- [ ] T077 Add ClosedXML dependency and export generation logic in server/Services/EstimateExportService
- [ ] T078 Add formula-injection sanitization for exported cell values in server/Services/EstimateExportService
- [ ] T079 Add GET /api/estimates/{id}/export endpoint (Manager-only) in server/Controllers/EstimatesController
- [ ] T080 Add Export Confirmation action in client/src/pages/EstimateEditor
- [ ] T081 Add integration test confirming exported data matches on-screen data in tests/server/EstimateExportServiceTests
- [ ] T082 Add integration test rejecting Viewer export requests in tests/server/EstimateExportServiceTests
