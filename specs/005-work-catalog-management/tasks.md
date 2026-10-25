# Tasks: Work Catalog Management

`[US#]` refers to the user stories of this feature in `spec.md`.

- [ ] T045 [US1] Add Work and WorkMaterialLine entities (own ID, repeated materials allowed) in server/SmartEstimatePlatform.Api/Models/Entities
- [ ] T046 [US1] Add EF Core migration for Work/WorkMaterialLine (foreign key to Material with restrict behaviour) in server/SmartEstimatePlatform.Api/Migrations
- [ ] T047 [US1] Implement work CRUD with composition lines and range validation (rate and norm ≥ 0, waste % 0–100 inclusive, unit in the fixed list) in server/SmartEstimatePlatform.Api/Services/WorkService.cs
- [ ] T048 [US1] Reject a duplicate Work name+unit (case-insensitive, per company) in server/SmartEstimatePlatform.Api/Services/WorkService.cs
- [ ] T049 [US2] Implement case-insensitive partial-name search for Works in server/SmartEstimatePlatform.Api/Services/WorkService.cs
- [ ] T050 [US1] Add Works CRUD + search endpoints in server/SmartEstimatePlatform.Api/Controllers/WorksController.cs (apply CompanyScopeFilter)
- [ ] T051 [US1] Add Works List (search field, delete confirmation with ConfirmDialog) and Work Form (composition table, repeated materials allowed) pages in client/src/pages/Works
- [ ] T052 [US2] Add search field and “no results” state to the Works List in client/src/pages/Works
- [ ] T053 [US1] Add unit tests: waste % of −1 and 101 rejected, 0 and 100 accepted, in tests/server/WorkServiceTests
- [ ] T054 [US1] Add test: duplicate Work name+unit rejected, same name with a different unit accepted, in tests/server/WorkServiceTests
- [ ] T055 [US1] Add test: the same material twice in one work is accepted in tests/server/WorkServiceTests
- [ ] T056 [US2] Add backend test for partial-name and case-insensitive search of Works in tests/server/WorkServiceTests
- [ ] T057 Add integration test confirming cross-company Work access is blocked in tests/server/CompanyIsolationTests
