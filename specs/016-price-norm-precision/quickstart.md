# Quickstart: Higher Price and Norm Precision

## Automated checks
```bash
cd server && dotnet test   # the solution is in server/
cd client && npm run test
```
Expected results:
- The sample-estimate acceptance test passes: total 135 414,29 € ± 0,50 € (research R6).
- The worked example still totals 1263,26 €.
- The model test reports scale 4 for prices and rates, and 6 for norms.

## Migration commands
Run in `server/SmartEstimatePlatform.Api`. The connection string is a placeholder; the design-time tools only build the model:
```bash
ConnectionStrings__DefaultConnection="Server=localhost;Database=unused;User=unused;Password=unused" dotnet ef migrations add WidenPriceAndNormPrecision
ConnectionStrings__DefaultConnection="Server=localhost;Database=unused;User=unused;Password=unused" dotnet ef migrations has-pending-model-changes
```

## Manual check (step 2 is required before merging)
1. `dotnet ef database update` in `server/SmartEstimatePlatform.Api`, then start the API and the client (`npm run dev`).
2. Existing data: open an existing estimate before and after the migration; its total is unchanged.
3. Materials → new material "Sraigtai terasoms", vnt., price `0,0616`. The list shows **0,0616 €**. Open and save without changes: still 0,0616 €.
4. Price `0,06164`: refused with a message about 4 decimal places.
5. Works → new work, labour rate `25`, labour norm `0,042002`, a material line with norm `0,000884`. The list and the form show 0,042002 and 0,000884, never 0.
6. Add the work to a Draft estimate with quantity 121,7. The labour cost is 127,79 €. Change a material price inline to `0,0616`: accepted. Change it to `0,06164`: refused.
7. Export to Excel. The price cells show 0,0616 € and the norm cells 0,000884, and amounts have 2 decimals.
8. At 360 px width, the longer numbers do not cause horizontal page scrolling (NFR-008).
