# API Contract: Estimate Status

Manager only: 401 without a token, 403 for a Viewer. Another company's estimate → 404 (feature 011). Neither endpoint takes a body; each returns the whole `EstimateResponse` (see `specs/007-estimate-creation-and-calculation/contracts/estimates-api.md`).

| Endpoint | Allowed from | Success | Errors |
|---|---|---|---|
| PATCH /api/estimates/{id}/confirm | Draft (any number of lines) | 200, `status: 1` (Confirmed) | 404; 409 "The estimate is already confirmed." |
| PATCH /api/estimates/{id}/draft | Confirmed | 200, `status: 0` (Draft) | 404; 409 "The estimate is already a draft." |

## Existing endpoints (unchanged; now reachable in practice)

While the estimate is Confirmed, `POST …/lines`, `PATCH …/lines/{lineId}`, `DELETE …/lines/{lineId}` and `PATCH …/percentages` return 409 "The estimate is confirmed and cannot be changed.". `GET /api/projects/{projectId}/estimates` returns each estimate's `status` (Manager only).
