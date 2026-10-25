# Plan: Work Catalog Management

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
CRUD for Work with a WorkMaterialLine table that has its own ID, so the same material can repeat within one work. A duplicate Work name+unit is rejected (unique per company, case-insensitive). Search matches part of the name, case-insensitively. Server validation: rate and norm ≥ 0, waste % between 0 and 100 inclusive, unit from the fixed list. The foreign key from WorkMaterialLine to Material uses restrict behaviour.

Shared technical rules (money, rounding, percentages, fixed units, confirmations) are in `specs/shared-technical-rules.md` and apply to this feature.
