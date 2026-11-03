# Quickstart: Validating Role-Based Estimate Viewing

## Checks after every task

```bash
cd server && dotnet build && dotnet test
cd client && npm run lint && npm run build && npm run test
```

## Manual walk-through (API + client running)

1. As the Manager: a project with one Draft and one Confirmed estimate. Each has "Siena A++" with its three materials.
2. Log in as a Viewer → Projects → the project. "Sąmatos" lists both estimates by name with "Atidaryti", and no status or total.
3. "Atidaryti" → the estimate name, "Siena A++", and under it Gipso plokštė, Varžtai and Plėvelė. There's no number, €, unit or "Juodraštis"/"Patvirtinta" anywhere on the page.
4. As the Viewer, call `GET /api/estimates/{id}` → 403, and `GET /api/estimates/{id}/names` → names only.
5. As the Manager, Project Detail and the editor are unchanged.

See [contracts/estimate-names-api.md](contracts/estimate-names-api.md).
