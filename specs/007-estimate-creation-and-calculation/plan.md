# Plan: Estimate Creation and Calculation

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
The Application layer copies work/material data into estimate lines when added (estimate lines hold copies and have no foreign keys to the catalog). Calculation logic is its own testable service. Money, rounding and percentage rules are listed under “Shared technical rules”. Removing a work line deletes its material lines in the same transaction and is allowed only for Draft estimates.

Shared technical rules (money, rounding, percentages, fixed units, confirmations) are in `specs/shared-technical-rules.md` and apply to this feature.
