# Quickstart: Estimate Line Editing

Prerequisites: API and client running (`dotnet run` in server/SmartEstimatePlatform.Api, `npm run dev` in client/), a Manager with the worked example estimate (spec 007: "Siena A++", 35 m², labour 0,8 h × 15,00 €/h; Gipso plokštė, Varžtai, Plėvelė; overhead 10 %, profit 15 %, VAT 21 %, total 1263,26 €) and a catalog material in m² (e.g. "Drėgmei atspari plokštė", 6,20 €).

1. Open the Draft estimate. Change the labour rate to 18 and press Enter → labour cost 504,00 €, totals updated.
2. Change "Gipso plokštė" price to 4 → cost 308,00 €. Open Materials: "Gipso plokštė" still costs 4,50 €.
3. Type -1 in a price → Lithuanian message under the field, nothing saved.
4. Choose "Pašalinti" on "Plėvelė", cancel → unchanged; confirm → the line is gone and totals drop.
5. Open "Pakeisti medžiagą" on the m² line → only m² materials are listed; choose "Drėgmei atspari plokštė" → name, unit and 6,20 € shown, norm and waste unchanged.
6. Confirm the estimate → no price fields, replace or remove controls.
7. Log in as a Viewer and open the estimate's names → the edited lines are shown (no "Plėvelė", the new material name).
8. Export to Excel → the file shows the edited prices and lines.

Automated: `dotnet test` (tests/server/EstimateLineEditingTests), `npm run test` (tests/client/EstimateEditorTests/EstimateLineEditing.test.tsx).
