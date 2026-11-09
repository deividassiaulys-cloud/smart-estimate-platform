# Quickstart: Estimate Price Refresh

Prerequisites: the API and client are running, and a Manager has the worked example estimate (spec 007: total 1263,26 €).

1. In Materials, change "Gipso plokštė" to 5,00 €. In Works, change "Siena A++" labour rate to 16,00 €/h.
2. Open the Draft estimate and choose "Atnaujinti kainas iš katalogo". The dialog lists:
   - "Siena A++ — Darbo įkainis: 15,00 € → 16,00 €"
   - "Siena A++ / Gipso plokštė — Kaina: 4,50 € → 5,00 €"
   - "Iš viso su PVM: 1 263,26 € → 1 365,05 €"
3. Choose "Atšaukti". The estimate is unchanged.
4. Open the dialog again and choose "Atnaujinti". The labour cost is 448,00 €, the board cost 385,00 € and the total 1365,05 €. Quantities and percentages are unchanged.
5. Choose "Atnaujinti kainas iš katalogo" again. The message "Sąmatos kainos ir normos jau sutampa su katalogu." appears and no dialog opens.
6. Rename "Plėvelė" in Materials and open the dialog after another catalog change. "Plėvelė" is listed as not found and keeps its price.
7. Confirm the estimate. The refresh button is no longer shown.
8. Open Materials and Works. Nothing there changed because of the refresh.

Automated: `dotnet test` (tests/server/EstimateRefreshTests) and `npm run test` (tests/client/EstimateEditorTests/EstimateRefresh.test.tsx).
