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
Given an estimate and a work with a quantity greater than 0, when the work is added, then its material lines are generated and totals are calculated automatically. Given the worked example in Independent Work 1 (BR-015–BR-017), the total with VAT is 394.91 €. Given a Draft estimate with a work line, when the Manager removes the line and confirms, then its material lines are removed too and the totals are recalculated. Negative cases: a quantity of 0 or less is rejected; an overhead, profit or VAT percentage below 0 or above 100 is rejected; an amount of exactly 0.005 € becomes 0.01 € and no quantity or norm is rounded before the final money amounts.

## Edge Cases
- Prices and norms are copied into the estimate when a work is added; later catalog changes do not affect it (BR-010).
- Rounding happens on each money amount; the grand total is the sum of the rounded parts (BR-017).
- Removing a work line is possible only while the estimate is Draft.

## Clarifications
- Can the same work be added to one estimate more than once? Yes — each addition creates its own independent line.
- Are overhead/profit/VAT percentages set once per estimate, or per line? Once per estimate, applied to the whole total.
- Which rounding is used? Money amounts are rounded to 2 decimals with 0.005 becoming 0.01 (half away from zero); the grand total is the sum of the rounded parts (BR-017). Quantities and norms are not rounded in calculations, only displayed with up to 3 decimals.
