# Quickstart: Validating Estimate Creation and Calculation

## Checks after every task

```bash
cd server && dotnet build && dotnet test
cd client && npm run lint && npm run build && npm run test
```

After the migration task: `dotnet ef migrations has-pending-model-changes` (with a dummy `ConnectionStrings__DefaultConnection`) must report no changes.

## The reference example (automated)

`EstimateCalculatorTests.WorkedExample_TotalIs1263_26` reproduces the table in [spec.md](spec.md): direct 825.31, overhead 82.53, profit 136.18, VAT 219.24, total 1263.26. Plėvelė costs 40.43, not 40.42.

## Manual walk-through (API + client running, Manager)

References [contracts/estimates-api.md](contracts/estimates-api.md) and [data-model.md](data-model.md) rather than repeating implementation code.

1. Create the materials Gipso plokštė (m², 4.50), Varžtai (vnt., 0.02) and Plėvelė (m², 1.10), and the work "Siena A++" (m², 15 €/h, 0.8 h/m²) with the norms from the example.
2. Projects → a project → "+ Nauja sąmata" → name "Pradinė sąmata" → the editor opens (Juodraštis; 0 %, 0 %, 21 %).
3. Add "Siena A++", 35 → three material lines appear with quantities 77, 918.75 and 36.75.
4. Set the percentages to 10 / 15 / 21 → the total is 1 263,26 €.
5. Change the quantity to 40 → every amount updates within a second.
6. Change the catalog price of Plėvelė → the estimate doesn't change.
7. Remove the work line → confirmation; cancel keeps it; confirm removes it and its materials, and the totals become 0.
8. Back on Project Detail → the estimate is listed with "Juodraštis" and its total.
9. Delete the project → its estimates are gone as well.
10. As a Viewer: Project Detail shows no estimates, and `GET /api/estimates/{id}` returns 403.

Export to Excel is feature 010.
