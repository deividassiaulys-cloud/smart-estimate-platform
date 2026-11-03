# Feature 7: Estimate Creation and Calculation

## Specification
As a Manager, I add a work with a quantity to an estimate; materials and costs generate automatically, and totals (incl. overhead/profit/VAT %) recalculate instantly on any change. I can remove a work from a Draft estimate together with its generated material lines (after confirmation). Overhead, profit and VAT must each be between 0 and 100. Money amounts are rounded to 2 decimals (0.005 → 0.01) and quantities and norms are not rounded during calculation. Edge case: prices/norms are fixed to the estimate at add-time and unaffected by later catalog changes. Success criteria: totals update within 1 second of a change.

## User Stories
**US1 (P1):** As a Manager, I want to add a work with a quantity to an estimate, so that its materials and costs are generated without manual entry.
**Independent verification:** Adding one work with known norms produces correctly calculated material lines and totals.

**US2 (P1):** As a Manager, I want totals to recalculate instantly when I change a quantity or percentage, so that I always see an accurate price.
**Independent verification:** Changing a quantity updates all dependent totals without a manual refresh.

**US3 (P1):** As a Manager, I want to remove a work from a Draft estimate together with its material lines, so that I can correct a mistake.
**Independent verification:** After removal the work and its material lines are gone and the totals are recalculated.

## Acceptance Scenarios
Given an estimate and a work with a quantity greater than 0, when the work is added, then its material lines are generated and totals are calculated automatically. Given the worked example below (BR-015–BR-017), the total with VAT is 1263.26 €. Given a Draft estimate with a work line, when the Manager removes the line and confirms, then its material lines are removed too and the totals are recalculated. Negative cases: a quantity of 0 or less is rejected; an overhead, profit or VAT percentage below 0 or above 100 is rejected; an amount of exactly 0.005 € becomes 0.01 € and no quantity or norm is rounded before the final money amounts.

## Edge Cases
- Prices and norms are copied into the estimate when a work is added; later catalog changes do not affect it (BR-010).
- Rounding happens on each money amount; the grand total is the sum of the rounded parts (BR-017).
- Removing a work line is possible only while the estimate is Draft.
- Calculation (BR-015–BR-017, docs/requirements.md): labour cost = quantity × labour norm × labour rate; material quantity = work quantity × consumption norm × (1 + waste %/100); material cost = material quantity × unit price; direct costs = sum of all labour and material costs; overhead = direct costs × p₁ %; profit = (direct costs + overhead) × p₂ %; VAT = (direct costs + overhead + profit) × p₃ %. Each money amount is rounded (half away from zero) before it is summed; the total is the sum of the rounded parts.
- A new estimate is created from Project Detail with a required name (trimmed, at most 200 characters) and opens straight in the Estimate Editor; it starts with overhead 0 %, profit 0 % and VAT 21 %, which the Manager can change.
- Scope: this feature covers creating an estimate (name), the Estimate Editor (add a work with a quantity, change a quantity, change the percentages, remove a work line), the totals, and the estimate list on Project Detail (name, status, total with VAT). Every estimate stores a status that is always Draft here (BR-012), and every change goes through one Draft-only check. Confirming and returning to Draft is feature 008, the Viewer's view of estimates is feature 009 (until then Viewers get 403 on every estimate endpoint and see no estimates), and export is feature 010. Renaming or deleting a single estimate is not part of Must Have.
- Input limits (docs/ux.md "Estimate Line — Quantity Edit", shared technical rules): a work quantity is greater than 0 with at most 3 decimals (DECIMAL(18,3)); overhead, profit and VAT are 0–100 inclusive with at most 2 decimals (DECIMAL(5,2)); more decimals are rejected, not rounded. A work without materials adds only its labour line.
- On Project Detail the estimates are listed A–Z by name.
- Every change is saved at once (a quantity or percentage when its field loses focus or on Enter; adding or removing a work immediately), and the server answers with the whole estimate recalculated. Only the inputs are stored (quantities, copied prices and norms, percentages); line amounts and totals are calculated by the server on every read, so they can never get out of step with the lines. The browser does not calculate totals (docs/architecture.md).
- The 1-second target is met by design: amounts are computed in memory for one estimate per request (up to 200 lines), so it is checked manually (quickstart step 5) rather than by a load test.

## Worked Example
Work "Siena A++", 35 m²; labour norm 0.8 h/m², labour rate 15 €/h. Materials: Gipso plokštė (norm 2, waste 10 %, 4.50 €), Varžtai (norm 25, waste 5 %, 0.02 €), Plėvelė (norm 1, waste 5 %, 1.10 €). Overhead 10 %, profit 15 %, VAT 21 %.

| Line | Calculation | Amount |
|---|---|---|
| Labour | 35 × 0.8 × 15 | 420.00 |
| Gipso plokštė | 35 × 2 × 1.10 = 77 × 4.50 | 346.50 |
| Varžtai | 35 × 25 × 1.05 = 918.75 × 0.02 = 18.375 | 18.38 |
| Plėvelė | 35 × 1 × 1.05 = 36.75 × 1.10 = 40.425 | 40.43 (banker's rounding would give 40.42) |
| Direct costs | sum of rounded parts | 825.31 |
| Overhead 10 % | 82.531 | 82.53 |
| Profit 15 % of 907.84 | 136.176 | 136.18 |
| VAT 21 % of 1044.02 | 219.2442 | 219.24 |
| **Total** | | **1263.26** |

## Clarifications
- Can the same work be added to one estimate more than once? Yes — each addition creates its own independent line.
- Are overhead/profit/VAT percentages set once per estimate, or per line? Once per estimate, applied to the whole total.
- Which rounding is used? Money amounts are rounded to 2 decimals with 0.005 becoming 0.01 (half away from zero); the grand total is the sum of the rounded parts (BR-017). Quantities and norms are not rounded in calculations, only displayed with up to 3 decimals.

### Session 2026-11-02
- Q: Where do the calculation rules come from? → A: BR-015–BR-017 in docs/requirements.md (now in the repository). The inputs of the original 394.91 € example are not available, so the acceptance example is the worked example above (1263.26 €), calculated by hand from those rules and including a half-away-from-zero midpoint (40.425 → 40.43).
- Q: What does a new estimate need, and what are its default percentages? → A: A required name (trimmed, at most 200 characters); overhead 0 %, profit 0 %, VAT 21 %; created from Project Detail and opened straight in the Estimate Editor.
- Q: Where does 007 end and 008/009 begin? → A: 007 builds estimate creation, the editor (add work, change quantity, change percentages, remove work line), the totals and the estimate list on Project Detail; status is stored (always Draft) behind one Draft-only check. Status transitions are 008, the Viewer view is 009 (Viewers get 403 until then), export is 010; no renaming or deleting single estimates.
- Q: When is a change saved and recalculated, and are amounts stored? → A: Each change is saved at once (on field blur/Enter, add/remove immediately) and the server returns the recalculated estimate; only inputs are stored and all amounts are calculated on every read by one calculation service.
