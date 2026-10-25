# Tasks: Project Management

- [ ] T058 Add Project entity in server/SmartEstimatePlatform.Api/Models/Entities
- [ ] T059 Add EF Core migration for Project in server/SmartEstimatePlatform.Api/Migrations
- [ ] T060 Implement project CRUD with cascade delete of its estimates in server/SmartEstimatePlatform.Api/Services/ProjectService.cs
- [ ] T061 Add Projects CRUD endpoints in server/SmartEstimatePlatform.Api/Controllers/ProjectsController.cs (apply CompanyScopeFilter)
- [ ] T062 Add Projects List, Project Detail (lists the project’s estimates) and Project Form pages, with delete confirmation using ConfirmDialog, in client/src/pages/Projects
- [ ] T063 Add integration test confirming cascade delete in tests/server/ProjectServiceTests
- [ ] T064 Add integration test confirming cross-company Project access is blocked in tests/server/CompanyIsolationTests
