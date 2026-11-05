# API: Estimate Line Editing

All endpoints: Manager only (Viewer → 403), JWT required (401), estimate/line of another company or not in this estimate → 404, estimate Confirmed → 409 with the existing "not a draft" detail. Success → 200 with the whole `EstimateResponse` (unchanged shape, spec 007).

| Method & path | Body | Extra errors |
|---|---|---|
| `PATCH /api/estimates/{id}/lines/{lineId}/labour-rate` | `{ "labourRate": 18.00 }` | 400 negative, > 2 decimals, too large |
| `PATCH /api/estimates/{id}/lines/{lineId}/materials/{materialLineId}/price` | `{ "price": 4.00 }` | 400 negative, > 2 decimals, too large |
| `PATCH /api/estimates/{id}/lines/{lineId}/materials/{materialLineId}/material` | `{ "materialId": 7 }` | 400 "Each line must use a material from your catalog." (missing or foreign material); 400 "The replacement material must have the same unit." |
| `DELETE /api/estimates/{id}/lines/{lineId}/materials/{materialLineId}` | — | — |

Validation errors use the existing ValidationProblemDetails format (field names `LabourRate`, `Price`).

## Client
- `client/src/api/estimates.ts`: `changeLabourRate`, `changeMaterialPrice`, `replaceMaterial`, `removeMaterialLine`, each returning `Estimate`.
- Lithuanian texts (client-side, by status, spec 012 Q2):
  - invalid price/rate: "Kaina turi būti 0 arba didesnė, ne daugiau kaip 2 skaitmenys po kablelio."
  - 409: the existing confirmed-estimate message
  - unit mismatch / unknown material (400): "Pasirinkite to paties mato vieneto medžiagą iš katalogo."
  - other: `unexpectedError(...)`
