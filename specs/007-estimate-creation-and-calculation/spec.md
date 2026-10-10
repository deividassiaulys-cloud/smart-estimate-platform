# Feature: Estimate Creation and Calculation

## Specification
As a Manager, I add a work with a quantity to an estimate; materials and costs generate automatically, and totals (incl. overhead/profit/VAT %) recalculate instantly on any change.

## User Stories
**US1 (P1):** As a Manager, I want to add a work with a quantity to an estimate, so that its materials and costs are generated without manual entry.
**Independent Verification:** Adding one work with known norms produces correctly calculated material lines and totals.

**US2 (P1):** As a Manager, I want totals to recalculate instantly when I change a quantity or percentage, so that I always see an accurate price.
**Independent Verification:** Changing a quantity updates all dependent totals without a manual refresh.

## Acceptance Scenarios
Given an estimate and a work with a quantity greater than 0, when the work is added, then its material lines are generated and totals are calculated automatically.

**Negative case:** when a work's quantity is submitted as 0 or less, the system rejects it.

## Edge Cases
- Prices/norms are fixed to the estimate at add-time and unaffected by later catalog changes.

## Success Criteria
- Totals update within 1 second of a change.

## Clarifications
- Can the same work be added to one estimate more than once? Yes — each addition creates its own independent line.
- Are overhead/profit/VAT percentages set once per estimate, or per line? Once per estimate, applied to the whole total.
