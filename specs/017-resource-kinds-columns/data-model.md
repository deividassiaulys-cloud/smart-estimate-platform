# Data Model: Resource Kinds and Estimate Columns

## New: MaterialKind (fixed enum, Models/Entities/MaterialKind.cs)
| Value | Number | Label (client) |
|---|---|---|
| Material | 0 | Medžiaga |
| Machinery | 1 | Mechanizmas |

## Changed entities (stored)
| Entity | Change | Column | Rule |
|---|---|---|---|
| Material | + `Kind` (MaterialKind), set in the constructor and `Update` | int NOT NULL DEFAULT 0 | must be a defined value (`MaterialRules`); default Material |
| EstimateMaterialLine | + `Kind`, copied when the work is added; `ReplaceMaterial(name, unit, price, kind)`; + `ChangeKind(kind)` for the refresh | int NOT NULL DEFAULT 0 | copied, never entered |

Migration `AddMaterialKind`: two added columns, default 0. Existing rows become Material (FR-017-3).

## Changed calculated records (not stored)
| Record | New members |
|---|---|
| WorkLineCalculation | `Hours` (quantity × labour norm, not rounded), `Materials`, `Machinery`, `Amount` (= LabourCost + Materials + Machinery), `UnitPrice` (= MoneyRounding.Round(Amount / Quantity)) |
| EstimateCalculation | `Labour`, `Materials`, `Machinery` (sums over work lines); `DirectCosts` = Labour + Materials + Machinery |
| RefreshedField | + `Kind` |
| CatalogMaterial | + `Kind` |

## Changed DTOs
| DTO | New members |
|---|---|
| MaterialRequest | `Kind` (default Material) |
| MaterialResponse | `Kind` |
| EstimateMaterialLineResponse | `Kind` |
| EstimateWorkLineResponse | `Hours`, `Materials`, `Machinery`, `Amount`, `UnitPrice` |
| EstimateResponse | `Labour`, `Materials`, `Machinery` |
| RefreshChangeResponse | doc only: for `Field = Kind`, the values are MaterialKind numbers |

`EstimateNamesResponse` (Viewer) is unchanged.
