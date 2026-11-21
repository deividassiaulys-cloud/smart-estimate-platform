# Data Model: Sections and Section Templates

## New entities
| Entity | Fields | Rules / relations |
|---|---|---|
| CatalogSection (ICompanyScoped) | Id, Name (≤ 200, case-insensitive collation), Unit (≤ 16, fixed list), CompanyId, Lines | Unique (CompanyId, Name, Unit) |
| CatalogSectionLine | Id, CatalogSectionId, WorkId, Coefficient DECIMAL(18,6) | Coefficient > 0, ≤ 6 decimals; FK Work **Restrict** (FR-019-2); cascade with its section; ordered by Id; the same work may repeat |
| EstimateSection | Id, EstimateId, Name, Unit, Quantity DECIMAL(18,3) | Copy of the catalog section's name and unit (BR-010); Quantity > 0, ≤ 3 decimals; cascade with its estimate; ordered by Id |

## Changed entity
| Entity | New fields | Rules |
|---|---|---|
| EstimateWorkLine | SectionId (nullable, FK EstimateSection, cascade), Coefficient (nullable, DECIMAL(18,6)) | Null SectionId = "Be skyriaus". Coefficient is set only for lines created from a catalog section; used by FR-019-10 |

Migration `AddSections`: 3 tables, 2 nullable columns, 1 unique index, FKs. Existing rows get SectionId = null, Coefficient = null (FR-019-11).

## Calculated (not stored)
- Section work quantity = round(section quantity × coefficient, 3, half away from zero); refused if 0.
- Section subtotal = Σ (labour cost + material costs) of its work lines; `UnsectionedSubtotal` likewise. The subtotals sum to DirectCosts.
