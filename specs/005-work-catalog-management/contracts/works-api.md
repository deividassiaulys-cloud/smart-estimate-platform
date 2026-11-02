# API Contract: Works

Every endpoint requires a logged-in **Manager**: 401 without a valid token, 403 for a Viewer. Another company's work is reported as 404, exactly like a missing one (feature 011).

| Endpoint | Body | Success | Errors |
|---|---|---|---|
| GET /api/works?search= | — | 200 `WorkResponse[]`; name contains `search` (case-insensitive), A–Z by name then unit; empty search → all | — |
| GET /api/works/{id} | — | 200 `WorkResponse` | 404 "Work not found." |
| POST /api/works | `WorkRequest` | 201 `WorkResponse` | 400 validation (per field) or "Each line must use a material from your catalog."; 409 "A work with this name and unit already exists." |
| PUT /api/works/{id} | `WorkRequest` | 200 `WorkResponse` (the composition is replaced by the request's lines) | 400; 404; 409 |
| DELETE /api/works/{id} | — | 204 | 404 |

```json
// WorkRequest
{ "name": "Sienų dažymas", "unit": "m²", "labourRate": 18.50, "labourNorm": 0.250,
  "lines": [ { "materialId": 3, "consumptionNorm": 0.150, "wastePercent": 5 },
             { "materialId": 3, "consumptionNorm": 0.050, "wastePercent": 0 } ] }

// WorkResponse
{ "id": 7, "name": "Sienų dažymas", "unit": "m²", "labourRate": 18.50, "labourNorm": 0.250,
  "lines": [ { "id": 11, "materialId": 3, "materialName": "Dažai", "materialUnit": "l",
               "consumptionNorm": 0.150, "wastePercent": 5.00 } ] }
```

## Change to an existing endpoint (completes 004 US3)

| Endpoint | New error |
|---|---|
| DELETE /api/materials/{id} | 409, detail "The material is used in works and cannot be deleted.", extension `works`: the names of the works using it (A–Z, distinct) |
