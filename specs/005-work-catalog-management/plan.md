# Plan: Work Catalog Management

## Constitution Check
No business logic in controllers or React components. No conflicts with the Constitution.

## Approach
CRUD for Work, plus a WorkMaterialLine table allowing the same material to repeat within one work (own ID, not a unique work+material pair).
