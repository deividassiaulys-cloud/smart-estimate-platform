# API contract changes: Higher Price and Norm Precision

No new endpoints and no new fields. Only the accepted precision of existing request fields changes. Responses return the stored values unchanged; JSON numbers carry up to 4 or 6 decimals.

| Endpoint | Field | Before | After |
|---|---|---|---|
| POST/PUT `/api/materials` | `price` | ≥ 0, ≤ 2 decimals | ≥ 0, ≤ 4 decimals |
| POST/PUT `/api/works` | `labourRate` | ≥ 0, ≤ 2 decimals | ≥ 0, ≤ 4 decimals |
| POST/PUT `/api/works` | `labourNorm` | ≥ 0, ≤ 3 decimals | ≥ 0, ≤ 6 decimals |
| POST/PUT `/api/works` | `lines[].consumptionNorm` | > 0, ≤ 3 decimals | > 0, ≤ 6 decimals |
| PATCH `/api/estimates/{id}/lines/{lineId}/labour-rate` | `labourRate` | ≥ 0, ≤ 2 decimals | ≥ 0, ≤ 4 decimals |
| PATCH `/api/estimates/{id}/lines/{lineId}/materials/{materialLineId}/price` | `price` | ≥ 0, ≤ 2 decimals | ≥ 0, ≤ 4 decimals |

Unchanged:
- `quantity` (add work line, change quantity): > 0, ≤ 3 decimals.
- `wastePercent`, `overheadPercent`, `profitPercent`, `vatPercent`: 0–100, ≤ 2 decimals.

A value with too many decimals or too large returns **400** with a validation problem naming the field, as today. The message text changes from "at most 2/3 decimal places" to "at most 4/6 decimal places".

Excel export (`GET /api/estimates/{id}/export`):
- Price and labour-rate cells are formatted `#,##0.00## "€"`.
- Consumption and labour norm cells are formatted `#,##0.######`.
- Amounts and quantities are unchanged.
