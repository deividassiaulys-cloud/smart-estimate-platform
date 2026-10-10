# Tasks: Estimate Creation and Calculation

- [ ] T050 Add Estimate, EstimateWorkLine, EstimateMaterialLine entities in server/Models/Entities
- [ ] T051 Add EF Core migration for estimate tables in server/Migrations
- [ ] T052 [US1] Implement material-line generation (quantity × consumption norm × (1+waste%)) in server/Services/EstimateCalculationService
- [ ] T053 [US1] Implement add-work-to-estimate logic, copying price/norm at add-time, in server/Services/EstimateService
- [ ] T054 [US1] Add quantity > 0 validation in server/Services/EstimateService
- [ ] T055 [US2] Implement total recalculation (overhead/profit/VAT) in server/Services/EstimateCalculationService
- [ ] T056 [US1] Add POST /api/estimates/{id}/lines endpoint in server/Controllers/EstimatesController (apply CompanyScopeFilter)
- [ ] T057 [US2] Add PATCH /api/estimates/{id}/lines/{lineId} and PATCH /api/estimates/{id}/percentages endpoints in server/Controllers/EstimatesController
- [ ] T058 [US1] Add Estimate Editor page with "Add Work" flow in client/src/pages/EstimateEditor
- [ ] T059 [US2] Add live total recalculation on quantity/percentage change in client/src/pages/EstimateEditor
- [ ] T060 [US1] Add unit test for material quantity formula in tests/server/EstimateCalculationServiceTests
- [ ] T061 [US1] Add integration test verifying price/norm copy-isolation in tests/server/EstimateServiceTests
- [ ] T062 [US2] Add unit test for sequential overhead/profit/VAT calculation in tests/server/EstimateCalculationServiceTests
- [ ] T063 Add integration test confirming cross-company Estimate access is blocked in tests/server/CompanyIsolationTests
