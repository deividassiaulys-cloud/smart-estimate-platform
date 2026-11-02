# Research: Project Management

## R1: Fields and validation

- **Decision**:
  - `Name`: required, stored trimmed, max 200, varchar(200).
  - `Description`: optional, max 2000, varchar(2000), nullable. Whitespace-only is stored as null, and a non-empty description is stored trimmed.
- **Where**:
  - `ProjectRequest` declares `[Required]` and `[StringLength(ProjectRules.NameMaxLength)]` for Name, and `[StringLength(ProjectRules.DescriptionMaxLength)]` for Description.
  - `[Required]` already rejects an empty or whitespace-only string, so no `IValidatableObject` is needed.
  - Trimming is normalisation, so it happens in `ProjectService`, as with materials.
- **Source**: spec Clarifications Q1.

## R2: No name uniqueness

- **Decision**: no unique index and no duplicate check (Q3). That means no 409 for projects, and `UniqueSave` isn't needed here.

## R3: Authorization split

- **Decision**: `ProjectsController` has class-level `[Authorize]`, so any logged-in user gets the read endpoints (`GET /api/projects`, `GET /api/projects/{id}`). The write endpoints (POST, PUT, DELETE) carry `[Authorize(Roles = Manager)]` on the action. Company scoping comes from `ICompanyScoped` (011).
- **Client**:
  - `/projects` and `/projects/:id` sit under `ProtectedRoute`; `/projects/new` and `/projects/:id/edit` under `ManagerRoute`.
  - The "+ Naujas projektas", "Redaguoti" and "Ištrinti" actions render only when `getRoleName() === 'Manager'`. That's display only; the server enforces the rule (Article IV).
- **Source**: Q2. 009 needs Viewers to see project names and descriptions.

## R4: Estimates deferred to 007

- **Decision**: no Estimate entity in 006 (Q4). Project Detail shows "Sąmatų dar nėra.". The following move to `specs/007-estimate-creation-and-calculation/tasks.md`:
  - the estimate list on Project Detail
  - the cascade delete of a project's estimates (FK Estimate → Project, cascade)
  - the integration test for it (old T063)
- **Rationale**: the same split as 004 → 005 (T038/T043), and no half-finished model (YAGNI).

## R5: Where delete lives in the UI

- **Decision**: the Projects list links each name to Project Detail. Project Detail holds the "Redaguoti" and "Ištrinti" actions (Manager only). Delete goes through `ConfirmDialog` and returns to `/projects` with a notice.
- **Rationale**: one place for actions on a project. The list stays read-friendly for Viewers.
- **List order**: A–Z by name, then by ID for the same name (names aren't unique).
