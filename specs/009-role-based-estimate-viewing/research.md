# Research: Role-Based Estimate Viewing

## R1: Separate endpoints with number-free records (spec Q3)

- **Decision**: two new read-only endpoints in a new `EstimateNamesController` (class-level `[Authorize]`, any role). Their records contain only IDs and names:
  - `EstimateNameResponse(Id, Name)`
  - `EstimateNamesResponse(Name, Works)`
  - `WorkNamesResponse(WorkName, MaterialNames)`
- **Rationale**: a record with no number or status property can't carry prices, quantities or status, even through a coding mistake. A structural test checks this by reflection, so it holds as the types evolve. The Manager endpoints stay as they are, Manager-only, and remain 403 for Viewers (tested since 007).
- **Alternatives considered**: role-based shaping inside the existing endpoints (the old plan). Rejected: one forgotten branch would leak data, and the endpoint's documentation would mix two shapes.

## R2: Content and order (spec Q1, Q2)

- **Decision**:
  - The project's list includes every estimate, Draft and Confirmed alike, A–Z by name and then by ID, the same order as the Manager's list. It doesn't filter by status, so the status can't be inferred.
  - An opened estimate gives its name and, in the order the works were added (by line ID), each work's name with its material names in their order. A repeated material appears each time.
- **Source**: spec Clarifications Q1–Q2, wireframe wf12.

## R3: Service

- **Decision**: add `ListNamesForProjectAsync(projectId)` and `GetNamesAsync(id)` to `IEstimateService`, reusing its existing company-scoped loading (`EstimatesWithLines`, `FindAsync`, `EnsureProjectExistsAsync`): 404 for a missing or another company's project or estimate. No calculation runs for the names.
- **Rationale**: same aggregate and same scoping; no new service needed (YAGNI).

## R4: Client

- **Decision**:
  - **Project Detail**: a Manager keeps the existing table (status, total, editor link). A Viewer gets `ProjectEstimateNamesTable` (name and "Atidaryti" → `/estimates/:id/view`) from `GET …/estimate-names`, with "Sąmatų dar nėra." when there are none.
  - **New `EstimateViewPage`** under `ProtectedRoute`: the estimate name, then each work as a heading with its materials as a list (semantic HTML). There are no numbers anywhere. A Manager may open it too; their editor stays at `/estimates/:id`.
- **Rationale**: one page per purpose; the Viewer's page can't show what its data doesn't contain.
