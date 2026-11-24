# Smart Estimate Management Platform

## About the Project
A web application for construction service providers and contractors that
enables fast, error-free preparation of construction estimates from a
defined catalog of materials, works, prices, and norms, with automatic
recalculation of totals.

## Problem
Estimates are currently prepared in Excel spreadsheets or in programs
installed on a single computer, which limits work to one location, makes
price updates manual, and carries a high risk of errors.

## Target User Groups
- **Manager** – the company owner / estimator, manages the catalog and estimates.
- **Viewer** – an employee (e.g. on a construction site), sees only the
  lists of works and materials, without prices.

## Core Functionality (MVP)
- Registration with company creation
- Material and work catalog with work compositions (material norms)
- Project and estimate creation with automatic recalculation
- Estimate status: Draft / Confirmed
- Role-based viewing
- Export to Excel

## Project Status
Requirements and design phase.

## Technologies
- **Backend:** ASP.NET Core Web API (.NET)
- **ORM / Data Access:** Entity Framework Core (Microting.EntityFrameworkCore.MySql provider)
- **Database:** MySQL (managed via MySQL Workbench)
- **Frontend:** React (Vite, TypeScript)
- **Authentication:** ASP.NET Core Identity (password hashing) + JWT

## Continuous Integration
GitHub Actions runs `.github/workflows/ci.yml` on every pull request that
targets `main` and on every push to `main`. It has two jobs, and a pull
request is merged only when both are green (a working rule: branch protection
is not available on the free private plan).

- **backend**: `dotnet restore`, then `dotnet build` and `dotnet test`
  (Release), then `dotnet list package --vulnerable --include-transitive`,
  which fails the job if any NuGet package has a known vulnerability.
- **frontend** (in `client/`): `npm ci`, `npm run lint`, `npm run test`,
  `npm run build`, then `npm audit --omit=dev --audit-level=moderate`.

### Release packages
After CI succeeds on `main`, `.github/workflows/release.yml` checks out the
same commit, publishes the API (`dotnet publish`, Release) and builds the
client (`npm run build`), and uploads them as two workflow artifacts:
**api** (the published API) and **client** (`client/dist`). Download them from
the Release run's page under GitHub Actions. They contain no secrets: the
deployment environment supplies `ConnectionStrings__DefaultConnection`,
`Jwt__Key` and `AllowedHosts` (see docs/architecture.md, section 4).

The tests use an in-memory SQLite database, so CI needs no MySQL and no real
secrets. `Jwt__Key` is a dummy value set in the workflow; never put a real
key or connection string there.

To run the same checks locally before opening a pull request:

```bash
cd server && dotnet build && dotnet test
dotnet list SmartEstimatePlatform.slnx package --vulnerable --include-transitive
cd ../client && npm ci && npm run lint && npm run test && npm run build && npm audit --omit=dev
```

## Documentation
- docs/requirements.md – full project requirements specification
- docs/ux.md – user experience, user flows, and wireframes
- specs/ – Spec Kit feature specifications
