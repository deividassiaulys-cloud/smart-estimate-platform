# API contract: Sections and Section Templates

## Catalog sections (Manager only; Viewer 403; another company's → 404)
- `GET /api/catalog-sections?search=` → `[{ id, name, unit, lines: [{ id, workId, workName, workUnit, coefficient }] }]`, sorted by name
- `GET /api/catalog-sections/{id}` → one section
- `POST /api/catalog-sections` `{ name, unit, lines: [{ workId, coefficient }] }` → 201
  - 400: invalid name, unit, empty lines, or a coefficient ≤ 0 or with > 6 decimals
  - 400: unknown or another company's work
  - 409: duplicate name + unit
- `PUT /api/catalog-sections/{id}` (same body) → 200; replaces the whole line list
- `DELETE /api/catalog-sections/{id}` → 204
- `DELETE /api/works/{id}` → **409** when the work is used in a catalog section; the message names the sections (new)

## Estimate sections (Manager only; Draft only → 409 when Confirmed)
- `POST /api/estimates/{id}/sections` `{ catalogSectionId, quantity }` → EstimateResponse
  - 400: quantity ≤ 0 or > 3 decimals, or a computed work quantity rounds to 0
  - 400: unknown catalog section
- `PATCH /api/estimates/{id}/sections/{sectionId}` `{ quantity }` → EstimateResponse; recalculates the lines that have a coefficient
- `DELETE /api/estimates/{id}/sections/{sectionId}` → EstimateResponse
- `POST /api/estimates/{id}/lines` `{ workId, quantity, sectionId? }`; a sectionId that isn't this estimate's → 404

## EstimateResponse additions
```jsonc
{
  "workLines": [{ "id": 21, "sectionId": 5, /* existing fields */ }],
  "sections": [{ "id": 5, "name": "A++ siena", "unit": "m²", "quantity": 121.7, "subtotal": 1234.56 }],
  "unsectionedSubtotal": 0
}
```

## EstimateNamesResponse (Viewer) additions
```jsonc
{ "name": "…", "sections": [{ "name": "A++ siena", "works": [{ "workName": "…", "materialNames": ["…"] }] }], "works": [/* lines without a section */] }
```

## Excel export
Only when the estimate has sections:
- per section: a bold heading row "<name> — <quantity> <unit>", its rows as today, then "Skyriaus suma" with the subtotal in the amount column;
- then a "Be skyriaus" group (only if it has lines), with its subtotal.

Without sections the file is unchanged.
