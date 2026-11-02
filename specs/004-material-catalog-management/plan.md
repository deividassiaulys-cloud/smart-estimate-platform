# Plan: Material Catalog Management

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
CRUD and case-insensitive partial-name search in the Application layer, scoped by company through `ICompanyScoped` and the global query filter from 011; another company's material is not found (404). All material endpoints are Manager-only; `GET /api/units` needs login only, because units contain no prices. Units come from one fixed list (MeasurementUnit, BR-008) that the server validates and exposes through GET /api/units. Rejecting a delete while a work uses the material (409 Conflict listing the works, with a restrict foreign key) needs `WorkMaterialLine`, which feature 005 adds, so that check and its test (T038, T043) are built in 005; in 004, deleting a material always succeeds. Estimate lines hold copies and never block deletion.

Shared technical rules (money, rounding, percentages, fixed units, confirmations) are in `specs/shared-technical-rules.md` and apply to this feature.

## Dependencies
None beyond those added by features 002 and 011. Feature 005 completes US3 (T038, T043).
