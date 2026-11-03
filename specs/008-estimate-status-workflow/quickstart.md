# Quickstart: Validating the Estimate Status Workflow

## Checks after every task

```bash
cd server && dotnet build && dotnet test
cd client && npm run lint && npm run build && npm run test
```

`dotnet ef migrations has-pending-model-changes` must still report no changes; this feature has no migration.

## Manual walk-through (API + client running, Manager, a Draft estimate with a work line)

1. Open the estimate → the badge "Juodraštis" and the button "Patvirtinti" are shown.
2. "Patvirtinti" → confirmation dialog. Cancel keeps it Draft; confirm → the badge is "Patvirtinta", there's no add-work form or "Pašalinti", and the quantity and percentage fields are disabled.
3. Project Detail → the estimate is listed as "Patvirtinta".
4. In a second tab, still showing the old Draft page, change a quantity → message, and the page reloads as Patvirtinta.
5. "Grąžinti į juodraštį" → confirmation → the editor is editable again; change a quantity → saved.
6. As a Viewer: `PATCH /api/estimates/{id}/confirm` returns 403.

See [contracts/estimate-status-api.md](contracts/estimate-status-api.md) and [data-model.md](data-model.md).
