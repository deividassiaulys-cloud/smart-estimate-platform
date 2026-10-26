# Data Model: Estimate Creation and Calculation

- **Estimate** — status, overhead / profit / VAT percentages (DECIMAL(5,2)).
- **EstimateWorkLine** — quantity (DECIMAL(18,3)) and a copy of the work's name, unit, labour rate and norm.
- **EstimateMaterialLine** — belongs to a work line; copy of the material's name, unit, price and consumption norm; computed quantity.
- Estimate lines hold copies and have **no foreign keys to the catalog**, so deleting a material or work never changes an estimate (BR-010).
- **WorkMaterialLine** has its own ID so the same material can appear on several lines of one work; its foreign key to Material uses restrict behaviour (deleting a used material is rejected, BR-018).
- **MeasurementUnit** is a fixed list in code, not a table.
- Money columns are DECIMAL(18,2); quantities and norms DECIMAL(18,3).

Reviewed and accepted — no unnecessary relationships (e.g. no many-to-many that the specification did not ask for).
