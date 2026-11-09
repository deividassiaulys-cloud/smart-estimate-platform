# API: Estimate Price Refresh

Both endpoints are Manager-only:
- 401 without a valid token
- 403 for a Viewer
- 404 for another company's estimate
- 409 for a Confirmed estimate, with the existing "confirmed and cannot be changed" detail

## GET /api/estimates/{id}/refresh: preview (changes nothing)
200:
```json
{
  "changes": [
    { "workName": "Siena A++", "materialName": null, "field": 0, "oldValue": 15.00, "newValue": 16.00 },
    { "workName": "Siena A++", "materialName": "Gipso plokštė", "field": 2, "oldValue": 4.50, "newValue": 5.00 }
  ],
  "notFound": [ { "workName": "Siena A++", "materialName": "Plėvelė" } ],
  "currentTotal": 1263.26,
  "newTotal": 1365.05
}
```
`field`: 0 LabourRate, 1 LabourNorm, 2 Price, 3 ConsumptionNorm, 4 WastePercent. With nothing to update, `changes` is empty and `newTotal` equals `currentTotal`.

## POST /api/estimates/{id}/refresh: update
200 with the whole recalculated `EstimateResponse` (spec 007 shape). With nothing to update, it answers 200 with the estimate unchanged.

## Client
- `client/src/api/estimates.ts`: `previewRefresh(id)` returns `EstimateRefreshPreview`, and `refreshEstimate(id)` returns `Estimate`.
- `client/src/interfaces/estimate.ts`: `RefreshedField` (a const object, like `EstimateStatus`) and `EstimateRefreshPreview`.
- Lithuanian messages:
  - 409: the editor's existing status message
  - other failures: `unexpectedError('įkelti kainų atnaujinimo')` for the preview, and the editor's save message for the update
