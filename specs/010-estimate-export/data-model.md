# Data Model: Estimate Export

**No schema change and no migration.** The export reads the existing estimate data through `IEstimateService.GetAsync` (007).

## New types (not stored)

| Type | Fields | Notes |
|---|---|---|
| `ExportFile` (record, `Services/`) | FileName, ContentType, Content (`byte[]`) | Result of `IEstimateExportService.ExportAsync`; the controller returns it as a file |
| `EstimateStatusNames` (static, `Services/`) | Draft → "Juodraštis", Confirmed → "Patvirtinta" | Lithuanian status names for the file. They must match the client's `estimateStatusNames` (client/src/utils/estimateStatus.ts); server and client can't share code, so each side is pinned by its own test |

## File content (spec Q1–Q2)

```text
Projektas     | Buto remontas
Sąmata        | Pradinė sąmata
Būsena        | Juodraštis
Eksportuota   | 2026-11-03

Pavadinimas | Mato vnt. | Kiekis | Norma | Atliekos % | Kaina / Įkainis | Suma
Siena A++   | m²        | 35     | 0,8   |            | 15,00 €         | 420,00 €
  Gipso plokštė | m²    | 77     | 2     | 10 %       | 4,50 €          | 346,50 €
  Varžtai   | vnt.      | 918,75 | 25    | 5 %        | 0,02 €          | 18,38 €
  Plėvelė   | m²        | 36,75  | 1     | 5 %        | 1,10 €          | 40,43 €

Tiesioginės išlaidos       825,31 €
Pridėtinės išlaidos (10 %)  82,53 €
Pelnas (15 %)              136,18 €
PVM (21 %)                 219,24 €
Iš viso su PVM           1 263,26 €
```

Numbers are number cells with formats; text is quote-prefixed text. There are no formulas.
