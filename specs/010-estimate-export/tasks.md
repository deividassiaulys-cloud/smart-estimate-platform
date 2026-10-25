# Tasks: Estimate Export

- [ ] T105 Add ClosedXML dependency and export generation logic in server/SmartEstimatePlatform.Api/Services/EstimateExportService.cs
- [ ] T106 Add formula-injection sanitization for exported cell values in server/SmartEstimatePlatform.Api/Services/EstimateExportService.cs
- [ ] T107 Add GET /api/estimates/{id}/export endpoint (Manager-only) in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T108 Add Export Confirmation dialog (uses ConfirmDialog) in client/src/pages/EstimateEditor
- [ ] T109 Add integration test confirming exported data matches on-screen data in tests/server/EstimateExportServiceTests
- [ ] T110 Add integration test rejecting Viewer export requests in tests/server/EstimateExportServiceTests
