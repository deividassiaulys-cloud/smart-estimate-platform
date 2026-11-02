# API Contract: Estimates

Every endpoint is **Manager only**: 401 without a token, 403 for a Viewer (until 009). Another company's project, estimate or line is reported as 404 (feature 011). Every editor action returns the whole recalculated `EstimateResponse`.

| Endpoint | Body | Success | Errors |
|---|---|---|---|
| GET /api/projects/{projectId}/estimates | — | 200 `EstimateSummaryResponse[]` (A–Z by name) | 404 project |
| POST /api/projects/{projectId}/estimates | `CreateEstimateRequest` | 201 `EstimateResponse` (Draft, 0/0/21 %, no lines) | 400; 404 project |
| GET /api/estimates/{id} | — | 200 `EstimateResponse` | 404 |
| POST /api/estimates/{id}/lines | `AddWorkLineRequest` | 200 `EstimateResponse` | 400 (quantity / "The work is not in your catalog."); 404; 409 not Draft |
| PATCH /api/estimates/{id}/lines/{lineId} | `ChangeQuantityRequest` | 200 `EstimateResponse` | 400; 404; 409 |
| DELETE /api/estimates/{id}/lines/{lineId} | — | 200 `EstimateResponse` (the line and its material lines removed) | 404; 409 |
| PATCH /api/estimates/{id}/percentages | `ChangePercentagesRequest` | 200 `EstimateResponse` | 400; 404; 409 |

```json
// EstimateResponse (worked example, abbreviated)
{ "id": 9, "projectId": 4, "name": "Pradinė sąmata", "status": 0,
  "overheadPercent": 10, "profitPercent": 15, "vatPercent": 21,
  "workLines": [ { "id": 21, "workName": "Siena A++", "workUnit": "m²", "quantity": 35,
      "labourNorm": 0.8, "labourRate": 15, "labourCost": 420.00,
      "materialLines": [ { "id": 51, "materialName": "Plėvelė", "materialUnit": "m²",
          "consumptionNorm": 1, "wastePercent": 5, "quantity": 36.75, "price": 1.10, "cost": 40.43 } ] } ],
  "directCosts": 825.31, "overhead": 82.53, "profit": 136.18, "vat": 219.24, "total": 1263.26 }

// EstimateSummaryResponse
{ "id": 9, "name": "Pradinė sąmata", "status": 0, "total": 1263.26 }
```

## Changed behaviour of an existing endpoint

| Endpoint | Change |
|---|---|
| DELETE /api/projects/{id} | Also deletes the project's estimates and all their lines (BR-011) |
