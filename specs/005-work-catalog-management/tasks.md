# Tasks: Work Catalog Management

- [ ] T036 Add Work and WorkMaterialLine entities (allowing repeated materials per work) in server/Models/Entities
- [ ] T037 Add EF Core migration for Work/WorkMaterialLine in server/Migrations
- [ ] T038 Implement work CRUD with composition lines and range validation (waste % 0–100) in server/Services/WorkService
- [ ] T039 Add Works CRUD endpoints in server/Controllers/WorksController (apply CompanyScopeFilter)
- [ ] T040 Add Works List and Work Form (with composition table) pages in client/src/pages/Works
- [ ] T041 Add unit test for waste % range validation in tests/server/WorkServiceTests
- [ ] T042 Add integration test confirming cross-company Work access is blocked in tests/server/CompanyIsolationTests
