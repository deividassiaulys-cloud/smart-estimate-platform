# API Contract: Estimate Export

| Endpoint | Who | Success | Errors |
|---|---|---|---|
| GET /api/estimates/{id}/export | Manager | 200 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`; `Content-Disposition: attachment; filename="<name>.xlsx"; filename*=UTF-8''<name>.xlsx`; any status (Draft or Confirmed) | 401; 403 Viewer; 404 missing or another company's estimate |

The body is the `.xlsx` described in [../data-model.md](../data-model.md). Its data is the same `EstimateResponse` that `GET /api/estimates/{id}` returns (007), plus the project name and the export date.
