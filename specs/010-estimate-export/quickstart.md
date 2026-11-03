# Quickstart: Validating Estimate Export

## Checks after every task

```bash
cd server && dotnet build && dotnet test
cd client && npm run lint && npm run build && npm run test
```

## Manual walk-through (API + client running, Manager)

1. Open the worked-example estimate (1 263,26 €) → "Eksportuoti į Excel" → the dialog "Eksportuoti sąmatą?". Cancel downloads nothing.
2. Confirm → `Pradinė sąmata.xlsx` downloads. Open it in Excel or LibreOffice:
   - the header shows the project, estimate, "Juodraštis" and today's date
   - the lines and totals equal the screen, including 40,43 € and 1 263,26 €
   - the amounts are numbers (they can be summed), and no cell has a formula
3. Rename a material to `=1+1` before adding the work → in the file it reads `=1+1` as text, not 2.
4. Confirm the estimate and export again → the status reads "Patvirtinta".
5. As a Viewer: `GET /api/estimates/{id}/export` returns 403, and the Viewer's Estimate View has no export button.

See [contracts/estimate-export-api.md](contracts/estimate-export-api.md).
