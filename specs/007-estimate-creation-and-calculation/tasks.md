# Tasks: Estimate Creation and Calculation

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T065 Add Estimate, EstimateWorkLine and EstimateMaterialLine entities (lines hold copied names, units, prices and norms; no foreign keys to the catalog) in server/SmartEstimatePlatform.Api/Models/Entities
- [ ] T066 Configure decimal precision: money DECIMAL(18,2), quantities and norms DECIMAL(18,3), percentages DECIMAL(5,2), in server/SmartEstimatePlatform.Api/Data/AppDbContext.cs
- [ ] T067 Add EF Core migration for the estimate tables in server/SmartEstimatePlatform.Api/Migrations
- [ ] T068 [US2] Add the MoneyRounding helper (2 decimals, MidpointRounding.AwayFromZero) used for every money amount in server/SmartEstimatePlatform.Api/Services/MoneyRounding.cs
- [ ] T069 [US1] Implement material-line generation (quantity × consumption norm × (1 + waste %)) in server/SmartEstimatePlatform.Api/Services/EstimateCalculationService.cs
- [ ] T070 [US1] Implement add-work-to-estimate logic, copying price/norm at add-time, in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T071 [US1] Add quantity > 0 validation in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T072 [US2] Implement total recalculation (sequential overhead, profit, VAT with MoneyRounding) in server/SmartEstimatePlatform.Api/Services/EstimateCalculationService.cs
- [ ] T073 [US2] Validate overhead, profit and VAT each between 0 and 100 inclusive in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T074 [US3] Implement removal of a work line together with its material lines (Draft estimates only) in server/SmartEstimatePlatform.Api/Services/EstimateService.cs
- [ ] T075 [US1] Add POST /api/estimates/{id}/lines endpoint in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs (apply CompanyScopeFilter)
- [ ] T076 [US2] Add PATCH /api/estimates/{id}/lines/{lineId} and PATCH /api/estimates/{id}/percentages endpoints in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T077 [US3] Add DELETE /api/estimates/{id}/lines/{lineId} endpoint in server/SmartEstimatePlatform.Api/Controllers/EstimatesController.cs
- [ ] T078 [US1] Add Estimate Editor page with the “Add Work” flow in client/src/pages/EstimateEditor
- [ ] T079 [US2] Add live total recalculation on quantity/percentage change in client/src/pages/EstimateEditor
- [ ] T080 [US3] Add “Remove work” button with ConfirmDialog to the Estimate Editor in client/src/pages/EstimateEditor
- [ ] T081 [US1] Add unit test for the material quantity formula in tests/server/EstimateCalculationServiceTests
- [ ] T082 [US1] Add integration test verifying price/norm copy-isolation from later catalog changes in tests/server/EstimateServiceTests
- [ ] T083 [US2] Add unit tests for MoneyRounding: 0.005 → 0.01, 0.004 → 0.00, 0.015 → 0.02, in tests/server/MoneyRoundingTests
- [ ] T084 [US2] Add unit test for sequential overhead/profit/VAT rounding using the worked example (total 394.91 €) in tests/server/EstimateCalculationServiceTests
- [ ] T085 [US2] Add unit test that quantities and norms stay unrounded in intermediate calculations in tests/server/EstimateCalculationServiceTests
- [ ] T086 [US2] Add tests rejecting overhead < 0 or > 100, profit < 0 or > 100, and VAT < 0 or > 100 (one case per percentage) in tests/server/EstimateServiceTests
- [ ] T087 [US3] Add integration test: removing a work line removes its material lines and updates the totals in tests/server/EstimateServiceTests
- [ ] T088 Add integration test confirming cross-company Estimate access is blocked in tests/server/CompanyIsolationTests

### Moved from 006 (Project Management, spec Clarifications Q4)

- [ ] T185 Give Estimate a required ProjectId with a cascade foreign key to Project (deleting a project deletes its estimates, BR-011) in server/SmartEstimatePlatform.Api/Data/AppDbContext.cs and the estimate migration
- [ ] T186 List the project's estimates on Project Detail in place of "Sąmatų dar nėra." (the empty state stays when there are none) in client/src/pages/Projects/ProjectDetailPage.tsx, with the matching data from the project endpoint or an estimates endpoint
- [ ] T187 Add integration test (formerly 006 T063): deleting a project with estimates and confirming deletes all its estimates; another project's estimates are untouched, in tests/server/ProjectServiceTests
