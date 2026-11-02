# Quickstart: Validating Work Catalog Management

## Checks after every task

```bash
cd server && dotnet build && dotnet test
cd client && npm run lint && npm run build && npm run test
```

After the migration task:

```bash
ConnectionStrings__DefaultConnection="Server=localhost;Database=unused;User=unused;Password=unused" \
  dotnet ef migrations has-pending-model-changes --project server/SmartEstimatePlatform.Api
```

Expected: "No changes have been made to the model since the last migration."

## Manual walk-through (API + client running, logged in as a Manager)

1. Dashboard → "Darbų katalogas" opens the Works list.
2. "+ Naujas darbas": name "Sienų dažymas", unit m², rate 18.50, norm 0.250. Add the same paint material twice (norms 0.150 / 0.050, waste 5 / 0). Save → the work appears in the list.
3. Edit it: remove one line and change the norm → after saving, the work has exactly the remaining line.
4. Search "SIENA" → the work is listed. Search "xyz" → the "nothing found" message appears.
5. Try waste 101, consumption norm 0, or a duplicate name+unit → each is rejected with a message.
6. Materials → delete the paint → rejected, and the message names "Sienų dažymas".
7. Works → delete the work → confirmation dialog. Cancel keeps it; confirm removes it. Now the paint can be deleted.
8. Log in as a Viewer → there's no Works link, and /works redirects away. `GET /api/works` returns 403.
