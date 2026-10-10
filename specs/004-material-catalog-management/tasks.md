# Tasks: Material Catalog Management

- [ ] T028 Add Material entity in server/Models/Entities
- [ ] T029 Add EF Core migration for Material in server/Migrations
- [ ] T030 Implement material CRUD with duplicate (name+unit) and negative-price checks in server/Services/MaterialService
- [ ] T031 Implement name search in server/Services/MaterialService
- [ ] T032 Add Materials CRUD + search endpoints in server/Controllers/MaterialsController (apply CompanyScopeFilter)
- [ ] T033 Add Materials List and Material Form pages in client/src/pages/Materials
- [ ] T034 Add unit test rejecting negative price and duplicates in tests/server/MaterialServiceTests
- [ ] T035 Add integration test confirming cross-company Material access is blocked in tests/server/CompanyIsolationTests
