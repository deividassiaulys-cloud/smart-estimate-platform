# Plan: Material Catalog Management

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
CRUD and case-insensitive partial-name search in the Application layer, scoped by company. Units come from one fixed list (MeasurementUnit, BR-008) that the server validates and exposes through GET /api/units. Deleting a material is rejected (409 Conflict with the names of the works) while any WorkMaterialLine refers to it; the foreign key uses restrict behaviour. Estimate lines hold copies and never block deletion.

Shared technical rules (money, rounding, percentages, fixed units, confirmations) are in `specs/shared-technical-rules.md` and apply to this feature.
