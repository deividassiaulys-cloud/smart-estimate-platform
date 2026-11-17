# Sample estimates – reference data model

Extracted from two depersonalized local estimates (2026-10 and 2026-05 price levels).
`reference_calculator.py --check` reproduces the first estimate: 135,414.56 EUR vs. 135,414.29 EUR in the PDF
(0.27 EUR difference because the PDF prints resource quantities with only 3 decimals).

## Files

| File | Content |
|---|---|
| `resources.json` / `.csv` | Resource catalog, 204 items: `code`, `name`, `unit`, `type` (`labour` / `material` / `machinery`), `price`, `priceDate`, `source` |
| `price-history.csv` | Price per date; items present in both estimates with different prices appear twice |
| `rate-templates.json` | 102 rate templates (= "works"): `name`, `unit`, `category`, `resources[]` with `norm` per 1 unit and an optional `price` override |
| `settings.json` | Surcharges (3 × 1.5 %), profit 6 %, discount, VAT 21 %, labour rates |
| `sample-estimate.json` | The full first estimate: 24 sections, 81 lines, with `pdfAmount` per line for testing |
| `reference_calculator.py` | Reference implementation of the method, `--check` compares with the PDF, default run writes `sample-estimate-export.csv` |

## Structure of the source estimates

```
Estimate (no., date, price level "2026 m. 10 mėn. kainomis", client, project, notes, exclusions)
 └─ Section            "++Lauko siena 28+9,5+195+45 mm ..."
     └─ Line           No., name, unit, quantity, unit price (= amount / quantity)
         └─ Resource   code, norm per 1 unit, price  → type labour / material / machinery
```

Columns: No. | Name | Code | Unit | Norm | Price | Quantity | Amount | Labour | Materials | Machinery.

## Calculation method

1. Resource quantity = norm × line quantity; resource amount = quantity × price, rounded to cents (half away from zero).
2. Line amount = sum of resource amounts, split into labour / materials / machinery by resource type.
3. Section and estimate totals are sums of line amounts.
4. Surcharges: +1.5 % of materials, +1.5 % of machinery, +1.5 % of labour (social insurance and site costs are 0) → direct costs.
5. Profit 6 % → discount → VAT 21 % (0 % in the second estimate).

Three 1.5 % surcharges per column equal one 1.5 % overhead on the total, so the platform's BR-016 produces the same grand total.

## Data-quality issues found in the source PDFs

- Line 41 contains a labour resource (201.43 EUR) and line 42 charges the same labour again for the same 11.51 m² – probably double-counted. Line 38 shows the correct pattern (labour at price 0, charged in line 39).
- One name, two products: "Skardos lankstinukas" costs 9.23 and 3.75 EUR/m – split into two names here.
- Two names, one product: "Siga Wigluv 60 mm" and "Lipni juosta Siga Wigluv 60 mm" – unified.
- Line 76 had duplicate rows (WAF 6.0X100 and XSI 120 tape) – merged; the usage text belongs in a line note, not in the product name.
- Glulam (GL24): each element is listed as a separate "product"; better one resource "GL24, m³" with dimensions as line details.
- PDF prices are printed with 2 decimals but are more precise (e.g. deck screws 0.0616 EUR); prices are stored with up to 6 decimals.
- "Įrankiai ir įrengimai" is 0.20 EUR/m² in one line and 20 EUR/set in another – handled by the per-line `price` override.
- Lines with quantity 0 (31, 37, 40, 56, 66, 67, 79) are alternative options and are kept.
