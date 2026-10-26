# Tasks: Material Catalog Management

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T032 [US1] Define the fixed list of measurement units (vnt., m, m², m³, kg, t, l, val., kompl.) in server/SmartEstimatePlatform.Api/Models/Entities/MeasurementUnit.cs
- [ ] T033 [US1] Add GET /api/units endpoint (any logged-in user) in server/SmartEstimatePlatform.Api/Controllers/UnitsController.cs
- [ ] T034 [US1] Add Material entity (implements ICompanyScoped; Price decimal stored as DECIMAL(18,2); Name with collation utf8mb4_0900_as_ci; unique index on CompanyId+Name+Unit) in server/SmartEstimatePlatform.Api/Models/Entities and server/SmartEstimatePlatform.Api/Data/AppDbContext.cs
- [ ] T035 [US1] Add EF Core migration for Material in server/SmartEstimatePlatform.Api/Migrations
- [ ] T036 [US1] Implement material create/edit/delete with validation — name trimmed, duplicate name+unit rejected case-insensitively on create and edit (a concurrent duplicate caught from the unique index), price ≥ 0 with at most 2 decimal places and within DECIMAL(18,2), unit in the fixed list, missing material → NotFoundException — in server/SmartEstimatePlatform.Api/Services/MaterialService.cs
- [ ] T037 [US2] Implement case-insensitive partial-name search sorted by name A–Z, where an empty search returns all materials, in server/SmartEstimatePlatform.Api/Services/MaterialService.cs
- [ ] T039 [US1] Add Materials list/search, create, edit and delete endpoints with [Authorize(Roles="Manager")] in server/SmartEstimatePlatform.Api/Controllers/MaterialsController.cs
- [ ] T040 [US1] Add Materials List (search field that waits briefly while typing, "no results" message) and Material Form (unit dropdown loaded from /api/units) pages under ManagerRoute in client/src/pages/Materials, and link the dashboard's "Medžiagų katalogas" action to /materials
- [ ] T041 [US3] Add delete confirmation with ConfirmDialog to the Materials List, showing the server's "used in works" message when a delete is rejected, in client/src/pages/Materials
- [ ] T042 [US1] Add tests: negative price (−0.01), more than 2 decimals (12.345) and a unit outside the list are rejected, while 0 and 12.34 are accepted; a duplicate name+unit ("cementas " vs "Cementas") is rejected on create and on edit, while the same name with a different unit is accepted; editing or deleting a missing ID returns 404 — in tests/server/MaterialServiceTests
- [ ] T119 [US2] Add backend test for search: partial match, any letter case, A–Z order, and an empty search returning all materials, in tests/server/MaterialServiceTests
- [ ] T120 [US1] Add integration test: a Viewer gets 403 on listing, creating, editing and deleting materials, in tests/server/MaterialServiceTests
- [ ] T044 Add integration test confirming cross-company Material access is blocked (another company's ID → 404 on edit and delete, and its materials never appear in the list) in tests/server/CompanyIsolationTests
