# Quickstart: Resource Kinds and Estimate Columns

## Automated checks
```bash
cd server && dotnet test   # the solution is in server/
cd client && npm run test
```
Expected results:
- The worked example still totals 1263,26 €, with Labour 420,00 + Materials 405,31 + Machinery 0,00 = 825,31 € and a unit price of 23,58 €.
- With a 0,20 € machinery line added, Machinery is 7,00 € and the unit price 23,78 € (spec US2-2).
- The export test finds the 11 headings, the labour row and the kind columns.

## Migration commands
Run in `server/SmartEstimatePlatform.Api`. The connection string is a placeholder:
```bash
ConnectionStrings__DefaultConnection="Server=localhost;Database=unused;User=unused;Password=unused" dotnet ef migrations add AddMaterialKind
ConnectionStrings__DefaultConnection="Server=localhost;Database=unused;User=unused;Password=unused" dotnet ef migrations has-pending-model-changes
```

## Manual check (step 2 is required before merging)
1. `dotnet ef database update`, then start the API and `npm run dev`.
2. Existing data: every material is "Medžiaga", and an existing estimate has the same total as before, with Machinery 0,00 €.
3. Materials → new "Įrankiai ir įrengimai", vnt., 0,20 €, kind "Mechanizmas". The list shows the kind.
4. Works → "Siena A++" with the worked-example materials plus "Įrankiai ir įrengimai" (norm 1, waste 0). Add 35 m² to a Draft estimate. Expected:
   - work row: No. 1, unit price 23,78 €, amount 832,31 €, Darbas 420,00 €, Medžiagos 405,31 €, Mechanizmai 7,00 €
   - labour row: 0,8 · 15,00 € · 28 val. · 420,00 €
5. Replace "Plėvelė" with a Machinery material of the same unit: its cost moves to "Mechanizmai".
6. Change a material's kind in the catalog, then open "Atnaujinti kainas iš katalogo": the dialog lists "Rūšis: Medžiaga → Mechanizmas".
7. Export to Excel: the columns and values are the same as on screen.
8. Log in as a Viewer: only names are shown, as before.
9. At 360 px: the editor rows stack, with no horizontal page scroll (NFR-008).
