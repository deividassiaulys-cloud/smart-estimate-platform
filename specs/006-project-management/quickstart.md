# Quickstart: Validating Project Management

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

## Manual walk-through (API + client running)

1. Log in as the Manager. Dashboard → "Projektai" opens the Projects list ("Projektų dar nėra." when empty).
2. "+ Naujas projektas": name "  Buto remontas  ", description "Klientas: J. Jonaitis". Save → it's listed as "Buto remontas".
3. Create a second project with the same name → it's accepted, and both are listed.
4. Open a project → Detail shows the name, the description and "Sąmatų dar nėra.".
5. "Redaguoti" → change the description → the new description is shown.
6. Try an empty name, or a name longer than 200 characters → a message, and nothing is saved.
7. "Ištrinti" → confirmation. Cancel keeps the project; confirm returns to the list without it.
8. Log in as a Viewer → Projects and Project Detail are visible, with no create, edit or delete buttons. `/projects/new` redirects to the Dashboard, and `POST /api/projects` returns 403.
