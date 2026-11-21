# Quickstart: Sections and Section Templates

## Automated checks
```bash
cd server && dotnet test
cd client && npm run test
```
Expected results:
- An estimate built from a catalog section totals the same as the same works added one by one (SC-019-2).
- Section subtotals add up to the direct costs (SC-019-3).
- The worked example without sections is unchanged: 1263,26 € (SC-019-4).

## Migration commands
Run in `server/SmartEstimatePlatform.Api`:
```bash
ConnectionStrings__DefaultConnection="Server=localhost;Database=unused;User=unused;Password=unused" dotnet ef migrations add AddSections
ConnectionStrings__DefaultConnection="Server=localhost;Database=unused;User=unused;Password=unused" dotnet ef migrations has-pending-model-changes
```

## Manual check (step 2 is required before merging)
1. `dotnet ef database update`, then start the API and `npm run dev`.
2. Existing data: every existing estimate shows and exports exactly as before, with the same totals.
3. Skyriai → new "A++ siena", m², lines: Tašelis × 1, Pelių barjeras × 0,4, Gipso plokštės montavimas × 1.
4. Add it to a Draft estimate with 121,7. Expected: a section with 121,7 / 48,68 / 121,7 and a subtotal; the total equals the sum of the parts.
5. Change Pelių barjeras to 50, then the section quantity to 130, and confirm the warning. Expected: 130 / 52 / 130.
6. Add a single work to the section and one "Be skyriaus". Remove the section and confirm: its lines disappear.
7. Export: the same sections and subtotals as on screen. As a Viewer: section names with work and material names only.
8. Try to delete a work used in "A++ siena": refused with a message naming the section.
9. At 360 px: the section headings and subtotals stay inside the editor's scrolling table; the page doesn't scroll sideways.
