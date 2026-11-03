# API Contract: Estimate Names (every logged-in user)

Both endpoints: 401 without a token. Any role of the company may call them. Another company's project or estimate is 404 (feature 011). Neither response contains a unit, quantity, norm, price, percentage, total or status.

| Endpoint | Success | Errors |
|---|---|---|
| GET /api/projects/{projectId}/estimate-names | 200 `EstimateNameResponse[]`: every estimate (Draft and Confirmed), A–Z by name | 404 project |
| GET /api/estimates/{id}/names | 200 `EstimateNamesResponse` | 404 estimate |

```json
// GET /api/projects/4/estimate-names
[ { "id": 9, "name": "Pradinė sąmata" }, { "id": 10, "name": "Virtuvė" } ]

// GET /api/estimates/9/names
{ "name": "Pradinė sąmata",
  "works": [ { "workName": "Siena A++", "materialNames": [ "Gipso plokštė", "Varžtai", "Plėvelė" ] } ] }
```

## Unchanged

Every Manager estimate endpoint (`specs/007-…/contracts/estimates-api.md`, `specs/008-…/contracts/estimate-status-api.md`) stays Manager only: 403 for a Viewer.
