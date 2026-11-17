# API contract changes: Resource Kinds and Estimate Columns

All changes are additions; no field is removed or renamed. Enums are numbers.

## Materials (Manager only)
- `POST /api/materials`, `PUT /api/materials/{id}`: optional `kind` (0 = Material, 1 = Machinery; default 0). Any other number → 400, field `Kind`.
- `GET /api/materials`, `GET /api/materials/{id}`: + `kind`.

## Estimate (Manager only)
`GET /api/estimates/{id}`, and every edit endpoint that returns the estimate:
```jsonc
{
  "workLines": [{
    "quantity": 35, "labourNorm": 0.8, "labourRate": 15, "labourCost": 420.00,
    "hours": 28,                 // new: quantity × labour norm, not rounded
    "materials": 405.31,         // new: Σ cost of kind 0
    "machinery": 7.00,           // new: Σ cost of kind 1
    "amount": 832.31,            // new: labourCost + materials + machinery
    "unitPrice": 23.78,          // new: amount ÷ quantity, rounded to 2
    "materialLines": [{ "kind": 1, "cost": 7.00 /* … */ }]
  }],
  "labour": 420.00, "materials": 405.31, "machinery": 7.00,   // new
  "directCosts": 832.31        // unchanged meaning, = labour + materials + machinery
}
```
- `PATCH /api/estimates/{id}/lines/{lineId}/materials/{materialLineId}/material`: the line takes the replacement material's `kind`.
- `GET|POST /api/estimates/{id}/refresh`: `changes[].field` may be `5` (Kind); then `oldValue`/`newValue` are 0 or 1.

## Viewer
`GET /api/estimates/{id}/names` and the Viewer project views are unchanged: no kinds, parts or unit prices (BR-005).

## Excel export (`GET /api/estimates/{id}/export`)
Table columns: Nr. | Pavadinimas | Mato vnt. | Norma | Atliekos % | Kaina | Kiekis | Suma | Darbas | Medžiagos | Mechanizmai

| Row | Nr. | Pavadinimas | Mato vnt. | Norma | Atliekos % | Kaina | Kiekis | Suma | Darbas | Medžiagos | Mechanizmai |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Work | 1 | work name | work unit | – | – | unit price | quantity | amount | labour | materials | machinery |
| Labour | – | Darbas | val. | labour norm | – | labour rate | hours | labour cost | labour cost | – | – |
| Material | – | material name | unit | norm | waste % | price | quantity | cost | – | cost if Material | cost if Machinery |

Totals rows, with the amount in "Suma":
- Darbas, Medžiagos, Mechanizmai
- Tiesioginės išlaidos, which also carries the three totals in their columns
- Pridėtinės išlaidos (p %), Pelnas (p %), PVM (p %)
- Iš viso su PVM

Empty parts on a work row are written as 0 (0,00 €). On labour and material rows, the cells of the other parts are left empty.
