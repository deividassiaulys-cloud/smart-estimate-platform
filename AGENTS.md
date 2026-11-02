# AGENTS.md

## Project map
- server/SmartEstimatePlatform.Api/Controllers — HTTP endpoints only, no business logic
- server/SmartEstimatePlatform.Api/Services — business logic and calculations (the Application layer)
- server/SmartEstimatePlatform.Api/Models/Entities — entities and fixed lists (measurement units)
- server/SmartEstimatePlatform.Api/Models/DTOs — request/response DTOs
- server/SmartEstimatePlatform.Api/Data — EF Core DbContext
- server/SmartEstimatePlatform.Api/Migrations — EF Core migrations
- client/src — React + TypeScript SPA (components/ holds shared UI such as ConfirmDialog)
- specs/ — Spec Kit feature specifications
- tests/ — backend and frontend automated tests

## Required references
Follow the project constitution in `.specify/memory/constitution.md` for all
architecture, testing, security, and simplicity rules. Do not restate those
rules here — this file only explains day-to-day working conventions.

## Common commands
- dotnet restore
- dotnet build
- dotnet test
- npm install (in client/)
- npm run test (in client/)
- npm run dev (in client/)

## Feature workflow
1. Start from an updated main branch.
2. Create a feature branch for the active spec.
3. Run /speckit.specify, /speckit.clarify, /speckit.plan, /speckit.tasks, /speckit.analyze in order before writing implementation code.
4. Implement only tasks listed in the feature's tasks.md.
5. Run dotnet test and npm run test before suggesting a commit.

## Verification
- Run backend and frontend tests after any code change and report failures before proposing a commit.
- Confirm the change matches the feature's spec.md and plan.md before marking a task complete.

## Do not edit or commit
- .env files or any local configuration containing secrets
- bin/, obj/, node_modules/, migration-generated files unless the task explicitly requires a new migration

## Repository-specific conventions
- Business logic always goes in server/SmartEstimatePlatform.Api/Services, never in controllers or React components (Constitution Article II).
- New dependencies require a one-line justification in the relevant plan.md.
- Every delete/remove action uses the shared ConfirmDialog component (NFR-011).
- Money is always decimal and is rounded only through the MoneyRounding helper; never use double or float for money.

## Code structure conventions (details: Constitution Article X)
- Controller actions make one service call and return the result; no logic.
- A new service comes with IXxxService in the same folder and is registered in AddApplicationServices().
- A new DTO is one record per file in Models/DTOs/<Feature>/; entity → DTO mapping only in the mapper.
- Frontend: Tailwind utility classes only, no new .css files, one component per file.
- Before adding a helper, parameter or option, check that the current spec needs it (YAGNI).