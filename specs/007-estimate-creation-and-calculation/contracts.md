# Contracts: Estimate Creation and Calculation

- `POST /api/estimates/{id}/lines` — add a work with a quantity; returns the generated lines and totals.
- `PATCH /api/estimates/{id}/lines/{lineId}` — change a quantity.
- `PATCH /api/estimates/{id}/percentages` — set overhead, profit and VAT (0–100 inclusive).
- `DELETE /api/estimates/{id}/lines/{lineId}` — remove a work line together with its material lines (Draft only).
- `GET /api/units` — the fixed unit list.
- `DELETE /api/materials/{id}` — `409 Conflict` with the names of the works that use the material.
- Validation failures return `400` with field-level messages; a request for another company's resource returns `404`.
- Manager and Viewer receive different response shapes (the Viewer shape has no prices, quantities or status).

No controller or service implementation is included — only the contract.
