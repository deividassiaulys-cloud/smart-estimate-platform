# Tasks: Material Catalog Management

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T032 [US1] Define the fixed list of measurement units (vnt., m, m², m³, kg, t, l, val., kompl.) in server/SmartEstimatePlatform.Api/Models/Entities/MeasurementUnit.cs
- [ ] T033 [US1] Add GET /api/units endpoint in server/SmartEstimatePlatform.Api/Controllers/UnitsController.cs
- [ ] T034 [US1] Add Material entity in server/SmartEstimatePlatform.Api/Models/Entities
- [ ] T035 [US1] Add EF Core migration for Material in server/SmartEstimatePlatform.Api/Migrations
- [ ] T036 [US1] Implement material CRUD with duplicate (name+unit), negative-price and unit-in-list validation in server/SmartEstimatePlatform.Api/Services/MaterialService.cs
- [ ] T037 [US2] Implement case-insensitive partial-name search in server/SmartEstimatePlatform.Api/Services/MaterialService.cs
- [ ] T038 [US3] Reject deleting a material that is used in a work composition (409 Conflict listing the works) in server/SmartEstimatePlatform.Api/Services/MaterialService.cs
- [ ] T039 [US1] Add Materials CRUD + search endpoints in server/SmartEstimatePlatform.Api/Controllers/MaterialsController.cs (apply CompanyScopeFilter)
- [ ] T040 [US1] Add Materials List (search field) and Material Form (unit dropdown loaded from /api/units) pages in client/src/pages/Materials
- [ ] T041 [US3] Add delete confirmation with ConfirmDialog and the “used in works” message to the Materials List in client/src/pages/Materials
- [ ] T042 [US1] Add unit tests rejecting a negative price, a duplicate name+unit and a unit outside the fixed list in tests/server/MaterialServiceTests
- [ ] T043 [US3] Add integration test: deleting a used material is rejected, deleting an unused one succeeds, in tests/server/MaterialServiceTests
- [ ] T044 Add integration test confirming cross-company Material access is blocked in tests/server/CompanyIsolationTests
